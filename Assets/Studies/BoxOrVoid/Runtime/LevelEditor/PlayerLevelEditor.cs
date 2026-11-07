using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BoxOrVoid.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BoxOrVoid.Study.LevelEditing
{
    public enum CreatorPage { Library,Editing,Playing }
    public sealed partial class PlayerLevelEditor:MonoBehaviour
    {
        [SerializeField] private StudyAssets assets;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Canvas canvas;
        [SerializeField] private GameObject background;
        [SerializeField] private RectTransform editRoot,libraryRoot,playRoot,modalRoot,stackRoot,selectionRoot;
        [SerializeField] private EditorBoardCanvas board,ghost;
        [SerializeField] private EditorGridGraphic grid;
        [SerializeField] private Image hover;
        [SerializeField] private EditorCornerGraphic cursor;
        [SerializeField] private TMP_Text toast,tooltip;
        private PuzzleApp app;
        private readonly Dictionary<string,EditorWidget> widgets=new Dictionary<string,EditorWidget>();
        private readonly Dictionary<string,TMP_Text> labels=new Dictionary<string,TMP_Text>();
        private readonly HashSet<int> strokeCells=new HashSet<int>();
        private readonly List<Command> playCommands=new List<Command>();
        private List<LibraryEntry> entries=new List<LibraryEntry>();
        private SelectionStamp clipboard,paste;
        private bool movePaste,cutClipboard,drawing,panning,stackOpen,recycled,solidRectangle;
        private int libraryIndex,libraryPage,hoverCell=-1,startCell=-1,lastCell=-1,layerPage;
        private float lastEditedAt,toastUntil,lastAutoSavedAt;
        private int savedRevision=-1;
        private Vector2 panOrigin,pointerOrigin;
        private string playHash;
        private TMP_InputField dialogInput;
        private Action dialogCancel;
        private StandaloneInputModule inputModule;
        private bool initialized;
        public LevelEditSession Session {get;private set;}
        public UserLevelLibrary Library {get;private set;}
        public CreatorPage Page {get;private set;}
        public bool IsOpen=>canvas!=null&&canvas.gameObject.activeSelf;
        public bool ModalOpen=>modalRoot!=null&&modalRoot.gameObject.activeSelf;
        public bool CanInteract=>initialized&&!pickerOpen&&!app.IsTransitioning;
        public Canvas Canvas=>canvas;
        public EditorBoardCanvas BoardCanvas=>board;
        public void Configure(StudyAssets source,Camera camera,Canvas ui,GameObject bg,RectTransform edit,RectTransform library,RectTransform play,RectTransform modal,EditorBoardCanvas tiles,EditorGridGraphic lines,EditorBoardCanvas preview,Image hoverImage,EditorCornerGraphic selected,RectTransform stack,RectTransform selection,TMP_Text notice,TMP_Text help)
        {assets=source;viewCamera=camera;canvas=ui;background=bg;editRoot=edit;libraryRoot=library;playRoot=play;modalRoot=modal;board=tiles;grid=lines;ghost=preview;hover=hoverImage;cursor=selected;stackRoot=stack;selectionRoot=selection;toast=notice;tooltip=help;}
        public void Initialize(PuzzleApp host,string folder)
        {
            app=host;Library=new UserLevelLibrary(Path.Combine(folder,"UserLevels"));
            if(canvas==null)throw new InvalidOperationException("The scene needs its authored player editor UI.");
            foreach(var w in canvas.GetComponentsInChildren<EditorWidget>(true)){widgets[w.Id]=w;w.Bind(this);}
            foreach(var text in canvas.GetComponentsInChildren<TMP_Text>(true))if(!labels.ContainsKey(text.name))labels[text.name]=text;
            editRoot.GetComponentInChildren<EditorBoardPointer>(true).Bind(this);
            var system=EventSystem.current;
            if(system==null){var go=new GameObject("Creator EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));go.transform.SetParent(transform,false);system=go.GetComponent<EventSystem>();}
            inputModule=system.GetComponent<StandaloneInputModule>();
            if(inputModule!=null)inputModule.enabled=!app.TestInputOnly;
            app.ActionApplied+=ObservePlay;initialized=true;Hide();
        }
        private void OnDestroy(){if(app!=null)app.ActionApplied-=ObservePlay;}
        private void OnApplicationQuit(){AutoSave();}
        private void OnApplicationFocus(bool focus){if(!focus){CancelGesture();AutoSave();}}
        public void Hide(){if(canvas!=null)canvas.gameObject.SetActive(false);CloseDialog();}
        public void OpenLibrary()
        {canvas.gameObject.SetActive(true);Page=CreatorPage.Library;recycled=false;SetRoots();RefreshLibrary();}
        public void ShowEditing()
        {Page=CreatorPage.Editing;canvas.gameObject.SetActive(true);CloseDialog();SetRoots();Refresh();}
        public void ShowPlaytest()
        {Page=CreatorPage.Playing;CloseDialog();SetRoots();FindText(playRoot,"Play title").text=Session.Document.name+" · 试玩";}
        private void SetRoots()
        {background.SetActive(Page!=CreatorPage.Playing);editRoot.gameObject.SetActive(Page==CreatorPage.Editing);libraryRoot.gameObject.SetActive(Page==CreatorPage.Library);playRoot.gameObject.SetActive(Page==CreatorPage.Playing);tooltip.text="";toast.text="";}
        public void OpenDocument(LevelDocument d,bool saved=true)
        {
            if(Session!=null)Session.Changed-=Edited;
            Session=new LevelEditSession(d,saved);Session.Changed+=Edited;savedRevision=-1;lastEditedAt=Time.unscaledTime;
            stackOpen=drawing=panning=false;paste=null;clipboard=null;movePaste=cutClipboard=false;layerPage=0;hoverCell=-1;ShowEditing();
        }
        private void Edited(){lastEditedAt=Time.unscaledTime;Refresh();}
        private void Update()
        {
            if(!initialized||!IsOpen)return;
            if(Time.unscaledTime>toastUntil)toast.text="";
            if(Session!=null&&Session.Dirty&&Session.Revision!=savedRevision&&Time.unscaledTime-lastEditedAt>2&&Time.unscaledTime-lastAutoSavedAt>3)AutoSave();
            if(!CanInteract)return;
            var keys=app.EditorKeyboard;if(keys==null)return;
            if(Page==CreatorPage.Playing&&keys.Pressed(KeyCode.F5)){Act("return-edit");return;}
            bool typing=EventSystem.current!=null&&EventSystem.current.currentSelectedGameObject!=null&&EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>()!=null;
            if(Page==CreatorPage.Library&&!ModalOpen&&!typing)
            {
                if (!recycled && keys.Pressed(KeyCode.V) && (keys.Held(KeyCode.LeftControl)||keys.Held(KeyCode.RightControl)))
                { OpenImportPath(ClipboardText()); return; }
                if(!recycled && keys.Pressed(KeyCode.N)){NewDialog();return;}
                if(keys.Pressed(KeyCode.LeftArrow)||keys.Pressed(KeyCode.A)){SelectLibrary(-1);return;}
                if(keys.Pressed(KeyCode.RightArrow)||keys.Pressed(KeyCode.D)){SelectLibrary(1);return;}
                if(keys.Pressed(KeyCode.Return)||keys.Pressed(KeyCode.Space)){Act("open");return;}
            }
            string action=EditorShortcuts.Poll(keys,typing,ModalOpen);
            if(Page==CreatorPage.Playing){if(action=="play"||action=="back")Act("return-edit");return;}
            if(action!=null&&(Page==CreatorPage.Editing||action=="back"||action=="help"))Act(action);
        }
        public void Cue(){app.PlayCue("uiSubmit");}
        public void Tooltip(string value){if(!ModalOpen)tooltip.text=value;}
        public void Notice(string value){toast.text=value;toastUntil=Time.unscaledTime+2.7f;}
        private bool Save()
        {
            if(Session==null)return true;EndStroke();
            try{Library.Save(Session.Document);Session.MarkSaved();savedRevision=Session.Revision;RefreshStatus();Notice("关卡已保存");return true;}
            catch(Exception ex)when(UserLevelLibrary.IsFileFailure(ex)){Message("保存未完成",ex.Message+"\n修改仍保留在当前关卡中。");return false;}
        }
        private void AutoSave()
        {
            if(!initialized||Session==null||!Session.Dirty||Session.IsDrawing)return;
            lastAutoSavedAt=Time.unscaledTime;
            try{Library.AutoSave(Session.Document);savedRevision=Session.Revision;}
            catch(Exception ex)when(UserLevelLibrary.IsFileFailure(ex)){Notice("自动保存暂不可用，请手动保存或导出");}
        }
        private void LeaveEditing(Action next)
        {
            CancelGesture();
            if(Session==null||!Session.Dirty){next();return;}
            DrawDialog("离开编辑？","「"+Session.Document.name+"」有未保存的修改。",244,new DialogChoice("继续编辑",CloseDialog),new DialogChoice("保存并返回",()=>{if(Save()){CloseDialog();next();}},true),new DialogChoice("放弃修改",()=>{try{Library.RemoveRecovery(Session.Document.id);}catch(Exception ex)when(UserLevelLibrary.IsFileFailure(ex)){Message("暂时无法清理草稿",ex.Message);return;}CloseDialog();Session=null;next();}));
        }
        public void Act(string action)
        {
            if(!CanInteract)return;
            if(action=="back")
            {
                if(ModalOpen){CloseDialog();return;}
                if(Page==CreatorPage.Playing){app.ReturnToCreator();return;}
                if(paste!=null||drawing||panning){CancelGesture();Refresh();return;}
                if(stackOpen){stackOpen=false;Refresh();return;}
                if(Session!=null&&Session.HasSelection&&Page==CreatorPage.Editing){Session.HasSelection=false;Refresh();return;}
                if(Page==CreatorPage.Editing){LeaveEditing(()=>app.OpenCreatorLibrary());return;}
                if(recycled){recycled=false;RefreshLibrary();return;}app.CloseCreator();return;
            }
            if(action=="return-edit"){app.ReturnToCreator();return;}
            if(action=="home"){app.CloseCreator();return;}
            if(action=="help"){HelpDialog();return;}
            if(action=="import"){ImportPathDialog();return;}
            if(action=="recycle"){recycled=!recycled;libraryPage=libraryIndex=0;RefreshLibrary();return;}
            if(action=="library-prev"||action=="library-next"){libraryPage=Mathf.Clamp(libraryPage+(action=="library-prev"?-1:1),0,Math.Max(0,(entries.Count-1)/3));libraryIndex=Math.Min(libraryPage*3,entries.Count);RefreshLibrary(false);return;}
            if(action=="open"){OpenSelected();return;}
            if(action=="level-options"){LibraryOptions();return;}
            if(Session==null)return;
            if(action.StartsWith("brush-"))
            {int index=int.Parse(action.Substring(6));Session.Brush=new[]{ObjectKind.Wall,ObjectKind.Box,ObjectKind.Player,ObjectKind.BoxGoal,ObjectKind.PlayerGoal,ObjectKind.Wall}[index];Session.Tone=index==5?Tone.Gray:Session.Tone==Tone.Gray?Tone.White:Session.Tone;Session.Tool=EditTool.Place;Session.Repeat=false;stackOpen=false;Refresh();return;}
            if(action.StartsWith("tool-"))
            {Session.Tool=(EditTool)Enum.Parse(typeof(EditTool),action.Substring(5),true);if(Session.Tool==EditTool.Fill)Session.Brush=ObjectKind.Wall;Session.Repeat=false;stackOpen=false;paste=null;Refresh();return;}
            switch(action)
            {
                case "undo":drawing=panning=false;paste=null;cutClipboard=false;Session.Undo();break;case "redo":CancelGesture();Session.Redo();break;
                case "save":Save();break;
                case "play":StartPlay();break;
                case "more":MoreDialog();break;case "tools":ToolsDialog();break;
                case "tone":Session.Tone=Session.Tone==Tone.White?Tone.Black:Tone.White;break;
                case "tone-white":Session.Tone=Tone.White;break;case "tone-black":Session.Tone=Tone.Black;break;
                case "view":Session.ViewTone=Session.ViewTone==Tone.White?Tone.Black:Tone.White;break;
                case "grid":Session.Grid=!Session.Grid;break;case "fit":Session.Zoom=1;Session.Pan=Vector2.zero;break;
                case "rename":RenameDialog();break;
                case "copy":clipboard=Session.CopySelection();cutClipboard=false;Notice(clipboard==null?"先框选需要复制的区域":"选区已复制，Ctrl+V 放置");break;
                case "cut":clipboard=Session.CopySelection();cutClipboard=clipboard!=null;if(clipboard!=null){paste=clipboard;movePaste=true;Notice("选择目标位置，单击移动；Esc 取消");}else Notice("先框选需要移动的区域");break;
                case "paste":if(clipboard!=null){paste=clipboard;movePaste=cutClipboard;Notice("单击放置 · Esc 取消");}else Notice("先复制一片区域");break;
                case "rotate":TransformSelection(true,false,false);break;
                case "mirror-x":TransformSelection(false,true,false);break;
                case "mirror-y":TransformSelection(false,false,true);break;
                case "select-all":Session.Select(0,Session.Document.level.grid.Length-1);Session.Tool=EditTool.Select;break;
                case "delete":if(stackOpen)Session.RemoveLayer(Session.SelectedCell,Session.SelectedLayer);else Session.ClearSelected();break;
                case "lift":Session.MoveLayer(Session.SelectedCell,Session.SelectedLayer,1);break;
                case "lower":Session.MoveLayer(Session.SelectedCell,Session.SelectedLayer,-1);break;
                case "recolor":Session.RecolorLayer(Session.SelectedCell,Session.SelectedLayer);if(!string.IsNullOrEmpty(Session.LastMessage))Notice(Session.LastMessage);break;
                case "close-stack":stackOpen=false;break;
            }
            if(Page==CreatorPage.Editing)Refresh();
        }
        private void TransformSelection(bool rotate,bool mx,bool my)
        {if(paste==null){paste=Session.CopySelection();movePaste=true;}if(paste!=null){paste=paste.Transform(rotate,mx,my);Notice("预览已更新 · 单击放置 · Esc 取消");}else Notice("先框选需要变换的区域");}
        private void StartPlay()
        {
            EndStroke();paste=null;
            var problems=LevelValidation.ForPlay(Session.Document);
            if(problems.Count>0){ProblemDialog(problems);return;}
            AutoSave();playCommands.Clear();playHash=Session.Document.LayoutHash();app.StartCreatorPlay(Session.Document.level,Session.Document.theme,Session.Document.name);
        }
        private void ObservePlay(Command command,ActionResult result)
        {if(IsOpen&&Page==CreatorPage.Playing)playCommands.Add(command);}
        public void PlayedThrough()
        {
            if(Session.Document.LayoutHash()==playHash){Session.RecordCompletion(playHash,string.Join(",",playCommands.Select(c=>(int)c)));lastEditedAt=Time.unscaledTime;AutoSave();}
            Dialog("关卡完成",Session.Document.name,new DialogChoice("返回编辑",()=>app.ReturnToCreator(),true),new DialogChoice("再试一次",()=>{CloseDialog();app.RestartCreatorPlay();}));
        }
        private static TMP_Text FindText(Transform root,string name)=>root.GetComponentsInChildren<TMP_Text>(true).First(t=>t.name==name);
        private void RefreshStatus(){if(Session==null)return;widgets["rename"].Label.text=Session.Document.name;FindText(editRoot,"Save state").text=Session.Dirty?"未保存":"已保存";widgets["undo"].Enable(Session.CanUndo);widgets["redo"].Enable(Session.CanRedo);}
    }
}
