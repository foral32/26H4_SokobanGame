using System;
using System.Collections.Generic;
using System.Linq;
using DraculasBox.Core;
using DraculasBox.Runtime;
using UnityEngine;

namespace DraculasBox.Editor
{
    [Serializable]
    internal sealed class CampaignContent
    {
        public ChapterCatalog catalog;
        public LevelData[] boards;
        public ReplayCatalog replays;
        public Chapter Chapter(string id) => catalog.chapters.First(c => c.id == id);
        public LevelData Board(string id) => boards.First(b => b.id == id);
        public ReplayPlan Plan(string id) => replays.levels.FirstOrDefault(p => p.id == id);
    }

    internal sealed class CampaignDocument
    {
        public CampaignContent Data { get; private set; }
        public bool CanUndo => undo.Count > 0;
        public bool CanRedo => redo.Count > 0;
        private readonly List<string> undo = new();
        private readonly Stack<string> redo = new();
        public CampaignDocument(CampaignContent data) { Data = data; }

        public void Change(Action action)
        {
            string before = JsonUtility.ToJson(Data);
            try { action(); }
            catch { Restore(before); throw; }
            if (before == JsonUtility.ToJson(Data)) return;
            if (undo.Count == 48) undo.RemoveAt(0);
            undo.Add(before);
            redo.Clear();
        }

        private void Restore(string json) => Data = JsonUtility.FromJson<CampaignContent>(json);
        public void Undo()
        {
            if (!CanUndo) return;
            redo.Push(JsonUtility.ToJson(Data));
            Restore(undo[undo.Count - 1]);
            undo.RemoveAt(undo.Count - 1);
        }
        public void Redo()
        {
            if (!CanRedo) return;
            undo.Add(JsonUtility.ToJson(Data));
            Restore(redo.Pop());
        }

        public static List<ChapterLink> Main(Chapter c) => c.levels.Where(l => l.main).ToList();
        public static List<ChapterLink> Branches(Chapter c, string anchor) =>
            c.levels.Where(l => !l.main && l.requires.Contains(anchor)).ToList();
        public static List<ChapterLink> Outline(Chapter c) =>
            Main(c).SelectMany(l => new[] { l }.Concat(Branches(c, l.id))).ToList();
        private static string Id(string prefix) => prefix + "_" + Guid.NewGuid().ToString("N").Substring(0, 12);
        private static void Link(Chapter c, List<ChapterLink> main, IEnumerable<ChapterLink> branches)
        {
            var tails = branches.ToArray();
            for (int i = 0; i < main.Count; i++)
                main[i].requires = i == 0 ? Array.Empty<string>() : new[] { main[i - 1].id };
            c.levels = main.Concat(tails).ToArray();
        }
        private void LinkChapters()
        {
            var chapters = Data.catalog.chapters;
            for (int i = 0; i < chapters.Length; i++)
                chapters[i].requiresChapter = i == 0 ? "" : chapters[i - 1].id;
        }

