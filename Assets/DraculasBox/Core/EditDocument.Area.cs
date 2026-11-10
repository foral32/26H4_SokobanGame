using System;
using System.Linq;

namespace DraculasBox.Core
{
    public readonly struct PaintAreaResult
    {
        public readonly int Applied,Skipped;
        public PaintAreaResult(int applied,int skipped){Applied=applied;Skipped=skipped;}
    }
    public sealed partial class EditDocument
    {
        public PaintAreaResult PaintRectangle(Cell a,Cell b,Brush brush,int channel=0)
        {
            int x0=Math.Max(0,Math.Min(a.x,b.x)),x1=Math.Min(Data.rows[0].Length-1,Math.Max(a.x,b.x));
            int y0=Math.Max(0,Math.Min(a.y,b.y)),y1=Math.Min(Data.rows.Length-1,Math.Max(a.y,b.y));
            if(x1<x0||y1<y0)return new PaintAreaResult(0,0);
            int count=(x1-x0+1)*(y1-y0+1);
            if(!Enum.IsDefined(typeof(Brush),brush)||(count>1&&(brush==Brush.Player||brush==Brush.Coffin)))return new PaintAreaResult(0,count);
            bool owns=transaction==null;if(owns)Begin();int applied=0,skipped=0;
            try
            {
                for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)
                {
                    var cell=new Cell(x,y);
                    if(brush==Brush.Erase)
                    {
                        // A region clear removes the stacked content as well as wall/void terrain.
                        while(Data.player==cell||Data.coffin==cell||Data.pieces.Any(p=>p.cell==cell)||Data.plates.Any(p=>p.cell==cell)||Data.doors.Any(p=>p.cell==cell))Paint(cell,Brush.Erase);
                        Paint(cell,Brush.Erase);applied++;
                    }
                    else if(Paint(cell,brush,channel))applied++;else skipped++;
                }
                if(owns)End();return new PaintAreaResult(applied,skipped);
            }
            catch{if(owns)End(true);throw;}
        }
    }
}
