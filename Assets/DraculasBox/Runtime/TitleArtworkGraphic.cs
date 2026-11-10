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
        private Artwork artwork;
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
        public override Texture mainTexture {get{EnsureArtwork();return artwork?.texture??Texture2D.whiteTexture;}}
        public void RefreshTheme(){artwork=null;SetVerticesDirty();SetMaterialDirty();}
        private void EnsureArtwork()
        {
            if(artwork!=null&&artwork.texture!=null)return;
            if(source==null)return;var theme=GameThemes.Current;string key=source.GetInstanceID()+"/"+theme.Id;
            if(cache.TryGetValue(key,out artwork)&&artwork.texture!=null)return;
            int w=source.width,h=source.height;var pixels=source.GetPixels32();int x0=w,y0=h,x1=-1,y1=-1;
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
            vh.Clear();EnsureArtwork();if(artwork==null)return;var r=GetPixelAdjustedRect();
            Vector2 size=r.size;if(size.x/size.y>artwork.aspect)size.x=size.y*artwork.aspect;else size.y=size.x/artwork.aspect;
            var a=r.center-size*.5f;var b=r.center+size*.5f;var uv=artwork.uv;
            vh.AddVert(new Vector2(a.x,a.y),color,new Vector2(uv.xMin,uv.yMin));
            vh.AddVert(new Vector2(a.x,b.y),color,new Vector2(uv.xMin,uv.yMax));
            vh.AddVert(new Vector2(b.x,b.y),color,new Vector2(uv.xMax,uv.yMax));
            vh.AddVert(new Vector2(b.x,a.y),color,new Vector2(uv.xMax,uv.yMin));
            vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);
        }
    }
}
