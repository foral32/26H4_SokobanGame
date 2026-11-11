using System;
using System.Collections;
using System.IO;
using System.Linq;
using DraculasBox.Core;
using DraculasBox.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DraculasBox.Editor
{
    public static partial class CampaignPlayChecks
    {
        public static void RunNavigation()=>Begin(Navigation(),"navigation",true);
        private sealed class HeldKey:IStudyKeyboard
        {
            public KeyCode key;public int frame=Time.frameCount+1;
            public bool Held(KeyCode candidate)=>candidate==key;
            public bool Pressed(KeyCode candidate)=>candidate==key&&Time.frameCount==frame;
        }
        private static IEnumerator Navigation()
        {
            CreateFixture();Bind(fixture);
            var draft=LevelData.Blank();draft.title="导入预览";app.Library.Save(draft);app.LibrarySelection=draft.id;
            yield return Click("creator");yield return Click("import");
            var dialog=app.UI.Root.Find("Import dialog") as RectTransform;
            Need(app.Page==GamePage.Importing&&dialog!=null&&dialog.rect.height<300,"import opens a compact overlay");
            Need(app.UI.Root.Find("Library heading")!=null,"import retains the library behind the popup");
            Need(!app.UI.FindButton("confirm-import").interactable,"import requires a valid preview");
            yield return new WaitForSecondsRealtime(.16f);Capture("import_empty");
            var path=dialog.GetComponentInChildren<TMP_InputField>();path.text=Path.Combine(app.Library.Folder,"missing.json");
            yield return Click("preview-import");Need(!app.UI.FindButton("confirm-import").interactable,"invalid import stays in the popup without enabling confirmation");
            path.text=Path.Combine(app.Library.Folder,draft.id+".json");yield return Click("preview-import");
            Need(app.UI.FindButton("confirm-import").interactable&&dialog.rect.height<500&&dialog.GetComponentInChildren<LevelThumbnail>()!=null,"valid import expands an inline board preview");
            Need(((RectTransform)dialog.Find("Overlay surface")).rect.size==dialog.rect.size,"expanded popup surface encloses its header, preview and actions");
            Capture("import_preview");
            path.text+="x";Need(!app.UI.FindButton("confirm-import").interactable&&dialog.rect.height<300,"editing the path invalidates the previous preview immediately");
            yield return Press(KeyCode.Escape);yield return Ready();
            Need(app.Page==GamePage.Library&&app.Library.Read(out _).Count==1,"cancel import leaves library files unchanged");
            yield return Click("import");path=app.UI.Root.Find("Import dialog").GetComponentInChildren<TMP_InputField>();
            path.text=Path.Combine(app.Library.Folder,draft.id+".json");yield return Click("preview-import");yield return Click("confirm-import");
            Need(app.Page==GamePage.Editing&&app.Creator.Document.Data.id!=draft.id&&app.Library.Read(out _).Count==2,"confirmed import creates a separate editable level");
            var before=JsonUtility.ToJson(app.Creator.Document.Data);app.ExportEditor();yield return Ready();
            var export=app.UI.Root.Find("Export dialog") as RectTransform;
            Need(export!=null&&export.rect.height<300&&app.UI.Root.Find("Level title")!=null,"export is a compact overlay above the editor");
            var exportPath=export.GetComponentInChildren<TMP_InputField>();
            Need(exportPath.readOnly&&File.Exists(exportPath.text),"export presents the actual saved file path");
            yield return new WaitForSecondsRealtime(.16f);Capture("export_popup");yield return Click("export-back");
            Need(app.Page==GamePage.Editing&&JsonUtility.ToJson(app.Creator.Document.Data)==before,"closing export preserves the edited document");
            yield return Click("leave-editor");yield return Ready();app.ShowTitle();yield return Ready();

            var catalog=fixture.Catalog;
            // Keep one real finale uncompleted; no real player save is used by BackgroundPlaytest.
            var second=catalog.chapters[1];var final=second.levels.Last(l=>l.main);
            foreach(var c in catalog.chapters.Take(2))foreach(var l in c.levels.Where(l=>l.main&&l.id!=final.id))app.Progress.Complete(l.id);
            app.Progress.RememberUnlocks(catalog);app.LoadLevel(final.id);yield return Ready();
            bool sawSequence=false,sawParticles=false,sawStretch=false,captured=false;
            yield return SolveNavigationLevel(final.id);
            float until=Time.realtimeSinceStartup+10;
            while(app.Page==GamePage.Completing||app.Transitioning)
            {
                NeedTime(until,"chapter celebration did not finish");
                if(app.UI.IsChapterCelebrating)
                {
                    sawSequence=true;
                    if(app.UI.FindButton("next-chapter")!=null)throw new InvalidOperationException("Next chapter entry appeared before the clear sequence finished.");
                    sawParticles|=app.UI.Root.GetComponentsInChildren<SaveCelebrationGraphic>().Length>0;
                    sawStretch|=app.UI.Root.GetComponentsInChildren<JuicyButton>().Any(b=>Mathf.Abs(b.transform.localScale.x-b.transform.localScale.y)>.03f);
                    if(!captured&&sawParticles&&sawStretch){Capture("chapter_clear");captured=true;}
                }
                yield return null;
            }
            Need(sawSequence&&sawParticles&&sawStretch,"first chapter completion plays particles and elastic tile motion");
            Need(app.Page==GamePage.Levels&&app.ChapterId==second.id&&app.Session==null,"chapter finale returns to its own level map");
            Need(second.levels.Where(l=>!l.main).All(l=>!app.Progress.Done(l.id)),"optional branches are not marked complete by the celebration");
            Need(app.UI.FindButton("next-chapter")!=null&&app.Progress.ChapterUnlocked(catalog.chapters[2],catalog),"next chapter is available after the sequence");Capture("chapter_cleared");
            app.LoadLevel(final.id);yield return Ready();yield return SolveNavigationLevel(final.id);yield return WaitForCompletion();
            Need(app.Page==GamePage.Levels&&!app.UI.IsChapterCelebrating,"replaying a completed finale returns without repeating the chapter ceremony");
            var branch=second.levels.First(l=>!l.main);app.LoadLevel(branch.id);yield return Ready();yield return SolveNavigationLevel(branch.id);yield return WaitForCompletion();
            Need(app.Page==GamePage.Levels&&app.ChapterId==second.id,"branch completion stays in its own chapter");
            app.LoadLevel(catalog.chapters[0].levels.First(l=>l.main).id);yield return Ready();yield return SolveNavigationLevel(app.Session.State.Level.Export().id);yield return WaitForCompletion();
            Need(app.Page==GamePage.Playing&&app.Session.State.Level.Export().id==catalog.chapters[0].levels.Where(l=>l.main).Skip(1).First().id,"ordinary completion still advances within the chapter");
            var lastChapter=catalog.chapters.Last();var lastLevel=lastChapter.levels.Last(l=>l.main);
            foreach(var c in catalog.chapters)foreach(var l in c.levels.Where(l=>l.main&&l.id!=lastLevel.id))app.Progress.Complete(l.id);
            app.Progress.RememberUnlocks(catalog);app.LoadLevel(lastLevel.id);yield return Ready();yield return SolveNavigationLevel(lastLevel.id);yield return WaitForCompletion();
            Need(app.Page==GamePage.Levels&&app.ChapterId==lastChapter.id&&app.UI.FindButton("next-chapter")==null,"final chapter stays on its map without an invalid next chapter");

            app.ShowTitle();yield return Ready();yield return Click("campaign");yield return Click("select-chapter-"+second.id);
            var node=app.UI.FindButton("select-level-"+second.levels[1].id);var pointer=new PointerEventData(EventSystem.current);
            app.UI.HoverForCheck(node.name);var space=new HeldKey{key=KeyCode.Space};app.SetTestKeyboard(space);yield return null;yield return null;
            var peek=app.UI.Root.Find("Level peek") as RectTransform;
            Need(peek!=null&&app.Page==GamePage.Campaign,"hover and held Space preview without opening the editor");
            Need(peek.GetComponent<CanvasGroup>().blocksRaycasts==false&&peek.GetComponentInChildren<LevelThumbnail>()!=null,"preview draws the actual level and does not intercept the mouse");
            var corners=new Vector3[4];peek.GetWorldCorners(corners);
            Need(corners.All(p=>app.UI.Root.rect.Contains(app.UI.Root.InverseTransformPoint(p))),"preview stays inside the canvas");Capture("level_peek");
            ExecuteEvents.Execute(node.gameObject,pointer,ExecuteEvents.pointerExitHandler);yield return null;
            Need(app.UI.Root.Find("Level peek")==null,"moving off the tile dismisses its preview");
            var other=app.UI.FindButton("select-level-"+branch.id);app.UI.HoverForCheck(other.name);yield return null;yield return null;
            Need(app.UI.Root.Find("Level peek").GetComponentInChildren<TMP_Text>().text==fixture.Level(branch.id).title,"holding Space while moving to a branch refreshes the preview");
            app.SetTestKeyboard(driver);yield return null;yield return null;
            Need(app.UI.Root.Find("Level peek")==null,"releasing Space dismisses the preview");
            yield return Drag(second.levels[1].id,second.levels[2].id,new Vector2(0,0),true);
            Need(app.Page==GamePage.Campaign&&app.UI.Root.Find("Level peek")==null,"drag cancellation remains available after previewing");
            Need(new CampaignProject(fixture).Revision==Get("start")&&!new CampaignProject(fixture).HasDraft,"previewing and canceling drag do not write campaign changes");
            app.ShowTitle();yield return Ready();
        }
        private static void NeedTime(float deadline,string message){if(Time.realtimeSinceStartup>deadline)throw new TimeoutException(message);}
        private static IEnumerator SolveNavigationLevel(string id)
        {
            var route=JsonUtility.FromJson<ReplayCatalog>(fixture.replays.text).levels.First(p=>p.id==id).route;
            foreach(char move in route){yield return Ready();app.Act((Command)"URDL".IndexOf(move));}
            Need(app.Progress.Done(id),"actual winning route completes "+id);
        }
        private static IEnumerator WaitForCompletion()
        {
            float deadline=Time.realtimeSinceStartup+12;
            while(app.Page==GamePage.Completing||app.Transitioning||app.Board.IsAnimating){NeedTime(deadline,"completion flow timed out");yield return null;}
            yield return Ready();
        }
    }
}
