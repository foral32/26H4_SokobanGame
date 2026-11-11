using System;
using System.Linq;

namespace DraculasBox.Core
{
    public enum SelectionLayer { Region, Player, Coffin, Piece, Plate, Door }
    public readonly struct EditSelection
    {
        public readonly Cell Min,Max;
        public readonly SelectionLayer Layer;
        public EditSelection(Cell a,Cell b,SelectionLayer layer=SelectionLayer.Region)
        {Min=new Cell(Math.Min(a.x,b.x),Math.Min(a.y,b.y));Max=new Cell(Math.Max(a.x,b.x),Math.Max(a.y,b.y));Layer=layer;}
        public bool Contains(Cell p)=>p.x>=Min.x&&p.x<=Max.x&&p.y>=Min.y&&p.y<=Max.y;
        public EditSelection Offset(Cell d)=>new(Min+d,Max+d,Layer);
    }
    public sealed partial class EditDocument
    {
        public EditSelection PickSelection(Cell at)
        {
            var layer=Data.player==at?SelectionLayer.Player:Data.coffin==at?SelectionLayer.Coffin:
                Data.pieces.Any(p=>p.cell==at)?SelectionLayer.Piece:Data.plates.Any(p=>p.cell==at)?SelectionLayer.Plate:
                Data.doors.Any(p=>p.cell==at)?SelectionLayer.Door:SelectionLayer.Region;
            return new EditSelection(at,at,layer);
        }
        private static bool SelectedCell(EditSelection s,SelectionLayer layer,Cell p)=>s.Contains(p)&&(s.Layer==SelectionLayer.Region||s.Layer==layer);
        private bool Inside(EditSelection s)=>s.Min.x>=0&&s.Min.y>=0&&s.Max.x<Data.rows[0].Length&&s.Max.y<Data.rows.Length;
        public string PreviewMove(EditSelection s,Cell delta,out LevelData next)
        {
            next=null;var dest=s.Offset(delta);
            if(!Inside(s))return "选区超出棋盘。";
            int w=Data.rows[0].Length,h=Data.rows.Length;
            if(dest.Min.x<0||dest.Min.y<0||dest.Max.x>=w||dest.Max.y>=h)return "移动超出棋盘。";
            var candidate=Data.Copy();
            bool Moves(SelectionLayer layer,Cell p)=>SelectedCell(s,layer,p);
            if(s.Layer==SelectionLayer.Region)
            {
                bool Occupied(Cell p)=>Data.player==p||Data.coffin==p||Data.pieces.Any(i=>i.cell==p)||Data.plates.Any(i=>i.cell==p)||Data.doors.Any(i=>i.cell==p);
                for(int y=dest.Min.y;y<=dest.Max.y;y++)for(int x=dest.Min.x;x<=dest.Max.x;x++)
                {var p=new Cell(x,y);if(!s.Contains(p)&&(Occupied(p)||Data.rows[y][x]!='.'))return "目标区域已有内容。";}
                var rows=Data.rows.Select(r=>r.ToCharArray()).ToArray();
                for(int y=s.Min.y;y<=s.Max.y;y++)for(int x=s.Min.x;x<=s.Max.x;x++)rows[y][x]='.';
                for(int y=s.Min.y;y<=s.Max.y;y++)for(int x=s.Min.x;x<=s.Max.x;x++)rows[y+delta.y][x+delta.x]=Data.rows[y][x];
                candidate.rows=rows.Select(r=>new string(r)).ToArray();
            }
            if(Moves(SelectionLayer.Player,candidate.player))candidate.player+=delta;
            if(Moves(SelectionLayer.Coffin,candidate.coffin))candidate.coffin+=delta;
            foreach(var p in candidate.pieces)if(Moves(SelectionLayer.Piece,p.cell))p.cell+=delta;
            foreach(var p in candidate.plates)if(Moves(SelectionLayer.Plate,p.cell))p.cell+=delta;
            foreach(var p in candidate.doors)if(Moves(SelectionLayer.Door,p.cell))p.cell+=delta;
            string error=Definition.DraftError(candidate);
            if(error!=null)return error;
            next=candidate;return null;
        }
        public bool MoveSelection(EditSelection selection,Cell delta,out string error)
        {
            End();error=PreviewMove(selection,delta,out var next);
            if(error!=null||delta==new Cell(0,0))return false;
            Begin();Data=next;End();return true;
        }
        public void DeleteSelection(EditSelection s)
        {
            if(!Inside(s))return;
            End();Begin();
            bool Removes(SelectionLayer layer,Cell p)=>SelectedCell(s,layer,p);
            if(Removes(SelectionLayer.Player,Data.player))Data.player=new Cell(-1,-1);
            if(Removes(SelectionLayer.Coffin,Data.coffin))Data.coffin=new Cell(-1,-1);
            Data.pieces=Data.pieces.Where(p=>!Removes(SelectionLayer.Piece,p.cell)).ToArray();
            Data.plates=Data.plates.Where(p=>!Removes(SelectionLayer.Plate,p.cell)).ToArray();
            Data.doors=Data.doors.Where(p=>!Removes(SelectionLayer.Door,p.cell)).ToArray();
            if(s.Layer==SelectionLayer.Region)
                for(int y=s.Min.y;y<=s.Max.y;y++){var row=Data.rows[y].ToCharArray();for(int x=s.Min.x;x<=s.Max.x;x++)row[x]='.';Data.rows[y]=new string(row);}
            End();
        }
    }
}
