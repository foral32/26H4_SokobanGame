#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DraculasBox.Core;
using DraculasBox.Runtime;
using UnityEditor;
using UnityEngine;

namespace DraculasBox.Editor
{
    // Review data stays outside Assets and never enters the shipped campaign.
    // This driver only runs in the existing isolated, muted background session.
    public sealed class LevelCandidateReview : MonoBehaviour, IStudyKeyboard
    {
        [Serializable] public sealed class Result
        {
            public string status="running",error,profile;
            public int checks;
            public bool isolated;
            public List<string> completed=new List<string>(),screenshots=new List<string>(),actions=new List<string>(),errors=new List<string>();
        }
        private DraculaApp app;
        private Result report=new Result();
        private string folder;
        private KeyCode held;
        private int pressedAt;
        private ActionResult lastAction;
        public bool Held(KeyCode key)=>held==key;
        public bool Pressed(KeyCode key)=>held==key&&Time.frameCount==pressedAt;
        public void Begin(DraculaApp host)
        {
            if(!host.Automated||!SessionState.GetBool("DraculasBox.Playtest",false))throw new InvalidOperationException("Use an isolated background playtest.");
            app=host;app.SetTestKeyboard(this);
            folder=Path.GetFullPath("DraculasBox/Reviews/LevelCandidates-20261111/Play");Directory.CreateDirectory(folder);
            app.ActionApplied+=Observe;Application.logMessageReceived+=Log;
            StartCoroutine(Guard(Run()));
        }
        private void Observe(ActionResult action)
        {
            lastAction=action;
            report.actions.Add(action.After.Level.Title+" "+action.Command+" "+action.Kind+" "+action.After.Key());
        }
        private void Log(string message,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Exception)report.errors.Add(message);}
        private void Require(bool value,string message)
        {report.checks++;if(!value)throw new InvalidOperationException(message);}
        private IEnumerator Ready()
        {
            float until=Time.realtimeSinceStartup+15;
            while(app.Transitioning||app.Board.IsAnimating)
            {if(Time.realtimeSinceStartup>until)throw new TimeoutException("Transition or board motion");yield return null;}
            yield return null;
        }
        private IEnumerator Tap(KeyCode key,bool settle=true)
        {
            yield return Ready();held=key;pressedAt=Time.frameCount+1;
            yield return null;yield return null;held=KeyCode.None;
            yield return new WaitForSecondsRealtime(.12f);
            if(settle)yield return Ready();
        }
        private IEnumerator Click(string id)
        {yield return Ready();app.UI.ClickForCheck(id);yield return null;yield return Ready();}
        private void Capture(string name)
        {
            const int width=1280,height=720;
            var camera=app.viewCamera;var previous=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;camera.aspect=(float)width/height;app.Board.FitForCapture();Canvas.ForceUpdateCanvases();
                camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
                File.WriteAllBytes(Path.Combine(folder,name+".png"),texture.EncodeToPNG());report.screenshots.Add(name+".png");
            }
            finally {camera.targetTexture=previous;camera.aspect=aspect;RenderTexture.active=active;app.Board.FitForCapture();target.Release();Destroy(target);Destroy(texture);}
        }
        private IEnumerator Run()
        {
            yield return Ready();
            report.profile=Path.GetFullPath(app.Library.Folder);
            report.isolated=report.profile.StartsWith(Path.GetFullPath(Application.temporaryCachePath),StringComparison.OrdinalIgnoreCase);
            Require(report.isolated,"The review must preserve the real profile.");
            var inputs=Directory.GetFiles(Path.GetFullPath("DraculasBox/LevelDrafts/LightBoxes-20261111/levels"),"*.json").OrderBy(p=>p).ToArray();
            Require(inputs.Length==5,"Exactly five reviewed candidates.");
            var drafts=inputs.Select(p=>JsonUtility.FromJson<LevelData>(File.ReadAllText(p))).ToArray();
            foreach(var data in drafts)
            {
                Require(LevelSolver.Proves(data,data.solution,out _),"Recorded route is legal: "+data.title);
                app.Library.Save(data);
            }
            var library=app.Library.Read(out int invalid);
            Require(invalid==0&&library.Count==5,"Five readable custom levels in isolated library.");
            foreach(var data in drafts)Require(library.Single(d=>d.id==data.id).ContentEquals(data),"Library round trip: "+data.title);
            app.OpenLibrary();yield return null;yield return Ready();Capture("00-library");
            for(int index=0;index<drafts.Length;index++)
            {
                var data=drafts[index];
                Require(app.Page==GamePage.Library&&app.UI.FindButton("custom-"+data.id)!=null,"Draft visible in native custom library: "+data.title);
                app.SelectLibrary(data.id);yield return Click("edit-custom");
                Require(app.Page==GamePage.Editing&&app.Creator.Document.Data.ContentEquals(data),"Native editor loads exact draft.");
                Capture((index+1).ToString("00")+"-editor");
                if(index==0)
                {Require(app.Creator.HandleShortcut(KeyCode.F5),"Editor shared F5 dispatcher starts preview.");yield return null;yield return Ready();}
                else yield return Click("test");
                Require(app.Page==GamePage.Playing&&app.Session.State.Level.Export().LayoutEquals(data),"Native preview enters the authored layout.");
                Capture((index+1).ToString("00")+"-start");
                string initial=app.Session.State.Key();
                var first=Rules.Move(app.Session.State,(Command)LevelSolver.Directions.IndexOf(data.solution[0]));
                yield return Tap(Key(data.solution[0]));
                Require(app.Session.State.Key()==first.After.Key(),"First ordinary input reaches expected state.");
                yield return Tap(KeyCode.R);Require(app.Session.State.Key()==initial,"Reset restores review draft.");
                yield return Tap(KeyCode.Z);Require(app.Session.State.Key()==first.After.Key(),"Reset can be undone.");
                yield return Tap(KeyCode.R);Require(app.Session.State.Key()==initial,"Review starts from initial state.");
                var expected=new Session(new Definition(data));
                for(int step=0;step<data.solution.Length;step++)
                {
                    var result=expected.Apply((Command)LevelSolver.Directions.IndexOf(data.solution[step]));lastAction=null;
                    bool final=step==data.solution.Length-1;
                    yield return Tap(Key(data.solution[step]),!final);
                    Require(lastAction!=null&&lastAction.Changed&&!lastAction.After.Dead&&lastAction.After.Key()==result.After.Key(),"Native input matches core at "+data.title+" step "+(step+1));
                    if(!final&&result.PushedCount>0)Capture((index+1).ToString("00")+"-step-"+(step+1).ToString("00"));
                }
                Require(lastAction.After.Won,"Native route completes: "+data.title);Capture((index+1).ToString("00")+"-win");
                report.completed.Add(data.title);
                yield return new WaitForSecondsRealtime(.85f);yield return Ready();
                Require(app.Page==GamePage.Editing,"Custom completion returns to its editor.");
                Require(app.Creator.Document.Data.solution==data.solution,"Editor retains the legal review solution.");
                app.OpenLibrary();yield return null;yield return Ready();
            }
            var audio=app.GetComponent<GameAudio>();
            app.StartEditorPlay(app.assets.Level("c1_02"));yield return null;yield return Ready();
            var clip=app.assets.Clip("burn");Require(clip!=null&&Mathf.Abs(clip.length-.2035625f)<.001f,"Selected error_005 imported without audition padding.");
            string beforeDeath=app.Session.State.Key();yield return Tap(KeyCode.DownArrow,false);yield return new WaitForSecondsRealtime(.05f);
            var source=(AudioSource)typeof(GameAudio).GetField("burn",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(audio);
            Require(app.Session.State.Dead&&source.clip==clip&&source.volume==0,"Light death chooses approved clip while background audio stays muted.");
            yield return Tap(KeyCode.Z);Require(!app.Session.State.Dead&&app.Session.State.Key()==beforeDeath&&!source.isPlaying,"Undo cancels death audio and restores state.");
            Require(report.errors.Count==0,"No runtime Console errors.");
        }
        private static KeyCode Key(char direction)=>direction=='U'?KeyCode.UpArrow:direction=='R'?KeyCode.RightArrow:direction=='D'?KeyCode.DownArrow:KeyCode.LeftArrow;
        private IEnumerator Guard(IEnumerator operation)
        {
            var stack=new Stack<IEnumerator>();stack.Push(operation);
            while(stack.Count>0)
            {
                bool next;object value=null;
                try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}
                catch(Exception e){report.status="failed";report.error=e.ToString();Finish();yield break;}
                if(!next){stack.Pop();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;
            }
            report.status=report.errors.Count==0?"passed":"failed";Finish();
        }
        private void Finish()
        {
            held=KeyCode.None;app.ActionApplied-=Observe;Application.logMessageReceived-=Log;
            File.WriteAllText(Path.Combine(folder,"report.json"),JsonUtility.ToJson(report,true));
            if(app.Automated&&SessionState.GetBool("DraculasBox.Playtest",false))EditorApplication.isPlaying=false;
        }
    }
}
#endif
