#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BoxOrVoid.Core;
using UnityEngine;

namespace BoxOrVoid.Study
{
    /// <summary>Explicit development-player mode; it never polls physical devices or touches real saves.</summary>
    public sealed class StudyPlayerCheck:MonoBehaviour,IStudyKeyboard
    {
        [Serializable] private sealed class LevelResult {public string id;public int commands;public bool solved,resetUndo,fullUndo;}
        [Serializable] private sealed class Report
        {
            public string status="running",error="",unity,graphics,device,currentLevel,savePath;
            public int width,height,firstFrame,lastFrame;
            public bool flowPassed,midMoveUndoPassed,transitionPassed,authoredMenuReused;
            public float audioRms;
            public List<LevelResult> levels=new List<LevelResult>();
            public List<string> errors=new List<string>();
        }
        public static bool Requested=>Array.IndexOf(Environment.GetCommandLineArgs(),"--study-validate")>=0;
        private PuzzleApp app;
        private StudyAssets assets;
        private KeyCode held;
        private int pressFrame;
        private string output;
        private Report report;
        private readonly float[] audioSamples=new float[1024];
        private readonly List<Command> observed=new List<Command>();
        public bool Held(KeyCode key)=>held==key;
        public bool Pressed(KeyCode key)=>held==key && Time.frameCount==pressFrame;

