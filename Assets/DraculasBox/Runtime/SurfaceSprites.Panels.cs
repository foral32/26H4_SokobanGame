using UnityEngine;
namespace DraculasBox.Runtime
{
    public static partial class SurfaceSprites
    {
        private static Sprite panel,panelBorder,hatch;
        public static Sprite Panel=>panel!=null?panel:(panel=PanelTexture(false));
        public static Sprite PanelBorder=>panelBorder!=null?panelBorder:(panelBorder=PanelTexture(true));
        public static Sprite Hatch=>hatch!=null?hatch:(hatch=Make("Erase hatch",(x,y)=>new Color(1,1,1,((x+y)%16)<2?.85f:.12f)));
        private static Sprite PanelTexture(bool border)
        {
            var t=new Texture2D(64,64,TextureFormat.RGBA32,false){name=border?"Panel outline":"Panel surface",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontSave};var pixels=new Color[4096];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){float dx=Mathf.Max(5.5f-x,x-57.5f),dy=Mathf.Max(5.5f-y,y-57.5f);float d=new Vector2(Mathf.Max(dx,0),Mathf.Max(dy,0)).magnitude+Mathf.Min(Mathf.Max(dx,dy),0)-5;float a=Mathf.Clamp01(.5f-d);if(border)a*=Mathf.Clamp01(d+1.5f);pixels[y*64+x]=new Color(1,1,1,a);}
            t.SetPixels(pixels);t.Apply(false,true);var s=Sprite.Create(t,new Rect(0,0,64,64),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,Vector4.one*6);s.name=t.name;s.hideFlags=HideFlags.DontSave;return s;
        }
    }
}
