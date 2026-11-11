using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    /// <summary>The approved wordmark retains its letterforms while using the active palette.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TitleArtworkGraphic : MaskableGraphic
    {
        [SerializeField] private Texture2D source;
        private sealed class Artwork {public Texture2D texture;public Rect uv;public float aspect;}
        private static readonly Dictionary<string,Artwork> cache=new Dictionary<string,Artwork>();
        private Artwork artwork;private bool animatedEyes,cachedAnimatedEyes;
        // Normalized eye bounds in the approved source artwork, using bottom-left coordinates.
        private static readonly Rect[] EyeSource={new Rect(768f/1721,244f/914,35f/1721,76f/914),new Rect(886f/1721,244f/914,35f/1721,77f/914)};
        private static void RemoveEyes(Color32[] pixels,int width,int height)
        {
            foreach(var eye in EyeSource)
            {
                int left=Mathf.Max(1,Mathf.FloorToInt(eye.xMin*width)-4),right=Mathf.Min(width-2,Mathf.CeilToInt(eye.xMax*width)+4);
                int bottom=Mathf.Max(0,Mathf.FloorToInt(eye.yMin*height)-4),top=Mathf.Min(height-1,Mathf.CeilToInt(eye.yMax*height)+4);
                for(int y=bottom;y<=top;y++)
                {
                    Color a=pixels[y*width+left-1],b=pixels[y*width+right+1];
                    for(int x=left;x<=right;x++)pixels[y*width+x]=Color.Lerp(a,b,(x-left+1f)/(right-left+2));
                }
            }
        }
        public bool EyeBounds(out Rect left,out Rect right)
        {
            EnsureArtwork();left=right=default;if(artwork==null)return false;
            var fitted=FittedRect();
            Rect Map(Rect sourceRect)=>new Rect(fitted.xMin+(sourceRect.xMin-artwork.uv.xMin)/artwork.uv.width*fitted.width,fitted.yMin+(sourceRect.yMin-artwork.uv.yMin)/artwork.uv.height*fitted.height,sourceRect.width/artwork.uv.width*fitted.width,sourceRect.height/artwork.uv.height*fitted.height);
            left=Map(EyeSource[0]);right=Map(EyeSource[1]);return true;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void BeginSession()=>ReleaseCache(false);
        private static void ReleaseCache(bool immediate)
        {
            foreach(var item in cache.Values)
                if(item.texture!=null){if(immediate)DestroyImmediate(item.texture);else Destroy(item.texture);}
            cache.Clear();
        }
        #if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterEditorCleanup()
        {
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload+=BeforeReload;
            UnityEditor.EditorApplication.playModeStateChanged+=PlayStateChanged;
        }
        private static void BeforeReload()=>ReleaseCache(true);
        private static void PlayStateChanged(UnityEditor.PlayModeStateChange state)
        {if(state==UnityEditor.PlayModeStateChange.ExitingPlayMode)ReleaseCache(true);}
        #endif
        public Texture2D Source {get=>source;set{source=value;RefreshTheme();}}
        public void UseAnimatedEyes(bool value){if(animatedEyes==value)return;animatedEyes=value;RefreshTheme();}
        public override Texture mainTexture {get{EnsureArtwork();return artwork?.texture??Texture2D.whiteTexture;}}
        public void RefreshTheme(){artwork=null;SetVerticesDirty();SetMaterialDirty();}
        private void EnsureArtwork()
        {
            bool live=Application.isPlaying&&animatedEyes;
            if(artwork!=null&&artwork.texture!=null&&cachedAnimatedEyes==live)return;
            cachedAnimatedEyes=live;
            if(source==null)return;var theme=GameThemes.Current;string key=source.GetInstanceID()+"/"+theme.Id+(live?"/animated":"/still");
            if(cache.TryGetValue(key,out artwork)&&artwork.texture!=null)return;
            int w=source.width,h=source.height;var pixels=source.GetPixels32();int x0=w,y0=h,x1=-1,y1=-1;
            if(live)RemoveEyes(pixels,w,h);
            for(int i=0;i<pixels.Length;i++)
            {
                Color32 p=pixels[i];if(p.a<4){pixels[i]=new Color32(0,0,0,0);continue;}
                if(p.a>=32){int x=i%w,y=i/w;x0=Mathf.Min(x0,x);y0=Mathf.Min(y0,y);x1=Mathf.Max(x1,x);y1=Mathf.Max(y1,y);}
                Color c=p;float alpha=c.a;Color result=c;
                if(c.r-c.g>.12f&&c.r>c.g*1.3f)result=Color.Lerp(theme.CapeShade,theme.TitleDepth,Mathf.InverseLerp(.28f,.63f,c.r));
                else if(c.grayscale>.38f)result=theme.TitleFace*Mathf.Clamp(c.grayscale/.94f,.40f,1.045f);
                result.a=alpha;pixels[i]=result;
            }
            var texture=new Texture2D(w,h,TextureFormat.RGBA32,false){name="Title wordmark "+theme.Id,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            texture.SetPixels32(pixels);texture.Apply(false,true);
            if(x1<x0||y1<y0){x0=y0=0;x1=w-1;y1=h-1;}
            artwork=new Artwork{texture=texture,uv=new Rect(x0/(float)w,y0/(float)h,(x1-x0+1)/(float)w,(y1-y0+1)/(float)h),aspect=(x1-x0+1)/(float)(y1-y0+1)};
            cache[key]=artwork;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();EnsureArtwork();if(artwork==null)return;
            var fitted=FittedRect();var a=fitted.min;var b=fitted.max;var uv=artwork.uv;
            vh.AddVert(new Vector2(a.x,a.y),color,new Vector2(uv.xMin,uv.yMin));
            vh.AddVert(new Vector2(a.x,b.y),color,new Vector2(uv.xMin,uv.yMax));
            vh.AddVert(new Vector2(b.x,b.y),color,new Vector2(uv.xMax,uv.yMax));
            vh.AddVert(new Vector2(b.x,a.y),color,new Vector2(uv.xMax,uv.yMin));
            vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);
        }
        private Rect FittedRect()
        {
            var r=GetPixelAdjustedRect();
            Vector2 size=r.size;if(size.x/size.y>artwork.aspect)size.x=size.y*artwork.aspect;else size.y=size.x/artwork.aspect;
            return new Rect(r.center-size*.5f,size);
        }
    }
}
