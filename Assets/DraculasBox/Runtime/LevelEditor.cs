using System;
using System.Collections.Generic;
using System.Linq;
using DraculasBox.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    public sealed partial class LevelEditor:MonoBehaviour
    {
        private DraculaApp app;private GameUI ui;private BoardView board;
        private TMP_Text status,validation,dirtyMark;private TMP_InputField titleInput,widthInput,heightInput;
        private float nextRecovery,savedUntil,noticeUntil;private bool stroke,connected,previewQueued,panning;private Cell last;private Vector2 previousMouse;
        private readonly List<Button> brushes=new List<Button>();private readonly List<RaycastResult> hits=new List<RaycastResult>();private PointerEventData pointer;
        private readonly List<BoardView.StampCell> pendingStamps=new List<BoardView.StampCell>();
        public EditDocument Document {get;private set;}
        public Brush Selected {get;private set;}=Brush.Box;
        public int Channel {get;private set;}
        public void Initialize(DraculaApp owner){app=owner;ui=app.UI;board=app.Board;pointer=new PointerEventData(EventSystem.current);}
        public void Open(LevelData data)=>Open(data,app.Library.Read(out _).FirstOrDefault(d=>d.id==data.id));
        public void Open(LevelData data,LevelData baseline){ResetAuthoringTool();proofBoard=null;ClearSelection();FinishStroke();Operation=EditOperation.Paint;Document=new EditDocument(data,false);Document.SavedBaseline(baseline);Selected=Brush.Box;Shape=PaintShape.Brush;board.ResetEditorView();Show();}
        public void Show()=>BuildWorkshop();
        public void Select(Brush brush){if(brush==Brush.Erase){SetOperation(EditOperation.Erase);return;}ClosePanels();ClearSelection();CancelRectangle(false);FinishStroke();Selected=brush;Operation=EditOperation.Paint;RefreshOperation();RefreshShapeControls();RefreshChannelControls();RevealBrush(brush);}
        public void SelectChannel(int channel){CancelRectangle(false);FinishStroke();Channel=Mathf.Clamp(channel,0,3);board.EditorChannel=Channel;RefreshChannelControls();}
        private void RefreshLabels()
        {
            if(Document==null)return;if(dirtyMark!=null)dirtyMark.text=Document.Dirty?"*":"";
            string error=Document.Validation();if(validation!=null){validation.text=error??"";validation.gameObject.SetActive(error!=null&&(status==null||string.IsNullOrEmpty(status.text)));}ui.SetEnabled("test",error==null);ui.SetEnabled("edit-undo",Document.UndoCount>0);ui.SetEnabled("edit-redo",Document.RedoCount>0);
            ui.SetLabel("sun",Document.Data.sunEnabled?"阳光  开":"阳光  关");
            for(int i=0;i<4;i++){ui.SetEnabled("sun-dir-"+i,Document.Data.sunEnabled);ui.WorkshopChoice("sun-dir-"+i,Document.Data.sunEnabled&&i==(int)Document.Data.sunFrom);}
            ui.SetLabel("zoom-fit",Mathf.RoundToInt(board.EditorZoom*100)+"%");ui.SetLabel("save",!Document.Dirty&&Time.unscaledTime<savedUntil?"已保存":app.EditingOfficial?"保存草稿":"保存");
            if(titleInput!=null&&!titleInput.isFocused)titleInput.SetTextWithoutNotify(Document.Data.title);
            if(widthInput!=null&&!widthInput.isFocused)widthInput.SetTextWithoutNotify(Document.Data.rows[0].Length.ToString());if(heightInput!=null&&!heightInput.isFocused)heightInput.SetTextWithoutNotify(Document.Data.rows.Length.ToString());
            Cell? invalid=null;if(error!=null&&board.State!=null&&board.State.Level.Floor(Document.Data.player)&&!LightField.Compute(board.State).Safe(Document.Data.player))invalid=Document.Data.player;board.HighlightInvalid(invalid);
        }
        private void Notice(string text){if(status!=null)status.text=text;if(validation!=null)validation.gameObject.SetActive(string.IsNullOrEmpty(text)&&Document.Validation()!=null);noticeUntil=Time.unscaledTime+3;}
        public void CommitPending(){CancelSelectionGesture();FinishStroke();ui.CommitText();FlushPreview();}
        public bool Save()
        {
            CommitPending();bool changed=Document.Dirty;try{if(app.EditingOfficial)app.Campaign.StageBoard(Document.Data);else app.Library.Save(Document.Data);}catch(Exception e){Notice("保存失败："+e.Message);noticeUntil=0;return false;}
            Document.Saved();try{if(!app.EditingOfficial)app.Library.DiscardRecovery(Document.Data.id);Notice("");}catch(Exception e){Notice("关卡已保存，草稿未清理："+e.Message);noticeUntil=0;}savedUntil=Time.unscaledTime+.95f;RefreshLabels();if(changed)ui.CelebrateSave();return true;
        }
        public void Play(){CommitPending();string error=Document.Validation();if(error!=null){Notice(error);return;}app.StartEditorPlay(Document.Data.Copy());}
        public void Undo(){if(CancelSelectionGesture()||CancelRectangle())return;ClearSelection();FinishStroke();if(Document.Undo()){pendingStamps.Clear();board.ClearEditorStamps();RefreshDocument();}}
        public void Redo(){if(CancelSelectionGesture()||CancelRectangle())return;ClearSelection();FinishStroke();if(Document.Redo()){pendingStamps.Clear();board.ClearEditorStamps();RefreshDocument();}}
        public bool ResizeTo(int width,int height){ClearSelection();FinishStroke();string error=Document.ResizeError(width,height);if(error!=null){Notice(error);return false;}Document.Resize(width,height);RefreshDocument();return true;}
        private void Dimension(string value,bool width){if(int.TryParse(value,out var size))ResizeTo(width?size:Document.Data.rows[0].Length,width?Document.Data.rows.Length:size);else Notice("请输入 3–32 的整数。");(width?widthInput:heightInput)?.SetTextWithoutNotify((width?Document.Data.rows[0].Length:Document.Data.rows.Length).ToString());}
        private void Resize(int dx,int dy)=>ResizeTo(Document.Data.rows[0].Length+dx,Document.Data.rows.Length+dy);
        private void Zoom(float scale){board.ZoomAt(board.Screen(new Cell(Document.Data.rows[0].Length/2,Document.Data.rows.Length/2)),scale);RefreshLabels();}
        private void RefreshDocument(){previewQueued=true;FlushPreview();RefreshLabels();}
        public void FlushPreview(){if(!previewQueued)return;previewQueued=false;board.Preview(Document.Data,true);if(pendingStamps.Count>0){board.StampCells(pendingStamps);pendingStamps.Clear();}RefreshLabels();}
        private void FinishStroke(){if(RegionActive){CommitRectangle();return;}if(stroke){stroke=false;connected=false;Document.End();RefreshLabels();}}
        private bool OverUI(Vector2 screen){hits.Clear();pointer.position=screen;EventSystem.current.RaycastAll(pointer,hits);return hits.Count>0;}
        public Brush PickAt(Vector2 screen)
        {
            if(OverUI(screen)||!board.CellAt(screen,out var p))return Selected;var data=Document.Data;Brush brush=data.player==p?Brush.Player:data.coffin==p?Brush.Coffin:Brush.Floor;
            foreach(var item in data.pieces)if(item.cell==p)brush=item.kind==PieceKind.Box?Brush.Box:item.kind==PieceKind.Lamp?Brush.Lamp:item.kind==PieceKind.MirrorSlash?Brush.MirrorSlash:Brush.MirrorBackslash;
            if(brush==Brush.Floor){foreach(var device in data.plates)if(device.cell==p){brush=Brush.Plate;SelectChannel(device.channel);}foreach(var device in data.doors)if(device.cell==p){brush=Brush.Door;SelectChannel(device.channel);}}
            if(data.rows[p.y][p.x]=='#')brush=Brush.Wall;else if(data.rows[p.y][p.x]==' ')brush=Brush.Void;Select(brush);return brush;
        }
        public bool PointerSample(Vector2 screen,bool begin,bool held,bool end,bool erase=false,bool rectangle=false)
        {
            if(ToolOpen||panning)return false;
            if(begin&&!OverUI(screen)&&ClosePanels())return false;
            if(Operation==EditOperation.Select){if(erase){ClearSelection();return true;}return SelectionSample(screen,begin,held,end);}
            erase|=Operation==EditOperation.Erase;
            if(RegionActive||begin&&((Shape==PaintShape.Rectangle&&CanRectangle)||rectangle)&&(erase||CanRectangle))return RectangleSample(screen,begin,held,end,erase);
            if(end){FinishStroke();return true;}if(!begin&&!held)return false;if(OverUI(screen)||!board.CellAt(screen,out var cell)){connected=false;return false;}
            if(!begin&&stroke&&connected&&cell==last)return false;if(begin){FinishStroke();Document.Begin();stroke=true;connected=false;}if(!stroke)return false;if(!connected)last=cell;
            var brush=erase?Brush.Erase:Selected;bool painted=false;int steps=Mathf.Max(Mathf.Abs(cell.x-last.x),Mathf.Abs(cell.y-last.y));
            for(int k=steps==0?0:1;k<=steps;k++){float t=steps==0?0:(float)k/steps;var p=new Cell(Mathf.RoundToInt(Mathf.Lerp(last.x,cell.x,t)),Mathf.RoundToInt(Mathf.Lerp(last.y,cell.y,t)));bool applied=Document.Paint(p,brush,Channel);painted|=applied;if(applied&&brush!=Brush.Erase)pendingStamps.Add(new BoardView.StampCell(p,brush));}
            last=cell;connected=true;if(painted){previewQueued=true;Notice("");}else if(begin)Notice("请先移开物件，或选择空地板。");return painted;
        }
        public bool PanSample(Vector2 screen,bool begin,bool held,bool end)
        {
            if(ToolOpen)return false;
            if(begin)
            {
                if(OverUI(screen)||!board.InViewport(screen))return false;
                ui.CommitText();ClosePanels();CancelSelectionGesture();
                if(!CancelRectangle(false))FinishStroke();
                panning=true;previousMouse=screen;
                board.BrushPreview(null,Selected,true);
            }
            if(!panning)return false;
            if(held||end){board.Pan(screen-previousMouse);previousMouse=screen;}
            if(end)panning=false;
            return true;
        }
        private bool CanPreview(Cell p)
        {
            var d=Document.Data;bool device=d.plates.Any(e=>e.cell==p)||d.doors.Any(e=>e.cell==p);bool occupied=d.player==p||d.coffin==p||d.pieces.Any(e=>e.cell==p);if(EffectiveBrush==Brush.Erase||Selected==Brush.Floor)return true;if(Selected==Brush.Wall||Selected==Brush.Void)return !occupied&&!device;if(Selected==Brush.Plate||Selected==Brush.Door)return d.rows[p.y][p.x]=='.'&&d.coffin!=p;return d.rows[p.y][p.x]=='.'&&!occupied;
        }
        private void LateUpdate(){if(app!=null&&app.Page==GamePage.Editing){FlushPreview();if(RegionActive)board.RegionPreview(regionStart,regionEnd,regionBrush==Brush.Erase);else RefreshSelectionPreview();}}
        private void Update()
        {
            if(app==null||app.Page!=GamePage.Editing||app.Transitioning)return;
            if(Document.Dirty&&Time.unscaledTime>nextRecovery&&!stroke){nextRecovery=Time.unscaledTime+15;try{app.RecoverBoard(Document.Data);}catch(Exception e){Notice("恢复记录未写入："+e.Message);}}
            if(ToolOpen){UpdateAuthoringTool();return;}
            if(savedUntil>0&&Time.unscaledTime>savedUntil){savedUntil=0;RefreshLabels();}if(noticeUntil>0&&Time.unscaledTime>noticeUntil){noticeUntil=0;if(status!=null)status.text="";}
            if(app.Automated)return;if(!Application.isFocused){CancelSelectionGesture();if(!CancelRectangle(false))FinishStroke();panning=false;board.BrushPreview(null,Selected,true);return;}
            if(PanSample(Input.mousePosition,Input.GetMouseButtonDown(2),Input.GetMouseButton(2),Input.GetMouseButtonUp(2)))return;
            bool control=Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl);
            foreach(var key in ShortcutKeys)if(Input.GetKeyDown(key)&&HandleShortcut(key,control))return;
            if(ui.HasTextFocus){if(Input.GetKeyDown(KeyCode.Escape))ui.CommitText();return;}
            if(Input.GetKeyDown(KeyCode.Escape)){if(ClosePanels()||CancelSelectionGesture()||ClearSelection()||CancelRectangle())return;FinishStroke();app.LeaveEditor();return;}
            Vector2 mouse=Input.mousePosition;bool over=OverUI(mouse);if(Operation!=EditOperation.Select&&!over&&board.CellAt(mouse,out var hover)){bool valid=CanPreview(hover);board.BrushPreview(hover,EffectiveBrush,valid);board.DeviceGhost(EffectiveBrush,valid);}else board.BrushPreview(null,Selected,true);
            if(!over&&board.InViewport(mouse)&&Mathf.Abs(Input.mouseScrollDelta.y)>.01f){CancelSelectionGesture();if(!CancelRectangle(false))FinishStroke();board.ZoomAt(mouse,Mathf.Pow(1.15f,Input.mouseScrollDelta.y));RefreshLabels();}
            if(Input.GetMouseButtonDown(0)&&(Input.GetKey(KeyCode.LeftAlt)||Input.GetKey(KeyCode.RightAlt))){PickAt(mouse);return;}
            bool down=Input.GetMouseButtonDown(0)||Input.GetMouseButtonDown(1),held=Input.GetMouseButton(0)||Input.GetMouseButton(1),up=Input.GetMouseButtonUp(0)||Input.GetMouseButtonUp(1);if(down||held||up)PointerSample(mouse,down,held,up,Input.GetMouseButton(1),Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift));
        }
        private static readonly KeyCode[] ShortcutKeys={KeyCode.S,KeyCode.F5,KeyCode.Z,KeyCode.Y,KeyCode.A,KeyCode.B,KeyCode.X,KeyCode.P,KeyCode.R,KeyCode.Delete,KeyCode.Backspace,KeyCode.LeftBracket,KeyCode.RightBracket,
            KeyCode.Alpha1,KeyCode.Alpha2,KeyCode.Alpha3,KeyCode.Alpha4,KeyCode.Alpha5,KeyCode.Alpha6,KeyCode.Alpha7,KeyCode.Alpha8,KeyCode.Alpha9,KeyCode.Alpha0,KeyCode.Q,KeyCode.E};
        // The ordinary keyboard path and isolated checks use the same dispatcher.
        public bool HandleShortcut(KeyCode key,bool control=false)
        {
            if(Document==null||ToolOpen)return false;
            if(control&&key==KeyCode.S){Save();return true;}
            if(key==KeyCode.F5){Play();return true;}
            if(ui.HasTextFocus)return false;
            if(key==KeyCode.Z){Undo();return true;}if(key==KeyCode.Y){Redo();return true;}
            if(control)return false;
            if(SelectionShortcut(key))return true;
            if(key==KeyCode.A){SetOperation(EditOperation.Select);return true;}
            if(key==KeyCode.Q||key==KeyCode.B){SetOperation(EditOperation.Paint);return true;}
            if(key==KeyCode.E||key==KeyCode.X){SetOperation(EditOperation.Erase);return true;}
            if(key==KeyCode.P){SelectShape(PaintShape.Brush);return true;}
            if(key==KeyCode.R){if(CanRectangle)SelectShape(PaintShape.Rectangle);return true;}
            if(key>=KeyCode.Alpha1&&key<=KeyCode.Alpha9){Select((Brush)((int)key-(int)KeyCode.Alpha1));return true;}
            if(key==KeyCode.Alpha0){Select(Brush.Door);return true;}
            if(key==KeyCode.LeftBracket){Select(Brush.MirrorSlash);return true;}
            if(key==KeyCode.RightBracket){Select(Brush.MirrorBackslash);return true;}
            return false;
        }
        private void OnDisable(){ResetAuthoringTool();CancelSelectionGesture();panning=false;if(!CancelRectangle(false))FinishStroke();}
    }
}