        public void Begin(PuzzleApp host,StudyAssets source)
        {
            app=host;assets=source;
            string[] args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--study-output");
            if(Array.IndexOf(args,"--study-quiet")>=0){app.Progress.Data.musicVolume=app.Progress.Data.effectsVolume=0;app.ApplyVolumes();}
            output=Path.GetFullPath(i>=0 && i+1<args.Length?args[i+1]:Path.Combine(Application.temporaryCachePath,"BoxOrVoidValidation"));
            Directory.CreateDirectory(output);
            report=new Report{unity=Application.unityVersion,graphics=SystemInfo.graphicsDeviceType.ToString(),device=SystemInfo.graphicsDeviceName,width=Screen.width,height=Screen.height,firstFrame=Time.frameCount,savePath=app.Progress.SavePath};
            Application.logMessageReceived+=ObserveLog;app.ActionApplied+=ObserveAction;app.SetTestKeyboard(this);
            WriteReport();StartCoroutine(Guarded(Run()));
        }
        private void ObserveAction(Command command,ActionResult result) {observed.Add(command);}
        private void ObserveLog(string message,string stack,LogType type)
        {if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) report.errors.Add(message);}
        private IEnumerator Tap(KeyCode key)
        {
            while(app.IsTransitioning) yield return null;
            held=key;pressFrame=Time.frameCount+1;yield return null;yield return null;held=KeyCode.None;yield return null;
            while(app.IsTransitioning) yield return null;
            while((app.Board.IsAnimating || app.HasPendingHistoryAction) && app.Page==StudyPage.Playing) yield return null;
        }
        private IEnumerator Run()
        {
            while(app.IsTransitioning) yield return null;
            yield return null;yield return null;Capture("title.png");
            var authoredTitle=app.GetComponent<StudyMenu>().AuthoredTitle;
            int authoredId=authoredTitle.GetInstanceID();Require(authoredTitle.activeInHierarchy,"Authored title is not visible");
            StudyFilm transitionFilm=app.Board.Camera.gameObject.AddComponent<StudyFilm>();
            transitionFilm.Begin(app.Board.Camera,Path.Combine(output,"TransitionFrames"));
            yield return new WaitForSecondsRealtime(.35f);
            held=KeyCode.Space;pressFrame=Time.frameCount+1;yield return null;yield return null;held=KeyCode.None;
            Require(app.IsTransitioning && app.Page==StudyPage.Title,"Transition did not retain the old page while closing");
            app.ShowTitle(); // A competing request must not cancel the accepted chapter navigation.
            yield return new WaitForSecondsRealtime(.15f);Capture("iris_closing.png");
            while(app.IsTransitioning && app.Transition.Aperture>.001f) yield return null;
            Require(app.Transition.Aperture<=.001f,"Transition never fully covered the page");Capture("iris_covered.png");
            while(app.IsTransitioning) yield return null;
            Require(app.Page==StudyPage.Chapters,"Title to chapter failed or repeated activation replaced it");report.transitionPassed=true;Capture("chapters.png");
            yield return Tap(KeyCode.Return);Require(app.Page==StudyPage.Levels,"Chapter to selection failed");Capture("selection_fresh.png");
            yield return Tap(KeyCode.Return);Require(app.Page==StudyPage.Playing && app.CurrentLevel=="n_tut_1","First level navigation failed");
            yield return new WaitForSecondsRealtime(.35f);transitionFilm.Finish();Destroy(transitionFilm);
            // Finish writes the captured audio synchronously. Do not inject a 100 ms
            // movement on the flush frame or measure that disk stall as animation time.
            yield return null;yield return null;
            string beforeMotion=app.Session.State.PositionKey();
            held=KeyCode.LeftArrow;pressFrame=Time.frameCount+1;yield return null;
            Require(app.Board.BlocksMovement,"Expected the moving frame for undo check");
            held=KeyCode.Z;pressFrame=Time.frameCount+1;yield return null;
            Require(app.HasPendingHistoryAction && app.Session.State.PositionKey()!=beforeMotion,"Undo was not queued during motion");
            held=KeyCode.None;while(app.Board.IsAnimating || app.HasPendingHistoryAction) yield return null;
            report.midMoveUndoPassed=app.Session.State.PositionKey()==beforeMotion;Require(report.midMoveUndoPassed,"Queued undo failed to restore initial state");
            yield return Tap(KeyCode.Escape);Require(app.Page==StudyPage.Paused,"Pause failed");Capture("pause.png");
            yield return Tap(KeyCode.Escape);Require(app.Page==StudyPage.Playing,"Resume failed");
            foreach(char c in assets.Replay("n_tut_1").commands) yield return Tap(KeyFor(c));
            yield return new WaitForSecondsRealtime(1.3f);
            while(app.IsTransitioning) yield return null;
            Require(app.Page==StudyPage.Levels && app.Progress.IsComplete("n_tut_1") && app.Progress.IsUnlocked("n_tut_2"),"Completion/unlock failed");
            var reload=new ProgressStore(Path.GetDirectoryName(app.Progress.SavePath),assets.Data.levels);reload.Load();
            Require(reload.IsComplete("n_tut_1"),"Progress reload failed");report.flowPassed=true;Capture("selection_completed.png");WriteReport();

            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--study-smoke")>=0)
            {
                app.ShowTitle();while(app.IsTransitioning) yield return null;
                report.authoredMenuReused=authoredTitle.GetInstanceID()==authoredId && authoredTitle.activeInHierarchy;
                Require(report.authoredMenuReused,"Title objects were replaced on return");
                app.LoadLevel("n_box_6");while(app.IsTransitioning) yield return null;yield return null;Capture("n_box_6_initial.png");
                app.ShowSettings();yield return null;Capture("settings.png");
                Require(report.errors.Count==0,"Player emitted errors");yield break;
            }

            app.HoldCompletionForTests=true;
            foreach(var level in assets.Data.levels)
            {
                report.currentLevel=level.id;WriteReport();app.LoadLevel(level.id);while(app.IsTransitioning) yield return null;yield return null;
                var replay=assets.Replay(level.id);var initial=app.Session.State.PositionKey();observed.Clear();
                bool representative=level.id=="n_tut_1" || level.id=="n_tut_8" || level.id=="n_box_6" || level.id=="n_box_12";
                if(representative) Capture(level.id+"_initial.png");
                StudyFilm film=null;
                if(level.id=="n_box_6")
                {film=app.Board.Camera.gameObject.AddComponent<StudyFilm>();film.Begin(app.Board.Camera,Path.Combine(output,"FilmFrames"));yield return new WaitForSecondsRealtime(.8f);}
                foreach(char c in replay.commands) yield return Tap(KeyFor(c));
                var item=new LevelResult{id=level.id,commands=replay.commands.Length,solved=app.Session.State.IsSolved};
                Require(item.solved,"Replay failed: "+level.id);
                Require(observed.Count==replay.commands.Length,"Input command count mismatch: "+level.id);
                for(int n=0;n<observed.Count;n++) Require((int)observed[n]=="URDLS".IndexOf(replay.commands[n]),"Input mapping mismatch");
                string final=app.Session.State.PositionKey();yield return new WaitForSecondsRealtime(.35f);if(representative) Capture(level.id+"_solved.png");
                if(film!=null) {yield return new WaitForSecondsRealtime(.8f);film.Finish();Destroy(film);}
                yield return Tap(KeyCode.R);yield return Tap(KeyCode.Z);item.resetUndo=app.Session.State.PositionKey()==final;Require(item.resetUndo,"Reset undo failed");
                foreach(char c in replay.commands) yield return Tap(KeyCode.Z);
                item.fullUndo=app.Session.State.PositionKey()==initial;Require(item.fullUndo,"Full undo failed");report.levels.Add(item);WriteReport();
            }
            app.HoldCompletionForTests=false;app.ShowTitle();while(app.IsTransitioning) yield return null;
            report.authoredMenuReused=authoredTitle.GetInstanceID()==authoredId && authoredTitle.activeInHierarchy;
            Require(report.authoredMenuReused,"Title objects were replaced on return");Require(report.errors.Count==0,"Player emitted errors");
        }
        private IEnumerator Guarded(IEnumerator work)
        {
            var stack=new Stack<IEnumerator>();stack.Push(work);
            while(stack.Count>0)
            {
                bool more=false;object current=null;Exception error=null;
                try {more=stack.Peek().MoveNext();if(more) current=stack.Peek().Current;}catch(Exception ex) {error=ex;}
                if(error!=null) {report.error=error.ToString();break;}
                if(!more) {stack.Pop();continue;}
                if(current is IEnumerator nested) {stack.Push(nested);continue;}
                yield return current;
            }
            held=KeyCode.None;app.SetTestKeyboard(null);app.ActionApplied-=ObserveAction;
            report.status=string.IsNullOrEmpty(report.error)?"passed":"failed";report.lastFrame=Time.frameCount;WriteReport();
            Application.logMessageReceived-=ObserveLog;
            Debug.Log("BoxOrVoid Windows validation: "+report.status+"; "+report.levels.Count+" levels");
            Application.Quit(report.status=="passed"?0:1);
        }
        private void Update()
        {
            if(report==null) return;AudioListener.GetOutputData(audioSamples,0);float sum=0;
            foreach(float sample in audioSamples) sum+=sample*sample;report.audioRms=Mathf.Max(report.audioRms,Mathf.Sqrt(sum/audioSamples.Length));
        }
        private void WriteReport() {File.WriteAllText(Path.Combine(output,"report.json"),JsonUtility.ToJson(report,true));}
        private void Capture(string name)
        {
            var camera=app.Board.Camera;var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;float aspect=camera.aspect;
            int width=Screen.width,height=Screen.height;
            var texture=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                camera.aspect=(float)width/height;camera.targetTexture=texture;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=texture;
                image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name),image.EncodeToPNG());
            }
            finally {camera.targetTexture=oldTarget;camera.aspect=aspect;RenderTexture.active=oldActive;Destroy(image);texture.Release();Destroy(texture);}
        }
        private static KeyCode KeyFor(char c)
        {switch(c) {case 'U':return KeyCode.UpArrow;case 'R':return KeyCode.RightArrow;case 'D':return KeyCode.DownArrow;case 'L':return KeyCode.LeftArrow;case 'S':return KeyCode.Space;default:throw new ArgumentException("Invalid replay character");}}
        private static void Require(bool value,string message) {if(!value) throw new InvalidOperationException(message);}
    }
}
#endif
