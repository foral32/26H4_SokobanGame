using DraculasBox.Core;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class BoardView
    {
        private static readonly Cell[] StoneOffsets={new Cell(0,-1),new Cell(1,0),new Cell(0,1),new Cell(-1,0),new Cell(1,-1),new Cell(1,1),new Cell(-1,1),new Cell(-1,-1)};
        private void BuildStoneWall(Cell p,Vector3 position)
        {
            int neighbors=0;
            for(int i=0;i<StoneOffsets.Length;i++)if(state.Level.Terrain(p+StoneOffsets[i])=='#')neighbors|=1<<i;
            var shadow=Make("Wall shadow "+p,"solid",-12,position+ContactOffset(),1);
            shadow.color=new Color(0,0,0,.15f);
            MakeSprite("Wall "+p,SurfaceSprites.StoneWall(neighbors,state.SunEnabled?(int)state.SunFrom:-1),0,position,1);
        }
    }
}
