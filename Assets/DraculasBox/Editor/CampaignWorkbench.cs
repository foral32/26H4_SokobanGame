using System;
using System.Collections.Generic;
using System.Linq;
using DraculasBox.Core;
using DraculasBox.Runtime;
using UnityEditor;
using UnityEngine;

namespace DraculasBox.Editor
{
    [InitializeOnLoad]
    internal static class CampaignRegistration
    {
        static CampaignRegistration()
        {
            DraculaApp.CreateCampaignWorkshop = app => new CampaignWorkbench(app, new CampaignProject(app.assets));
            EditorApplication.playModeStateChanged += change =>
            {
                if (change != PlayModeStateChange.ExitingPlayMode) return;
                var app = UnityEngine.Object.FindFirstObjectByType<DraculaApp>();
                if (app == null || !app.EditingOfficial || app.Creator?.Document == null) return;
                if (app.Page == GamePage.Editing) app.Creator.CommitPending();
                if (app.Creator.Document.Dirty) app.RecoverBoard(app.Creator.Document.Data);
            };
        }
    }

    internal sealed partial class CampaignWorkbench : ICampaignWorkshop
    {
        private readonly DraculaApp app;
        private readonly CampaignProject project;
        private CampaignDocument doc;
        private CampaignDraftFile draft;
        private string chapterId, selectedId, notice, trialRoute;
        private LevelData recovery, trialBoard;
        private bool rescan;
        private enum Panel { Chapters, Outline, Actions, Chapter, Anchor, Transfer, Review, Discard, Remove }
        private Panel panel;
        private int chapterPage, choicePage, reviewPage;
        private const int Rows = 8;
        public bool EditingBoard { get; private set; }
        private CampaignContent Data => doc.Data;
        private Chapter Current => Data.Chapter(chapterId);
        private ChapterLink Selected => Current.levels.First(l => l.id == selectedId);
        private GameUI UI => app.UI;
        private bool Dirty => JsonUtility.ToJson(draft.baseline) != JsonUtility.ToJson(Data);
        internal CampaignWorkbench(DraculaApp app, CampaignProject project)
        { this.app = app; this.project = project; Load(); }

