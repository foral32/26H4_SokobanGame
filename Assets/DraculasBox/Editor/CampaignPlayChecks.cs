using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DraculasBox.Core;
using DraculasBox.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DraculasBox.Editor
{
    // Two separate Editor Play sessions exercise real asset imports and recovery on domain reload.
    public static partial class CampaignPlayChecks
    {
        private const string Key = "DraculasBox.CheckCampaign.";
        private static string Evidence => SessionState.GetString("DraculasBox.CampaignEvidenceFolder","DraculasBox/Evidence/CampaignMapPlay");
        [Serializable] private sealed class Report { public bool passed; public string error; public List<string> checks=new(); }
        private static DraculaApp app;
        private static PlaytestDriver driver;
        private static GameAssets original,fixture;
        private static Report report;
        public static string Status { get; private set; }
        private static string Folder => SessionState.GetString(Key+"folder","");
        private static string Get(string name)=>SessionState.GetString(Key+name,"");
        private static void Set(string name,string value)=>SessionState.SetString(Key+name,value);

        public static void Run(bool resume)
            =>Begin(resume?Finish():Prepare(),resume?"roundtrip":"authoring",resume);
        public static void RunInputs()=>Begin(Inputs(),"inputs",true);
        private static void Begin(IEnumerator checks,string name,bool cleanup)
        {
            app=UnityEngine.Object.FindFirstObjectByType<DraculaApp>();
            driver=UnityEngine.Object.FindFirstObjectByType<PlaytestDriver>();
            if(app==null||!app.Automated||driver==null||Status=="running"||app.Page!=GamePage.Title)
                throw new InvalidOperationException("Start an isolated BackgroundPlaytest and wait for its title screen.");
            Directory.CreateDirectory(Evidence);
            report=new Report();original=app.assets;Status="running";
            app.StartCoroutine(Guard(checks,name,cleanup));
        }
        private static IEnumerator Guard(IEnumerator first,string name,bool cleanup)
        {
            var stack=new Stack<IEnumerator>();stack.Push(first);
            while(stack.Count>0)
            {
                bool more;object value=null;
                try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}
                catch(Exception e){report.error=e.ToString();break;}
                if(!more){stack.Pop();continue;}
                if(value is IEnumerator nested)stack.Push(nested);else yield return value;
            }
            Bind(original);
            report.passed=report.error==null;Status=report.passed?"passed":"failed";
            File.WriteAllText(Evidence+"/"+name+".json",JsonUtility.ToJson(report,true));
            if(cleanup&&report.passed)Cleanup();
        }
        public static void Cleanup()
        {
            string path=Folder;
            if(!path.StartsWith("Assets/DraculasBoxCampaignCheck_",StringComparison.Ordinal)||
                path.Substring("Assets/DraculasBoxCampaignCheck_".Length).Length!=32||
                !Guid.TryParseExact(path.Substring("Assets/DraculasBoxCampaignCheck_".Length),"N",out _))return;
            if(AssetDatabase.IsValidFolder(path))
            {
                string guid=AssetDatabase.AssetPathToGUID(path+"/Chapters.json");
                string cacheRoot=Path.GetFullPath(Path.Combine("Library","DraculasBoxCampaign"));
                string cache=Path.GetFullPath(Path.Combine(cacheRoot,guid));
                if(Guid.TryParseExact(guid,"N",out _)&&Path.GetDirectoryName(cache)==cacheRoot&&Directory.Exists(cache))
                    Directory.Delete(cache,true);
                AssetDatabase.DeleteAsset(path);
            }
            Set("folder","");
        }
        private static void Need(bool ok,string message)
        {if(!ok)throw new InvalidOperationException(message);report.checks.Add(message);}
        private static IEnumerator Ready()
        {
            yield return null;float start=Time.realtimeSinceStartup;
            while(app.Transitioning||app.Board.IsAnimating)
            {if(Time.realtimeSinceStartup-start>12)throw new TimeoutException(app.Page.ToString());yield return null;}
            Canvas.ForceUpdateCanvases();
        }
        private static IEnumerator Click(string id)
        {
            yield return Ready();
            var button=app.UI.FindButton(id);
            if(button==null||!button.isActiveAndEnabled||!button.interactable)throw new InvalidOperationException("Unavailable control: "+id);
            var hits=new List<RaycastResult>();
            var rect=(RectTransform)button.transform;
            var p=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(app.viewCamera,rect.TransformPoint(rect.rect.center))};
            EventSystem.current.RaycastAll(p,hits);
            if(hits.Count==0||ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)!=button.gameObject)
                throw new InvalidOperationException("Control is covered: "+id+" by "+hits.FirstOrDefault().gameObject);
            app.UI.ClickForCheck(id);
            yield return Ready();
        }
        private static IEnumerator Page(GamePage page)
        {
            float start=Time.realtimeSinceStartup;
            while(app.Page!=page||app.Transitioning)
            {if(Time.realtimeSinceStartup-start>12)throw new TimeoutException("Waiting for "+page+" at "+app.Page);yield return null;}
            yield return Ready();
        }
        private static void Capture(string name)
        {File.Copy(driver.Capture("campaign_"+name),Evidence+"/"+name+".png",true);}
        private static IEnumerator Options(string id)
        {
            yield return Ready();app.UI.FocusButton("select-level-"+id);
            var button=app.UI.FindButton("select-level-"+id);
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Right};
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return Ready();
        }
        private static IEnumerator Chapter(string id)
        {
            if(app.UI.FindButton("campaign-new")!=null)yield return Click("campaign-back");
            if(app.UI.FindButton("select-chapter-"+id)==null)yield return Click("chapters-prev");
            yield return Click("select-chapter-"+id);
        }
        private sealed class Chord : IStudyKeyboard
        {
            public KeyCode[] keys;
            public int frame=Time.frameCount+1;
            public bool Held(KeyCode key)=>Time.frameCount==frame&&Array.IndexOf(keys,key)>=0;
            public bool Pressed(KeyCode key)=>Held(key);
        }
        private static IEnumerator Press(params KeyCode[] keys)
        {
            app.SetTestKeyboard(new Chord{keys=keys});
            yield return null;yield return null;
            app.SetTestKeyboard(driver);
        }
        private static IEnumerator Drag(string source,string target,Vector2 offset,bool cancel=false,string preview=null)
        {
            yield return Ready();
            app.UI.FocusButton("select-level-"+source);
            var from=app.UI.FindButton("select-level-"+source).gameObject;
            var to=(RectTransform)app.UI.FindButton("select-level-"+target).transform;
            var start=RectTransformUtility.WorldToScreenPoint(app.viewCamera,from.transform.position);
            string prior=preview==null?null:JsonUtility.ToJson(Draft()?.working);
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,pressPosition=start,position=start,eligibleForClick=true};
            ExecuteEvents.Execute(from,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(from,pointer,ExecuteEvents.beginDragHandler);
            pointer.position=RectTransformUtility.WorldToScreenPoint(app.viewCamera,to.position+to.parent.TransformVector(offset));
            ExecuteEvents.Execute(from,pointer,ExecuteEvents.dragHandler);
            Need(!pointer.eligibleForClick&&app.Page==GamePage.Campaign,"drag previews without opening the board: "+source);
            if(preview!=null)
            {
                yield return new WaitForSecondsRealtime(.2f);
                Need(JsonUtility.ToJson(Draft()?.working)==prior,"drag preview leaves the working draft untouched: "+source);
                Capture(preview);
            }
            if(cancel)yield return Press(KeyCode.Escape);
            ExecuteEvents.Execute(from,pointer,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(from,pointer,ExecuteEvents.endDragHandler);
            yield return new WaitForSecondsRealtime(.16f);
            yield return Ready();
        }
        private static IEnumerator EdgeScroll(string id)
        {
            app.UI.FocusButton("select-level-"+id);
            var node=app.UI.FindButton("select-level-"+id).gameObject;
            var sheet=(RectTransform)node.transform.parent;
            var viewport=(RectTransform)sheet.parent;
            float initial=sheet.anchoredPosition.x;
            var start=RectTransformUtility.WorldToScreenPoint(app.viewCamera,node.transform.position);
            var edge=viewport.TransformPoint(new Vector2(viewport.rect.xMax-9,0));
            var pointer=new PointerEventData(EventSystem.current){pressPosition=start,position=RectTransformUtility.WorldToScreenPoint(app.viewCamera,edge)};
            ExecuteEvents.Execute(node,pointer,ExecuteEvents.beginDragHandler);
            yield return new WaitForSecondsRealtime(.32f);
            Need(sheet.anchoredPosition.x<initial-10,"holding a level at the map edge scrolls to offscreen destinations");
            yield return Press(KeyCode.Escape);
            ExecuteEvents.Execute(node,pointer,ExecuteEvents.endDragHandler);
            yield return Ready();
        }
        private static void CreateFixture()
        {
            Cleanup();
            string name="DraculasBoxCampaignCheck_"+Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets",name);Set("folder","Assets/"+name);
            AssetDatabase.CreateFolder(Folder,"Levels");
            TextAsset Copy(TextAsset asset,string relative)
            {
                string path=Folder+"/"+relative;
                File.Copy(AssetDatabase.GetAssetPath(asset),path);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                return AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            }
            fixture=UnityEngine.Object.Instantiate(original);
            fixture.catalog=Copy(original.catalog,"Chapters.json");fixture.replays=Copy(original.replays,"Replays.json");
            fixture.levels=original.levels.Select(a=>Copy(a,"Levels/"+a.name+".json")).ToArray();
            AssetDatabase.CreateAsset(fixture,Folder+"/GameAssets.asset");
            AssetDatabase.SaveAssetIfDirty(fixture);
            Set("start",new CampaignProject(fixture).Revision);
        }
        private static CampaignDraftFile Draft()=>new CampaignProject(fixture).ReadDraft();
        private static void Bind(GameAssets assets)
        {
            app.assets=assets;
            app.UI.Initialize(assets,app,app.viewCamera);
        }
        private static IEnumerator TypeTitle(string value)
        {
            yield return Ready();
            var field=app.UI.Root.Find("Level title").GetComponent<TMP_InputField>();
            var p=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(app.viewCamera,field.transform.position)};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(p,hits);
            Need(hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==field.gameObject,"level title is reachable by a pointer");
            p.pointerCurrentRaycast=p.pointerPressRaycast=hits[0];
            ExecuteEvents.Execute(field.gameObject,p,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(field.gameObject,p,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(field.gameObject,p,ExecuteEvents.pointerClickHandler);
            yield return null;yield return null;
            Need(field.isFocused&&app.UI.HasTextFocus,"clicking the title activates text editing");
            var caret=field.textViewport.Find("Caret");
            Need(caret!=null,"runtime input activation creates the caret and selection renderer");
            field.ProcessEvent(new Event{type=EventType.KeyDown,keyCode=KeyCode.A,modifiers=EventModifiers.Control});
            field.ForceLabelUpdate();
            yield return null;Canvas.ForceUpdateCanvases();
            var mesh=caret.GetComponent<CanvasRenderer>().GetMesh();
            Need(field.selectionAnchorPosition!=field.selectionFocusPosition&&mesh!=null&&mesh.vertexCount>0,"Ctrl+A draws a visible text selection");
            Capture("editor-title-selection");
            foreach(char character in value)field.ProcessEvent(new Event{type=EventType.KeyDown,character=character});
            // The isolated input module is disabled. Mirror OnUpdateSelected's final
            // label refresh after its queued key events have been processed.
            field.ForceLabelUpdate();
            yield return null;Canvas.ForceUpdateCanvases();
            Need(field.text==value,"text editing accepts the complete Chinese level name");
            mesh=caret.GetComponent<CanvasRenderer>().GetMesh();
            float until=Time.unscaledTime+1.5f;
            while((mesh==null||mesh.vertexCount==0)&&Time.unscaledTime<until)
            {
                yield return null;Canvas.ForceUpdateCanvases();
                mesh=caret.GetComponent<CanvasRenderer>().GetMesh();
            }
            Need(field.selectionAnchorPosition==field.selectionFocusPosition&&mesh!=null&&mesh.vertexCount>0&&
                mesh.bounds.size.y>mesh.bounds.size.x*3&&!caret.GetComponent<CanvasRenderer>().cull&&
                field.textComponent.text.StartsWith(value,StringComparison.Ordinal),"typing renders the updated text and a narrow insertion caret");
            Capture("editor-title-caret");
        }
        private static IEnumerator Inputs()
        {
            CreateFixture();Bind(fixture);
            yield return Click("campaign");yield return Chapter("chapter_1");yield return Click("select-level-c1_01");
            var editor=app.Creator;var doc=editor.Document;var before=doc.Data.Copy();
            Need(app.UI.FindButton("scene").GetComponentInChildren<TMP_Text>().text=="场景设置","scene settings has an explicit action label");
            Vector2 start=app.Board.Screen(new Cell(4,3)),delta=new(48,28);
            int history=doc.UndoCount;
            Need(editor.PanSample(start,true,true,false),"middle drag begins inside the fitted canvas");
            editor.PanSample(start+delta,false,true,false);
            Need(Vector2.Distance(app.Board.Screen(new Cell(4,3)),start+delta)<1,"a fitted board follows the middle drag at 100 percent zoom");
            Need(!editor.PointerSample(start+delta,true,true,false),"panning suppresses simultaneous brush input");
            editor.PanSample(start+delta,false,false,true);
            Need(before.ContentEquals(doc.Data)&&history==doc.UndoCount,"panning changes neither the level nor undo history");
            Need(!editor.PanSample(start+delta*2,false,true,false),"releasing middle stops panning");
            yield return Click("zoom-fit");
            Need(Vector2.Distance(app.Board.Screen(new Cell(4,3)),start)<1,"fit recenters the board after a middle drag");
            var tool=app.UI.FindButton("scene");
            Need(!editor.PanSample(RectTransformUtility.WorldToScreenPoint(app.viewCamera,tool.transform.position),true,true,false),"pressing middle on a toolbar control does not move the board");
            yield return TypeTitle("夜窗");
            var typing=app.UI.Root.Find("Level title").GetComponent<TMP_InputField>();
            typing.ProcessEvent(new Event{type=EventType.KeyDown,keyCode=KeyCode.LeftArrow});
            typing.ProcessEvent(new Event{type=EventType.KeyDown,keyCode=KeyCode.Backspace});
            Need(typing.text=="窗","arrow navigation and Backspace edit at the visible insertion point");
            typing.ProcessEvent(new Event{type=EventType.KeyDown,character='夜'});
            Need(editor.PanSample(start,true,true,false)&&doc.Data.title=="夜窗","starting a canvas drag commits the typed level name");
            editor.PanSample(start+delta,false,true,false);editor.PanSample(start+delta,false,false,true);
            Need(app.Board.CellAt(app.Board.Screen(new Cell(4,3)),out var picked)&&picked==new Cell(4,3),"cell picking remains accurate after panning");
            yield return Click("save");
            Need(!doc.Dirty&&Draft().working.Board("c1_01").title=="夜窗","save draft persists the name entered through the input field");
            yield return TypeTitle("归棺");
            var title=app.UI.Root.Find("Level title").GetComponent<TMP_InputField>();
            title.ProcessEvent(new Event{type=EventType.KeyDown,keyCode=KeyCode.Escape});
            EventSystem.current.SetSelectedGameObject(null);
            Need(doc.Data.title=="归棺"&&title.text=="归棺","Escape finishes editing without restoring the previous level name");
            yield return Click("edit-undo");Need(doc.Data.title=="夜窗","rename is one undoable document operation");
            yield return Click("edit-redo");Need(doc.Data.title=="归棺","redo restores the complete name");
            yield return Click("save");yield return Click("leave-editor");
            Need(app.UI.FindButton("select-level-c1_01").transform.Find("Name").GetComponent<TMP_Text>().text=="归棺","returning to the chapter map shows the edited name");
            yield return Click("select-level-c1_01");
            Need(app.Creator.Document.Data.title=="归棺","reopening the board retains its saved name");
            yield return Click("scene");yield return new WaitForSecondsRealtime(.25f);Capture("editor-inputs");yield return Click("scene");
            yield return Click("leave-editor");yield return Click("campaign-review");yield return Click("campaign-publish");
            Need(fixture.Level("c1_01").title=="归棺","publishing the renamed board updates the actual level asset");
            app.ShowLevels("chapter_1");yield return Ready();
            Need(app.assets.Level("c1_01").title=="归棺","ordinary gameplay reads the published name");
            app.OpenCampaign();yield return Ready();
            yield return Click("campaign-new-chapter");string extra=Draft().chapter;
            yield return Click("chapter-apply");
            Need(app.UI.Root.Find("Chapter page").GetComponent<TMP_Text>().text=="2 / 2","returning from the fifth chapter explicitly shows the second page");
            Capture("chapter-page-last");
            yield return Click("campaign-back");yield return Click("campaign");
            Need(app.UI.Root.Find("Chapter page").GetComponent<TMP_Text>().text=="1 / 2"&&
                fixture.Catalog.chapters.All(c=>app.UI.FindButton("select-chapter-"+c.id)!=null),"the main-menu entry reopens all four existing chapters on the first page");
            Need(Draft().working.catalog.chapters.Length==5&&Draft().chapter==extra,"opening the first page preserves the fifth chapter and its saved draft selection");
            Capture("chapter-page-first");
            yield return Click("chapters-next");
            Need(app.UI.FindButton("select-chapter-"+extra)!=null&&!app.UI.FindButton("chapters-next").interactable,"next page shows the draft chapter and disables forward navigation at the end");
            yield return Click("chapters-prev");
            Need(app.UI.FindButton("select-chapter-chapter_1")!=null&&!app.UI.FindButton("chapters-prev").interactable,"previous page restores the original chapters without changing campaign content");
        }
        private static IEnumerator Prepare()
        {
            CreateFixture();Bind(fixture);
            var custom=LevelData.Blank();app.Library.Recover(custom);
            var entry=(RectTransform)app.UI.FindButton("campaign").transform;
            Need(entry.anchorMin==Vector2.right&&entry.rect.width>=200&&entry.rect.height>=48,"authoring has its own bottom-right title action");
            Capture("title");yield return Click("campaign");
            Need(app.Page==GamePage.Campaign,"the title opens the editable chapter gallery");Capture("chapters");
            yield return Chapter("chapter_2");yield return Click("select-level-c2_04");yield return Page(GamePage.Editing);
            Need(app.Creator.Document.Data.id=="c2_04","a single click opens the chosen board");yield return Click("leave-editor");
            Need(app.UI.SelectedId=="select-level-c2_04","board returns to the same chapter and node");
            Vector2 upper=new(0,158),lower=new(0,-158),right=new(80,0);
            yield return Drag("c2_b01","c2_02",upper);
            yield return Drag("c2_09","c2_02",lower,preview:"drag-branch");
            var siblings=CampaignDocument.Branches(Draft().working.Chapter("chapter_2"),"c2_02");
            Need(siblings.Select(n=>n.id).SequenceEqual(new[]{"c2_b01","c2_09"})&&siblings[0].branchAbove&&!siblings[1].branchAbove,"upper 4A and lower 4B have one unlock and distinct sides");
            Capture("workspace");
            yield return Drag("c2_02","c2_01",upper);
            Need(CampaignDocument.Branches(Draft().working.Chapter("chapter_2"),"c2_02").Count==2,"demotion of a main with attached branches preserves all unlocks");
            yield return Drag("c2_09","c2_02",upper+Vector2.right*60);
            Need(app.UI.FindButton("select-level-c2_b01").transform.position.x<app.UI.FindButton("select-level-c2_09").transform.position.x,"two siblings on the upper side occupy distinct columns");
            yield return Click("campaign-undo");yield return Drag("c2_02","c2_01",right);
            Need(app.UI.FindButton("select-level-c2_09").transform.Find("Number").GetComponent<TMP_Text>().text=="2B","moving a main carries both side lanes and renumbers their prefix");
            yield return Click("campaign-undo");yield return Drag("c2_09","c2_01",upper,true);
            Need(CampaignDocument.Branches(Draft().working.Chapter("chapter_2"),"c2_02").Count==2,"cancelling a drag restores the unmodified graph");
            yield return Click("campaign-undo");yield return Click("campaign-undo");
            yield return Chapter("chapter_1");yield return Options("c1_01");
            yield return Click("campaign-copy");
            string copy=Draft().selection;Set("copy",copy);
            yield return EdgeScroll(copy);
            yield return Drag(copy,"c1_02",right,preview:"drag-order");
            var path=CampaignDocument.Main(Draft().working.Chapter("chapter_1"));
            Need(path[2].id==copy&&path[3].requires.Single()==copy,"moving a main node rewires both neighboring unlocks");
            yield return Press(KeyCode.LeftControl,KeyCode.Z);
            Need(CampaignDocument.Main(Draft().working.Chapter("chapter_1"))[1].id==copy,"undo restores order and stable IDs");
            yield return Press(KeyCode.LeftControl,KeyCode.LeftShift,KeyCode.Z);
            Need(CampaignDocument.Main(Draft().working.Chapter("chapter_1"))[2].id==copy,"keyboard redo restores the same committed drag");
            yield return Drag(copy,"c1_01",upper);
            Need(Draft().working.Chapter("chapter_1").levels.Single(l=>l.id==copy).requires.Single()=="c1_01","branch anchor is stored by stable ID");
            yield return Drag(copy,"c1_02",upper);
            Need(Draft().working.Chapter("chapter_1").levels.Single(l=>l.id==copy).requires.Single()=="c1_02","dropping a branch on a main node changes its unlock");
            yield return Drag(copy,"c1_02",right);
            Need(Draft().working.Chapter("chapter_1").levels.Single(l=>l.id==copy).main,"promoting a branch inserts it immediately after its old anchor");
            yield return Click("campaign-undo");yield return Click("campaign-undo");
            yield return Click("campaign-new");string fresh=Draft().selection;Set("fresh",fresh);
            yield return Page(GamePage.Editing);
            var title=app.UI.Root.Find("Level title").GetComponent<TMP_InputField>();title.text="入棺测试";title.onEndEdit.Invoke(title.text);
            yield return Click("brush-4");var cell=app.Board.Screen(new Cell(3,3));
            app.Creator.PointerSample(cell,true,true,false);app.Creator.PointerSample(cell,false,false,true);yield return null;
            Need(app.Creator.Document.Data.coffin==new Cell(3,3),"existing board brush edits the new official level");
            yield return Click("save");yield return Click("leave-editor");yield return Page(GamePage.Campaign);
            yield return Click("campaign-review");
            Need(!app.UI.FindButton("campaign-publish").interactable,"missing winning route leaves a new board editable and unpublished");
            yield return Click("campaign-fix-"+fresh);yield return Page(GamePage.Playing);
            app.Act(Command.Left);app.Act(Command.Reset);app.Act(Command.Undo);app.Act(Command.Undo);app.Act(Command.Right);
            yield return Page(GamePage.Editing);
            Need(Draft().working.Plan(fresh).route=="R"&&!app.Progress.Done(fresh),"F5 records the winning route across reset and undo without awarding progress");
            yield return Click("leave-editor");yield return Click("campaign-popup-close");yield return Drag(fresh,"c1_01",lower);
            yield return Click("campaign-back");
            yield return Click("campaign-new-chapter");string newChapter=Draft().chapter,seed=Draft().selection;
            Set("chapter",newChapter);Set("seed",seed);
            var chapterTitle=app.UI.Root.GetComponentInChildren<TMP_InputField>();
            chapterTitle.text="第五章 · 夜访";yield return Click("chapter-earlier");
            var changed=Draft().working.catalog.chapters;
            Need(changed[3].id==newChapter&&changed[4].requiresChapter==newChapter,"chapter rename and reorder update the actual chapter prerequisites");
            yield return Click("chapter-later");yield return Click("chapter-apply");
            yield return Chapter("chapter_1");yield return Options("c1_02");yield return Click("campaign-copy");
            string moved=Draft().selection;Set("moved",moved);
            yield return Options(moved);yield return Click("campaign-transfer");yield return Click("target-"+newChapter);
            Need(Draft().working.Chapter(newChapter).levels.Any(l=>l.id==moved),"transfer moves the same level ID into the selected chapter");
            yield return Options(seed);yield return Click("campaign-remove");yield return Click("campaign-confirm");
            Need(Draft().working.Chapter(newChapter).levels.Single().id==moved,"removing a draft node reconnects the chapter");
            yield return Options(moved);yield return Click("campaign-remove");yield return Click("campaign-confirm");
            Need(Draft().working.Chapter(newChapter).levels.Length==1,"last main in a chapter cannot be removed");
            if(app.UI.FindButton("campaign-cancel")!=null)yield return Click("campaign-cancel");
            yield return Chapter("chapter_1");app.UI.FocusButton("select-level-"+fresh);
            yield return Click("campaign-review");Capture("review");
            Need(app.UI.FindButton("campaign-publish").interactable,"review lists publishable graph, boards and verified routes");
            yield return Click("campaign-popup-close");
            Need(new CampaignProject(fixture).Revision==Get("start"),"all pre-publication edits left the original project content intact");
            Need(app.Library.Recovery().id==custom.id&&app.Library.Read(out _).Count==0,"campaign boards and recovery stay out of the personal library");
            yield return Click("select-level-"+fresh);
            var unsaved=app.UI.Root.Find("Level title").GetComponent<TMP_InputField>();
            unsaved.text="夜访校验";unsaved.onEndEdit.Invoke(unsaved.text);
            Need(app.Creator.Document.Dirty,"leave Play with an uncommitted official board edit");
        }
        private static IEnumerator Finish()
        {
            fixture=AssetDatabase.LoadAssetAtPath<GameAssets>(Folder+"/GameAssets.asset");
            if(fixture==null)throw new InvalidOperationException("Run the preparation session first.");
            Bind(fixture);
            yield return Ready();yield return Press(KeyCode.DownArrow);
            Need(app.UI.SelectedId=="creator","the corner action preserves navigation among the four primary title actions");
            app.UI.FocusButton("exit");yield return Press(KeyCode.DownArrow);
            Need(app.UI.SelectedId=="campaign","keyboard navigation reaches the editor action after the four main actions");
            yield return Press(KeyCode.Return);yield return Page(GamePage.Campaign);yield return Chapter("chapter_1");
            string fresh=Get("fresh"),copy=Get("copy"),chapter=Get("chapter"),moved=Get("moved");
            Need(Draft().working.Board(fresh).title=="夜访校验","project draft and uncommitted brush document survive leaving and re-entering Play");
            yield return Click("campaign-review");yield return Click("campaign-publish");
            Need(!new CampaignProject(fixture).HasDraft,"successful publication clears its working draft");
            var loaded=new CampaignProject(fixture);
            var first=loaded.Source.Chapter("chapter_1");
            Need(CampaignDocument.Branches(first,"c1_01").Count==2,"two independent optional nodes retain the same main anchor on disk");
            Need(first.levels.Single(l=>l.id==copy).branchAbove&&!first.levels.Single(l=>l.id==fresh).branchAbove,"upper and lower placement survives Play restart and asset publication");
            Need(loaded.Source.catalog.chapters.Last().id==chapter&&loaded.Source.Chapter(chapter).levels.Single().id==moved,"published chapter order and transfer round-trip");
            Need(fixture.Level(fresh).coffin==new Cell(3,3)&&fixture.Level(fresh).title=="夜访校验","normal GameAssets.Level reads the edited official board in the same Play session");
            string guid=AssetDatabase.AssetPathToGUID(Folder+"/Levels/"+fresh+".json");
            Need(guid.Length==32&&File.ReadAllText(Folder+"/GameAssets.asset").Contains(guid),"new TextAsset reference is serialized for subsequent builds");
            Need(CampaignDocument.Proves(fixture.Level(fresh),loaded.Source.Plan(fresh).route,out _),"reimported demonstration solves the reloaded level");
            var baseline=loaded.Revision;
            var staged=JsonUtility.FromJson<CampaignContent>(JsonUtility.ToJson(loaded.Source));
            staged.Board(fresh).title+="!";
            string external=Folder+"/Levels/"+fresh+".json";File.AppendAllText(external,"\n");var bytes=File.ReadAllBytes(external);
            bool refused=false;try{loaded.Publish(staged,baseline);}catch(IOException){refused=true;}
            Need(refused&&bytes.SequenceEqual(File.ReadAllBytes(external)),"an external edit is preserved when a stale publication is rejected");
            loaded.Reload();
            var model=new CampaignDocument(JsonUtility.FromJson<CampaignContent>(JsonUtility.ToJson(loaded.Source)));
            var ch=model.Data.Chapter(chapter);string extra=model.AddLevel(ch,ch.levels[0],true);
            string before=loaded.Revision;int refs=fixture.levels.Length;bool interrupted=false;
            try{loaded.Publish(model.Data,before,()=>throw new IOException("injected after asset binding"));}
            catch(IOException e){interrupted=e.Message.Contains("injected");}
            Need(interrupted&&new CampaignProject(fixture).Revision==before,"interrupted multi-file publication restores exact original JSON bytes");
            Need(fixture.levels.Length==refs&&!File.Exists(Folder+"/Levels/"+extra+".json.meta")&&!File.Exists(Folder+"/Levels/"+extra+".json"),"rollback removes new asset, meta and build reference");
            yield return Click("campaign-back");yield return Click("campaign-back");yield return Click("play");
            yield return Click("chapter-chapter_1");yield return Click("level-c1_01");yield return Page(GamePage.Playing);
            foreach(char k in loaded.Source.Plan("c1_01").route)app.Act((Command)"URDL".IndexOf(k));
            yield return Page(GamePage.Playing);app.Pause();yield return Click("levels");yield return Page(GamePage.Levels);
            Need(app.UI.FindButton("level-"+fresh).interactable&&app.UI.FindButton("level-"+copy).interactable,"normal campaign progression unlocks both authored branches");
            Need(app.UI.FindButton("level-"+fresh).transform.position!=app.UI.FindButton("level-"+copy).transform.position,"two branches at one anchor have separate selection targets");
            var anchor=app.UI.FindButton("level-c1_01").transform;
            Need(app.UI.FindButton("level-"+fresh).transform.position.y<anchor.position.y&&anchor.position.y<app.UI.FindButton("level-"+copy).transform.position.y,"the player selection map respects the published sides");
            Capture("selection");yield return Click("level-"+fresh);yield return Page(GamePage.Playing);
            app.Act(Command.Right);yield return Page(GamePage.Levels);
            Need(app.Progress.Done(fresh)&&!app.Progress.ChapterDone(first),"authored optional win returns to its chapter without completing the main line");
            yield return Click("back");yield return Page(GamePage.Chapters);yield return Click("map-next");
            Need(app.UI.FindButton("chapter-"+chapter)!=null,"new fifth chapter is reachable on the next chapter selection page");
            var retire=new CampaignDocument(JsonUtility.FromJson<CampaignContent>(JsonUtility.ToJson(loaded.Source)));
            var owner=retire.Data.Chapter("chapter_1");
            retire.Remove(owner,owner.levels.Single(l=>l.id==copy));
            loaded.Publish(retire.Data,loaded.Revision);
            Need(File.Exists(Folder+"/Levels/"+copy+".json")&&fixture.levels.All(a=>a.name!=copy)&&!fixture.Catalog.chapters.SelectMany(c=>c.levels).Any(l=>l.id==copy),"retiring a published board preserves its source and removes its player build reference");
        }
    }
}
