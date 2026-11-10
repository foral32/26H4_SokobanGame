using UnityEngine;
namespace DraculasBox.Runtime
{
    public static partial class SurfaceSprites
    {
        private static Sprite floor,glow,vignette,plateIcon,doorIcon,sunSource,sourceArrow;private static readonly Sprite[] light=new Sprite[16];
        public static Sprite SunSource=>sunSource??(sunSource=Make("Sun source",(x,y)=>{float dx=x-31.5f,dy=y-31.5f;if(dx*dx+dy*dy<169)return Color.white;for(int i=0;i<8;i++){float a=i*Mathf.PI/4,t=dx*Mathf.Cos(a)+dy*Mathf.Sin(a),n=Mathf.Abs(-dx*Mathf.Sin(a)+dy*Mathf.Cos(a));if(t>19&&t<28&&n<1.7f)return Color.white;}return Color.clear;}));
        public static Sprite SourceArrow=>sourceArrow??(sourceArrow=Make("Light direction",(x,y)=>{float dy=Mathf.Abs(y-31.5f);return x>=9&&x<=35&&dy<3.5f||x>=29&&x<=53&&dy<(53-x)*.65f?Color.white:Color.clear;}));
        public static Sprite DeviceIcon(bool door)
        {
            if(door)return doorIcon??(doorIcon=Make("Door icon",(x,y)=>{bool outer=x<6||x>57||y<5||y>58;bool seam=x>29&&x<34;return new Color(outer?.65f:seam?.3f:1,outer?.65f:seam?.3f:1,outer?.65f:seam?.3f:1,1);}));
            return plateIcon??(plateIcon=Make("Plate icon",(x,y)=>{bool outer=x<7||x>56||y<7||y>56;return new Color(outer?1:.4f,outer?1:.4f,outer?1:.4f,x<2||x>61||y<2||y>61?0:1);}));
        }
        public static Sprite Floor=>floor??(floor=Make("Stone floor",(x,y)=>{int n=(x*73856093)^(y*19349663);float grain=((n&255)/255f-.5f)*.035f+Mathf.Sin(x*.11f+y*.07f)*.012f;float edge=x==0||y==0?.80f:x==1||y==1?.94f:x==63||y==63?.99f:1;return new Color(edge+grain,edge+grain,edge+grain,1);}));
        public static Sprite Glow=>glow??(glow=Make("Soft glow",(x,y)=>{float r=new Vector2((x-31.5f)/31.5f,(y-31.5f)/31.5f).magnitude;return new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-r),2));}));
        public static Sprite Vignette=>vignette??(vignette=Make("Damage vignette",(x,y)=>{float r=new Vector2((x-31.5f)/31.5f,(y-31.5f)/31.5f).magnitude;return new Color(1,1,1,Mathf.SmoothStep(0,1,Mathf.Clamp01((r-.42f)/.9f)));}));
        // Feather only the inside of exposed lit edges; it never changes the danger mask.
        public static Sprite Light(int edges)
        {
            if(light[edges]!=null)return light[edges];return light[edges]=Make("Illumination "+edges,(x,y)=>{float a=1;if((edges&1)!=0)a*=Mathf.SmoothStep(0,1,x/7f);if((edges&2)!=0)a*=Mathf.SmoothStep(0,1,(63-x)/7f);if((edges&4)!=0)a*=Mathf.SmoothStep(0,1,(63-y)/7f);if((edges&8)!=0)a*=Mathf.SmoothStep(0,1,y/7f);return new Color(1,1,1,a);});
        }
        private static Sprite Make(string name,System.Func<int,int,Color> pixel)
        {
            var t=new Texture2D(64,64,TextureFormat.RGBA32,false){name=name,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontSave};var p=new Color[4096];for(int y=0;y<64;y++)for(int x=0;x<64;x++)p[y*64+x]=pixel(x,y);t.SetPixels(p);t.Apply(false,true);var s=Sprite.Create(t,new Rect(0,0,64,64),Vector2.one*.5f,64,0,SpriteMeshType.FullRect);s.name=name;s.hideFlags=HideFlags.DontSave;return s;
        }
    }
}
