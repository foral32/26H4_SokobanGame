using DraculasBox.Core;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class LevelEditor
    {
        public EditSelection? Selection {get;private set;}
        private bool selecting,movingSelection,selectionValid;
        private Cell selectionOrigin,selectionEnd,selectionDelta;
        public bool SelectionGesture=>selecting||movingSelection;
        private string selectionError;
        public bool ClearSelection()
        {
            bool any=Selection.HasValue||SelectionGesture;
            CancelSelectionGesture();Selection=null;board.RegionPreview(null,null);return any;
        }
        private bool CancelSelectionGesture()
        {
            if(!SelectionGesture)return false;
            selecting=movingSelection=false;board.Preview(Document.Data,true);RefreshSelectionPreview();return true;
        }
        private void RefreshSelectionPreview()
        {
            if(Operation!=EditOperation.Select||Document==null)return;
            if(selecting)board.RegionPreview(selectionOrigin,selectionEnd);
            else if(movingSelection&&Selection.HasValue)
            {var s=Selection.Value.Offset(selectionDelta);board.RegionPreview(s.Min,s.Max,false,selectionValid);}
            else if(Selection.HasValue){var s=Selection.Value;board.RegionPreview(s.Min,s.Max);}
            else board.RegionPreview(null,null);
        }
        public bool SelectionSample(Vector2 screen,bool begin,bool held,bool end)
        {
            bool onBoard=!OverUI(screen)&&board.CellAt(screen,out _);
            if(end)
            {
                if(!SelectionGesture)return false;
                if(!onBoard){CancelSelectionGesture();return true;}
                if(movingSelection&&Selection.HasValue)
                {
                    if(selectionValid&&Document.MoveSelection(Selection.Value,selectionDelta,out var error))
                    {Selection=Selection.Value.Offset(selectionDelta);Notice("");}
                    else if(!selectionValid)Notice(selectionError);
                }
                else if(selecting)Selection=selectionOrigin==selectionEnd?Document.PickSelection(selectionOrigin):new EditSelection(selectionOrigin,selectionEnd);
                selecting=movingSelection=false;RefreshDocument();RefreshSelectionPreview();return true;
            }
            if(!onBoard)return false;
            board.CellAt(screen,out var cell);
            if(begin)
            {
                if(SelectionGesture)CancelSelectionGesture();FinishStroke();ui.CommitText();
                selectionOrigin=selectionEnd=cell;selectionDelta=new Cell(0,0);selectionValid=true;selectionError=null;
                if(Selection.HasValue&&Selection.Value.Contains(cell))movingSelection=true;
                else
                {
                    var picked=Document.PickSelection(cell);
                    if(picked.Layer!=SelectionLayer.Region){Selection=picked;movingSelection=true;}
                    else{Selection=null;selecting=true;}
                }
                board.BrushPreview(null,Selected,true);
            }
            if(held&&movingSelection&&Selection.HasValue)
            {
                var delta=new Cell(cell.x-selectionOrigin.x,cell.y-selectionOrigin.y);
                if(delta!=selectionDelta)
                {
                    selectionDelta=delta;selectionError=Document.PreviewMove(Selection.Value,delta,out var preview);selectionValid=selectionError==null;
                    board.Preview(selectionValid?preview:Document.Data,true);
                }
            }
            else if(held&&selecting)selectionEnd=cell;
            RefreshSelectionPreview();return SelectionGesture;
        }
        private bool SelectionShortcut(KeyCode key)
        {
            if(Operation!=EditOperation.Select||!Selection.HasValue)return false;
            if(key==KeyCode.Delete||key==KeyCode.Backspace)
            {CancelSelectionGesture();Document.DeleteSelection(Selection.Value);Selection=null;RefreshDocument();board.RegionPreview(null,null);return true;}
            return false;
        }
    }
}
