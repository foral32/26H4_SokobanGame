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
        private string chapterId, selectedId, notice, trialRoute, publicationError;
        private LevelData recovery, trialBoard;
        private bool rescan,reviewOnReturn;
        private enum Panel { Chapters, Outline, Anchor, Transfer, Discard, Remove }
        private Panel panel;
        private int chapterPage, choicePage;
        private const int Rows = 8;
        public bool EditingBoard { get; private set; }
        private CampaignContent Data => doc.Data;
        private Chapter Current => Data.Chapter(chapterId);
        private ChapterLink Selected => Current.levels.First(l => l.id == selectedId);
        private GameUI UI => app.UI;
        private bool Dirty => !CampaignPublication.SameContent(draft.baseline,Data);
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
            overlay=returnToMap&&reviewOnReturn?Overlay.Review:Overlay.None;
            reviewOnReturn=false;
            Draw();
        }
        public void Back()
        {
            if(overlay!=Overlay.None){CloseOverlay();return;}
            if(dragId!=null){CancelDrag();return;}
            if(panel==Panel.Chapters){rescan=true;app.ShowTitle();return;}
            if(panel==Panel.Outline){chapterPage=Array.IndexOf(Data.catalog.chapters,Current)/4;Switch(Panel.Chapters);return;}
            Switch(Panel.Outline);
        }
        private void Switch(Panel next)
        {
            panel = next; overlay=Overlay.None;notice = ""; choicePage = 0; Draw();
        }
        private bool Change(Action change)
        {
            var chapter=chapterId;var selected=selectedId;var brush=recovery;var route=trialRoute;var trial=trialBoard;
            try
            {
                // Include persistence in the document transaction. A failed draft
                // write must not leave a deleted chapter selected by its popup.
                doc.Change(()=>{change();NormalizeSelection();Persist();});
                notice="";return true;
            }
            catch(Exception e)
            {
                chapterId=chapter;selectedId=selected;recovery=brush;trialRoute=route;trialBoard=trial;
                NormalizeSelection();draft.working=Data;draft.chapter=chapterId;draft.selection=selectedId;
                draft.brushRecovery=recovery;draft.trial=trialRoute;notice=e.Message;return false;
            }
        }
        private void Edit(Action change)
        {
            if(Change(change)){panel=Panel.Outline;overlay=Overlay.None;}
            Draw();
        }
        private void History(bool undo)
        {
            try
            {
                int previousIndex=Array.FindIndex(Data.catalog.chapters,c=>c.id==chapterId);
                var before=new HashSet<string>(Data.catalog.chapters.Select(c=>c.id));
                if (undo) doc.Undo(); else doc.Redo();
                var restored=Data.catalog.chapters.FirstOrDefault(c=>!before.Contains(c.id));
                if(restored!=null){chapterId=restored.id;selectedId=restored.levels.First(l=>l.main).id;}
                else if(!Data.catalog.chapters.Any(c=>c.id==chapterId))
                {
                    var neighbor=Data.catalog.chapters[Mathf.Clamp(previousIndex,0,Data.catalog.chapters.Length-1)];
                    chapterId=neighbor.id;selectedId=neighbor.levels.First(l=>l.main).id;
                }
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
        private void OpenBoard(bool play=false,bool fromReview=false)
        {
            reviewOnReturn=fromReview;overlay=Overlay.None;
            if(mapView?.Alive==true)mapOffset=mapView.Offset;
            var baseline = Data.Board(selectedId).Copy();
            baseline.solution=Data.Plan(selectedId)?.route;
            EditingBoard = true;
            app.OpenCampaignBoard(recovery?.id == selectedId ? recovery : baseline, baseline, play);
        }
        public void StageBoard(LevelData data)
        {
            var prior=recovery;
            try
            {
                doc.Change(()=>{doc.SaveBoard(data,trialRoute);recovery=null;Persist();});
                data.solution=Data.Plan(data.id)?.route;
            }
            catch { recovery=prior;draft.brushRecovery=prior;draft.working=Data;throw; }
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
                var baseline=Data.Board(selectedId).Copy();baseline.solution=trialBoard.solution;
                var editing=app.Creator.Document;
                editing.RecordSolution(trialRoute);
                if(baseline.ContentEquals(trialBoard)){StageBoard(editing.Data);editing.Saved();}
                else RecoverBoard(editing.Data);
                notice = "已记录通关路线";
            }
            catch (Exception e) { notice = e.Message; }
        }
        private void Publish()
        {
            var review=CreatePublication();
            string error=review.Check(out var candidate,out _);
            if(error!=null){publicationError=error;Draw();return;}
            try
            {
                var remaining=CampaignPublication.Copy(review.Working);
                // The project transaction includes saving the rebased remaining draft.
                // A failed draft write rolls back the asset imports as well.
                CampaignDraftFile next=null;
                project.Publish(candidate,draft.revision,()=>
                {
                    next=new CampaignDraftFile{revision=project.Revision,baseline=CampaignPublication.Copy(project.Source),
                        working=remaining,chapter=chapterId,selection=selectedId};
                    project.SaveDraft(next);
                },true);
                draft=next;doc=new CampaignDocument(remaining);recovery=null;trialRoute=null;
                NormalizeSelection();app.Progress.Reconcile(app.assets.Catalog);
                notice="已写回所选修改";publicationError=null;excludedChanges.Clear();overlay=Overlay.None;
                if(!Dirty)try{project.ForgetDraft();}catch(Exception e){notice="已写回工程；草稿清理失败："+e.Message;}
            }
            catch(Exception e){publicationError=e.Message;}
            Draw();
        }
        private void Reload()
        {
            try
            {
                project.Reload();
                string archive=draft.revision!=project.Revision?project.ArchiveDraft():null;
                project.ForgetDraft(); Load();if(panel!=Panel.Chapters)panel=Panel.Outline;
                overlay=Overlay.None;
                notice = archive==null?"已从工程重新读取":"已从工程重新读取，冲突草稿保存在 "+archive;
            }
            catch (Exception e) { notice = e.Message; }
            Draw();
        }
        private static string ChapterName(string title)=>System.Text.RegularExpressions.Regex.Replace(title,@"^第[一二三四五六七八九十百零〇\d]+章\s*[·・.、:：/\-—]?\s*","");
    }
}
