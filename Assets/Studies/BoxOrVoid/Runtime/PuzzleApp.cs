using System;
using System.IO;
using BoxOrVoid.Core;
using BoxOrVoid.Study.LevelEditing;
using UnityEngine;

namespace BoxOrVoid.Study
{
    public enum StudyPage { Title, Chapters, Levels, Playing, Paused, Settings, Completing, Creating }

    /// <summary>One scene hosts every page and every data level.</summary>
    public sealed class PuzzleApp:MonoBehaviour
    {
        [SerializeField] private StudyAssets assets;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private StudyMenu authoredMenu;
        [SerializeField] private StudyTransition transition;
        [SerializeField,HideInInspector] private bool automatedInput;
        private BoardView board;
        private PuzzleHud hud;
        private StudyMenu menu;
        private PuzzleAudio audioPlayer;
        private PuzzleSession session;
        private LevelEntry level;
        private readonly StudyInput input=new StudyInput();
        private readonly HistoryActionQueue historyActions=new HistoryActionQueue();
        private readonly UnityStudyKeyboard hardwareKeys=new UnityStudyKeyboard();
        private IStudyKeyboard testKeys;
        private bool priorBackground;
        private int priorFrameRate;
        private UnityEngine.Rendering.RenderPipelineAsset priorPipeline;
        private StudyPage settingsReturn;
        private float completedAt,nextVanishSound;
        private bool customLevel;
        private PlayerLevelEditor creator;
        private bool creatorPlay;
        public PlayerLevelEditor Creator=>creator;
        public IStudyKeyboard EditorKeyboard=>TestInputOnly?testKeys:Application.isFocused?hardwareKeys:null;

        public PuzzleSession Session=>session;
        public BoardView Board=>board;
        public string CurrentLevel=>level?.id;
        public StudyPage Page {get;private set;}
        public int SelectedChapter {get;private set;}
        public ProgressStore Progress {get;private set;}
        public bool TestInputOnly {get=>automatedInput;set=>automatedInput=value;}
        public bool HoldCompletionForTests {get;set;}
        public bool HasPendingHistoryAction=>historyActions.HasPending;
        public bool IsTransitioning=>transition!=null && transition.IsBusy;
        public StudyTransition Transition=>transition;
        public event Action<Command,ActionResult> ActionApplied;

