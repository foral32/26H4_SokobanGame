using UnityEngine;
namespace DraculasBox.Runtime
{
    public static partial class SurfaceSprites
    {
        private static readonly Sprite[] mirrors=new Sprite[10],mirrorRays=new Sprite[16];
        public static Sprite Mirror(bool backslash)
        {
            int index=GameThemes.Current.Index*2+(backslash?1:0);if(mirrors[index]!=null)return mirrors[index];
            return mirrors[index]=Make(backslash?"Mirror backslash":"Mirror slash",(x,y)=>
            {
                if(x<3||x>60||y<3||y>60)return Color.clear;
                if(x<9||x>54||y<9||y>54)
                    return GameThemes.IsClassic?(y>54||x<9?new Color32(192,209,218,255):new Color32(84,100,117,255)):(y>54||x<9?GameThemes.Current.WallLight:GameThemes.Current.WallFront);
                float diagonal=Mathf.Abs(backslash?x+y-63:x-y);
                if(diagonal<2.2f)return new Color32(242,249,250,255);
                if(diagonal<4.2f)return new Color32(142,191,211,255);
                float sheen=Mathf.Clamp01((x+y)/126f);
                return Color.Lerp(new Color32(45,68,90,255),new Color32(82,117,139,255),sheen);
            });
        }
        public static Sprite MirrorRay(int edges)
        {
            if(mirrorRays[edges]!=null)return mirrorRays[edges];
            return mirrorRays[edges]=Make("Mirror light "+edges,(x,y)=>
            {
                float dx=x-31.5f,dy=y-31.5f,a=0;
                if((edges&1)!=0&&dy>=-1||(edges&4)!=0&&dy<=1)a=Mathf.Exp(-dx*dx/12f);
                if((edges&2)!=0&&dx>=-1||(edges&8)!=0&&dx<=1)a=Mathf.Max(a,Mathf.Exp(-dy*dy/12f));
                return new Color(1,1,1,a);
            });
        }
    }
}
