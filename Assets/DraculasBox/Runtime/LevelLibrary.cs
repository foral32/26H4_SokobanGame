using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DraculasBox.Core;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed class LevelLibrary
    {
        private readonly string folder,trash;
        public readonly string RecoveryPath;
        public const int MaxFileBytes=262144;
        public readonly List<string> LastReadIssues=new List<string>();
        public string Folder=>folder;
        public LevelLibrary(string root)
        {folder=Path.Combine(root,"UserLevels");trash=Path.Combine(root,"Trash");Directory.CreateDirectory(folder);RecoveryPath=Path.Combine(root,"draft-recovery.json");}
        private static LevelData ReadDraft(string path)
        {
            var file=new FileInfo(path);if(!file.Exists)throw new FileNotFoundException("文件不存在。");
            if(file.Length>MaxFileBytes)throw new InvalidDataException("文件不能超过 256 KB。");
            LevelData data;try{data=JsonUtility.FromJson<LevelData>(File.ReadAllText(file.FullName));}catch{throw new InvalidDataException("不是可读取的 JSON 关卡。");}
            string error=Definition.DraftError(data);if(error!=null)throw new InvalidDataException(error);if(string.IsNullOrWhiteSpace(data.title))data.title="我的关卡";data.hint=data.hint??"";return data;
        }
        public List<LevelData> Read(out int invalid)
        {
            invalid=0;LastReadIssues.Clear();var list=new List<LevelData>();var ids=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(string file in Directory.GetFiles(folder,"*.json"))try
            {
                var data=ReadDraft(file);
                if(!Guid.TryParseExact(data.id,"N",out _)||!string.Equals(Path.GetFileNameWithoutExtension(file),data.id,StringComparison.OrdinalIgnoreCase)||!ids.Add(data.id))throw new InvalidDataException("关卡标识与文件不一致。");
                list.Add(data);
            }catch(Exception e){invalid++;LastReadIssues.Add(Path.GetFileName(file)+"："+e.Message);}
            return list.OrderBy(d=>d.title,StringComparer.OrdinalIgnoreCase).ThenBy(d=>d.id).ToList();
        }
        public void Save(LevelData source)
        {
            string error=Definition.DraftError(source);if(error!=null)throw new InvalidDataException(error);
            string id=Guid.TryParseExact(source.id,"N",out _)?source.id:Guid.NewGuid().ToString("N");
            var copy=source.Copy();copy.id=id;if(string.IsNullOrWhiteSpace(copy.title))copy.title="我的关卡";copy.hint=copy.hint??"";string text=JsonUtility.ToJson(copy,true);
            if(System.Text.Encoding.UTF8.GetByteCount(text)>MaxFileBytes)throw new InvalidDataException("关卡内容超过 256 KB。");
            ProgressStore.Atomic(Path.Combine(folder,id+".json"),text);source.id=id;source.title=copy.title;source.hint=copy.hint;
        }
        public bool Exists(string id)=>Guid.TryParseExact(id,"N",out _)&&File.Exists(Path.Combine(folder,id+".json"));
        public LevelData Duplicate(LevelData source)
        {var copy=source.Copy();copy.id=Guid.NewGuid().ToString("N");copy.title=source.title+" · 副本";Save(copy);return copy;}
        public LevelData PreviewImport(string path)
        {if(string.IsNullOrWhiteSpace(path))throw new InvalidDataException("请选择关卡文件。");return ReadDraft(Path.GetFullPath(path.Trim().Trim('"'))).Copy();}
        public LevelData Import(LevelData source)
        {var data=source.Copy();data.id=Guid.NewGuid().ToString("N");if(string.IsNullOrWhiteSpace(data.title))data.title="导入关卡";Save(data);return data;}
        public string Export(LevelData source)
        {
            string error=Definition.DraftError(source);if(error!=null)throw new InvalidDataException(error);
            var data=source.Copy();if(!Guid.TryParseExact(data.id,"N",out _))data.id=Guid.NewGuid().ToString("N");
            string path=Path.Combine(Directory.GetParent(folder).FullName,"Exports",data.id+".json");ProgressStore.Atomic(path,JsonUtility.ToJson(data,true));return path;
        }
        public string Archive(string id)
        {
            if(!Guid.TryParseExact(id,"N",out _))throw new InvalidDataException("无效的关卡标识。");
            string source=Path.Combine(folder,id+".json");if(!File.Exists(source))throw new FileNotFoundException("关卡已不存在。");
            string target=Path.Combine(trash,id+"-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")+"-"+Guid.NewGuid().ToString("N")+".json");
            Directory.CreateDirectory(trash);File.Move(source,target);return target;
        }
        public string LatestArchive()
        {
            if(!Directory.Exists(trash))return null;
            foreach(var path in Directory.GetFiles(trash,"*.json").OrderByDescending(File.GetLastWriteTimeUtc))try{var data=ReadDraft(path);if(Guid.TryParseExact(data.id,"N",out _))return path;}catch{}
            return null;
        }
        public void Restore(string archive)
        {
            string full=Path.GetFullPath(archive),allowed=Path.GetFullPath(trash)+Path.DirectorySeparatorChar;
            if(!full.StartsWith(allowed,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("无效的恢复路径。");
            var data=ReadDraft(full);if(!Guid.TryParseExact(data.id,"N",out _))throw new InvalidDataException("无效的关卡标识。");
            if(Exists(data.id))throw new InvalidDataException("该关卡已存在，未覆盖当前文件。");Save(data);File.Delete(full);
        }
        public void Recover(LevelData data)
        {
            string error=Definition.DraftError(data);if(error!=null)throw new InvalidDataException(error);
            if(!Guid.TryParseExact(data.id,"N",out _))throw new InvalidDataException("无效的草稿标识。");
            if(Exists(data.id))try{if(data.ContentEquals(ReadDraft(Path.Combine(folder,data.id+".json")))){DiscardRecovery(data.id);return;}}catch(Exception e)when(e is IOException||e is InvalidDataException||e is UnauthorizedAccessException){}
            ProgressStore.Atomic(RecoveryPath,JsonUtility.ToJson(data,true));
        }
        public LevelData Recovery()
        {
            if(!File.Exists(RecoveryPath))return null;
            LevelData data;try{data=ReadDraft(RecoveryPath);}catch{try{data=ReadDraft(RecoveryPath+".bak");}catch{return null;}}
            if(!Guid.TryParseExact(data.id,"N",out _))return null;
            if(Exists(data.id))try{if(data.ContentEquals(ReadDraft(Path.Combine(folder,data.id+".json"))))return null;}catch{}
            // Removed levels are restored through Trash, rather than resurrected as an old draft.
            if(!Exists(data.id)&&Directory.Exists(trash)&&Directory.GetFiles(trash,data.id+"-*.json").Length>0)return null;
            return data;
        }
        public void DiscardRecovery(string id)
        {
            if(!File.Exists(RecoveryPath))return;LevelData data;try{data=ReadDraft(RecoveryPath);}catch{try{data=ReadDraft(RecoveryPath+".bak");}catch{return;}}
            if(string.Equals(data.id,id,StringComparison.OrdinalIgnoreCase))File.Delete(RecoveryPath);
        }
    }
}