        private void Load()
        {
            draft = project.ReadDraft() ?? new CampaignDraftFile { revision = project.Revision, baseline = project.Source,
                working = JsonUtility.FromJson<CampaignContent>(JsonUtility.ToJson(project.Source)) };
            doc = new CampaignDocument(draft.working);
            recovery = string.IsNullOrEmpty(draft.brushRecovery?.id) ? null : draft.brushRecovery;
            trialRoute = draft.trial;
            chapterId = draft.chapter;
            selectedId = recovery?.id ?? draft.selection;
            NormalizeSelection();
            if (project.HasDraft) notice = draft.revision == project.Revision ? "已恢复工作草稿" : "工程已更新，原草稿已保留";
            if(!string.IsNullOrEmpty(project.RecoveryNotice))notice=project.RecoveryNotice;
            if (recovery != null && Data.boards.Any(b=>b.id==recovery.id))
            {
                doc.Change(()=>doc.SaveBoard(recovery,trialRoute));
                recovery=null;
                Persist();
            }
        }
        private void NormalizeSelection()
        {
            var owner = Data.catalog.chapters.FirstOrDefault(c => c.levels.Any(l => l.id == selectedId));
            var c = owner ?? Data.catalog.chapters.FirstOrDefault(ch => ch.id == chapterId) ?? Data.catalog.chapters[0];
            chapterId = c.id;
            if (owner == null) selectedId = c.levels.First(l => l.main).id;
            chapterPage = Array.IndexOf(Data.catalog.chapters, c) / 4;
        }
        private void Persist()
        {
            draft.working = Data; draft.chapter = chapterId; draft.selection = selectedId;
            draft.brushRecovery = recovery; draft.trial = trialRoute;
            if (Dirty || recovery != null) project.SaveDraft(draft);
            else project.ForgetDraft();
        }
        public void Show(bool returnToMap)
        {
            if(rescan)
            {
                rescan=false;project.Reload();
                if(!Dirty&&recovery==null)Load();
                else if(draft.revision!=project.Revision)notice="工程已更新，原草稿已保留";
            }
            EditingBoard = false;
            panel = returnToMap ? Panel.Outline : Panel.Chapters;
            NormalizeSelection();
            if(!returnToMap)chapterPage=0;
            Draw();
        }
        public void Back()
        {
            if(dragId!=null){CancelDrag();return;}
            if(panel==Panel.Chapters){rescan=true;app.ShowTitle();return;}
            if(panel==Panel.Outline){chapterPage=Array.IndexOf(Data.catalog.chapters,Current)/4;Switch(Panel.Chapters);return;}
            Switch(Panel.Outline);
        }
        private void Switch(Panel next)
        {
            panel = next; notice = ""; choicePage = reviewPage = 0; Draw();
        }
        private void Edit(Action change)
        {
            try { doc.Change(change); NormalizeSelection(); Persist(); notice = ""; panel = Panel.Outline; }
            catch (Exception e) { notice = e.Message; NormalizeSelection(); }
            Draw();
        }
        private void History(bool undo)
        {
            try
            {
                if (undo) doc.Undo(); else doc.Redo();
                NormalizeSelection(); Persist(); notice = "";
            }
            catch (Exception e) { notice = e.Message; }
            Draw();
        }
        private void SelectChapter(Chapter chapter)
        {
            if(chapterId!=chapter.id){selectedId=chapter.levels.First(l=>l.main).id;mapOffset=0;}
            chapterId=chapter.id;NormalizeSelection();Switch(Panel.Outline);
        }
        private void SelectLevel(string id)
        {
            selectedId=id;OpenBoard();
        }
        private void OpenBoard(bool play=false)
        {
            if(mapView?.Alive==true)mapOffset=mapView.Offset;
            var baseline = Data.Board(selectedId);
            EditingBoard = true;
            app.OpenCampaignBoard(recovery?.id == selectedId ? recovery : baseline, baseline, play);
        }
        public void StageBoard(LevelData data)
        {
            doc.Change(() => doc.SaveBoard(data, trialRoute));
            recovery = null;
            Persist();
        }
        public void RecoverBoard(LevelData data)
        {
            recovery = data.Copy();
            Persist();
        }
        public void CloseBoard(bool discard)
        {
            if (recovery?.id == selectedId) recovery = null;
            trialRoute = null;
            Persist();
            EditingBoard = false;
        }
        private sealed class Walk { public Walk parent; public char key; }
        private readonly Dictionary<string, Walk> paths = new();
        public void StartTrial(LevelData board)
        {
            trialBoard = board.Copy();
            paths.Clear();
            paths[new Definition(board).Initial().Key()] = null;
        }
        public void ObserveTrial(ActionResult step)
        {
            if (!step.Changed || (int)step.Command > (int)Command.Left || step.After.Dead) return;
            string key = step.After.Key();
            if (!paths.TryGetValue(key, out var end))
            {
                end = new Walk { parent = paths[step.Before.Key()], key = "URDL"[(int)step.Command] };
                paths.Add(key, end);
            }
            if (!step.After.Won) return;
            var route = new Stack<char>();
            for (var walk = end; walk != null; walk = walk.parent) route.Push(walk.key);
            trialRoute = new string(route.ToArray());
            try
            {
                if (Data.Board(selectedId).ContentEquals(trialBoard)) StageBoard(trialBoard);
                else RecoverBoard(trialBoard);
                notice = "已记录通关路线";
            }
            catch (Exception e) { notice = e.Message; }
        }
        private void Publish()
        {
            try
            {
                project.Publish(Data, draft.revision);
                app.Progress.Reconcile(app.assets.Catalog);
                project.ForgetDraft();
                var chapter = chapterId; var level = selectedId;
                Load(); chapterId = chapter; selectedId = level; NormalizeSelection();
                notice = "已写回工程"; panel = Panel.Outline;
            }
            catch (Exception e) { notice = e.Message; }
            Draw();
        }
        private void Reload()
        {
            try
            {
                project.Reload();
                string archive=draft.revision!=project.Revision?project.ArchiveDraft():null;
                project.ForgetDraft(); Load(); panel = Panel.Outline;
                notice = archive==null?"已从工程重新读取":"已从工程重新读取，冲突草稿保存在 "+archive;
            }
            catch (Exception e) { notice = e.Message; }
            Draw();
        }
        private List<string> Changes()
        {
            var changes = new List<string>();
            var old = draft.baseline;
            string Describe(CampaignContent src, Chapter chapter, ChapterLink link)
            {
                if (link.main) return "主线 " + (CampaignDocument.Main(chapter).FindIndex(m => m.id == link.id) + 1);
                var siblings = CampaignDocument.Branches(chapter, link.requires[0]);
                string at = siblings.Count > 1 ? " #" + (siblings.IndexOf(link) + 1) : "";
                return (link.branchAbove?"上方支线":"下方支线") + at + " · " + src.Board(link.requires[0]).title + " 后解锁";
            }
            foreach (var c in Data.catalog.chapters)
            {
                var was = old.catalog.chapters.FirstOrDefault(o => o.id == c.id);
                if (was == null) changes.Add("新增章节　" + c.title);
                else if (was.title != c.title || was.requiresChapter != c.requiresChapter)
                    changes.Add("章节　" + c.title + "　·　第 " + (Array.IndexOf(Data.catalog.chapters,c)+1) + " 章");
                foreach (var l in CampaignDocument.Outline(c))
                {
                    var b = Data.Board(l.id);
                    var prevChapter = old.catalog.chapters.FirstOrDefault(o => o.levels.Any(n => n.id == l.id));
                    if (prevChapter == null) { changes.Add("新增　" + b.title + "　·　" + c.title + " / " + Describe(Data,c,l)); continue; }
                    var prev = prevChapter.levels.First(n => n.id == l.id);
                    string prior = Describe(old,prevChapter,prev), next = Describe(Data,c,l);
                    if (prior != next || prevChapter.id != c.id || !l.main&&!prev.main&&l.requires[0]!=prev.requires[0])
                        changes.Add(b.title + "　·　" + (prevChapter.id!=c.id?prevChapter.title+" / ":"") + prior + " → " + (prevChapter.id!=c.id?c.title+" / ":"") + next);
                    if (!b.ContentEquals(old.Board(b.id))) changes.Add("修改关卡　" + b.title);
                    else if (Data.Plan(b.id)?.route != old.Plan(b.id)?.route) changes.Add("通关路线　" + b.title);
                }
            }
            foreach (var b in old.boards.Where(b => !Data.boards.Any(n => n.id == b.id))) changes.Add("移出正式目录　" + b.title);
            return changes;
        }
    }
}
