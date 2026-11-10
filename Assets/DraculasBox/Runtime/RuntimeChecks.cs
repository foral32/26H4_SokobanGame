using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DraculasBox.Core;
using UnityEngine;

namespace DraculasBox.Runtime
{
    /// <summary>Isolated key and UI-event replay. Never sends OS input or writes real player progress.</summary>
    public sealed partial class RuntimeChecks:MonoBehaviour,IStudyKeyboard
    {
        [Serializable] private sealed class CheckReport
        {public string status="running",error,runtime;public int checks,width,height,exploredBranches,deathsRecovered,editorStressActions;public float longestTransition,maxEditorPreviewMs,deathPeakZoom=1,deathPeakPulse,maxDeathSampleGap;public List<float> completionTimes=new List<float>();public List<string> levels=new List<string>(),chapters=new List<string>(),screenshots=new List<string>(),errors=new List<string>();public bool editorRoundtrip,progressIsolated,chapterMainComplete;}
        private DraculaApp app;private CheckReport report=new CheckReport();private string folder;
        private KeyCode key;private int pressFrame,releaseFrame;
        public bool Held(KeyCode k)=>key==k&&Time.frameCount>=pressFrame&&Time.frameCount<releaseFrame;
        public bool Pressed(KeyCode k)=>key==k&&Time.frameCount==pressFrame;
        public void Begin(DraculaApp host)
        {
            app=host;app.SetTestKeyboard(this);report.runtime=Application.isEditor?"Unity Editor":"Windows Player";report.width=Screen.width;report.height=Screen.height;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--dracula-validate");
            folder=i>=0&&i+1<args.Length&&!args[i+1].StartsWith("-")?args[i+1]:Path.GetFullPath("DraculasBox/Evidence/Editor");
            Directory.CreateDirectory(folder);Application.logMessageReceived+=Log;StartCoroutine(Guard(Run()));
        }
        private void Require(bool value,string label){report.checks++;if(!value)throw new InvalidOperationException(label);}
        private void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)report.errors.Add(text);}
        private IEnumerator Guard(IEnumerator run)
        {
            var stack=new Stack<IEnumerator>();stack.Push(run);
            while(stack.Count>0)
            {
                object current=null;bool next=false;
                try{next=stack.Peek().MoveNext();if(next)current=stack.Peek().Current;}
                catch(Exception e){report.status="failed";report.error=e.ToString();Finish();yield break;}
                if(!next){stack.Pop();continue;}if(current is IEnumerator nested){stack.Push(nested);continue;}yield return current;
            }
            report.status=report.errors.Count==0?"passed":"failed";Finish();
        }
        private void Finish()
        {
            Application.logMessageReceived-=Log;File.WriteAllText(Path.Combine(folder,"report.json"),JsonUtility.ToJson(report,true));
            #if UNITY_EDITOR
            UnityEditor.SessionState.SetBool("DraculasBox.Validate",false);UnityEditor.EditorApplication.isPlaying=false;
            #else
            Application.Quit(report.status=="passed"?0:1);
            #endif
        }
        private void Press(KeyCode k){key=k;pressFrame=Time.frameCount+1;releaseFrame=pressFrame+1;}
        private IEnumerator Ready(){float start=Time.realtimeSinceStartup;while(app.Transitioning||app.Board!=null&&app.Board.IsAnimating){if(Time.realtimeSinceStartup-start>12)throw new TimeoutException("transition");yield return null;}report.longestTransition=Mathf.Max(report.longestTransition,Time.realtimeSinceStartup-start);yield return null;}
        private IEnumerator Tap(KeyCode k)
        {yield return Ready();Press(k);yield return null;yield return null;key=KeyCode.None;yield return new WaitForSecondsRealtime(.16f);yield return Ready();}
        private IEnumerator Click(string id)
        {
            yield return Ready();bool scene=id=="sun"||id.StartsWith("sun-dir-")||id.StartsWith("width-")||id.StartsWith("height-");
            if(app.Page==GamePage.Editing&&scene&&app.UI.FindButton(id)==null){app.UI.ClickForCheck("scene");yield return null;}
            if(app.Page==GamePage.Editing&&(id=="save-copy"||id=="export-editor"||id=="editor-help")&&app.UI.FindButton(id)==null){app.UI.ClickForCheck("editor-files");yield return null;}
            if(app.Page==GamePage.Library&&(id=="duplicate-custom"||id=="export-custom"||id=="remove-custom")&&app.UI.FindButton(id)==null){app.UI.ClickForCheck("more-custom");yield return null;}
            if(app.Page==GamePage.Editing&&id.StartsWith("brush-")&&int.TryParse(id.Substring(6),out int brush)&&brush!=7){app.Creator.RevealBrush((Brush)brush);Canvas.ForceUpdateCanvases();yield return null;}
            app.UI.ClickForCheck(id);yield return new WaitForSecondsRealtime(.08f);yield return Ready();
            if(scene&&app.Page==GamePage.Editing&&app.Creator.SceneOpen){app.UI.ClickForCheck("scene");yield return null;}
        }
        private static KeyCode Key(char c)=>c=='U'?KeyCode.UpArrow:c=='R'?KeyCode.RightArrow:c=='D'?KeyCode.DownArrow:KeyCode.LeftArrow;
        private void Capture(string name,int w=0,int h=0)
        {
            if(w<=0){w=Application.isEditor?1280:Screen.width;h=Application.isEditor?720:Screen.height;}
            var camera=app.viewCamera;var prior=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;
            var rt=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32);var image=new Texture2D(w,h,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=rt;camera.aspect=(float)w/h;app.Board.FitForCapture();Canvas.ForceUpdateCanvases();
                camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,w,h),0,0);image.Apply();
                if(app.Page==GamePage.Editing)
                {
                    var hero=app.Board.GetComponentsInChildren<SpriteRenderer>().FirstOrDefault(r=>r.name=="Vampire"&&r.enabled);
                    if(hero!=null)
                    {
                        var sample=camera.WorldToViewportPoint(hero.transform.TransformPoint(new Vector3(.17f,.23f,0)));
                        var point=(Vector2)camera.ViewportToScreenPoint(sample);
                        // A panned character can meet the toolbar or canvas clipping edge.
                        // Sample its artwork only when that pixel has a clear two-pixel margin.
                        bool clear=app.Board.InViewport(point-Vector2.one*2)&&app.Board.InViewport(point+Vector2.one*2);
                        if(clear&&sample.x>0&&sample.x<1&&sample.y>0&&sample.y<1)
                        {
                            var pixel=image.GetPixel((int)(sample.x*w),(int)(sample.y*h));
                            Require(pixel.r>.65f&&pixel.g>.65f,"editor renders hero artwork rather than a stale procedural texture");
                        }
                    }
                }
                File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());report.screenshots.Add(name+".png");
            }
            finally{camera.targetTexture=prior;camera.aspect=aspect;RenderTexture.active=active;app.Board.FitForCapture();rt.Release();Destroy(rt);Destroy(image);}
        }
        private IEnumerator Page(GamePage page)
        {float start=Time.realtimeSinceStartup;while(app.Page!=page||app.Transitioning){if(Time.realtimeSinceStartup-start>20)throw new TimeoutException("page "+page+" current "+app.Page);yield return null;}yield return null;}
        private IEnumerator Level(string id)
        {float start=Time.realtimeSinceStartup;while(app.Page!=GamePage.Playing||app.Transitioning||app.Session==null||app.Session.State.Level.Export().id!=id){if(Time.realtimeSinceStartup-start>20)throw new TimeoutException("level "+id+" current "+app.Page);yield return null;}yield return null;}
        private IEnumerator Undo(int count){for(int i=0;i<count;i++)yield return Tap(KeyCode.Z);}
        private IEnumerator Route(string route){foreach(char c in route)yield return Tap(Key(c));}
        private IEnumerator Run()
        {
            yield return Ready();Capture("01_title");Require(app.Page==GamePage.Title,"title page");Require(app.UI.SortingOrder>app.Board.HighestSortingOrder,"UI above board");
#if !UNITY_EDITOR
            Require(app.UI.FindButton("campaign")==null&&new[]{"play","creator","settings","exit"}.All(id=>app.UI.FindButton(id)!=null),"standalone title has its four player actions and no campaign editor entry");
#endif
            Require(app.assets.Clip("burn")!=null&&app.assets.Clip("burn").length>.3f,"burn audio is included");
            yield return LightingRegression();
            var artwork=app.UI.GetComponentsInChildren<UnityEngine.UI.Graphic>().Where(g=>g is MenuTileGraphic||g is GameIconGraphic||g is MenuAtmosphere||g is TitleArtworkGraphic).ToArray();
            Require(artwork.Length>=6&&artwork.All(g=>g.GetComponent<CanvasRenderer>()!=null),"menu graphics have canvas renderers");
            yield return Tap(KeyCode.DownArrow);Require(app.UI.SelectedId=="creator","vertical menu navigation");yield return Tap(KeyCode.UpArrow);Require(app.UI.SelectedId=="play","return through vertical menu");
            yield return Click("play");Require(app.Page==GamePage.Chapters,"chapters page");Capture("02_chapters");
            yield return Click("chapter-chapter_2");Require(app.Page==GamePage.Chapters,"locked chapter cannot open");app.LoadLevel("c2_01");app.ShowLevels("chapter_2");Require(!app.Transitioning&&app.Page==GamePage.Chapters,"direct entry respects chapter lock");
            yield return Click("chapter-chapter_1");yield return Click("level-c1_02");Require(app.Page==GamePage.Levels,"locked node cannot open");Capture("03_mainline_fresh");
            app.UI.HoverForCheck("level-c1_03");yield return Tap(KeyCode.LeftArrow);yield return Tap(KeyCode.LeftArrow);Require(app.UI.SelectedId=="level-c1_01","pointer and keyboard share focus");yield return Tap(KeyCode.Return);yield return Level("c1_01");
            string initial=app.Session.State.Key();yield return Tap(KeyCode.UpArrow);Require(app.Session.State.Key()==initial&&app.Session.UndoCount==0,"wall remains solid");
            Press(KeyCode.RightArrow);yield return null;yield return null;Press(KeyCode.Z);yield return null;yield return null;key=KeyCode.None;yield return new WaitForSecondsRealtime(.4f);yield return Ready();Require(app.Session.State.Key()==initial,"mid-move undo");
            Press(KeyCode.RightArrow);yield return null;yield return null;key=KeyCode.None;yield return null;Press(KeyCode.RightArrow);yield return null;yield return null;key=KeyCode.None;yield return new WaitForSecondsRealtime(.4f);
            Require(app.Session.State.Player==new Cell(3,3)&&app.Session.Moves==2,"two short taps register once each");yield return Undo(2);
            Press(KeyCode.RightArrow);yield return null;yield return null;Press(KeyCode.RightArrow);yield return null;yield return null;key=KeyCode.None;app.UI.ClickForCheck("undo");yield return new WaitForSecondsRealtime(.45f);Require(app.Session.State.Key()==initial,"UI undo clears pending direction");
            var catalog=app.assets.Catalog;int total=catalog.chapters.Sum(c=>c.levels.Length);Require(total>0&&catalog.chapters.SelectMany(c=>c.levels).All(link=>app.assets.levels.Any(a=>a!=null&&a.name==link.id)),"main and optional levels are all imported");
            string oldRoot=Path.Combine(Application.temporaryCachePath,"DraculaCompatibility-"+Guid.NewGuid().ToString("N"));var old=new ProgressStore(oldRoot);for(int i=1;i<=4;i++)old.Complete("c1_"+i.ToString("00"));var four=new ProgressStore(oldRoot);
            Require(!four.ChapterDone(catalog.chapters[0])&&four.Unlocked(catalog.chapters[0].levels[4])&&!four.ChapterUnlocked(catalog.chapters[1],catalog),"four-level save continues at fifth");for(int i=5;i<=7;i++)four.Complete("c1_"+i.ToString("00"));var seven=new ProgressStore(oldRoot);Require(seven.ChapterUnlocked(catalog.chapters[1],catalog)&&!seven.ChapterDone(catalog.chapters[1]),"seven-level save unlocks second chapter");
            var plans=JsonUtility.FromJson<ReplayCatalog>(app.assets.replays.text).levels.ToDictionary(p=>p.id);
            for(int chapter=0;chapter<catalog.chapters.Length;chapter++)
            {
                var mainNodes=catalog.chapters[chapter].levels.Where(l=>l.main).ToArray();
                for(int node=0;node<mainNodes.Length;node++)
                {
                    string id=mainNodes[node].id;yield return Level(id);
                    Capture(id+"_initial");string spawn=app.Session.State.Key();bool solar=app.assets.Level(id).sunEnabled;
                    Require(app.Session.State.SunEnabled==solar,"sun configuration "+id);Require(app.Board.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.name=="Incoming sunlight")==solar,"visible solar source "+id);
                    int lamps=app.Session.State.Level.Export().pieces.Count(p=>p.kind==PieceKind.Lamp);
                    Require(LightingMatches(),"floor illumination agrees with all light sources and shadow cells "+id);
                    Require(app.Board.GetComponentsInChildren<SpriteRenderer>().Count(r=>r.name.StartsWith("Lamp glow "))==lamps,"every lamp has its own glow "+id);
                    foreach(char direction in "URDL")
                    {
                        yield return Tap(Key(direction));report.exploredBranches++;
                        if(app.Session.State.Dead)
                        {
                            report.deathsRecovered++;Require(app.Page==GamePage.Dead&&app.Session.UndoCount==1,"exposure enters lethal tile and records history "+id);
                            Require(app.UI.FindButton("recover")!=null,"death shows undo prompt");Require(!app.Board.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Vampire").enabled,"burn hides vampire after animation");
                            string dead=app.Session.State.Key();yield return Tap(KeyCode.RightArrow);Require(app.Session.State.Key()==dead,"dead vampire cannot continue moving");
                        }
                        if(app.Session.UndoCount>0)yield return Tap(KeyCode.Z);
                        Require(app.Session.State.Key()==spawn&&!app.Session.State.Dead&&app.Session.UndoCount==0,"branch restores complete state "+id+" "+direction);
                    }
                    if(id=="c1_02")
                    {
                        yield return Tap(KeyCode.UpArrow);Require(app.Session.State.Dead&&app.Session.State.Player==new Cell(5,1),"sun is lethal, not solid");Capture("sun_death_prompt");
                        yield return Tap(KeyCode.R);Require(app.Session.State.Key()==spawn,"reset recovers death");yield return Tap(KeyCode.Z);Require(app.Session.State.Dead,"undo reset restores death");yield return Tap(KeyCode.Z);Require(app.Session.State.Key()==spawn,"undo death restores alive state");
                        var floor=app.Board.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Floor 6,2");Color atCommit=Color.clear;Action<ActionResult> sample=a=>atCommit=floor.color;app.ActionApplied+=sample;yield return Tap(KeyCode.LeftArrow);app.ActionApplied-=sample;
                        Require(Vector4.Distance(atCommit,BoardView.SafeColor)<.001f&&Vector4.Distance(floor.color,BoardView.LitColor)<.001f,"lighting moves with box");yield return Tap(KeyCode.R);yield return Tap(KeyCode.Z);yield return Tap(KeyCode.Z);Require(app.Session.State.Key()==spawn,"reset is undoable");
                    }
                    if(id=="c1_06")
                    {
                        var backdrop=app.viewCamera.backgroundColor;var boardTransform=app.Board.transform.Find("Board");var hud=(RectTransform)app.UI.FindButton("undo").transform;var hudPosition=hud.anchoredPosition;float peakZoom=1,peakPulse=0;
                        Press(KeyCode.RightArrow);yield return null;yield return null;key=KeyCode.None;
                        // Rendering and writing PNGs here can skip the short pulse. Observe ordinary frames before capturing.
                        bool neutral=true,fixedHud=true;float priorSample=Time.unscaledTime;
                        for(int frame=0;frame<42;frame++){report.maxDeathSampleGap=Mathf.Max(report.maxDeathSampleGap,Time.unscaledTime-priorSample);priorSample=Time.unscaledTime;peakZoom=Mathf.Max(peakZoom,boardTransform.localScale.x);var delta=app.viewCamera.backgroundColor-backdrop;peakPulse=Mathf.Max(peakPulse,delta.r);neutral&=Mathf.Abs(delta.r-delta.g)<.002f&&Mathf.Abs(delta.g-delta.b)<.002f;fixedHud&=hud.anchoredPosition==hudPosition&&app.UI.Root.Find("Burn vignette")==null;yield return null;}
                        report.deathPeakZoom=peakZoom;report.deathPeakPulse=peakPulse;yield return Ready();Require(neutral,"death backdrop stays neutral");Require(fixedHud,"death keeps HUD fixed without a damage overlay");Require(peakZoom>1.006f&&peakZoom<=1.019f&&peakPulse>0&&peakPulse<.029f,"restrained zoom and background pulse are visible: "+peakZoom+", "+peakPulse);Require(app.Page==GamePage.Dead&&app.Session.State.Player==new Cell(3,5),"walk into point light burns");Capture("lamp_death_prompt");yield return Tap(KeyCode.Z);Require(Vector4.Distance(app.viewCamera.backgroundColor,backdrop)<.002f&&Mathf.Abs(boardTransform.localScale.x-1)<.001f,"undo restores death framing and backdrop");
                        yield return Route("LURDRU");Require(app.Session.State.Position(1)==new Cell(3,2),"wrong lamp approach enters dead-end neck");yield return Undo(6);Require(app.Session.State.Key()==spawn,"undo recovers lamp trap");
                    }
                    if(id=="c2_07")
                    {
                        yield return Route("RDD");Require(app.Session.State.Dead&&app.Session.Moves==3,"unshielding a lamp commits push and kills");yield return Undo(3);Require(app.Session.State.Key()==spawn,"lethal push undo restores box and player");
                        yield return Route(plans[id].route.Substring(0,18));string turn=app.Session.State.Key();yield return Tap(KeyCode.LeftArrow);Require(app.Session.State.Key()==turn,"lamp still cannot enter coffin");Capture("c2_07_turn_probe");yield return Undo(18);Require(app.Session.State.Key()==spawn,"deep branch restores every piece");
                    }
                    yield return CircuitChecks(id,spawn);
                    yield return MirrorLevelChecks(id,plans[id]);
                    bool completed=false;Action<ActionResult> completion=a=>{if(a.After.Won)completed=true;};app.ActionApplied+=completion;
                    string route=plans[id].route;for(int step=0;step<route.Length;step++)
                    {
                        if(id=="c1_01"&&step==route.Length-1&&Screen.width==1280)
                        {
                            Press(Key(route[step]));yield return null;yield return null;key=KeyCode.None;float began=Time.unscaledTime;
                            for(int frame=0;frame<32;frame++){report.completionTimes.Add(Time.unscaledTime-began);Capture("completion_"+frame.ToString("00"));yield return new WaitForSecondsRealtime(.04f);}
                        }
                        else yield return Tap(Key(route[step]));
                    }
                    app.ActionApplied-=completion;Require(completed&&app.Progress.Done(id),"coffin completion "+id);Require(app.UI.FindButton("next")==null&&app.UI.FindButton("replay")==null,"no next-level result panel");report.levels.Add(id);
                    if(app.Session!=null&&app.Session.State.Level.Export().id==id){yield return new WaitForSecondsRealtime(.28f);if(app.Session!=null&&app.Session.State.Level.Export().id==id)Capture(id+"_coffin");}
                }
                Require(app.Progress.ChapterDone(catalog.chapters[chapter]),"chapter complete");report.chapters.Add(catalog.chapters[chapter].id);
                if(chapter+1<catalog.chapters.Length){var next=catalog.chapters[chapter+1];yield return Level(next.levels[0].id);Require(app.Progress.ChapterUnlocked(next,catalog),"iris advances directly into next chapter");yield return Tap(KeyCode.Escape);yield return Click("levels");Capture(next.id+"_selection");yield return Click("level-"+next.levels[0].id);}
            }
            yield return Page(GamePage.Chapters);Require(app.UI.SelectedId=="chapter-"+catalog.chapters.Last().id,"final iris returns to current chapter");report.chapterMainComplete=true;Capture("chapters_complete");
            yield return OptionalChecks(plans);yield return Page(GamePage.Chapters);
            yield return Click("chapter-chapter_1");yield return Click("level-c1_01");yield return Tap(KeyCode.Escape);Capture("pause");yield return Click("settings");var music=app.UI.FindButton("music");yield return Tap(KeyCode.RightArrow);Require(music==app.UI.FindButton("music")&&app.Progress.Data.music>.25f,"settings update in place");yield return Tap(KeyCode.DownArrow);yield return Tap(KeyCode.RightArrow);Require(app.UI.SelectedId=="effects","settings retain focus");yield return Tap(KeyCode.Escape);yield return Click("home");
            yield return Click("creator");Capture("library_empty");yield return Click("template");yield return Page(GamePage.Editing);
            var doc=app.Creator.Document;Require(Guid.TryParseExact(doc.Data.id,"N",out _)&&doc.Dirty,"new copy is independent and unsaved");int count=doc.Data.pieces.Length;
            yield return Click("brush-2");var a=app.Board.Screen(new Cell(6,3));var b=app.Board.Screen(new Cell(7,3));Require(app.Creator.PointerSample(a,true,true,false),"start brush");Require(app.Creator.PointerSample(b,false,true,false),"continue drag");app.Creator.PointerSample(b,false,false,true);yield return null;
            Require(doc.Data.pieces.Length==count+2&&doc.UndoCount==1,"drag is one history item");yield return Click("edit-undo");Require(doc.Data.pieces.Length==count,"undo drag");yield return Click("edit-redo");Require(doc.Data.pieces.Length==count+2,"redo drag");yield return Click("save");Require(!doc.Dirty,"save establishes baseline");
            var title=app.UI.Root.Find("Level title").GetComponent<TMPro.TMP_InputField>();title.text="编辑测试";title.onEndEdit.Invoke(title.text);Require(doc.Dirty,"rename marks draft dirty");yield return Click("edit-undo");Require(!doc.Dirty&&title==app.UI.Root.Find("Level title").GetComponent<TMPro.TMP_InputField>(),"undo to saved does not rebuild input UI");
            Require(app.Creator.ResizeTo(32,32),"full-size editor");Require(app.UI.FindButton("save").GetComponentInChildren<TMPro.TMP_Text>().text=="保存","editing invalidates saved confirmation");yield return null;for(int i=0;i<3;i++)yield return Click("zoom-in");app.Board.Pan(new Vector2(70,45));Require(app.Board.EditorZoom>1.9f&&app.Board.CellAt(app.Board.Screen(new Cell(16,16)),out var picked)&&picked==new Cell(16,16),"zoom and pan retain accurate picking");Capture("editor_large_zoom");yield return Click("edit-undo");yield return Click("zoom-fit");Require(doc.Data.rows[0].Length==9&&!doc.Dirty,"resize undo restores saved layout");
            yield return Click("brush-5");var lamp=app.Board.Screen(new Cell(4,2));Require(app.Creator.PointerSample(lamp,true,true,false),"place lamp");app.Creator.PointerSample(lamp,false,false,true);yield return null;Require(LightField.Compute(Definition.Preview(doc.Data).Initial()).Lamp.Count(v=>v)==3,"editor preview shares cardinal rays");Require(app.Creator.PickAt(lamp)==Brush.Lamp,"eyedropper selects actual object");Capture("editor_lamp_preview");yield return Click("edit-undo");
            yield return Click("brush-3");var exposed=app.Board.Screen(new Cell(4,3));Require(app.Creator.PointerSample(exposed,true,true,false),"move spawn");app.Creator.PointerSample(exposed,false,false,true);yield return null;yield return Click("sun");yield return Click("sun-dir-1");Require(doc.Validation()!=null&&!app.UI.FindButton("test").interactable,"unsafe spawn disables trial and explains issue");yield return UndoEditor(3);Require(!doc.Data.sunEnabled&&doc.Data.player==new Cell(1,3)&&!doc.Dirty,"editor undo restores configuration and saved baseline");
            yield return Click("save");int history=doc.UndoCount;string idSaved=doc.Data.id;app.Board.ZoomAt(app.Board.Screen(new Cell(4,2)),1.25f);float zoom=app.Board.EditorZoom;Capture("editor_saved");yield return Click("test");yield return Page(GamePage.Playing);yield return Route("RRRUU");yield return Page(GamePage.Editing);
            Require(app.Creator.Document==doc&&doc.UndoCount==history&&Mathf.Abs(app.Board.EditorZoom-zoom)<.001f&&app.Progress.Data.completed.Count==total,"trial auto-returns to original draft, history and viewport");Capture("editor_return");
            yield return Click("export-editor");string exported=app.UI.Root.Find("Export path").GetComponent<TMPro.TMP_InputField>().text;Require(File.Exists(exported)&&app.Library.PreviewImport(exported).pieces.Length==count+2,"export roundtrip");yield return Click("export-back");yield return Click("leave-editor");yield return Page(GamePage.Library);Capture("library_saved");
            Require(app.UI.GetComponentsInChildren<LevelThumbnail>().Any(t=>t.canvasRenderer.GetMesh()!=null&&t.canvasRenderer.GetMesh().vertexCount>0),"library displays real layout thumbnails");
            yield return Click("duplicate-custom");var list=app.Library.Read(out _);Require(list.Count==2&&list.Select(d=>d.id).Distinct().Count()==2,"duplicate creates new identity");yield return Click("remove-custom");yield return Click("cancel-remove");Require(app.Library.Read(out _).Count==2,"cancel removal preserves data");yield return Click("remove-custom");yield return Click("confirm-remove");Require(app.Library.Read(out _).Count==1&&app.CanRestoreArchive,"removal is recoverable");yield return Click("undo-remove");Require(app.Library.Read(out _).Count==2,"restore archived level");
            yield return Click("import");Require(app.Page==GamePage.Importing&&app.UI.Root.Find("Import format")!=null,"in-app format guide precedes any picker");var path=app.UI.Root.Find("Import path").GetComponent<TMPro.TMP_InputField>();path.text=exported;path.onEndEdit.Invoke(exported);yield return Click("preview-import");Require(app.UI.FindButton("confirm-import").interactable&&app.Library.Read(out _).Count==2,"preview writes no level");Capture("import_preview");yield return Click("cancel-import");Require(app.Library.Read(out _).Count==2,"cancel import preserves library");
            yield return Click("import");yield return Click("preview-import");yield return Click("confirm-import");yield return Page(GamePage.Editing);Require(app.Creator.Document.Data.id!=idSaved&&app.Library.Read(out _).Count==3,"confirmed import has independent identity");yield return Click("leave-editor");
            string bad=Path.Combine(app.Library.Folder,"broken-layout.json");File.WriteAllText(bad,"{\"schemaVersion\":1,\"rows\":[],\"pieces\":[]}");Require(app.Library.Read(out int invalid).Count==3&&invalid==1&&File.Exists(bad),"malformed file is preserved and skipped");
            yield return Click("import");path=app.UI.Root.Find("Import path").GetComponent<TMPro.TMP_InputField>();path.text=bad;path.onEndEdit.Invoke(bad);yield return Click("preview-import");Require(!app.UI.FindButton("confirm-import").interactable,"invalid import cannot confirm");yield return Click("cancel-import");
            yield return Click("custom-"+idSaved);Require(app.Page==GamePage.Library&&app.LibrarySelection==idSaved,"click selects without editing");yield return Click("edit-custom");yield return Click("sun");Require(app.Creator.Document.Dirty,"changed saved draft");app.Library.Recover(app.Creator.Document.Data);yield return Click("leave-editor");Require(app.Page==GamePage.Confirming,"unsaved changes prompt");yield return Click("cancel-leave");Require(app.Page==GamePage.Editing&&app.Creator.Document.Data.sunEnabled,"cancel leave preserves draft");yield return Click("leave-editor");yield return Click("discard-leave");Require(app.Library.Read(out _).First(d=>d.id==idSaved).sunEnabled==false&&app.Library.Recovery()==null&&app.UI.FindButton("recover")==null,"discard preserves disk version and removes rejected recovery");
            var recovery=app.Library.Read(out _).First(d=>d.id==idSaved);recovery.sunEnabled=true;app.Library.Recover(recovery);app.OpenLibrary();yield return Ready();yield return Click("recover");Require(app.Creator.Document.Dirty&&app.Creator.Document.Data.sunEnabled,"recovered unsaved data is still dirty");yield return Click("leave-editor");yield return Click("discard-leave");
            yield return CircuitEditorChecks();yield return MultiLampEditorChecks();yield return MirrorEditorChecks();
            yield return EditorRegression();
            yield return RectangleRegression();
            yield return WorkshopRegression();
            string root=Directory.GetParent(app.Library.Folder).FullName;Require(root.StartsWith(Path.GetFullPath(Application.temporaryCachePath),StringComparison.OrdinalIgnoreCase),"test progress is isolated");Require(new ProgressStore(root).Data.completed.Count==total,"all campaign progress persisted independently");report.editorRoundtrip=report.progressIsolated=true;Require(report.errors.Count==0,"no runtime errors");Capture("library_final");
        }
        private IEnumerator UndoEditor(int count){for(int i=0;i<count;i++)yield return Click("edit-undo");}
    }
}
