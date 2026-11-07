#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BoxOrVoid.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BoxOrVoid.Study.LevelEditing
{
    /// <summary>Opt-in QA: isolated keys and uGUI raycast events, never OS/device input.</summary>
    public sealed class CreatorRuntimeCheck:MonoBehaviour,IStudyKeyboard
    {
        [Serializable]private sealed class Report
        {public string status="running",error,output;public List<string> checks=new List<string>();public int initialFrame,finalFrame,screenWidth,screenHeight;public bool actualGameplaySolved,progressIsolated;}
        public static bool Requested=>Environment.GetCommandLineArgs().Contains("-creator-check");
        public static string Status {get;private set;}="idle";
        private PuzzleApp app;
        private PlayerLevelEditor editor;
        private Report report;
        private string output;
        private readonly HashSet<KeyCode> held=new HashSet<KeyCode>();
        private KeyCode down;
        private int downFrame;
        public bool Held(KeyCode k)=>held.Contains(k);
        public bool Pressed(KeyCode k)=>down==k&&Time.frameCount==downFrame;
        public static string Begin(PuzzleApp app)
        {
            if(!app.TestInputOnly||app.GetComponent<CreatorRuntimeCheck>()!=null)throw new InvalidOperationException("Expected an isolated, idle creator check.");
            var probe=app.gameObject.AddComponent<CreatorRuntimeCheck>();probe.app=app;probe.editor=app.Creator;
            probe.output=Path.GetFullPath("Studies/BoxOrVoid/Evidence/Creator/"+(Application.isEditor?"Editor":"Windows"));
            string[] args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-creator-check-output");if(i>=0&&i+1<args.Length)probe.output=Path.GetFullPath(args[i+1]);
            Directory.CreateDirectory(probe.output);probe.report=new Report{output=probe.output,initialFrame=Time.frameCount,screenWidth=Screen.width,screenHeight=Screen.height};Status="running";
            app.Progress.Data.musicVolume=app.Progress.Data.effectsVolume=0;app.ApplyVolumes();app.SetTestKeyboard(probe);probe.StartCoroutine(probe.Guarded(probe.Run()));return Status;
        }
        private IEnumerator Run()
        {
            while(app.IsTransitioning)yield return null;
            var mark=app.GetComponent<StudyMenu>().AuthoredTitle.transform.Find("Creator Button/Icon").GetComponent<Image>();
            Require(mark.sprite!=null&&mark.sprite.name=="CreatorMark","Saved title uses the dedicated creation glyph");
            Capture("01-title.png");yield return Tap(KeyCode.RightArrow);Capture("01-creator-focus.png");yield return Tap(KeyCode.Return);
            Require(app.Page==StudyPage.Creating&&editor.Page==CreatorPage.Library,"Main menu keyboard enters creator");Capture("02-empty-library.png");
            Require(!editor.Canvas.GetComponentsInChildren<EditorWidget>().Any(w=>w.Id=="open"),"Only the new card is shown in an empty library");
            yield return Click("new");yield return Click("new-size-0");yield return Click("dialog-choice-1");
            Require(editor.Session.Document.level.width==7,"New small board");Capture("03-blank.png");
            yield return Tap(KeyCode.Escape);Capture("18-leave-edit.png");yield return Click("dialog-choice-0");
            yield return Click("tools");Capture("19-tool-guide.png");yield return Click("choose-rectangle");yield return Click("dialog-choice-1");
            Require(editor.Session.Tool==EditTool.Rectangle,"Rectangle choice activates drawing mode");
            // All authoring below is routed through visible controls and pointer events.
            yield return Click("brush-1");yield return Cell(23);Require(editor.Session.Document.level.grid[23].Objects.Length==1,"Place a box");
            yield return Tap(KeyCode.Z);Require(editor.Session.Document.level.grid[23].Objects.Length==0,"Z undoes placement");
            yield return Tap(KeyCode.Y);Require(editor.Session.Document.level.grid[23].Objects.Length==1,"Y redoes placement");
            yield return Tap(KeyCode.S,KeyCode.LeftControl);Require(!editor.Session.Dirty,"Unfinished draft can be saved");
            yield return Click("brush-2");yield return Cell(22);yield return Tap(KeyCode.Tab);yield return Cell(8);yield return Tap(KeyCode.Tab);
            yield return Click("brush-3");yield return Cell(24);Require(LevelValidation.ForPlay(editor.Session.Document).Count==0,"Playable document via UI");
            yield return Click("brush-5");yield return DragCells(0,6);Require(editor.Session.Document.level.grid.Take(7).All(c=>c.Objects.Any(o=>o.Color==Tone.Gray)),"Continuous drag paints every crossed cell");
            yield return Tap(KeyCode.Z);Require(editor.Session.Document.level.grid.Take(7).All(c=>c.Objects.Length==0),"One drag is one undo");yield return Tap(KeyCode.Y);
            yield return Tap(KeyCode.S,KeyCode.LeftControl);Capture("04-editing.png");
            string initial=editor.Session.Document.LayoutHash();int historyRevision=editor.Session.Revision;
            yield return Tap(KeyCode.F5);Require(app.Page==StudyPage.Playing&&editor.Page==CreatorPage.Playing,"F5 enters shared gameplay");Capture("05-play.png");
            yield return Tap(KeyCode.RightArrow);Require(app.Session.State.IsSolved,"Actual push solves authored level");report.actualGameplaySolved=true;Capture("06-complete.png");
            Require(app.Progress.CompletedCount==0,"Creator completion does not alter campaign");report.progressIsolated=true;
            yield return Tap(KeyCode.F5);Require(editor.Page==CreatorPage.Editing&&app.Page==StudyPage.Creating,"F5 returns after completion");
            Require(editor.Session.Document.LayoutHash()==initial&&editor.Session.Revision==historyRevision,"Return preserves original layout and edit history");Require(editor.Session.Document.IsPlayedThrough,"Completion is tied to exact layout");
            yield return Click("brush-1");yield return Cell(33);Require(!editor.Session.Document.IsPlayedThrough,"Layout change invalidates completion");yield return Tap(KeyCode.Z);Require(editor.Session.Document.IsPlayedThrough,"Undo restores completion for matching layout");
            yield return Click("rename");var name=ActiveInput("Level name");name.text="环路 · UI 验证";EventSystem.current.SetSelectedGameObject(name.gameObject);yield return Tap(KeyCode.Z);Require(editor.Session.Document.LayoutHash()==initial,"Typing focus suppresses Z editing shortcut");yield return Click("dialog-choice-1");
            yield return Tap(KeyCode.S,KeyCode.LeftControl);string id=editor.Session.Document.id;string hash=editor.Session.Document.LayoutHash();
            yield return Click("more");yield return Click("option-4");yield return Click("dialog-choice-1");Require(Directory.GetFiles(editor.Library.ExportFolder,"*.json").Length==1,"Export writes shareable file");Capture("07-export.png");yield return Click("dialog-choice-0");
            yield return Tap(KeyCode.Escape);Require(editor.Page==CreatorPage.Library,"Saved editor returns to library");Capture("08-library.png");
            yield return Click("open");Require(editor.Session.Document.id==id&&editor.Session.Document.LayoutHash()==hash,"Reload restores saved data");
            yield return Tap(KeyCode.Escape);string shared=Directory.GetFiles(editor.Library.ExportFolder,"*.json").Single();
            int pickerCalls=0;
            editor.PickPath=(folders,start)=>{pickerCalls++;return System.Threading.Tasks.Task.FromResult<string>(null);};
            yield return Click("import");
            Require(editor.ModalOpen && pickerCalls==0 && !ActiveWidget("import-preview").Button.interactable,"Import opens its guide without calling Windows");
            Capture("08-import-guide.png");
            yield return Click("import-browse");
            Require(pickerCalls==1 && editor.ModalOpen && ActiveWidget("import-browse").Button.interactable,"Cancelling file selection keeps the import guide open");
            ActiveInput("Import path").text=shared+".missing";
            yield return Click("import-preview");
            Require(ActiveInput("Import path").text==shared+".missing","Invalid paths stay editable in the guide");
            editor.PickPath=(folders,start)=>System.Threading.Tasks.Task.FromResult(shared);
            yield return Click("import-browse");yield return Click("dialog-choice-0");
            Require(ActiveInput("Import path").text==shared,"Reselect returns to the import guide with the chosen file");
            yield return Click("import-preview");
            Require(editor.Library.List().Count==1,"Preview does not write to the library");
            yield return Click("dialog-close");
            editor.ClipboardText=()=>(char)34+shared+(char)34;
            yield return Tap(KeyCode.V,KeyCode.LeftControl);Capture("09-import-preview.png");yield return Click("dialog-choice-1");
            Require(editor.Session.Document.id!=id&&editor.Session.Document.LayoutHash()==hash,"Import creates independent non-overwriting copy");Require(editor.Library.List().Count==2,"Both original and imported level remain");
            yield return Click("more");yield return Click("option-2");yield return Click("width-minus");yield return Click("width-minus");Capture("10-resize.png");yield return Click("dialog-choice-1");Require(editor.ModalOpen,"Cropping has confirmation");yield return Click("dialog-choice-1");Require(editor.Session.Document.level.width==5,"Resize applied");yield return Tap(KeyCode.Z);Require(editor.Session.Document.LayoutHash()==hash,"Resize undo restores all objects");
            // Exercise the mechanism-specific obscured-object flow.
            yield return Click("brush-0");yield return Click("tone-black");yield return Cell(40);
            yield return Click("brush-2");yield return Click("tone-white");yield return Cell(40);
            yield return Click("brush-1");yield return Click("tone-black");yield return Cell(40);
            yield return Click("tool-look");yield return Cell(40);yield return Click("layer-1");Capture("11-stack.png");yield return Click("delete");
            yield return Tap(KeyCode.F5);Require(editor.ModalOpen,"Missing player produces actionable dialog");Capture("12-missing-player.png");yield return Click("dialog-choice-1");Require(editor.Session.Brush==ObjectKind.Player&&editor.Session.Tone==Tone.White,"Fix action selects correct character");yield return Cell(22);
            Require(LevelValidation.ForPlay(editor.Session.Document).Count==0,"Fix returns document to playable state");
            // Selection copy/transform is atomic and remains reversible.
            yield return Tap(KeyCode.M);yield return DragCells(38,40);yield return Tap(KeyCode.C,KeyCode.LeftControl);yield return Click("rotate");yield return Cell(10);yield return Tap(KeyCode.Z);
            Capture("13-final-edit.png");yield return Tap(KeyCode.S,KeyCode.LeftControl);
            report.checks.Add("UI pointer routing, modal controls, selection transform, save/reload/import/export round trip completed");
            yield return Tap(KeyCode.Escape);yield return Click("new");yield return Click("templates");yield return Click("template-next");yield return Click("template-n_box_6");
            Require(LevelValidation.ForPlay(editor.Session.Document).Count==0,"Built-in level creates a playable independent document");
            yield return Click("brush-5");Capture("14-creation.png");Capture("15-creation-1920.png",1920,1080);
            yield return Tap(KeyCode.F1);
            var guide=ActiveWidget("dialog-choice-0").transform.parent;
            var lines=guide.GetComponentsInChildren<TMPro.TMP_Text>();
            foreach(var line in lines)line.ForceMeshUpdate();
            Require(!lines.Any(line=>line.isTextTruncated),"All shortcut labels fit their printed rows and keycaps");
            Capture("16-shortcuts.png");yield return Tap(KeyCode.Escape);
            // Dense stacks expose paging and disabled ordering controls in the actual UI.
            int empty=Array.FindIndex(editor.Session.Document.level.grid,c=>c.Objects.Length==0);
            yield return Click("brush-1");yield return Click("tone-black");yield return Click("tools");yield return Click("repeat");
            for(int n=0;n<6;n++)yield return Cell(empty);
            yield return Click("tool-look");yield return Cell(empty);
            Require(!ActiveWidget("lift").Button.interactable,"Top layer cannot be moved further up");
            var drawer=(RectTransform)ActiveWidget("layer-next").transform.parent;float fullHeight=drawer.rect.height;
            yield return Click("layer-next");yield return Click("layer-0");
            Require(drawer.rect.height<fullHeight,"Layer inspector contracts on the shorter final page");
            Require(!ActiveWidget("lower").Button.interactable,"Bottom layer cannot be moved further down");
            Capture("17-stack-pages.png");yield return Click("close-stack");
            yield return Click("brush-5");yield return Tap(KeyCode.F);yield return Cell(empty);yield return Click("dialog-close");
            Require(!editor.ModalOpen&&editor.Canvas.GetComponentInChildren<EditorGridGraphic>().Highlights.Count==0,"Closing fill preview clears its highlight");
            yield return CheckDragOutsideCanvas();
        }
        private EditorWidget ActiveWidget(string id)=>editor.Canvas.GetComponentsInChildren<EditorWidget>().First(w=>w.Id==id&&w.gameObject.activeInHierarchy);
        private IEnumerator CheckDragOutsideCanvas()
        {
            yield return Click("brush-1");
            var surface=editor.Canvas.GetComponentInChildren<EditorBoardPointer>().gameObject;var rect=(RectTransform)surface.transform;
            var e=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(app.Board.Camera,rect.position),scrollDelta=Vector2.up,button=PointerEventData.InputButton.Left};
            for(int n=0;n<15;n++)ExecuteEvents.Execute(surface,e,ExecuteEvents.scrollHandler);
            Canvas.ForceUpdateCanvases();string before=editor.Session.Document.LayoutHash();
            ExecuteEvents.Execute(surface,e,ExecuteEvents.pointerDownHandler);yield return null;string afterDown=editor.Session.Document.LayoutHash();
            e.position=RectTransformUtility.WorldToScreenPoint(app.Board.Camera,rect.TransformPoint(new Vector3(0,-rect.rect.height*.5f-75,0)));
            ExecuteEvents.Execute(surface,e,ExecuteEvents.dragHandler);yield return null;
            Require(editor.Session.Document.LayoutHash()==afterDown,"Captured drag over bottom UI does not paint clipped board cells");
            ExecuteEvents.Execute(surface,e,ExecuteEvents.pointerUpHandler);yield return null;
            if(afterDown!=before){yield return Tap(KeyCode.Z);Require(editor.Session.Document.LayoutHash()==before,"Clipped stroke remains one undo");}
            yield return Tap(KeyCode.Home);
        }
        private TMPro.TMP_InputField ActiveInput(string name)=>editor.Canvas.GetComponentsInChildren<TMPro.TMP_InputField>().First(x=>x.name==name&&x.gameObject.activeInHierarchy);
        private IEnumerator Tap(KeyCode key,params KeyCode[] modifiers)
        {
            while(app.IsTransitioning)yield return null;
            down=key;downFrame=Time.frameCount+1;held.Add(key);foreach(var modifier in modifiers)held.Add(modifier);
            yield return null;yield return null;yield return new WaitForSecondsRealtime(.04f);down=KeyCode.None;held.Clear();yield return new WaitForSecondsRealtime(.13f);while(app.IsTransitioning)yield return null;
        }
        private IEnumerator Click(string id)
        {
            while(app.IsTransitioning)yield return null;yield return null;Canvas.ForceUpdateCanvases();
            var widget=editor.Canvas.GetComponentsInChildren<EditorWidget>(false).FirstOrDefault(w=>w.Id==id&&w.gameObject.activeInHierarchy&&w.Button.interactable);
            if(widget==null)throw new InvalidOperationException("Visible enabled control missing: "+id);
            Vector2 position=RectTransformUtility.WorldToScreenPoint(app.Board.Camera,widget.transform.position);
            var e=new PointerEventData(EventSystem.current){position=position,button=PointerEventData.InputButton.Left};var hit=Hit(e);
            var button=ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit);Require(button==widget.gameObject,"Raycast reaches "+id);
            ExecuteEvents.ExecuteHierarchy(hit,e,ExecuteEvents.pointerEnterHandler);ExecuteEvents.ExecuteHierarchy(hit,e,ExecuteEvents.pointerDownHandler);ExecuteEvents.ExecuteHierarchy(hit,e,ExecuteEvents.pointerUpHandler);ExecuteEvents.ExecuteHierarchy(hit,e,ExecuteEvents.pointerClickHandler);
            yield return null;ExecuteEvents.ExecuteHierarchy(hit,e,ExecuteEvents.pointerExitHandler);yield return new WaitForSecondsRealtime(.05f);while(app.IsTransitioning)yield return null;
        }
        private GameObject Hit(PointerEventData e)
        {var hits=new List<RaycastResult>();editor.Canvas.GetComponent<GraphicRaycaster>().Raycast(e,hits);if(hits.Count==0)throw new InvalidOperationException("UI raycast missed at "+e.position);return hits[0].gameObject;}
        private Vector2 CellScreen(int index)=>RectTransformUtility.WorldToScreenPoint(app.Board.Camera,editor.BoardCanvas.transform.TransformPoint(editor.BoardCanvas.Position(index)));
        private IEnumerator Cell(int index)
        {
            Canvas.ForceUpdateCanvases();var e=new PointerEventData(EventSystem.current){position=CellScreen(index),button=PointerEventData.InputButton.Left};var hit=Hit(e);
            Require(ExecuteEvents.GetEventHandler<IPointerDownHandler>(hit)?.GetComponent<EditorBoardPointer>()!=null,"Cell "+index+" receives pointer");
            ExecuteEvents.ExecuteHierarchy(hit,e,ExecuteEvents.pointerDownHandler);yield return null;ExecuteEvents.ExecuteHierarchy(hit,e,ExecuteEvents.pointerUpHandler);yield return new WaitForSecondsRealtime(.04f);
        }
        private IEnumerator DragCells(int from,int to)
        {
            Canvas.ForceUpdateCanvases();var e=new PointerEventData(EventSystem.current){position=CellScreen(from),button=PointerEventData.InputButton.Left};var hit=Hit(e);
            ExecuteEvents.ExecuteHierarchy(hit,e,ExecuteEvents.pointerDownHandler);yield return null;e.position=CellScreen(to);ExecuteEvents.ExecuteHierarchy(hit,e,ExecuteEvents.dragHandler);yield return null;ExecuteEvents.ExecuteHierarchy(hit,e,ExecuteEvents.pointerUpHandler);yield return new WaitForSecondsRealtime(.05f);
        }
        private void Require(bool condition,string description){if(!condition)throw new InvalidOperationException(description);report.checks.Add(description);}
        private IEnumerator Guarded(IEnumerator routine)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);
            while(stack.Count>0)
            {
                bool more=false;object item=null;Exception failure=null;
                try{more=stack.Peek().MoveNext();if(more)item=stack.Peek().Current;}catch(Exception ex){failure=ex;}
                if(failure!=null){report.error=failure.ToString();break;}
                if(!more){stack.Pop();continue;}if(item is IEnumerator nested){stack.Push(nested);continue;}yield return item;
            }
            held.Clear();down=KeyCode.None;app.SetTestKeyboard(null);report.finalFrame=Time.frameCount;report.status=report.error==null?"passed":"failed";Status=report.status;
            if(report.error!=null){Capture("failure.png");Debug.LogError(report.error);}
            File.WriteAllText(Path.Combine(output,"flow-report.json"),JsonUtility.ToJson(report,true));
            if(Requested&&!Application.isEditor)Application.Quit(report.error==null?0:1);
            Destroy(this);
        }
        public void Capture(string name,int width=0,int height=0)
        {
            if(width<=0)width=Screen.width;if(height<=0)height=Screen.height;
            var camera=app.Board.Camera;var prior=camera.targetTexture;var priorActive=RenderTexture.active;float aspect=camera.aspect;
            var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.aspect=(float)width/height;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name),image.EncodeToPNG());}
            finally{camera.targetTexture=prior;camera.aspect=aspect;RenderTexture.active=priorActive;Destroy(image);rt.Release();Destroy(rt);Canvas.ForceUpdateCanvases();}
        }
    }
}
#endif
