using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DraculasBox.Core;
using DraculasBox.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DraculasBox.Editor
{
    /// <summary>Steer an isolated session through the ordinary input path, without OS input or window focus.</summary>
    [InitializeOnLoad] public static class BackgroundPlaytest
    {
        static BackgroundPlaytest(){EditorApplication.playModeStateChanged+=Changed;}
        public static void StartThemeChecks()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Editor is busy.");
            SessionState.SetBool("DraculasBox.Playtest.Themes",true);try{Start();}catch{SessionState.SetBool("DraculasBox.Playtest.Themes",false);throw;}
        }
        [Serializable] private sealed class ThemeViews {public int[] ids,values;}
        private static void SaveThemeViews()
        {
            var views=Resources.FindObjectsOfTypeAll<EditorWindow>().Where(w=>w.GetType().Name=="GameView").ToArray();var saved=new ThemeViews{ids=new int[views.Length],values=new int[views.Length]};
            for(int i=0;i<views.Length;i++){var so=new SerializedObject(views[i]);var p=so.FindProperty("m_EnterPlayModeBehavior");if(p==null)throw new InvalidOperationException("Background Game View mode is unavailable.");saved.ids[i]=views[i].GetInstanceID();saved.values[i]=p.intValue;}
            SessionState.SetString("DraculasBox.Themes.Views",JsonUtility.ToJson(saved));
            foreach(var view in views){var so=new SerializedObject(view);so.FindProperty("m_EnterPlayModeBehavior").intValue=2;so.ApplyModifiedPropertiesWithoutUndo();}
        }
        private static void RestoreThemeViews()
        {
            string json=SessionState.GetString("DraculasBox.Themes.Views","");if(string.IsNullOrEmpty(json))return;var saved=JsonUtility.FromJson<ThemeViews>(json);
            for(int i=0;i<saved.ids.Length;i++){var view=EditorUtility.InstanceIDToObject(saved.ids[i]) as EditorWindow;if(view==null)continue;var so=new SerializedObject(view);var p=so.FindProperty("m_EnterPlayModeBehavior");if(p!=null&&p.intValue==2){p.intValue=saved.values[i];so.ApplyModifiedPropertiesWithoutUndo();}}
            SessionState.EraseString("DraculasBox.Themes.Views");SessionState.EraseString("DraculasBox.ThemeEvidenceFolder");SessionState.EraseString("DraculasBox.WorkshopEvidenceFolder");SessionState.EraseString("DraculasBox.CampaignEvidenceFolder");
        }
        public static void StartWorkshopChecks(){SessionState.SetBool("DraculasBox.Playtest.Workshop",true);try{Start();}catch{SessionState.SetBool("DraculasBox.Playtest.Workshop",false);throw;}}
        public static void Start()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Editor is busy.");
            if(SceneManager.GetActiveScene().path!=GameBuilder.ScenePath||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Open the saved game scene first.");
            SaveThemeViews();
            // A check must start with fresh lifecycle state even when the user
            // normally skips both domain and scene reload for fast iteration.
            SessionState.SetBool("DraculasBox.Playtest.PrefsSaved",true);SessionState.SetBool("DraculasBox.Playtest.PriorFastEnter",EditorSettings.enterPlayModeOptionsEnabled);
            EditorSettings.enterPlayModeOptionsEnabled=false;
            SessionState.SetBool("DraculasBox.Playtest",true);EditorApplication.isPaused=false;EditorApplication.isPlaying=true;
        }
        private static void Changed(PlayModeStateChange change)
        {
            if(change==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("DraculasBox.Playtest",false))EditorApplication.update+=Attach;
            if(change==PlayModeStateChange.EnteredEditMode){GameThemes.Apply(GameThemes.DefaultThemeId);RestoreThemeViews();EditorApplication.update-=Attach;SessionState.SetBool("DraculasBox.Playtest",false);if(SessionState.GetBool("DraculasBox.Playtest.PrefsSaved",false)){if(!EditorSettings.enterPlayModeOptionsEnabled)EditorSettings.enterPlayModeOptionsEnabled=SessionState.GetBool("DraculasBox.Playtest.PriorFastEnter",false);SessionState.SetBool("DraculasBox.Playtest.PrefsSaved",false);}}
        }
        private static void Attach()
        {
            var app=UnityEngine.Object.FindObjectOfType<DraculaApp>();if(app==null||app.Progress==null)return;
            EditorApplication.update-=Attach;if(SessionState.GetBool("DraculasBox.Playtest.Themes",false)){SessionState.SetBool("DraculasBox.Playtest.Themes",false);app.gameObject.AddComponent<RuntimeChecks>().BeginThemes(app);}else if(SessionState.GetBool("DraculasBox.Playtest.Workshop",false)){SessionState.SetBool("DraculasBox.Playtest.Workshop",false);app.gameObject.AddComponent<RuntimeChecks>().BeginWorkshop(app);}else app.gameObject.AddComponent<PlaytestDriver>().Initialize(app);
        }
    }
    public sealed class PlaytestDriver:MonoBehaviour,IStudyKeyboard
    {
        private DraculaApp app;private KeyCode key;private int pressFrame,releaseFrame;private string folder;
        private readonly List<string> trace=new List<string>();
        public string Status {get;private set;}="ready";
        public bool Held(KeyCode k)=>key==k&&Time.frameCount>=pressFrame&&Time.frameCount<releaseFrame;
        public bool Pressed(KeyCode k)=>key==k&&Time.frameCount==pressFrame;
        public void Initialize(DraculaApp host)
        {
            app=host;app.SetTestKeyboard(this);folder=Path.GetFullPath("DraculasBox/Evidence/Playtest");Directory.CreateDirectory(folder);
            app.ActionApplied+=Record;
        }
        private void Record(ActionResult result){trace.Add(Time.unscaledTime.ToString("F3")+" "+result.Command+" "+result.Kind+" "+result.Reason+" "+result.After.Key());}
        private void OnDestroy(){if(app!=null)app.ActionApplied-=Record;Flush();}
        private void Flush(){if(folder!=null)File.WriteAllLines(Path.Combine(folder,"actions.log"),trace);}
        private KeyCode Map(char c)=>c=='U'?KeyCode.UpArrow:c=='R'?KeyCode.RightArrow:c=='D'?KeyCode.DownArrow:c=='L'?KeyCode.LeftArrow:c=='Z'?KeyCode.Z:c=='X'?KeyCode.R:c=='E'?KeyCode.Escape:KeyCode.Return;
        private void Press(KeyCode value){key=value;pressFrame=Time.frameCount+1;releaseFrame=pressFrame+1;}
        private IEnumerator Ready(){float start=Time.realtimeSinceStartup;while(app.Transitioning||app.Board.IsAnimating){if(Time.realtimeSinceStartup-start>10)throw new TimeoutException();yield return null;}}
        private void Launch(IEnumerator operation){if(Status=="busy")throw new InvalidOperationException("Playtest is busy.");Status="busy";StartCoroutine(Guard(operation));}
        private IEnumerator Guard(IEnumerator operation)
        {
            var stack=new Stack<IEnumerator>();stack.Push(operation);
            while(stack.Count>0)
            {
                bool next;object value=null;try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}
                catch(Exception e){key=KeyCode.None;Status=e.ToString();Flush();yield break;}
                if(!next){stack.Pop();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;
            }
            key=KeyCode.None;Status="ready";Flush();
        }
        public void Click(string id)=>Launch(ClickRoutine(id));
        private IEnumerator ClickRoutine(string id){yield return Ready();app.UI.ClickForCheck(id);yield return null;yield return Ready();yield return new WaitForSecondsRealtime(.25f);}
        public void Run(string commands)=>Launch(RunRoutine(commands));
        private IEnumerator RunRoutine(string commands)
        {
            foreach(char c in commands){yield return Ready();Press(Map(c));yield return null;yield return null;key=KeyCode.None;yield return new WaitForSecondsRealtime(.16f);}
            yield return Ready();yield return new WaitForSecondsRealtime(.25f);
        }
        public void Burst(string commands)=>Launch(BurstRoutine(commands));
        private IEnumerator BurstRoutine(string commands)
        {
            yield return Ready();foreach(char c in commands){Press(Map(c));yield return null;yield return null;key=KeyCode.None;yield return null;}
            yield return new WaitForSecondsRealtime(.4f);yield return Ready();
        }
        public void Hold(char command,float seconds)=>Launch(HoldRoutine(command,seconds));
        private IEnumerator HoldRoutine(char command,float seconds){yield return Ready();Press(Map(command));releaseFrame=int.MaxValue;yield return new WaitForSecondsRealtime(seconds);key=KeyCode.None;yield return new WaitForSecondsRealtime(.3f);yield return Ready();}
        public object Read()
        {
            var s=app.Session?.State;
            return new {status=Status,page=app.Page.ToString(),selection=app.UI.SelectedId,level=s?.Level.Export().id,player=s?.Player.ToString(),moves=app.Session?.Moves,history=app.Session?.UndoCount,pieces=s==null?null:Enumerable.Range(0,s.Level.PieceCount).Select(i=>s.Level.Kind(i)+":"+s.Position(i)).ToArray(),recent=trace.Skip(Math.Max(0,trace.Count-12)).ToArray()};
        }
        public string Capture(string name)
        {
            if(name.Any(c=>!char.IsLetterOrDigit(c)&&c!='_'&&c!='-'))throw new ArgumentException("Use a simple image name.");
            var camera=app.viewCamera;var prior=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;
            var rt=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);string path=Path.Combine(folder,name+".png");
            try{camera.targetTexture=rt;camera.aspect=1280f/720;app.Board.FitForCapture();Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally{camera.targetTexture=prior;camera.aspect=aspect;RenderTexture.active=active;app.Board.FitForCapture();rt.Release();Destroy(rt);Destroy(image);}
            return path;
        }
    }
}
