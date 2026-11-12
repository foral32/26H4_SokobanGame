using DraculasBox.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    public sealed partial class LevelEditor
    {
        private RectTransform lineControls;private Button lineAdd,lineRemove;
        private bool lineIsColumn;private int lineIndex;
        private void BuildLineControls()
        {
            lineControls=ui.Rect("Row column controls",Vector2.zero,new Vector2(74,36));
            lineAdd=ui.EditorButton("line-insert","+",new Vector2(19,0),new Vector2(32,32),()=>ChangeLine(lineIsColumn,lineIndex+1,true),false,lineControls);
            lineRemove=ui.EditorButton("line-delete","−",new Vector2(-19,0),new Vector2(32,32),()=>ChangeLine(lineIsColumn,lineIndex,false),false,lineControls);
            lineControls.gameObject.SetActive(false);
        }
        private void HideLineControls()
        {
            if(lineControls!=null)lineControls.gameObject.SetActive(false);
            board?.LinePreview(false,-1,false);
        }
        private Vector2 CanvasUI(Vector2 screen)
        {RectTransformUtility.ScreenPointToLocalPointInRectangle(ui.Root,screen,app.viewCamera,out var local);return local;}
        public void ShowLineControls(bool column,int index)
        {
            if(lineControls==null||Document==null)return;
            int count=column?Document.Data.rows[0].Length:Document.Data.rows.Length;
            if(index< -1||index>=count){HideLineControls();return;}
            lineIsColumn=column;lineIndex=index;
            var origin=CanvasUI(board.Screen(new Cell(0,0)));
            var next=CanvasUI(board.Screen(new Cell(1,1)));float cell=next.x-origin.x;
            var position=column?new Vector2(origin.x+index*cell,origin.y+cell*.5f+23):new Vector2(origin.x-cell*.5f-23,origin.y-index*cell);
            var viewport=new Rect(paletteCollapsed?-594:-462,-284,paletteCollapsed?1188:1056,512);
            if(column&&(position.y<viewport.yMin+18||position.y>viewport.yMax-18)||!column&&(position.x<viewport.xMin+18||position.x>viewport.xMax-18))
            {HideLineControls();return;}
            position.x=Mathf.Clamp(position.x,viewport.xMin+38,viewport.xMax-38);
            position.y=Mathf.Clamp(position.y,viewport.yMin+38,viewport.yMax-38);
            lineControls.anchoredPosition=position;lineControls.sizeDelta=column?new Vector2(74,36):new Vector2(36,74);
            ((RectTransform)lineAdd.transform).anchoredPosition=column?new Vector2(19,0):new Vector2(0,-19);
            ((RectTransform)lineRemove.transform).anchoredPosition=column?new Vector2(-19,0):new Vector2(0,19);
            lineControls.gameObject.SetActive(true);
            lineRemove.gameObject.SetActive(index>=0);
            ui.SetEnabled("line-insert",Document.LineChangeError(column,index+1,true)==null);
            ui.SetEnabled("line-delete",index>=0&&Document.LineChangeError(column,index,false)==null);
        }
        private void UpdateLineControls(Vector2 mouse)
        {
            if(ToolOpen||SceneOpen||filesPanel!=null&&filesPanel.gameObject.activeSelf||ui.HasTextFocus||stroke||RegionActive||SelectionGesture||panning)
            {HideLineControls();return;}
            if(lineControls!=null&&lineControls.gameObject.activeSelf&&RectTransformUtility.RectangleContainsScreenPoint(lineControls,mouse,app.viewCamera))
            {
                bool remove=lineIndex>=0&&RectTransformUtility.RectangleContainsScreenPoint((RectTransform)lineRemove.transform,mouse,app.viewCamera);
                board.LinePreview(lineIsColumn,remove?lineIndex:lineIndex+1,!remove);return;
            }
            if(OverUI(mouse)||!board.InViewport(mouse)){HideLineControls();return;}
            var p=CanvasUI(mouse);var origin=CanvasUI(board.Screen(new Cell(0,0)));var next=CanvasUI(board.Screen(new Cell(1,1)));float cell=next.x-origin.x;
            float left=origin.x-cell*.5f,top=origin.y+cell*.5f;
            int width=Document.Data.rows[0].Length,height=Document.Data.rows.Length;
            if(p.y>top+3&&p.y<top+41&&p.x>=left-cell&&p.x<left+width*cell)
                ShowLineControls(true,Mathf.FloorToInt((p.x-left)/cell));
            else if(p.x<left-3&&p.x>left-41&&p.y<=top+cell&&p.y>top-height*cell)
                ShowLineControls(false,Mathf.FloorToInt((top-p.y)/cell));
            else {HideLineControls();return;}
            board.LinePreview(lineIsColumn,lineIndex+1,true);
        }
        public bool ChangeLine(bool column,int index,bool insert)
        {
            CommitPending();ClearSelection();HideLineControls();
            if(!Document.ChangeLine(column,index,insert,out var error)){Notice(error);return false;}
            pendingStamps.Clear();board.ClearEditorStamps();
            var offset=index==0?(column?new Cell(insert?1:-1,0):new Cell(0,insert?1:-1)):new Cell(0,0);
            board.PreviewExpanded(Document.Data,offset);previewQueued=false;
            Notice("");RefreshLabels();return true;
        }
    }
}
