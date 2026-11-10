using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DraculasBox.Core;
using DraculasBox.Runtime;
using UnityEditor;
using UnityEngine;

namespace DraculasBox.Editor
{
    [Serializable]
    internal sealed class CampaignDraftFile
    {
        public int version = 1;
        public string revision, chapter, selection;
        public CampaignContent baseline, working;
        public LevelData brushRecovery;
        public string trial;
    }

    internal sealed class CampaignProject
    {
        [Serializable] private sealed class FileEdit { public string path, before, after; }
        [Serializable] private sealed class Journal { public FileEdit[] files; }
        private readonly GameAssets assets;
        private readonly string folder, cache, chaptersPath, replaysPath;
        private Dictionary<string,string> sourceBytes = new();
        private string DraftPath => Path.Combine(cache, "draft.json");
        private string JournalPath => Path.Combine(cache, "publish.json");
        public CampaignContent Source { get; private set; }
        public string Revision { get; private set; }
        public bool HasDraft => File.Exists(DraftPath);
        public string RecoveryNotice { get; private set; }

        public CampaignProject(GameAssets assets)
        {
            this.assets = assets;
            chaptersPath = AssetDatabase.GetAssetPath(assets.catalog);
            replaysPath = AssetDatabase.GetAssetPath(assets.replays);
            folder = Path.GetDirectoryName(chaptersPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(folder) || !folder.StartsWith("Assets/", StringComparison.Ordinal) ||
                replaysPath != folder + "/Replays.json" || chaptersPath != folder + "/Chapters.json")
                throw new InvalidDataException("未找到正式关卡目录。");
            cache = Path.GetFullPath(Path.Combine("Library", "DraculasBoxCampaign", AssetDatabase.AssetPathToGUID(chaptersPath)));
            Directory.CreateDirectory(cache);
            using (Lock())
            {
                if (File.Exists(JournalPath)) RollBack(JsonUtility.FromJson<Journal>(File.ReadAllText(JournalPath)));
                ReadSource();
            }
        }
        private FileStream Lock() => new(Path.Combine(cache, "write.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        private string BoardPath(string id)
        {
            if (!CampaignDocument.ValidId(id)) throw new InvalidDataException("关卡 ID 无效。");
            return folder + "/Levels/" + id + ".json";
        }
        private static string Digest(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(bytes));
        }
        private void ReadSource()
        {
            var parts = new List<string>();
            var snapshot = new Dictionary<string,string>();
            T Read<T>(string path)
            {
                byte[] bytes = File.ReadAllBytes(path);
                snapshot[path] = Convert.ToBase64String(bytes);
                parts.Add(path + "|" + Digest(bytes));
                return JsonUtility.FromJson<T>(Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF'));
            }
            var data = new CampaignContent { catalog = Read<ChapterCatalog>(chaptersPath), replays = Read<ReplayCatalog>(replaysPath) };
            CampaignDocument.ValidateGraph(data);
            data.boards = data.catalog.chapters.SelectMany(c => c.levels).Select(l =>
            {
                var board = Read<LevelData>(BoardPath(l.id));
                if (board?.id != l.id) throw new InvalidDataException("关卡文件与目录 ID 不符：" + l.id);
                string error = Definition.DraftError(board);
                if (error != null) throw new InvalidDataException(l.id + " · " + error);
                return board.Copy();
            }).ToArray();
            if (data.replays?.levels == null || data.replays.levels.GroupBy(p => p.id).Any(g => g.Count() != 1))
                throw new InvalidDataException("试玩路线目录无效。");
            Revision = Digest(Encoding.UTF8.GetBytes(string.Join("\n", parts)));
            Source = data;
            sourceBytes = snapshot;
        }

        public CampaignDraftFile ReadDraft()
        {
            if (!HasDraft) return null;
            try { return ParseDraft(DraftPath); }
            catch (Exception e) when (e is ArgumentException || e is FormatException || e is InvalidDataException || e is NullReferenceException)
            {
                string archive=ArchiveDraft();
                if(File.Exists(DraftPath+".bak"))
                {
                    try
                    {
                        var prior=ParseDraft(DraftPath+".bak");
                        AtomicBytes(DraftPath,File.ReadAllBytes(DraftPath+".bak"));
                        RecoveryNotice="已恢复上一份制作草稿";
                        return prior;
                    }
                    catch(Exception fail) when(fail is ArgumentException || fail is FormatException || fail is InvalidDataException || fail is NullReferenceException) { }
                }
                File.Delete(DraftPath);
                RecoveryNotice="已从工程载入。损坏的草稿已保留在 "+archive;
                return null;
            }
        }
        private static CampaignDraftFile ParseDraft(string path)
        {
            var draft = JsonUtility.FromJson<CampaignDraftFile>(File.ReadAllText(path));
            if (draft?.version != 1 || draft.working?.boards == null || draft.baseline?.catalog == null)
                throw new InvalidDataException("正式关卡草稿无法读取：" + path);
            CampaignDocument.ValidateGraph(draft.working);
            return draft;
        }
        public string ArchiveDraft()
        {
            if(!HasDraft)return null;
            string archive=Path.Combine(cache,"draft-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff")+"-"+Guid.NewGuid().ToString("N").Substring(0,6)+".json");
            File.Copy(DraftPath,archive);
            return archive;
        }
        public void SaveDraft(CampaignDraftFile draft) => ProgressStore.Atomic(DraftPath, JsonUtility.ToJson(draft));
        public void ForgetDraft()
        {
            if (File.Exists(DraftPath)) File.Delete(DraftPath);
            if (File.Exists(DraftPath + ".bak")) File.Delete(DraftPath + ".bak");
        }
        public void Reload() { using (Lock()) ReadSource(); }

        public static string PublicationError(CampaignContent data)
        {
            CampaignDocument.ValidateGraph(data);
            foreach (var link in data.catalog.chapters.SelectMany(c => c.levels))
            {
                var b = data.Board(link.id);
                string error = new EditDocument(b).Validation();
                if (error != null) return b.title + " · " + error;
                var plan = data.Plan(b.id);
                if (!CampaignDocument.Proves(b, plan?.route, out int pushes) || pushes != plan.pushes)
                    return "请先在「" + b.title + "」中按 F5 通关一次，记录可用路线。";
            }
            return null;
        }
        // The callback is also used by the regression to interrupt a real multi-file import.
        public void Publish(CampaignContent working, string expectedRevision, Action afterImport = null)
        {
            using var writeLock = Lock();
            ReadSource();
            if (Revision != expectedRevision) throw new IOException("工程内容已被修改。草稿已保留。请核对差异后从工程重载。");
            string problem = PublicationError(working);
            if (problem != null) throw new InvalidOperationException(problem);
            var files = new List<FileEdit>();
            void Stage(string path, object oldData, object newData)
            {
                if (oldData != null && JsonUtility.ToJson(oldData) == JsonUtility.ToJson(newData)) return;
                if (oldData == null && (File.Exists(path) || File.Exists(path + ".meta")))
                    throw new IOException("同名文件已存在：" + path);
                files.Add(new FileEdit { path = path,
                    before = oldData == null ? null : sourceBytes[path],
                    after = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonUtility.ToJson(newData, true) + "\n")) });
            }
            foreach (var board in working.boards)
                Stage(BoardPath(board.id), Source.boards.FirstOrDefault(b => b.id == board.id), board);
            Stage(replaysPath, Source.replays, working.replays);
            Stage(chaptersPath, Source.catalog, working.catalog);
            if (files.Count == 0) return;
            bool membershipChanged=!new HashSet<string>(Source.boards.Select(b=>b.id)).SetEquals(working.boards.Select(b=>b.id));
            if (membershipChanged && EditorUtility.IsDirty(assets))
                throw new IOException("GameAssets 有其他未保存修改。请先在 Unity 中保存资源，再重试。");
            var journal = new Journal { files = files.ToArray() };
            AtomicBytes(JournalPath, Encoding.UTF8.GetBytes(JsonUtility.ToJson(journal)));
            try
            {
                foreach (var f in files)
                {
                    string current = File.Exists(f.path) ? Convert.ToBase64String(File.ReadAllBytes(f.path)) : null;
                    if (current != f.before) throw new IOException("写入期间文件发生变化：" + f.path);
                    AtomicBytes(f.path, Convert.FromBase64String(f.after));
                    Import(f.path);
                }
                if(membershipChanged)BindLevels(working.catalog);
                afterImport?.Invoke();
                ReadSource();
                File.Delete(JournalPath);
            }
            catch { RollBack(journal); ReadSource(); throw; }
        }
        private static void Import(string path) =>
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        internal static TextAsset[] LevelReferences(ChapterCatalog catalog,string contentFolder)
        {
            return catalog.chapters.SelectMany(c=>c.levels).OrderBy(link=>link.id,StringComparer.Ordinal).Select(link=>
            {
                if(!CampaignDocument.ValidId(link.id))throw new InvalidDataException("无效关卡 ID："+link.id);
                return AssetDatabase.LoadAssetAtPath<TextAsset>(contentFolder+"/Levels/"+link.id+".json")
                    ??throw new IOException("Unity 未能导入关卡："+link.id);
            }).ToArray();
        }
        private void BindLevels(ChapterCatalog catalog)
        {
            var levels=LevelReferences(catalog,folder);
            if (levels.SequenceEqual(assets.levels)) return;
            assets.levels = levels;
            EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssetIfDirty(assets);
        }
        private void RollBack(Journal journal)
        {
            var conflicts=new List<string>();
            foreach (var f in journal.files.Reverse())
            {
                // Only the three campaign JSON destinations can appear in a recovery journal.
                if (f.path != chaptersPath && f.path != replaysPath && f.path != BoardPath(Path.GetFileNameWithoutExtension(f.path)))
                    throw new InvalidDataException("无效的工程恢复路径。");
                string current = File.Exists(f.path) ? Convert.ToBase64String(File.ReadAllBytes(f.path)) : null;
                bool created = string.IsNullOrEmpty(f.before);
                if (current == f.before || created && current == null) continue;
                if (current != f.after) { conflicts.Add(f.path);continue; }
                if (created)
                {
                    if (!AssetDatabase.DeleteAsset(f.path))
                    {
                        File.Delete(f.path);
                        if (File.Exists(f.path + ".meta")) File.Delete(f.path + ".meta");
                    }
                }
                else { AtomicBytes(f.path, Convert.FromBase64String(f.before)); Import(f.path); }
            }
            BindLevels(JsonUtility.FromJson<ChapterCatalog>(File.ReadAllText(chaptersPath)));
            File.Delete(JournalPath);
            if(conflicts.Count>0)throw new IOException("写入时文件被其他工具修改，已保留外部版本："+string.Join("、",conflicts));
        }
        private void AtomicBytes(string path, byte[] data)
        {
            string scratch = Path.Combine(cache, Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllBytes(scratch, data);
                if (File.Exists(path)) File.Replace(scratch, path, null);
                else File.Move(scratch, path);
            }
            finally { if (File.Exists(scratch)) File.Delete(scratch); }
        }
    }
}
