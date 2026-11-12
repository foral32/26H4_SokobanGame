using System;
using System.Linq;

namespace DraculasBox.Core
{
    public sealed partial class EditDocument
    {
        public string ExpansionError(Cell a,Cell b)
        {
            long width=Math.Max(Data.rows[0].Length-1,Math.Max((long)a.x,b.x))-Math.Min(0,Math.Min((long)a.x,b.x))+1;
            long height=Math.Max(Data.rows.Length-1,Math.Max((long)a.y,b.y))-Math.Min(0,Math.Min((long)a.y,b.y))+1;
            return width>32||height>32?"棋盘最多 32 × 32 格。":null;
        }
        private bool OnBoard(Cell p)=>p.x>=0&&p.y>=0&&p.y<Data.rows.Length&&p.x<Data.rows[0].Length;
        private void Expand(Cell a,Cell b,out Cell offset)
        {
            int left=Math.Min(0,Math.Min(a.x,b.x)),top=Math.Min(0,Math.Min(a.y,b.y));
            int width=Math.Max(Data.rows[0].Length-1,Math.Max(a.x,b.x))-left+1;
            int height=Math.Max(Data.rows.Length-1,Math.Max(a.y,b.y))-top+1;
            offset=new Cell(-left,-top);
            if(width==Data.rows[0].Length&&height==Data.rows.Length)return;
            var rows=Enumerable.Range(0,height).Select(_=>Enumerable.Repeat('.',width).ToArray()).ToArray();
            for(int y=0;y<Data.rows.Length;y++)for(int x=0;x<Data.rows[0].Length;x++)rows[y+offset.y][x+offset.x]=Data.rows[y][x];
            Data.rows=rows.Select(r=>new string(r)).ToArray();
            OffsetContents(offset);
        }
        private void OffsetContents(Cell offset)
        {
            if(Data.player.x>=0)Data.player+=offset;if(Data.coffin.x>=0)Data.coffin+=offset;
            foreach(var p in Data.pieces)p.cell+=offset;
            foreach(var p in Data.plates)p.cell+=offset;
            foreach(var p in Data.doors)p.cell+=offset;
        }
        // Expansion and painting share the caller's stroke transaction, including the
        // negative-origin translation. New cells default to floor; existing terrain is preserved.
        public bool PaintExpanded(Cell cell,Brush brush,int channel,out Cell offset,out string error)
        {
            offset=new Cell(0,0);error=null;bool outside=!OnBoard(cell);
            if(!outside)return Paint(cell,brush,channel);
            if(brush==Brush.Erase)return false;
            error=ExpansionError(cell,cell);if(error!=null)return false;
            if(!Enum.IsDefined(typeof(Brush),brush))return false;
            if((brush==Brush.Plate||brush==Brush.Door)&&(channel<0||channel>3))return false;
            if((brush==Brush.Plate&&Data.plates.Length>=32)||(brush==Brush.Door&&Data.doors.Length>=32)||
               ((brush==Brush.Box||brush==Brush.Lamp||brush==Brush.MirrorSlash||brush==Brush.MirrorBackslash)&&Data.pieces.Length>=128))
            {error="已达到物件数量上限。";return false;}
            bool owns=transaction==null;if(owns)Begin();
            Expand(cell,cell,out offset);cell+=offset;
            bool applied=Paint(cell,brush,channel);
            if(owns)End(!applied);
            return applied;
        }
        public PaintAreaResult PaintExpandedRectangle(Cell a,Cell b,Brush brush,int channel,out Cell offset,out string error)
        {
            offset=new Cell(0,0);error=null;
            if(brush==Brush.Erase)return PaintRectangle(a,b,brush,channel);
            error=ExpansionError(a,b);if(error!=null)return new PaintAreaResult(0,0);
            long count=((long)Math.Abs(a.x-b.x)+1)*(Math.Abs(a.y-b.y)+1);
            if(!Enum.IsDefined(typeof(Brush),brush)||(count>1&&(brush==Brush.Player||brush==Brush.Coffin)))
            {error="玩家和棺材请逐个放置。";return new PaintAreaResult(0,0);}
            if((brush==Brush.Plate||brush==Brush.Door)&&(channel<0||channel>3))
            {error="连接组无效。";return new PaintAreaResult(0,0);}
            bool owns=transaction==null;if(owns)Begin();
            Expand(a,b,out offset);
            var result=PaintRectangle(a+offset,b+offset,brush,channel);
            if(owns)End();return result;
        }
        public string LineChangeError(bool column,int index,bool insert)
        {
            int size=column?Data.rows[0].Length:Data.rows.Length;
            if(index<0||index>=(insert?size+1:size))return "行列位置已改变。";
            if(insert&&size>=32)return "棋盘最多 32 × 32 格。";
            if(!insert&&size<=3)return "棋盘至少保留 3 × 3 格。";
            return null;
        }
        public bool ChangeLine(bool column,int index,bool insert,out string error)
        {
            End();error=LineChangeError(column,index,insert);if(error!=null)return false;
            Begin();
            int Axis(Cell p)=>column?p.x:p.y;
            bool Removed(Cell p)=>!insert&&Axis(p)==index;
            Cell Shift(Cell p)
            {
                if(p.x<0)return p;
                if(Removed(p))return new Cell(-1,-1);
                if(Axis(p)>=index)return p+(column?new Cell(insert?1:-1,0):new Cell(0,insert?1:-1));
                return p;
            }
            Data.player=Shift(Data.player);Data.coffin=Shift(Data.coffin);
            Data.pieces=Data.pieces.Where(p=>!Removed(p.cell)).ToArray();foreach(var p in Data.pieces)p.cell=Shift(p.cell);
            Data.plates=Data.plates.Where(p=>!Removed(p.cell)).ToArray();foreach(var p in Data.plates)p.cell=Shift(p.cell);
            Data.doors=Data.doors.Where(p=>!Removed(p.cell)).ToArray();foreach(var p in Data.doors)p.cell=Shift(p.cell);
            if(column)Data.rows=Data.rows.Select(r=>insert?r.Insert(index,"."):r.Remove(index,1)).ToArray();
            else {var rows=Data.rows.ToList();if(insert)rows.Insert(index,new string('.',Data.rows[0].Length));else rows.RemoveAt(index);Data.rows=rows.ToArray();}
            End();return true;
        }
    }
}
