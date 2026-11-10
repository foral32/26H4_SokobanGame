using UnityEngine;

namespace DraculasBox.Runtime
{
    public static partial class SurfaceSprites
    {
        private static Sprite vampireFangs;private static readonly Sprite[] vampireCapes=new Sprite[5],vampireIcons=new Sprite[5];
        private static Color CapeInk=>GameThemes.Current.CapeShade;private static Color CapeWine=>GameThemes.Current.Cape;
        public static Sprite VampireCape{get{int i=GameThemes.Current.Index;return vampireCapes[i]!=null?vampireCapes[i]:(vampireCapes[i]=Make("Vampire small cape "+i,CapePixel));}}
        public static Sprite VampireFangs=>vampireFangs!=null?vampireFangs:(vampireFangs=Make("Vampire small fangs",(x,y)=>y>=3&&y<=8&&(Mathf.Abs(x-22.5f)<(y-2)*.36f||Mathf.Abs(x-41.5f)<(y-2)*.36f)?new Color32(255,249,238,255):Color.clear));
        public static Sprite VampireIcon{get{int index=GameThemes.Current.Index;return vampireIcons[index]!=null?vampireIcons[index]:(vampireIcons[index]=Make("Vampire portrait "+index,(x,y)=>
        {
            Color c=CapePixel(x,y);
            if(x>=6&&x<=57&&y>=8&&y<=59)
            {
                bool bevel=x<9||x>54||y<11||y>56;c=bevel?(x<9||y>56?new Color32(250,249,247,255):new Color32(190,188,202,255)):new Color32(245,241,238,255);if(!GameThemes.IsClassic)c=bevel?(x<9||y>56?GameThemes.Current.ObjectLight:GameThemes.Current.ObjectShade):GameThemes.Current.ObjectTop;
                if(!bevel&&(x>=24&&x<=27||x>=39&&x<=42)&&y>=27&&y<=39)c=new Color32(36,34,43,255);
            }
            if(y>=6&&y<=10&&(Mathf.Abs(x-24.5f)<(y-5)*.36f||Mathf.Abs(x-40.5f)<(y-5)*.36f))c=new Color32(255,249,238,255);
            return c;
        }));}}
        private static Color CapePixel(int x,int y)
        {
            float side=Mathf.Abs(x-31.5f);
            if(y<3||y>35)return Color.clear;
            float width=Mathf.Min(31.5f,y<23?31-(y-3)*.42f:22.6f+(y-23)*.85f);
            if(side>width)return Color.clear;
            if(y<7&&side<7)return Color.clear;
            return side>width-2||y<5?CapeInk:CapeWine;
        }
    }
}
