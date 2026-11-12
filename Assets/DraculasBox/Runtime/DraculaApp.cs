using System;
using System.IO;
using System.Linq;
using DraculasBox.Core;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public enum GamePage {Title,Chapters,Levels,Playing,Paused,Settings,Completing,Library,Editing,Confirming,Dying,Dead,Importing,Campaign}
    public sealed partial class DraculaApp:MonoBehaviour
    {
        public GameAssets assets;public Camera viewCamera;public GameUI ui;public StudyTransition transition;
        private GameAudio audioPlayer;private LevelEditor creator;private readonly StudyInput input=new StudyInput();private readonly HistoryActionQueue history=new HistoryActionQueue();
        private readonly UnityStudyKeyboard hardware=new UnityStudyKeyboard();private IStudyKeyboard testKeys;
        private GamePage settingsReturn;private float completedAt,diedAt;private bool deathCue,chapterJustCompleted;private string levelId;private bool creatorPlay;private LevelData playingData;
        private UnityEngine.Rendering.RenderPipelineAsset priorPipeline;private bool priorBackground,globalsRestored;private int priorRate;
        private int libraryPageIndex,importRequest;private string importPath,lastArchive;private LevelData importData;private Action confirmationCancel;
        public string LibrarySelection {get;set;}
        private string ArchiveCandidate()=>!string.IsNullOrEmpty(lastArchive)&&File.Exists(lastArchive)?lastArchive:Library?.LatestArchive();
        public bool CanRestoreArchive=>ArchiveCandidate()!=null;
        public bool Automated {get;private set;}
        private GamePage page;
        public GamePage Page
        {
            get=>page;
            private set
            {
                if(page!=value&&(page==GamePage.Editing||page==GamePage.Library))creator?.ResetAuthoringTool();
                page=value;if(audioPlayer==null)return;
                if(value==GamePage.Title||value==GamePage.Chapters||value==GamePage.Levels||value==GamePage.Playing&&!creatorPlay)
                    audioPlayer.Music(MusicContext.Main);
                else if(value==GamePage.Library||value==GamePage.Editing||value==GamePage.Importing||value==GamePage.Campaign)
                    audioPlayer.Music(MusicContext.Editor);
                // Pause, settings, confirmations and editor trials keep their context.
            }
        }
        public string ChapterId {get;private set;}="chapter_1";
        public GameUI UI=>ui;public BoardView Board {get;private set;}public Session Session {get;private set;}public LevelEditor Creator=>creator;
        public ProgressStore Progress {get;private set;}public LevelLibrary Library {get;private set;}
        public bool Transitioning=>(transition!=null&&transition.IsBusy)||(ui!=null&&(ui.IsEntering||ui.IsChapterCelebrating||ui.IsResettingProgress));
        public event Action<ActionResult> ActionApplied;
        public void SetTestKeyboard(IStudyKeyboard keys){if(!Automated)throw new InvalidOperationException("Test keyboard requires isolated validation mode.");testKeys=keys;input.Clear();}
        private void Awake()
        {
            globalsRestored=false;assets.ResetSpriteCache();
            Automated=Array.IndexOf(Environment.GetCommandLineArgs(),"--dracula-validate")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"--dracula-theme-checks")>=0;
            #if UNITY_EDITOR
            Automated|=UnityEditor.SessionState.GetBool("DraculasBox.Validate",false)||UnityEditor.SessionState.GetBool("DraculasBox.Playtest",false);
            #endif
            priorPipeline=QualitySettings.renderPipeline;priorBackground=Application.runInBackground;priorRate=Application.targetFrameRate;
            QualitySettings.renderPipeline=assets.pipeline;Application.targetFrameRate=60;if(Automated)Application.runInBackground=true;
        }
        private void Start()
        {
            string root=Automated?Path.Combine(Application.temporaryCachePath,"DraculasBoxChecks",Guid.NewGuid().ToString("N")):Path.Combine(Application.persistentDataPath,"DraculasBox");
            Progress=new ProgressStore(root);Library=new LevelLibrary(root);
            GameThemes.Apply(Progress.Data.theme);
            Board=gameObject.AddComponent<BoardView>();Board.Initialize(assets,viewCamera);ui.Initialize(assets,this,viewCamera);
            audioPlayer=gameObject.AddComponent<GameAudio>();audioPlayer.Initialize(assets);audioPlayer.Volume(Automated?0:Progress.Data.music,Automated?0:Progress.Data.effects);
            if(Automated&&UnityEngine.EventSystems.EventSystem.current!=null)
                foreach(var module in UnityEngine.EventSystems.EventSystem.current.GetComponents<UnityEngine.EventSystems.BaseInputModule>())module.enabled=false;
            Progress.Reconcile(assets.Catalog);
            ChapterId=assets.Catalog.chapters.First(c=>c.levels.Any(l=>l.id==Progress.Data.lastLevel)).id;
            creator=gameObject.AddComponent<LevelEditor>();creator.Initialize(this);
            ShowTitleNow();transition.Reveal(()=>input.Clear());
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(Automated
            #if UNITY_EDITOR
                &&!UnityEditor.SessionState.GetBool("DraculasBox.Playtest",false)
            #endif
            ){var checks=gameObject.AddComponent<RuntimeChecks>();if(Array.IndexOf(Environment.GetCommandLineArgs(),"--dracula-theme-checks")>=0)checks.BeginThemes(this);else checks.Begin(this);}
            #endif
        }
        private void RestoreGlobals(){if(globalsRestored)return;globalsRestored=true;QualitySettings.renderPipeline=priorPipeline;Application.runInBackground=priorBackground;Application.targetFrameRate=priorRate;}
        private void OnDisable(){RestoreGlobals();}
        private void OnDestroy(){RestoreGlobals();}
        private void OnApplicationQuit()
        {
            bool editing=Page==GamePage.Editing||Page==GamePage.Settings&&settingsReturn==GamePage.Editing;
            if(editing&&creator!=null)creator.CommitPending();
            if((editing||Page==GamePage.Confirming||creatorPlay)&&creator!=null&&creator.Document!=null&&creator.Document.Dirty)
                try{RecoverBoard(creator.Document.Data);}catch(Exception e){Debug.LogWarning("Draft recovery could not be saved: "+e.Message);}
            Progress?.Save();
        }
        private void Navigate(Action action)
        {if(Transitioning)return;history.Clear();input.Clear();audioPlayer?.CancelCircuitReplies();Cue("IrisClose");transition.Change(()=>{action();Cue("IrisOpen");},()=>input.Clear());}
        public void ShowTitle()=>Navigate(ShowTitleNow);
        private void ShowTitleNow(){Page=GamePage.Title;Session=null;Board.Clear();ui.Title();}
        public void ShowChapters()=>Navigate(()=>{Page=GamePage.Chapters;Session=null;Board.Clear();ui.Chapters();});
        public void ShowLevels(string chapter)=>ShowLevels(chapter,false);
        private void ShowLevels(string chapter,bool celebrate)
        {
            var catalog=assets.Catalog;var selected=catalog.chapters.FirstOrDefault(c=>c.id==chapter);
            if(selected==null||!Progress.ChapterUnlocked(selected,catalog))return;
            Navigate(()=>{ChapterId=chapter;Page=GamePage.Levels;Session=null;Board.Clear();ui.Levels(selected);if(celebrate)ui.CelebrateChapter(selected);});
        }
        public void LoadLevel(string id)
        {
            var chapter=assets.Catalog.chapters.FirstOrDefault(c=>c.levels.Any(l=>l.id==id));
            if(chapter==null||!Progress.ChapterUnlocked(chapter,assets.Catalog)||!Progress.Unlocked(chapter.levels.First(l=>l.id==id)))return;
            var data=assets.Level(id);Navigate(()=>{ChapterId=chapter.id;levelId=id;creatorPlay=false;Enter(data);Progress.Data.lastLevel=id;Progress.Save();});
        }
        private void Enter(LevelData data)
        {chapterJustCompleted=false;playingData=data.Copy();Session=new Session(new Definition(data));Board.Load(Session.State);Page=GamePage.Playing;ui.Playing(data.title,creatorPlay);input.Clear();history.Clear();}
        public void RequestHistory(Command c){if(Page==GamePage.Playing||Page==GamePage.Dying||Page==GamePage.Dead){input.Clear();history.Request(c);}}
        public void Act(Command cmd)
        {
            bool recovering=Page==GamePage.Dying||Page==GamePage.Dead;
            if(Page!=GamePage.Playing&&!(recovering&&(cmd==Command.Undo||cmd==Command.Reset)))return;
            var result=Session.Apply(cmd);if(result.Kind==ActionKind.NoChange)return;
            Board.Present(result);ActionApplied?.Invoke(result);
            if(creatorPlay&&EditingOfficial)Campaign.ObserveTrial(result);
            else if(creatorPlay&&result.Changed&&Session.State.Won)creator.Document.RecordSolution(Session.WinningRoute());
            CueCircuits(result);
            string clip=result.Kind==ActionKind.Blocked?"footstep_grass_004":result.Kind==ActionKind.Pushed?"impactWood_heavy_001":result.Kind==ActionKind.Undone?"undo":result.Kind==ActionKind.Reset?"reset":"footstep_concrete_003";Cue(clip);
            if(Session.State.Dead){history.Clear();input.Clear();Page=GamePage.Dying;diedAt=Time.unscaledTime;deathCue=false;return;}
            if(recovering){Page=GamePage.Playing;ui.Playing(playingData.title,creatorPlay);}
            if(Session.State.Won)
            {
                history.Clear();input.Clear();
                if(!creatorPlay)
                {
                    var chapter=assets.Catalog.chapters.First(c=>c.id==ChapterId);bool wasComplete=Progress.ChapterDone(chapter);
                    Progress.Complete(levelId);chapterJustCompleted=!wasComplete&&Progress.ChapterDone(chapter);
                    // Progress is durable before presentation; leaving during the celebration cannot lose an unlock.
                    Progress.RememberUnlocks(assets.Catalog);
                }
                Page=GamePage.Completing;completedAt=Time.unscaledTime;Cue("goal");
            }
        }
        public void Pause(){if((Page!=GamePage.Playing&&Page!=GamePage.Dead)||Transitioning)return;history.Clear();input.Clear();audioPlayer?.CancelCircuitReplies();Board.SetPaused(true);Page=GamePage.Paused;ui.Pause();Cue("pauseMenu");}
        public void Resume(){Board.SetPaused(false);Page=Session.State.Dead?GamePage.Dead:GamePage.Playing;input.Clear();ui.Playing(playingData.title,creatorPlay);if(Session.State.Dead)ui.DeathHint();}
        public void ResetFromPause(){Resume();RequestHistory(Command.Reset);}
        public void Replay()=>Navigate(()=>Enter(playingData));
        public void NextLevel()
        {
            var chapter=assets.Catalog.chapters.First(c=>c.id==ChapterId);
            if(chapterJustCompleted){ShowLevels(ChapterId,true);chapterJustCompleted=false;return;}
            if(chapter.levels.Any(l=>l.id==levelId&&!l.main)){ShowLevels(ChapterId);return;}
            var main=chapter.levels.Where(l=>l.main).ToArray();int index=Array.FindIndex(main,l=>l.id==levelId);
            if(index>=0&&index+1<main.Length&&Progress.Unlocked(main[index+1])){LoadLevel(main[index+1].id);return;}
            ShowLevels(ChapterId);
        }
        public void Settings(){if(Transitioning)return;if(Page==GamePage.Editing){creator.CommitPending();creator.ClosePanels();}settingsReturn=Page;Page=GamePage.Settings;input.Clear();ui.Settings();}
        public void CycleTheme(int direction)
        {int count=GameThemes.All.Count;SelectTheme(GameThemes.All[(GameThemes.Current.Index+Math.Sign(direction)+count)%count].Id);}
        public void SelectTheme(string id)
        {
            if(Page!=GamePage.Settings&&Page!=GamePage.Paused)return;
            var theme=GameThemes.Resolve(id);Progress.Data.theme=theme.Id;Progress.Save();
            bool changed=GameThemes.Apply(theme.Id);if(changed)Board.RefreshTheme();
            ui.RefreshThemeSettings("theme");input.Clear();
        }
        public void AdjustVolume(int channel,float delta){if(channel==0)Progress.Data.music=Mathf.Clamp01(Progress.Data.music+delta);else Progress.Data.effects=Mathf.Clamp01(Progress.Data.effects+delta);audioPlayer.Volume(Automated?0:Progress.Data.music,Automated?0:Progress.Data.effects);Progress.Save();ui.RefreshSettings();Cue("uiNavigate");}
        public void CloseSettings(){if(settingsReturn==GamePage.Paused){Page=GamePage.Paused;ui.Pause();}else if(settingsReturn==GamePage.Editing){Page=GamePage.Editing;creator.Show();}else ShowTitle();input.Clear();}
        public void Back()
        {
            if((Page==GamePage.Editing||Page==GamePage.Library)&&creator.CloseAuthoringTool())return;
            if(Page==GamePage.Campaign){Campaign.Back();return;}
            if(Page==GamePage.Editing&&(creator.ClosePanels()||creator.CancelRectangle())||Page==GamePage.Library&&ui.CloseWorkshopPopup())return;
            switch(Page){case GamePage.Chapters:ShowTitle();break;case GamePage.Levels:ShowChapters();break;case GamePage.Playing:case GamePage.Dead:if(creatorPlay)ReturnToEditor();else Pause();break;case GamePage.Paused:Resume();break;case GamePage.Settings:CloseSettings();break;case GamePage.Library:ShowTitle();break;case GamePage.Editing:LeaveEditor();break;case GamePage.Confirming:(confirmationCancel??CancelLeave)();break;case GamePage.Importing:CloseImport();break;}
        }
        public void Cue(string name){if(audioPlayer!=null)audioPlayer.Cue(name);}
        public void CueCircuits(ActionResult result){if(audioPlayer!=null)audioPlayer.Circuits(result);}
        public void CancelCircuitReplies(){audioPlayer?.CancelCircuitReplies();}
        public void OpenLibrary()=>LibraryPage(libraryPageIndex);
        public void LibraryPage(int page)
        {Navigate(()=>{Page=GamePage.Library;Session=null;Board.Clear();try{var list=Library.Read(out int invalid);int max=Math.Max(0,(list.Count-1)/GameUI.LibraryPageSize);ui.Library(list,libraryPageIndex=Mathf.Clamp(page,0,max),invalid,Library.Recovery()!=null);}catch(Exception e){OperationError("无法打开关卡库",e,ShowTitle);}});}
        public void SelectLibrary(string id){LibrarySelection=id;if(Page==GamePage.Library)ui.RefreshLibrarySelection(id);}
        private LevelData SelectedCustom()=>Library.Read(out _).FirstOrDefault(d=>d.id==LibrarySelection);
        private void OperationError(string heading,Exception error,Action back){confirmationCancel=back;Page=GamePage.Confirming;ui.OperationError(heading,error.Message,back);}
        private void LibraryOperation(string heading,Action action){try{action();}catch(Exception e){OperationError(heading,e,OpenLibrary);}}
        public void EditSelectedCustom(){LibraryOperation("无法打开关卡",()=>{var data=SelectedCustom();if(data==null)throw new FileNotFoundException("关卡文件已变化或无法读取，请返回关卡库检查。");Edit(data);});}
        public void ShowLibraryIssues(){Library.Read(out _);confirmationCancel=OpenLibrary;Page=GamePage.Confirming;ui.OperationError("无法读取的文件",string.Join("\n",Library.LastReadIssues.Take(5)),OpenLibrary);}
        public void DuplicateCustom(){LibraryOperation("无法复制关卡",()=>{var selected=SelectedCustom();if(selected==null)throw new FileNotFoundException("关卡已不存在。");var copy=Library.Duplicate(selected);LibrarySelection=copy.id;LibraryPage(Library.Read(out _).FindIndex(d=>d.id==copy.id)/GameUI.LibraryPageSize);});}
        public void ArchiveCustom()
        {
            var data=SelectedCustom();if(data==null)return;confirmationCancel=OpenLibrary;Page=GamePage.Confirming;
            ui.ConfirmRemove(data,()=>LibraryOperation("无法移除关卡",()=>{lastArchive=Library.Archive(data.id);LibrarySelection=null;OpenLibrary();}),OpenLibrary);
        }
        public void RestoreArchive(){var archive=ArchiveCandidate();if(archive==null)return;LibraryOperation("无法恢复关卡",()=>{var data=Library.PreviewImport(archive);Library.Restore(archive);lastArchive=null;LibrarySelection=data.id;OpenLibrary();});}
        public void SaveEditorCopy(){creator.CommitPending();try{var data=creator.Document.Data.Copy();data.id=Guid.NewGuid().ToString("N");data.title+=" · 副本";Library.Save(data);LibrarySelection=data.id;creator.Open(data);ui.CelebrateSave();}catch(Exception e){OperationError("副本未保存",e,CancelLeave);}}
        public void ExportEditor(){creator.CommitPending();ExportLevel(creator.Document.Data,CancelLeave);}
        public void ExportCustom(){var data=SelectedCustom();if(data!=null)ExportLevel(data,CloseLibraryOverlay);}
        private void ExportLevel(LevelData data,Action back)
        {try{ui.CommitText();string path=Library.Export(data);confirmationCancel=back;Page=GamePage.Confirming;ui.ExportResult(path,data.title,back);}catch(Exception e){OperationError("导出失败",e,back);}}
        public void OpenImport(){if(Page!=GamePage.Library||Transitioning)return;importRequest++;Page=GamePage.Importing;importData=null;input.Clear();ui.ImportPage(importPath);}
        public void CloseImport(){importRequest++;importData=null;CloseLibraryOverlay();}
        private void CloseLibraryOverlay()
        {
            input.Clear();Page=GamePage.Library;
            try{var list=Library.Read(out int invalid);libraryPageIndex=Mathf.Clamp(libraryPageIndex,0,Math.Max(0,(list.Count-1)/GameUI.LibraryPageSize));ui.Library(list,libraryPageIndex,invalid,Library.Recovery()!=null);}
            catch(Exception e){OperationError("无法打开关卡库",e,ShowTitle);}
        }
        public void SetImportPath(string value){if(importPath==value)return;importRequest++;importPath=value;importData=null;if(Page==GamePage.Importing)ui.ImportResult(null,"");}
        public async void BrowseImport()
        {
            if(Automated)return;
            int request=++importRequest;
            try{string path=await LevelFilePicker.PickAsync(false,Library.Folder);if(this==null||request!=importRequest||Page!=GamePage.Importing||string.IsNullOrEmpty(path))return;importPath=path;ui.SetImportPathText(path);PreviewImport();}
            catch(Exception e){if(this!=null&&Page==GamePage.Importing)ui.ImportResult(null,"无法选择文件："+e.Message);}
        }
        public void PreviewImport()
        {
            if(Page!=GamePage.Importing)return;
            try{ui.CommitText();importPath=ui.ImportPathText.Trim().Trim('"');importData=Library.PreviewImport(importPath);string warning=new EditDocument(importData).Validation();ui.ImportResult(importData,importData.title+(warning==null?"":" · "+warning));}
            catch(Exception e){importData=null;ui.ImportResult(null,e.Message);}
        }
        public void ConfirmImport(){if(importData==null)return;try{var data=Library.Import(importData);LibrarySelection=data.id;importData=null;Edit(data);}catch(Exception e){ui.ImportResult(importData,"导入失败："+e.Message);}}
        public void EditNew()=>Edit(LevelData.Blank());
        public void CopyCampaign()
        {
            if(Page!=GamePage.Library||Transitioning)return;
            confirmationCancel=CloseLibraryOverlay;Page=GamePage.Confirming;input.Clear();
            ui.CampaignCopyPicker(CopySelectedCampaign,CloseLibraryOverlay);
        }
        private void CopySelectedCampaign(string id)
        {
            if(Page!=GamePage.Confirming||!assets.Catalog.chapters.Any(c=>c.levels.Any(l=>l.id==id)))return;
            var data=assets.Level(id);
            if(assets.replays!=null)data.solution=JsonUtility.FromJson<ReplayCatalog>(assets.replays.text)?.levels?.FirstOrDefault(p=>p.id==id)?.route;
            data.id=Guid.NewGuid().ToString("N");data.title+=" · 副本";Edit(data);
        }
        public void Edit(LevelData data)=>Navigate(()=>{Page=GamePage.Editing;creator.Open(data);});
        public void RecoverDraft(){var data=Library.Recovery();if(data!=null)Edit(data);}
        public void StartEditorPlay(LevelData data)=>Navigate(()=>{creatorPlay=true;if(EditingOfficial)Campaign.StartTrial(data);Enter(data);});
        public void ReturnToEditor()=>Navigate(()=>{Page=GamePage.Editing;creatorPlay=false;Session=null;creator.Show();});
        public void LeaveEditor()
        {
            creator.CommitPending();
            if(creator.Document.Dirty){confirmationCancel=CancelLeave;Page=GamePage.Confirming;ui.ConfirmLeave(()=>{if(creator.Save())CloseBoard(false);else CancelLeave();},()=>CloseBoard(true),CancelLeave);}
            else CloseBoard(false);
        }
        public void ShowEditorHelp(){creator.CommitPending();confirmationCancel=CancelLeave;Page=GamePage.Confirming;ui.EditorHelp(CancelLeave);}
        private void CancelLeave(){Page=GamePage.Editing;creator.Show();input.Clear();}
        public void Exit(){Progress.Save();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
            #else
            Application.Quit();
            #endif
        }
        private void Update()
        {
            if(Board==null||Transitioning)return;
            if(Page==GamePage.Completing){if(Time.unscaledTime-completedAt>.68f){if(creatorPlay)ReturnToEditor();else NextLevel();}return;}
            if(Page==GamePage.Dying){if(!deathCue&&Time.unscaledTime-diedAt>=BoardView.DeathImpactSeconds){deathCue=true;Cue("burn");}if(Time.unscaledTime-diedAt>=BoardView.DeathSeconds){Page=GamePage.Dead;ui.DeathHint();}}
            if(Page==GamePage.Editing)return;
            var keyboard=Automated?testKeys:Application.isFocused?hardware:null;
            #if UNITY_EDITOR
            if(ui.ShowcaseKeys(keyboard)){input.Clear();return;}
            #endif
            if(ui.ProcessHoldInput(keyboard)){input.Clear();return;}
            if(keyboard==null)return;
            if(ui.HasTextFocus){if(keyboard.Pressed(KeyCode.Escape))ui.CommitText();return;}
#if UNITY_EDITOR
            if(Page==GamePage.Campaign&&Campaign.HandleKeys(keyboard)){input.Clear();return;}
#endif
            if(creatorPlay&&(Page==GamePage.Playing||Page==GamePage.Dead)&&keyboard.Pressed(KeyCode.F5)){ReturnToEditor();return;}
            bool puzzle=Page==GamePage.Playing||Page==GamePage.Dying||Page==GamePage.Dead;
            if(puzzle){var ready=history.Tick(Board.IsAnimating);if(ready.HasValue){Act(ready.Value);return;}}
            var key=input.Poll(keyboard,Time.unscaledTime,puzzle,Board.IsAnimating);
            if(!key.HasValue)return;
            if(puzzle)
            {
                if(key==StudyAction.Back){Back();return;}
                if(key==StudyAction.Undo||key==StudyAction.Reset){RequestHistory((Command)key.Value);return;}
                if(Page==GamePage.Playing&&(int)key.Value<4&&!history.HasPending)Act((Command)key.Value);
            }
            else ui.Handle(key.Value);
        }
    }
}
