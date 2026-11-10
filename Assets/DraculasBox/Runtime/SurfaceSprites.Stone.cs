using System.Collections.Generic;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public static partial class SurfaceSprites
    {
        private static readonly Dictionary<int,Sprite> stoneWalls=new Dictionary<int,Sprite>();
        private static readonly Sprite[] raisedBoxes=new Sprite[5];private static readonly Dictionary<int,Sprite> sunlitBoxes=new Dictionary<int,Sprite>();
        // North, east, south, west, then the four diagonals, in texture space.
        private static readonly Vector2Int[] stoneNeighbors={new Vector2Int(0,1),new Vector2Int(1,0),new Vector2Int(0,-1),new Vector2Int(-1,0),new Vector2Int(1,1),new Vector2Int(1,-1),new Vector2Int(-1,-1),new Vector2Int(-1,1)};
        private static Color StoneTop=>GameThemes.Current.WallTop;private static Color StoneFront=>GameThemes.Current.WallFront;private static Color StoneFoot=>GameThemes.Current.WallFoot;

        public static Sprite StoneWall(int neighbors,int sunlight=-1)
        {
            neighbors&=255;
            int key=neighbors|((sunlight+1)<<8)|(GameThemes.Current.Index<<12);if(stoneWalls.TryGetValue(key,out var existing)&&existing!=null)return existing;
            // Sample a continuous distance to the floor rectangles. Shared wall
            // edges have no bevel; diagonally exposed corners join the outline.
            var sprite=Make("Raised stone wall "+key,(x,y)=>StonePixel(x+.5f,y+.5f,neighbors,sunlight));
            stoneWalls[key]=sprite;return sprite;
        }
        private static Color SunOnStone(Color color,Vector2 normal,int side)
        {
            if(side<0)return color;
            var incoming=side==0?Vector2.left:side==1?Vector2.up:side==2?Vector2.right:Vector2.down;
            float face=Vector2.Dot(normal,incoming);
            float alpha=color.a;color*=face<-.25f?.93f:1;color.a=alpha;
            return Color.Lerp(color,new Color32(237,227,201,255),normal==Vector2.zero?.055f:Mathf.Max(0,face)*.28f);
        }
        private static Color StonePixel(float x,float y,int neighbors,int sunlight)
        {
            bool front=(neighbors&4)==0;
            if(front&&y<12)
            {
                if(y<2)return SunOnStone(StoneFoot,Vector2.down,sunlight);
                if(y<4)return SunOnStone(Color.Lerp(StoneFoot,StoneFront,.45f),Vector2.down,sunlight);
                if(y>10)return SunOnStone(Color.Lerp(StoneFront,StoneTop,.28f),Vector2.down,sunlight);
                return SunOnStone(Color.Lerp(StoneFront,StoneTop,(y-4)/70f),Vector2.down,sunlight);
            }
            float distance=1000;Vector2 normal=Vector2.zero;
            for(int i=0;i<stoneNeighbors.Length;i++)
            {
                if((neighbors&(1<<i))!=0)continue;
                var n=stoneNeighbors[i];
                float dx=Mathf.Clamp(x,n.x*64,(n.x+1)*64)-x,dy=Mathf.Clamp(y,n.y*64,(n.y+1)*64)-y;
                float square=dx*dx+dy*dy;
                if(square>=distance)continue;
                distance=square;normal=new Vector2(dx,dy);
            }
            float edge=Mathf.Sqrt(distance);
            if(front)edge=Mathf.Min(edge,y-12);
            if(edge>=4.5f)return SunOnStone(StoneTop,Vector2.zero,sunlight);
            float facing=normal.sqrMagnitude>0?Vector2.Dot(normal.normalized,new Vector2(-.7071f,.7071f)):0;
            var bevel=Color.Lerp(GameThemes.Current.WallDark,GameThemes.Current.WallLight,(facing+1)*.5f);
            if(front&&y<16.5f)bevel=GameThemes.Current.WallRim;
            return SunOnStone(Color.Lerp(bevel,StoneTop,Mathf.Clamp01(edge/5f)*.12f),normal.normalized,sunlight);
        }

        public static Sprite RaisedBox{get{int i=GameThemes.Current.Index;return raisedBoxes[i]!=null?raisedBoxes[i]:(raisedBoxes[i]=Make("Raised white box "+i,(x,y)=>ThemeBox(BoxPixel(x,y))));}}
        public static Sprite RaisedBoxForSun(int sunlight)
        {
            if(sunlight<0)return RaisedBox;
            int key=sunlight|(GameThemes.Current.Index<<3);if(sunlitBoxes.TryGetValue(key,out var sprite)&&sprite!=null)return sprite;
            sprite=Make("Sunlit white box "+sunlight,(x,y)=>
            {
                var color=ThemeBox(BoxPixel(x,y));if(color.a==0)return color;
                int edge=sunlight==0?x:sunlight==1?63-y:sunlight==2?63-x:y;
                if(edge<7&&y>=15)color=Color.Lerp(color,new Color32(249,234,200,255),.55f);
                return color;
            });sunlitBoxes[key]=sprite;return sprite;
        }
        private static Color ThemeBox(Color original)
        {
            if(GameThemes.IsClassic||original.a==0)return original;var theme=GameThemes.Current;float value=original.grayscale;
            var result=value<.65f?Color.Lerp(theme.ObjectShade*.60f,theme.ObjectShade,Mathf.Clamp01((value-.30f)/.35f)):Color.Lerp(theme.ObjectShade,theme.ObjectLight,Mathf.Clamp01((value-.65f)/.33f));result.a=original.a;return result;
        }
        private static Color BoxPixel(int x,int y)
        {
            if(x<2||x>61||y<2||y>61)return Color.clear;
            // The box renders at .9 cells, so a 13px front matches the wall's
            // 12px front. Both remain one ordinary-height blocker in the rules.
            if(y<15)
            {
                if(y<4)return new Color32(91,85,105,255);
                if(y<7)return new Color32(123,116,136,255);
                if(y>12)return new Color32(168,163,179,255);
                return new Color32(147,140,158,255);
            }
            int edge=Mathf.Min(Mathf.Min(x-2,61-x),Mathf.Min(y-15,61-y));
            if(edge<4)return y>57||x<6?new Color32(250,248,245,255):new Color32(179,176,190,255);
            if(x>=15&&x<=48&&y>=27&&y<=51)
            {
                int inner=Mathf.Min(Mathf.Min(x-15,48-x),Mathf.Min(y-27,51-y));
                if(inner<3)return x<18||y>48?new Color32(168,165,178,255):new Color32(250,248,245,255);
                if(inner<5)return new Color32(219,216,224,255);
                return new Color32(239,237,240,255);
            }
            return new Color32(232,231,236,255);
        }
    }
}