        public void Configure(StudyAssets source,Camera camera) {assets=source;viewCamera=camera;}
        public void ConfigurePresentation(StudyMenu sceneMenu,StudyTransition sceneTransition) {authoredMenu=sceneMenu;transition=sceneTransition;}
        public void SetTestKeyboard(IStudyKeyboard keys)
        {
            if(!TestInputOnly) throw new InvalidOperationException("Test input was not isolated before Play Mode.");
            testKeys=keys;input.Clear();
        }
        private void Awake()
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(StudyPlayerCheck.Requested || CreatorRuntimeCheck.Requested) automatedInput=true;
            #endif
            priorBackground=Application.runInBackground;priorFrameRate=Application.targetFrameRate;
            priorPipeline=QualitySettings.renderPipeline;
            if(assets.RenderPipeline!=null) QualitySettings.renderPipeline=assets.RenderPipeline;
            if(TestInputOnly) Application.runInBackground=true;
            Application.targetFrameRate=60;
        }
        private void OnDestroy()
        {Application.runInBackground=priorBackground;Application.targetFrameRate=priorFrameRate;QualitySettings.renderPipeline=priorPipeline;}
        private void Start()
        {
            assets.Initialize();
            string folder=TestInputOnly?Path.Combine(Application.temporaryCachePath,"BoxOrVoidStudyTests",Guid.NewGuid().ToString("N")):Path.Combine(Application.persistentDataPath,"BoxOrVoidStudy");
            Progress=new ProgressStore(folder,assets.Data.levels);Progress.Load();SelectedChapter=Progress.Data.lastChapter;
            board=gameObject.AddComponent<BoardView>();board.Initialize(assets,viewCamera);
            hud=gameObject.AddComponent<PuzzleHud>();hud.Initialize(assets,viewCamera);
            menu=authoredMenu;
            if(menu==null || transition==null) throw new InvalidOperationException("Study scene presentation is missing. Use Tools/Box or Void Study/Author scene presentation.");
            menu.Initialize(assets,viewCamera,this);
            audioPlayer=gameObject.AddComponent<PuzzleAudio>();audioPlayer.Initialize(assets);ApplyVolumes();
            creator=GetComponent<PlayerLevelEditor>();if(creator!=null)creator.Initialize(this,folder);
            ShowTitleNow();PlayCue("irisOpen");transition.Reveal(()=>input.Clear());
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(StudyPlayerCheck.Requested) gameObject.AddComponent<StudyPlayerCheck>().Begin(this,assets);
            else if(CreatorRuntimeCheck.Requested)CreatorRuntimeCheck.Begin(this);
            #endif
        }
        public void LoadLevel(string id)
        {
            Navigate(()=>
            {
                level=assets.Level(id);customLevel=false;creatorPlay=false;SelectedChapter=level.chapter;
                EnterPuzzle(assets.Definition(id));
                Progress.Data.lastLevel=id;Progress.Data.lastChapter=level.chapter;Progress.Save();
            });
        }
        public void LoadCustom(LevelData data,int chapter=0)
        {
            var definition=PuzzleDefinition.FromData(data);
            Navigate(()=>
            {
                level=new LevelEntry{id="custom",chapter=chapter,displayId="编辑",hintGroups=new[]{"ui_undo_reset"}};
                customLevel=true;creatorPlay=false;SelectedChapter=chapter;EnterPuzzle(definition);
            });
        }
        private void EnterPuzzle(PuzzleDefinition definition)
        {
            session=new PuzzleSession(definition);Page=StudyPage.Playing;input.Clear();historyActions.Clear();
            menu.Hide();hud.SetVisible(true);board.Load(session.State,level.chapter);hud.Refresh(level,session.State);
            audioPlayer.PlayMusic(level.chapter);PlayCue("selectionSelect");
        }
        public ActionResult Act(Command command)
        {
            if(session==null) throw new InvalidOperationException("No puzzle is open.");
            if(Page!=StudyPage.Playing || IsTransitioning) return new ActionResult(ActionKind.NoChange,session.State,session.State);
            var result=session.Apply(command);board.Present(result);hud.Refresh(level,session.State);
            string cue=result.Kind==ActionKind.Switched?"playerSwitch":result.Kind==ActionKind.Undone?"undo":result.Kind==ActionKind.Reset?"reset":result.Kind==ActionKind.Blocked || result.Kind==ActionKind.Covered?"playerMoveBlocked":result.Kind==ActionKind.Pushed?"boxPush":result.Kind==ActionKind.Moved?"playerMove":null;
            PlayCue(cue);
            for(int i=0;i<session.State.Definition.Goals.Count;i++)
                if(!result.Before.GoalFulfilled(i) && result.After.GoalFulfilled(i)) PlayCue("goalFulfilled");
            ActionApplied?.Invoke(command,result);
            if(session.State.IsSolved && !(TestInputOnly && HoldCompletionForTests))
            {
                if(creatorPlay){Page=StudyPage.Completing;input.Clear();creator.PlayedThrough();return result;}
                if(!customLevel) Progress.Complete(level.id);
                Page=StudyPage.Completing;completedAt=Time.unscaledTime;nextVanishSound=completedAt+.3f;input.Clear();
            }
            return result;
        }
        private void Navigate(Action change)
        {
            if(transition.Change(()=>{change();PlayCue("irisOpen");},()=>input.Clear()))
            {input.Clear();historyActions.Clear();PlayCue("irisClose");}
        }
        public void ShowTitle() {Navigate(ShowTitleNow);}
        private void ShowTitleNow()
        {Page=StudyPage.Title;creatorPlay=false;creator?.Hide();session=null;board.ShowMenu(1);hud.SetVisible(false);audioPlayer.PlayMusic(0);menu.Title();input.Clear();}
        public void OpenCreatorLibrary()
        {
            if(creator==null)throw new InvalidOperationException("The scene has no player editor.");
            Navigate(()=>{CreatorSurface();creator.OpenLibrary();});
        }
        public void EnterCreatorDocument(LevelDocument document,bool saved)
        {Navigate(()=>{CreatorSurface();creator.OpenDocument(document,saved);});}
        private void CreatorSurface()
        {Page=StudyPage.Creating;creatorPlay=false;session=null;board.ShowMenu(1);hud.SetVisible(false);menu.Hide();audioPlayer.PlayMusic(1);input.Clear();}
        public void StartCreatorPlay(LevelData data,int chapter,string title)
        {
            var definition=PuzzleDefinition.FromData(data);
            Navigate(()=>{level=new LevelEntry{id="creator",chapter=chapter,displayId=title,hintGroups=Array.Empty<string>()};customLevel=true;creatorPlay=true;EnterPuzzle(definition);hud.SetVisible(false);creator.ShowPlaytest();});
        }
        public void ReturnToCreator(){if(creator==null||!creator.IsOpen)return;Navigate(()=>{CreatorSurface();creator.ShowEditing();});}
        public void RestartCreatorPlay(){if(!creatorPlay)return;Page=StudyPage.Playing;Act(Command.Reset);}
        public void CloseCreator(){creator?.Hide();ShowTitle();}
        public void ShowChapters() {Navigate(ShowChaptersNow);}
        private void ShowChaptersNow()
        {Page=StudyPage.Chapters;session=null;board.ShowMenu(1);hud.SetVisible(false);audioPlayer.PlayMusic(0);menu.Chapters(SelectedChapter);input.Clear();}
        public void ShowLevels(int chapter)
        {Navigate(()=>ShowLevelsNow(chapter));}
        private void ShowLevelsNow(int chapter)
        {
            SelectedChapter=chapter;Page=StudyPage.Levels;session=null;board.ShowMenu(chapter,chapter==0?"Tut World Background":"Box World Background");hud.SetVisible(false);audioPlayer.PlayMusic(chapter);
            string current=level!=null && level.chapter==chapter && !customLevel?level.id:Progress.Data.lastLevel;
            if(!Array.Exists(assets.Data.levels,l=>l.id==current && l.chapter==chapter)) current=chapter==0?"n_tut_1":"n_box_1";
            menu.Levels(chapter,current);input.Clear();
        }
        public void Pause() {if(Page!=StudyPage.Playing || IsTransitioning) return;historyActions.Clear();Page=StudyPage.Paused;menu.Pause();PlayCue("pauseOpen");input.Clear();}
        public void Resume() {if(IsTransitioning) return;Page=StudyPage.Playing;menu.Hide();PlayCue("pauseClose");input.Clear();}
        public void RestartFromPause() {Resume();Act(Command.Reset);}
        public void ShowSettings()
        {
            if(IsTransitioning) return;
            settingsReturn=Page;
            Action open=()=>{Page=StudyPage.Settings;menu.Settings();input.Clear();};
            if(Page==StudyPage.Title) Navigate(open);else open();
        }
        public void CloseSettings()
        {Progress.Save();if(settingsReturn==StudyPage.Paused) {Page=StudyPage.Paused;menu.Pause();input.Clear();}else ShowTitle();}
        public void ApplyVolumes() {audioPlayer.SetVolumes(Progress.Data.musicVolume,Progress.Data.effectsVolume);}
        public void PlayCue(string key) {audioPlayer.Cue(key);}
        public void Back()
        {
            switch(Page)
            {
                case StudyPage.Playing:Pause();break;
                case StudyPage.Paused:Resume();break;
                case StudyPage.Settings:CloseSettings();break;
                case StudyPage.Levels:ShowChapters();break;
                case StudyPage.Chapters:ShowTitle();break;
            }
        }
        public void ExitStudy()
        {
            Progress.Save();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
            #else
            Application.Quit();
            #endif
        }
        private void Update()
        {
            if(board==null || IsTransitioning) return;
            if(Page==StudyPage.Creating)return;
            if(Page==StudyPage.Completing)
            {
                if(creatorPlay)return;
                float t=Time.unscaledTime-completedAt;board.SetCompletion(Mathf.Clamp01((t-.3f)/.6f));
                if(t>=.3f && t<.9f && Time.unscaledTime>=nextVanishSound) {audioPlayer.Cue("levelVanishTick",Mathf.Lerp(.92f,1.12f,t));nextVanishSound=Time.unscaledTime+.04f;}
                if(t>=1) ShowLevels(SelectedChapter);
                return;
            }
            if(TestInputOnly?testKeys==null:!Application.isFocused) return;
            if(Page==StudyPage.Playing)
            {
                var ready=historyActions.Tick(board.IsAnimating);
                if(ready.HasValue) {Act(ready.Value);return;}
            }
            else historyActions.Clear();
            var action=input.Poll(TestInputOnly?testKeys:hardwareKeys,Time.unscaledTime,Page==StudyPage.Playing,Page==StudyPage.Playing && board.BlocksMovement);
            if(action.HasValue)
            {
                if(Page==StudyPage.Playing)
                {
                    if(action.Value==StudyAction.Back){if(creatorPlay)ReturnToCreator();else Pause();}
                    else if(action.Value==StudyAction.Undo || action.Value==StudyAction.Reset) historyActions.Request((Command)action.Value);
                    else if(action.Value<=StudyAction.Reset && !historyActions.HasPending) Act((Command)action.Value);
                }
                else menu.Handle(action.Value);
            }
            if(Page!=StudyPage.Playing) menu.PollMouse();
        }
    }
}
