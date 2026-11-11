using System.Collections.Generic;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public static partial class SurfaceSprites
    {
        private static readonly Dictionary<int,Sprite> pits=new Dictionary<int,Sprite>();
        public static Color PitDepth=>Color.Lerp(GameThemes.Current.Background,Color.black,.66f);
        public static Color PitRim=>Color.Lerp(GameThemes.Current.WallTop,GameThemes.Current.FloorSafe,.36f);
        public static Color PitWall=>Color.Lerp(PitRim,PitDepth,.60f);
        // N/E/S/W followed by NE/SE/SW/NW, matching the board's connected walls.
        public static Sprite Pit(int neighbors)
        {
            int key=(neighbors&255)|(GameThemes.Current.Index<<8);
            if(pits.TryGetValue(key,out var cached)&&cached!=null)return cached;
            var sprite=Make("Connected pit "+key,(x,y)=>
            {
                float edge=64;bool north=false;
                if((neighbors&1)==0&&63-y<edge){edge=63-y;north=true;}
                if((neighbors&2)==0&&63-x<edge){edge=63-x;north=false;}
                if((neighbors&4)==0&&y<edge){edge=y;north=false;}
                if((neighbors&8)==0&&x<edge){edge=x;north=false;}
                // The fill is deliberately constant: adjacent cells have no seams.
                if(edge<2)return PitRim;
                if(edge<9)return Color.Lerp(PitWall,PitDepth,Mathf.Clamp01((edge-2)/(north?10f:7f)));
                return PitDepth;
            });
            pits[key]=sprite;return sprite;
        }
    }
}
