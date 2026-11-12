using System;
using System.Collections;
using System.IO;
using System.Linq;
using DraculasBox.Core;
using DraculasBox.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DraculasBox.Editor
{
    public static partial class CampaignPlayChecks
    {
        private static ShowcaseRecorder recorder;
        public static void RunShowcaseCapture()=>Begin(RecordShowcase(),"showcase-capture",true);
        public static void RunShowcasePolishCapture()=>Begin(RecordPolishShowcase(),"showcase-polish-capture",false);
        public static void RunShowcaseMenuCapture()=>Begin(RecordMenuShowcase(),"showcase-menu-capture",false);
        private static IEnumerator RecordMenuShowcase()
        {
            string theme=GameThemes.Current.Id;
            recorder=app.gameObject.AddComponent<ShowcaseRecorder>();recorder.app=app;
            try
            {
                app.SelectTheme(GameThemes.DefaultThemeId);app.ShowTitle();yield return Page(GamePage.Title);yield return Hold(1);
                Directory.CreateDirectory("Document/Images/Readme");
                File.Copy(driver.Capture("readme-main-menu"),"Document/Images/Readme/main-menu.png",true);
                recorder.Begin("pr01");yield return Hold(1);
                Button previous=null;
                foreach(string id in new[]{"play","creator","settings","play"})
                {
                    if(previous!=null)ExecuteEvents.Execute(previous.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerExitHandler);
                    var b=app.UI.FindButton(id);recorder.Point(RectTransformUtility.WorldToScreenPoint(app.viewCamera,b.transform.position));
                    app.UI.HoverForCheck(id);previous=b;yield return Hold(.65f);
                }
                yield return FilmClick("play",.7f);yield return Page(GamePage.Chapters);
                yield return FilmClick("back",.6f);yield return Page(GamePage.Title);
                yield return FilmClick("settings",.8f);yield return Page(GamePage.Settings);
                yield return FilmClick("theme-next",.9f);yield return FilmClick("back",1.2f);yield return Page(GamePage.Title);
                yield return FilmClick("settings",.5f);yield return FilmClick("theme-next",.9f);
                yield return FilmClick("back",1.2f);yield return Page(GamePage.Title);
                yield return FilmClick("settings",.4f);yield return FilmClick("theme-prev",.4f);yield return FilmClick("theme-prev",.5f);
                yield return FilmClick("back",.5f);yield return Page(GamePage.Title);recorder.HidePointer();yield return Hold(.8f);
                Need(GameThemes.Current.Id==GameThemes.DefaultThemeId,"menu recording completes a theme roundtrip");
                recorder.End();
            }
            finally
            {
                if(recorder!=null){recorder.End();UnityEngine.Object.Destroy(recorder);recorder=null;}
                app.SelectTheme(theme);app.ShowTitle();
            }
        }
        private static IEnumerator RecordPolishShowcase()
        {
            yield return RecordMenuShowcase();
            string progress=JsonUtility.ToJson(app.Progress.Data);string theme=GameThemes.Current.Id;
            recorder=app.gameObject.AddComponent<ShowcaseRecorder>();recorder.app=app;
            try
            {
                var chapter=app.assets.Catalog.chapters[0];var main=chapter.levels.Where(l=>l.main).ToArray();string final=main.Last().id;
                app.Progress.Data=new ProgressData();foreach(var level in main.Take(main.Length-1))app.Progress.Complete(level.id);
                app.LoadLevel(final);yield return Page(GamePage.Playing);
                var route=JsonUtility.FromJson<ReplayCatalog>(app.assets.replays.text).levels.First(p=>p.id==final).route;
                recorder.Begin("pr02");yield return Hold(.8f);yield return FilmMoves(route,.24f);
                yield return Page(GamePage.Levels);yield return Hold(1.3f);
                Need(app.Progress.ChapterDone(chapter)&&app.UI.FindButton("next-chapter")!=null,"chapter film returns to its map and unlocks the next chapter");
                recorder.End();app.ShowTitle();yield return Page(GamePage.Title);
            }
            finally
            {
                if(recorder!=null){recorder.End();UnityEngine.Object.Destroy(recorder);recorder=null;}
                app.Progress.Data=JsonUtility.FromJson<ProgressData>(progress);app.Progress.Save();GameThemes.Apply(theme);
            }
        }
        private static IEnumerator Hold(float seconds){yield return new WaitForSecondsRealtime(seconds);}
        private static IEnumerator FilmClick(string id,float hold=.7f)
        {
            yield return Ready();var b=app.UI.FindButton(id);if(b==null)throw new InvalidOperationException("Missing "+id);
            recorder.Point(RectTransformUtility.WorldToScreenPoint(app.viewCamera,b.transform.position));yield return Hold(.3f);
            yield return Click(id);yield return Hold(hold);
        }
        private static IEnumerator Stroke(Cell from,Cell to,bool erase=false)
        {
            var a=app.Board.Screen(from);var b=app.Board.Screen(to);recorder.Point(a);yield return Hold(.3f);
            app.Creator.PointerSample(a,true,true,false,erase);
            for(int i=1;i<=15;i++){var p=Vector2.Lerp(a,b,i/15f);recorder.Point(p);app.Creator.PointerSample(p,false,true,false,erase);app.Creator.FlushPreview();yield return Hold(.045f);}
            app.Creator.PointerSample(b,false,false,true,erase);app.Creator.FlushPreview();yield return Hold(.8f);
        }
        private static IEnumerator FilmMoves(string route,float spacing=.55f)
        {
            recorder.HidePointer();
            foreach(char c in route){yield return Ready();app.Act((Command)"URDL".IndexOf(c));Need(!app.Session.State.Dead,"recorded route remains safe");yield return Hold(spacing);}
        }
        private static IEnumerator FilmDrag(string source,string target,Vector2 offset)
        {
            yield return Ready();
            var from=app.UI.FindButton("select-level-"+source).gameObject;
            var to=(RectTransform)app.UI.FindButton("select-level-"+target).transform;
            var start=RectTransformUtility.WorldToScreenPoint(app.viewCamera,from.transform.position);
            var end=RectTransformUtility.WorldToScreenPoint(app.viewCamera,to.position+to.parent.TransformVector(offset));
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,pressPosition=start,position=start,eligibleForClick=true};
            recorder.Point(start);yield return Hold(.35f);
            ExecuteEvents.Execute(from,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(from,pointer,ExecuteEvents.beginDragHandler);
            for(int i=1;i<=20;i++)
            {
                pointer.position=Vector2.Lerp(start,end,Mathf.SmoothStep(0,1,i/20f));recorder.Point(pointer.position);
                ExecuteEvents.Execute(from,pointer,ExecuteEvents.dragHandler);yield return Hold(.04f);
            }
            yield return Hold(.45f);
            ExecuteEvents.Execute(from,pointer,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(from,pointer,ExecuteEvents.endDragHandler);yield return Ready();recorder.HidePointer();
        }
        private static IEnumerator RecordShowcase()
        {
            CreateFixture();Bind(fixture);recorder=app.gameObject.AddComponent<ShowcaseRecorder>();recorder.app=app;
            try
            {
                // Each take uses isolated saves and a copied campaign, never the user's workbench.
                var blank=LevelData.Blank();blank.player=new Cell(2,5);app.Edit(blank);yield return Page(GamePage.Editing);
                recorder.Begin("td01");yield return Hold(.7f);
                app.Creator.Select(Brush.Wall);yield return FilmClick("shape-rectangle");yield return Stroke(new Cell(2,2),new Cell(3,3));
                yield return Stroke(new Cell(2,2),new Cell(3,2),true);
                yield return FilmClick("edit-undo");yield return FilmClick("operation-select");
                yield return Stroke(new Cell(2,2),new Cell(3,3));yield return Stroke(new Cell(2,2),new Cell(3,2));
                yield return FilmClick("edit-undo");recorder.End();

                var room=DraftPresets.All[1].Create();app.Edit(room);yield return Page(GamePage.Editing);
                recorder.Begin("td02");yield return Hold(1);yield return FilmClick("test",.6f);yield return Page(GamePage.Playing);
                yield return FilmMoves("RUU",.65f);app.ReturnToEditor();yield return Page(GamePage.Editing);yield return Hold(1.2f);
                Need(app.Creator.Document.Data.LayoutEquals(room),"trial film preserves original board");recorder.End();

                app.Creator.Document.DiscardSolution();recorder.Begin("td03");yield return FilmClick("solutions");yield return FilmClick("solve-level",.4f);
                yield return WaitUntilReady(()=>CanClick("play-solution"),"showcase solve");yield return Hold(.7f);
                yield return FilmClick("play-solution",.6f);yield return FilmClick("play-pause");yield return FilmClick("next-step");yield return FilmClick("previous-step");yield return FilmClick("play-pause",2f);
                yield return FilmClick("stop-playback");recorder.End();

                recorder.Begin("td04");yield return FilmClick("drafts",.7f);yield return WaitUntilReady(()=>CanClick("adopt-draft"),"draft footage");
                yield return Hold(1);yield return FilmClick("reroll",1f);yield return WaitUntilReady(()=>CanClick("adopt-draft"),"draft reroll");
                yield return FilmClick("adopt-draft",1);yield return FilmClick("edit-undo",1);recorder.End();

                app.ShowTitle();yield return Page(GamePage.Title);yield return Click("campaign");yield return Chapter("chapter_2");
                recorder.Begin("td05");yield return Hold(1);
                yield return FilmDrag("c2_02","c2_01",new Vector2(-64,0));yield return Hold(1);
                yield return FilmDrag("c2_b01","c2_04",new Vector2(0,150));yield return Hold(1);
                var node=app.UI.FindButton("select-level-c2_b01");
                ExecuteEvents.Execute(node.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);
                app.UI.ShowLevelPeek(Draft().working.Board("c2_b01"),(RectTransform)node.transform,"支线预览");yield return Hold(1.6f);app.UI.HideLevelPeek();
                yield return FilmClick("select-level-c2_b01",1);yield return Page(GamePage.Editing);recorder.End();yield return Click("leave-editor");
                // Undo the two recorded graph edits before the import take.
                yield return Click("campaign-undo");yield return Click("campaign-undo");
                room.title="并推";app.Library.Save(room);
                recorder.Begin("td06");yield return FilmClick("campaign-import-custom",1);yield return FilmClick("custom-import-item-0",1);
                yield return FilmClick("custom-import-confirm",1);yield return FilmClick("campaign-review",1.2f);
                yield return FilmClick("campaign-publish",1);recorder.End();

                string[] ids={"c1_02","c1_04","c2_04","c3_03","c4_02"};
                var replays=JsonUtility.FromJson<ReplayCatalog>(fixture.replays.text);
                for(int i=0;i<ids.Length;i++)
                {
                    // Use actual campaign level data in an editor trial, isolating progress.
                    var data=fixture.Level(ids[i]);app.Edit(data);yield return Page(GamePage.Editing);app.Creator.Play();yield return Page(GamePage.Playing);
                    recorder.Begin("ld0"+(i+1));yield return Hold(1);
                    if(i==0)
                    {
                        app.Act(Command.Down);yield return Hold(1.2f);Need(app.Session.State.Dead,"intro shows sunlight death");
                        app.Act(Command.Undo);yield return Hold(.8f);Need(!app.Session.State.Dead,"intro restores with undo");
                    }
                    var route=replays.levels.First(p=>p.id==ids[i]).route;
                    // Stop the film just after the coffin animation, before the editor returns.
                    foreach(char move in route.Take(route.Length-1)){yield return FilmMoves(move.ToString(),.55f);}
                    yield return Ready();app.Act((Command)"URDL".IndexOf(route.Last()));
                    Need(app.Session.State.Won,"showcase campaign take reaches coffin: "+ids[i]);yield return Hold(.45f);recorder.End();
                    yield return Page(GamePage.Editing);
                }
                app.ShowTitle();yield return Page(GamePage.Title);
            }
            finally{if(recorder!=null){recorder.End();UnityEngine.Object.Destroy(recorder);recorder=null;}}
        }
    }
}