        public string AddChapter()
        {
            var chapter = new Chapter { id = Id("ch"), title = "新章节", description = "", levels = Array.Empty<ChapterLink>() };
            Data.catalog.chapters = Data.catalog.chapters.Append(chapter).ToArray();
            LinkChapters();
            AddLevel(chapter, null, false);
            return chapter.id;
        }
        public void MoveChapter(string id, int offset)
        {
            var chapters = Data.catalog.chapters;
            int i = Array.FindIndex(chapters, c => c.id == id), to = i + offset;
            if (to < 0 || to >= chapters.Length) return;
            (chapters[i], chapters[to]) = (chapters[to], chapters[i]);
            LinkChapters();
        }
        public void RemoveChapter(string id)
        {
            if(Data.catalog.chapters.Length==1)throw new InvalidOperationException("至少保留一个章节。");
            var chapter=Data.Chapter(id);
            var removed=new HashSet<string>(chapter.levels.Select(l=>l.id));
            Data.catalog.chapters=Data.catalog.chapters.Where(c=>c.id!=id).ToArray();
            Data.boards=Data.boards.Where(b=>!removed.Contains(b.id)).ToArray();
            Data.replays.levels=Data.replays.levels.Where(p=>!removed.Contains(p.id)).ToArray();
            LinkChapters();
        }
        public string AddLevel(Chapter c, ChapterLink after, bool duplicate)
        {
            var board = duplicate ? Data.Board(after.id).Copy() : LevelData.Blank();
            board.id = Id("lvl");
            board.title = duplicate ? board.title + " · 副本" : "新关卡";
            Data.boards = Data.boards.Append(board).ToArray();
            var main = Main(c);
            var branches = c.levels.Where(l => !l.main).ToList();
            var node = new ChapterLink { id = board.id, main = !duplicate || after.main, requires = Array.Empty<string>() };
            if (node.main)
            {
                string anchor = after == null ? null : after.main ? after.id : after.requires[0];
                int pos = anchor == null ? main.Count : main.FindIndex(l => l.id == anchor) + 1;
                main.Insert(pos, node);
            }
            else
            {
                node.requires = new[] { after.requires[0] };
                node.branchAbove = after.branchAbove;
                branches.Insert(branches.IndexOf(after) + 1, node);
            }
            Link(c, main, branches);
            if (duplicate && Data.Plan(after.id) is ReplayPlan original)
            {
                var copy = JsonUtility.FromJson<ReplayPlan>(JsonUtility.ToJson(original));
                copy.id = board.id;
                Data.replays.levels = Data.replays.levels.Append(copy).ToArray();
            }
            return board.id;
        }
        public static List<ChapterLink> Peers(Chapter c, ChapterLink node) =>
            node.main ? Main(c) : Branches(c, node.requires[0]);
        public void MoveLevel(Chapter c, ChapterLink node, int offset)
        {
            var peers = Peers(c, node);
            int to = peers.IndexOf(node) + offset;
            if (to < 0 || to >= peers.Count) return;
            var all = c.levels.ToArray();
            int a = Array.IndexOf(all, node), b = Array.IndexOf(all, peers[to]);
            (all[a], all[b]) = (all[b], all[a]);
            Link(c, all.Where(l => l.main).ToList(), all.Where(l => !l.main));
        }
        // Insertion indices belong to the map before lifting the source. Preview and commit share this operation.
        public static bool Place(Chapter c, ChapterLink node, MapInsertion target)
        {
            var path=Main(c);var branches=c.levels.Where(l=>!l.main).ToList();
            if(!target.Main)
            {
                if(node.id==target.anchor||!path.Any(n=>n.id==target.anchor))return false;
                CanDetach(c,node);
            }
            bool wasMain=node.main;
            var peers=target.Main?path:branches.Where(n=>n.requires[0]==target.anchor&&n.branchAbove==target.above).ToList();
            int from=peers.IndexOf(node),to=Mathf.Clamp(target.index,0,peers.Count);
            if(from>=0&&from<to)to--;
            if(from==to)return false;
            path.Remove(node);branches.Remove(node);
            if(target.Main)path.Insert(to,node);
            else
            {
                // Keep the sibling letter on a simple change of side, bounded by the requested neighbours.
                int oldIndex=Array.IndexOf(c.levels.Where(l=>!l.main).ToArray(),node);
                peers.Remove(node);
                int lo=to==0?0:branches.IndexOf(peers[to-1])+1;
                int hi=to==peers.Count?branches.Count:branches.IndexOf(peers[to]);
                bool sameParent=!wasMain&&node.requires[0]==target.anchor;
                branches.Insert(Mathf.Clamp(sameParent?oldIndex:branches.Count,lo,hi),node);
                node.requires=new[]{target.anchor};
            }
            node.main=target.Main;node.branchAbove=!target.Main&&target.above;
            Link(c,path,branches);return true;
        }
        private static void CanDetach(Chapter c, ChapterLink node)
        {
            if (!node.main) return;
            if (Main(c).Count == 1) throw new InvalidOperationException("本章需要保留一关主线。");
            if (Branches(c, node.id).Count > 0) throw new InvalidOperationException("先为这关的支线另选解锁关，再移动或移出主线。");
        }
        public void SetRole(Chapter c, ChapterLink node, string anchor)
        {
            bool main = anchor == null;
            if (main == node.main && (main || node.requires[0] == anchor)) return;
            if (!main && !Main(c).Any(l => l.id == anchor && l.id != node.id))
                throw new InvalidOperationException("请选择本章的另一关主线作为解锁关。");
            var path = Main(c);
            var branches = c.levels.Where(l => !l.main).ToList();
            if (node.main) { CanDetach(c, node); path.Remove(node); }
            else branches.Remove(node);
            if (main) { path.Insert(path.FindIndex(l => l.id == node.requires[0]) + 1, node); node.branchAbove = false; }
            else { node.requires = new[] { anchor }; branches.Add(node); }
            node.main = main;
            Link(c, path, branches);
        }
        public void Transfer(Chapter from, ChapterLink node, Chapter to)
        {
            if (from == to) return;
            if (node.main && Main(from).Count == 1) throw new InvalidOperationException("本章需要保留一关主线。");
            var group = node.main ? Branches(from, node.id) : new List<ChapterLink>();
            group.Insert(0, node);
            var remaining = from.levels.Except(group).ToArray();
            Link(from, remaining.Where(l => l.main).ToList(), remaining.Where(l => !l.main));
            if (!node.main) node.requires = new[] { Main(to).Last().id };
            var joined = to.levels.Concat(group).ToArray();
            Link(to, joined.Where(l => l.main).ToList(), joined.Where(l => !l.main));
        }
        public void Remove(Chapter c, ChapterLink node)
        {
            CanDetach(c, node);
            var left = c.levels.Where(l => l != node).ToArray();
            Link(c, left.Where(l => l.main).ToList(), left.Where(l => !l.main));
            Data.boards = Data.boards.Where(b => b.id != node.id).ToArray();
            Data.replays.levels = Data.replays.levels.Where(p => p.id != node.id).ToArray();
        }
        public static bool Proves(LevelData board, string route, out int pushes)
        {
            pushes = 0;
            if (string.IsNullOrEmpty(route)) return false;
            var session = new Session(new Definition(board));
            foreach (char key in route)
            {
                int index = "URDL".IndexOf(key);
                if (index < 0 || session.State.Won) return false;
                var step = session.Apply((Command)index);
                if (!step.Changed || session.State.Dead) return false;
                if (step.Kind == ActionKind.Pushed) pushes++;
            }
            return session.State.Won;
        }
        public void SaveBoard(LevelData data, string trial)
        {
            var previous = Data.Board(data.id);
            var plan = Data.Plan(data.id);
            var renamed = previous.Copy();
            renamed.title = data.title;
            renamed.hint = data.hint;
            if (!renamed.ContentEquals(data) || plan == null)
            {
                string path = plan?.route;
                int pushes = 0;
                bool valid = new EditDocument(data).Validation() == null;
                if (!valid || !Proves(data, path, out pushes)) path = valid && Proves(data, trial, out pushes) ? trial : null;
                var plans = Data.replays.levels.Where(p => p.id != data.id).ToList();
                if (path != null) plans.Add(new ReplayPlan { id = data.id, route = path, pushes = pushes, essential = Array.Empty<string>() });
                Data.replays.levels = plans.ToArray();
            }
            Data.boards[Array.IndexOf(Data.boards, previous)] = data.Copy();
        }
        public static void ValidateGraph(CampaignContent content)
        {
            var chapters = content.catalog?.chapters;
            if (chapters == null || chapters.Length == 0) throw new FormatException("正式目录缺少章节。");
            var levelIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var chapterIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < chapters.Length; i++)
            {
                var c = chapters[i];
                if (string.IsNullOrWhiteSpace(c.title) || !ValidId(c.id) || !chapterIds.Add(c.id)) throw new FormatException("章节名称或 ID 无效。");
                string prior = i == 0 ? "" : chapters[i-1].id;
                if ((c.requiresChapter ?? "") != prior) throw new FormatException(c.title + " 的前置章节与编排顺序不符。");
                if (c.levels == null || !c.levels.Any(l => l.main)) throw new FormatException(c.title + " 至少需要一关主线。");
                string previous = null;
                foreach (var l in c.levels)
                {
                    if (!ValidId(l.id) || !levelIds.Add(l.id)) throw new FormatException("关卡 ID 无效或重复：" + l.id);
                    var requires = l.requires ?? Array.Empty<string>();
                    bool ok = l.main ? requires.SequenceEqual(previous == null ? Array.Empty<string>() : new[] { previous })
                                     : requires.Length == 1 && c.levels.Any(m => m.main && m.id == requires[0]);
                    if (!ok) throw new FormatException(c.title + " 中的解锁关系无效：" + l.id);
                    if (l.main) previous = l.id;
                }
            }
        }
        public static bool ValidId(string id) => !string.IsNullOrEmpty(id) && id.Length <= 80 &&
            id.All(c => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_' || c == '-');
    }
}
