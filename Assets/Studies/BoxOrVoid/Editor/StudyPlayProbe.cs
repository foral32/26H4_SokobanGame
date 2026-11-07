using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BoxOrVoid.Core;
using UnityEditor;
using UnityEngine;

namespace BoxOrVoid.Study.Editor
{
    /// <summary>Background-only virtual keys enter the same runtime binding/repeat path as hardware.</summary>
    public sealed class StudyPlayProbe:IStudyKeyboard
    {
        [Serializable] private sealed class ProbeReport
        {
            public string level,status,error,commands,observed,initialKey,finalKey;
            public int initialFrame,finalFrame;
            public bool solved,resetUndo,fullUndo;
            public float maximumAudioRms;
        }
        public static string Status {get;private set;}="idle";
        private static StudyPlayProbe active;
        private PuzzleApp app;
        private ProbeReport report;
        private KeyCode key;
        private int pressFrame;
        private string output;
        private readonly List<Command> observed=new List<Command>();
        private readonly float[] audioSamples=new float[1024];

        public bool Held(KeyCode value)=>key==value;
        public bool Pressed(KeyCode value)=>key==value && Time.frameCount==pressFrame;
        public static string Begin(string level,string commands)
        {
            if(!EditorApplication.isPlaying || active!=null) throw new InvalidOperationException("Study must be playing with no active probe.");
            var app=UnityEngine.Object.FindFirstObjectByType<PuzzleApp>();
            if(app==null || !app.TestInputOnly) throw new InvalidOperationException("Input isolation must be enabled before Play Mode.");
            var probe=active=new StudyPlayProbe();
            probe.app=app;probe.report=new ProbeReport{level=level,commands=commands,status="running",initialFrame=Time.frameCount};
            probe.output=Path.GetFullPath("Studies/BoxOrVoid/Evidence/Runtime/"+level+"_input_report.json");
            app.HoldCompletionForTests=true;app.SetTestKeyboard(probe);app.ActionApplied+=probe.Observe;
            Status="running "+level;app.StartCoroutine(probe.Guarded(probe.Run()));return Status;
        }
        public static string BeginFlow()
        {
            if(!EditorApplication.isPlaying || active!=null) throw new InvalidOperationException("Study must be playing with no active probe.");
            var app=UnityEngine.Object.FindFirstObjectByType<PuzzleApp>();
            if(app==null || !app.TestInputOnly) throw new InvalidOperationException("Expected isolated study.");
            var probe=active=new StudyPlayProbe();probe.app=app;probe.report=new ProbeReport{level="flow",status="running",initialFrame=Time.frameCount};
            probe.output=Path.GetFullPath("Studies/BoxOrVoid/Evidence/Runtime/flow_report.json");
            app.HoldCompletionForTests=false;app.SetTestKeyboard(probe);app.ActionApplied+=probe.Observe;
            Status="running flow";app.StartCoroutine(probe.Guarded(probe.RunFlow()));return Status;
        }
        private IEnumerator RunFlow()
        {
            while(app.IsTransitioning) yield return null;
            app.ShowTitle();while(app.IsTransitioning) yield return null;StudyBuilder.Capture("flow_title.png");
            yield return Tap(KeyCode.Space);Require(app.Page==StudyPage.Chapters,"Title confirmation failed");StudyBuilder.Capture("flow_chapters.png");
            yield return Tap(KeyCode.Return);Require(app.Page==StudyPage.Levels,"Chapter confirmation failed");StudyBuilder.Capture("flow_levels_fresh.png");
            yield return Tap(KeyCode.Return);Require(app.Page==StudyPage.Playing && app.CurrentLevel=="n_tut_1","First level selection failed");
            string initial=app.Session.State.PositionKey();yield return Tap(KeyCode.LeftArrow);yield return Tap(KeyCode.Escape);
            Require(app.Page==StudyPage.Paused,"Escape failed to pause");StudyBuilder.Capture("flow_pause.png");
            yield return Tap(KeyCode.Escape);yield return Tap(KeyCode.Z);Require(app.Session.State.PositionKey()==initial,"Resume/undo lost board state");
            foreach(char c in "LLLLDLSRRRRUR") yield return Tap(KeyFor(c));
            yield return new WaitForSecondsRealtime(1.2f);
            while(app.IsTransitioning) yield return null;
            Require(app.Page==StudyPage.Levels,"Completion did not return to level selection");
            Require(app.Progress.IsComplete("n_tut_1") && app.Progress.IsUnlocked("n_tut_2"),"Completion did not unlock following level");
            StudyBuilder.Capture("flow_level_complete.png");
            var reload=new ProgressStore(Path.GetDirectoryName(app.Progress.SavePath),JsonUtility.FromJson<CatalogData>(File.ReadAllText(StudyBuilder.Root+"/Content/Catalog.json")).levels);reload.Load();
            Require(reload.IsComplete("n_tut_1") && reload.IsUnlocked("n_tut_2"),"Progress did not survive reload");
            yield return Tap(KeyCode.Return);yield return Tap(KeyCode.Escape);
            yield return Tap(KeyCode.DownArrow);yield return Tap(KeyCode.DownArrow);yield return Tap(KeyCode.DownArrow);yield return Tap(KeyCode.Return);
            Require(app.Page==StudyPage.Settings,"Pause settings navigation failed");
            float volume=app.Progress.Data.musicVolume;yield return Tap(KeyCode.RightArrow);
            Require(app.Progress.Data.musicVolume>volume,"Volume key did not change music");StudyBuilder.Capture("flow_settings.png");
            yield return Tap(KeyCode.Escape);Require(app.Page==StudyPage.Paused,"Settings did not return to pause");
            yield return Tap(KeyCode.Escape);Require(app.Page==StudyPage.Playing,"Pause did not resume");
            report.solved=true;report.resetUndo=true;report.fullUndo=true;report.observed="title > chapters > levels > puzzle > pause > resume > solve > unlock > reload > settings > resume";
        }
        private void Observe(Command command,ActionResult result) { observed.Add(command); }
        private void Press(KeyCode value) {key=value;pressFrame=Time.frameCount+1;}
        private IEnumerator Tap(KeyCode value)
        {
            while(app.IsTransitioning) yield return null;
            Press(value);yield return new WaitForSecondsRealtime(.045f);key=KeyCode.None;
            yield return new WaitForSecondsRealtime(.13f);
            while(app.IsTransitioning) yield return null;
        }
        private IEnumerator Run()
        {
            while(app.IsTransitioning) yield return null;
            app.LoadLevel(report.level);while(app.IsTransitioning) yield return null;yield return null;
            report.initialKey=app.Session.State.PositionKey();
            StudyBuilder.Capture(report.level+"_initial.png");
            int referenceStep=0;
            foreach(char c in report.commands)
            {
                yield return Tap(KeyFor(c));
                referenceStep++;
                if(report.level=="n_tut_2" && report.commands=="RRSDDSSUSRRRRSD")
                {
                    int[] steps={5,6,8,12,14,15};int checkpoint=Array.IndexOf(steps,referenceStep);
                    if(checkpoint>=0) {yield return new WaitForSecondsRealtime(.35f);StudyBuilder.Capture("reference_tut2_"+(102+checkpoint).ToString("D4")+".png");}
                }
            }
            report.solved=app.Session.State.IsSolved;report.finalKey=app.Session.State.PositionKey();
            Require(report.solved,"Virtual-key replay did not solve the level");
            report.observed=string.Concat(observed.ConvertAll(c=>"URDLSZX"[(int)c].ToString()));
            Require(report.observed==report.commands,"Binding path dispatched different or repeated commands: "+report.observed);
            StudyBuilder.Capture(report.level+"_solved.png");
            yield return Tap(KeyCode.R);yield return Tap(KeyCode.Z);
            report.resetUndo=app.Session.State.PositionKey()==report.finalKey;
            Require(report.resetUndo,"Reset/undo through keys lost the solved state");
            foreach(char c in report.commands) yield return Tap(KeyCode.Z);
            report.fullUndo=app.Session.State.PositionKey()==report.initialKey;
            Require(report.fullUndo,"Full undo through keys failed");
            StudyBuilder.Capture(report.level+"_undo_restored.png");
        }
        private IEnumerator Guarded(IEnumerator routine)
        {
            // Flatten nested iterators so assertion failures are reported and input is always released.
            var stack=new Stack<IEnumerator>();stack.Push(routine);
            while(stack.Count>0)
            {
                bool more=false;object current=null;Exception failure=null;
                try {more=stack.Peek().MoveNext();if(more) current=stack.Peek().Current;}
                catch(Exception ex) {failure=ex;}
                if(failure!=null) {report.error=failure.ToString();break;}
                if(!more) {stack.Pop();continue;}
                if(current is IEnumerator nested) {stack.Push(nested);continue;}
                SampleAudio();yield return current;
            }
            key=KeyCode.None;report.status=report.error==null?"passed":"failed";report.finalFrame=Time.frameCount;
            Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonUtility.ToJson(report,true));
            Status=report.status+" "+report.level;Debug.Log("BoxOrVoid probe: "+Status);
            if(report.error!=null) Debug.LogError(report.error);
            app.ActionApplied-=Observe;app.SetTestKeyboard(null);app.HoldCompletionForTests=false;active=null;
        }
        private void SampleAudio()
        {
            if(report==null) return;
            AudioListener.GetOutputData(audioSamples,0);float sum=0;
            foreach(float sample in audioSamples) sum+=sample*sample;
            report.maximumAudioRms=Mathf.Max(report.maximumAudioRms,Mathf.Sqrt(sum/audioSamples.Length));
        }
        private static KeyCode KeyFor(char c)
        {
            switch(c) {case 'U':return KeyCode.UpArrow;case 'R':return KeyCode.RightArrow;case 'D':return KeyCode.DownArrow;case 'L':return KeyCode.LeftArrow;case 'S':return KeyCode.Space;default:throw new ArgumentException("Unknown replay key");}
        }
        private static void Require(bool condition,string message) {if(!condition) throw new InvalidOperationException(message);}
    }
}
