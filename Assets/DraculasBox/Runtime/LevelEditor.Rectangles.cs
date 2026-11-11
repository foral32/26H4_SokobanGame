using System.Collections.Generic;
using DraculasBox.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    public enum PaintShape {Brush,Rectangle}
    public sealed partial class LevelEditor
    {
        private readonly List<Button> channelButtons=new List<Button>();private TMP_Text channelHeading;
        private Cell regionStart,regionEnd;private Brush regionBrush;private int regionChannel;
        public PaintShape Shape {get;private set;}
        public bool RegionActive {get;private set;}
        private bool CanRectangle=>Operation==EditOperation.Erase||Selected!=Brush.Player&&Selected!=Brush.Coffin;
        public void SelectShape(PaintShape shape){ClosePanels();CancelRectangle(false);FinishStroke();Shape=shape;RefreshShapeControls();}
        private void RefreshShapeControls()
        {
            ui.SetEnabled("shape-rectangle",CanRectangle);bool area=Shape==PaintShape.Rectangle&&CanRectangle;
            foreach(string id in new[]{"shape-brush","shape-rectangle"})ui.WorkshopChoice(id,(id=="shape-rectangle")==area);
        }
        private void RefreshChannelControls()
        {
            bool visible=Operation==EditOperation.Paint&&(Selected==Brush.Plate||Selected==Brush.Door);if(channelHeading!=null)channelHeading.gameObject.SetActive(visible);
            for(int i=0;i<channelButtons.Count;i++){var button=channelButtons[i];button.gameObject.SetActive(visible);button.GetComponent<Image>().color=BoardView.CircuitColors[i];button.transform.Find("Face").GetComponent<Image>().color=i==Channel?BoardView.CircuitColors[i]:GameThemes.IsClassic?new Color32(43,53,67,255):GameUI.WorkshopSurface;button.GetComponentInChildren<TMP_Text>().color=i==Channel?GameThemes.Current.Ink:GameThemes.IsClassic?GameUI.White:GameUI.WorkshopInk;}
            if(visible)Notice("连接组 "+(Channel+1)+" · 同组压力板控制同组门");
        }
        private bool RectangleSample(Vector2 screen,bool begin,bool held,bool end,bool erase)
        {
            if(end){if(RegionActive)CommitRectangle();return true;}
            if(!begin&&!held)return false;if(OverUI(screen)||!board.CellAt(screen,out var cell))return false;
            if(begin){CancelRectangle(false);FinishStroke();RegionActive=true;regionStart=cell;regionBrush=erase?Brush.Erase:Selected;regionChannel=Channel;}
            if(!RegionActive)return false;regionEnd=cell;board.BrushPreview(null,Selected,true);board.RegionPreview(regionStart,regionEnd,regionBrush==Brush.Erase);
            Notice("矩形 "+(Mathf.Abs(regionEnd.x-regionStart.x)+1)+" × "+(Mathf.Abs(regionEnd.y-regionStart.y)+1)+" · 松开应用，Esc 取消");return true;
        }
        private void CommitRectangle()
        {
            if(!RegionActive)return;RegionActive=false;board.RegionPreview(null,null);
            var before=Document.Data.Copy();
            var result=Document.PaintRectangle(regionStart,regionEnd,regionBrush,regionChannel);if(result.Applied>0)
            {
                if(regionBrush!=Brush.Erase)for(int y=Mathf.Min(regionStart.y,regionEnd.y);y<=Mathf.Max(regionStart.y,regionEnd.y);y++)for(int x=Mathf.Min(regionStart.x,regionEnd.x);x<=Mathf.Max(regionStart.x,regionEnd.x);x++)
                {var cell=new Cell(x,y);if(StampChanged(before,cell,regionBrush))pendingStamps.Add(new BoardView.StampCell(cell,regionBrush));}
                previewQueued=true;FlushPreview();
            }
            RefreshLabels();Notice("应用 "+result.Applied+" 格"+(result.Skipped>0?" · 保留 "+result.Skipped+" 个不可放置格":""));
        }
        private bool StampChanged(LevelData before,Cell cell,Brush brush)
        {
            var after=Document.Data;if(cell.y<0||cell.y>=after.rows.Length||cell.x<0||cell.x>=after.rows[0].Length)return false;
            if(brush==Brush.Floor||brush==Brush.Wall||brush==Brush.Void)return before.rows[cell.y][cell.x]!=after.rows[cell.y][cell.x];
            if(brush==Brush.Player)return before.player!=after.player&&after.player==cell;
            if(brush==Brush.Coffin)return before.coffin!=after.coffin&&after.coffin==cell;
            if(brush==Brush.Plate||brush==Brush.Door)
            {
                var old=System.Array.Find(brush==Brush.Plate?before.plates:before.doors,d=>d.cell==cell);
                var current=System.Array.Find(brush==Brush.Plate?after.plates:after.doors,d=>d.cell==cell);
                return current!=null&&(old==null||old.channel!=current.channel);
            }
            var was=System.Array.Find(before.pieces,p=>p.cell==cell);var now=System.Array.Find(after.pieces,p=>p.cell==cell);
            return now!=null&&(was==null||was.id!=now.id||was.kind!=now.kind);
        }
        public bool CancelRectangle(bool notify=true)
        {
            if(!RegionActive)return false;RegionActive=false;board.RegionPreview(null,null);if(notify)Notice("已取消矩形");return true;
        }
    }
}
