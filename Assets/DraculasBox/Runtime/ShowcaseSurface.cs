#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    // Flat, cut-corner bookbinding: the project's palette supplies all colors.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ShowcaseSurface:MaskableGraphic
    {
        public bool tab,dot;
        private static void Poly(VertexHelper vh,Color c,params Vector2[] points)
        {int n=vh.currentVertCount;foreach(var p in points)vh.AddVert(p,c,Vector2.zero);for(int i=1;i<points.Length-1;i++)vh.AddTriangle(n,n+i,n+i+1);}
        private static void Shape(VertexHelper vh,Rect r,float cut,Color c)
        {Poly(vh,c,new Vector2(r.xMin+cut,r.yMin),new Vector2(r.xMax-cut,r.yMin),new Vector2(r.xMax,r.yMin+cut),new Vector2(r.xMax,r.yMax-cut),new Vector2(r.xMax-cut,r.yMax),new Vector2(r.xMin+cut,r.yMax),new Vector2(r.xMin,r.yMax-cut),new Vector2(r.xMin,r.yMin+cut));}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var theme=GameThemes.Current;Rect r=rectTransform.rect;
            if(dot){MenuTileGraphic.Disc(vh,r.center,r.width*.5f,color);return;}
            var rim=Color.Lerp(tab?theme.TitleFace:theme.Foreground,theme.TitleDepth,.52f);
            if(tab)
            {
                Shape(vh,new Rect(r.x-2,r.y-6,r.width,r.height),12,new Color(0,0,0,.22f));
                Shape(vh,new Rect(r.x-3,r.y-3,r.width,r.height),12,Color.Lerp(theme.TitleFace,theme.TitleDepth,.25f));
            }
            Shape(vh,r,tab?13:14,rim);r=new Rect(r.x+2,r.y+2,r.width-4,r.height-4);
            var face=tab?Color.Lerp(theme.TitleDepth,theme.Accent,.3f):Color.Lerp(theme.Background,theme.Panel,.35f);
            Shape(vh,r,tab?12:13,face);
            if(tab)
            {
                var stitch=Color.Lerp(theme.TitleFace,theme.TitleDepth,.28f);
                for(float x=r.xMin+20;x<r.xMax-7;x+=13)
                {Poly(vh,stitch,new Vector2(x,r.yMin+8),new Vector2(x+6,r.yMin+8),new Vector2(x+6,r.yMin+9),new Vector2(x,r.yMin+9));Poly(vh,stitch,new Vector2(x,r.yMax-8),new Vector2(x+6,r.yMax-8),new Vector2(x+6,r.yMax-9),new Vector2(x,r.yMax-9));}
                MenuTileGraphic.Disc(vh,new Vector2(r.center.x-79,r.center.y),17,new Color(stitch.r,stitch.g,stitch.b,.16f));
            }
            else
            {
                // A recessed title interrupts the top rule, like the approved book-cover frame.
                float top=rectTransform.rect.yMax;
                Poly(vh,theme.Background,new Vector2(-112,top+3),new Vector2(112,top+3),
                    new Vector2(100,top-14),new Vector2(-100,top-14));
                foreach(float x in new[]{r.xMin,r.xMax})foreach(float y in new[]{r.yMin,r.yMax})
                {
                    float dx=x<0?1:-1,dy=y<0?1:-1;
                    Poly(vh,rim,new Vector2(x+dx*3,y+dy*13),new Vector2(x+dx*13,y+dy*3),new Vector2(x+dx*22,y+dy*3),new Vector2(x+dx*3,y+dy*22));
                }
            }
        }
    }
    public sealed class ShowcaseTabMotion:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        private const float InitialShakeDelaySeconds=4f;
        public RectTransform artwork;public Func<bool> viewed;public float shakeInterval;private bool hover;
        private float nextShake;private Vector2 rest;
        private float ShakeInterval=>Mathf.Max(0,shakeInterval);
        private void Start(){rest=artwork.anchoredPosition;nextShake=Time.unscaledTime+InitialShakeDelaySeconds;}
        public void OnPointerEnter(PointerEventData e){hover=true;}
        public void OnPointerExit(PointerEventData e){hover=false;}
        private void Update()
        {
            float blend=1-Mathf.Exp(-Time.unscaledDeltaTime*12);
            float now=Time.unscaledTime,angle=0;
            if(hover||viewed?.Invoke()==true)nextShake=now+ShakeInterval;
            else if(now>=nextShake)
            {
                float t=(now-nextShake)/.9f;
                if(t>=1)nextShake=now+ShakeInterval;
                else angle=Mathf.Sin(t*Mathf.PI*6)*Mathf.Sin(t*Mathf.PI)*4.5f;
            }
            var rotation=Quaternion.Euler(0,0,hover?0:2+angle);
            artwork.localRotation=angle!=0?rotation:Quaternion.Slerp(artwork.localRotation,rotation,blend);
            artwork.anchoredPosition=Vector2.Lerp(artwork.anchoredPosition,rest+new Vector2(hover?-7:0,0),blend);
        }
    }
    public sealed class ShowcaseMediaHover:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        public CanvasGroup tools;private bool hover;
        public void OnPointerEnter(PointerEventData e){hover=true;}
        public void OnPointerExit(PointerEventData e){hover=false;}
        private void Update(){tools.alpha=Mathf.MoveTowards(tools.alpha,hover?1:0,Time.unscaledDeltaTime*9);tools.blocksRaycasts=hover;}
    }
    public sealed class ShowcasePageMotion:MonoBehaviour
    {
        private CanvasGroup group;private float time;
        private void Awake(){group=gameObject.AddComponent<CanvasGroup>();}
        public void Restart(){time=0;group.alpha=0;}
        private void Update(){time+=Time.unscaledDeltaTime;group.alpha=Mathf.SmoothStep(0,1,Mathf.Clamp01(time/.15f));}
    }
    public sealed class ShowcaseWindowMotion:MonoBehaviour
    {
        public RectTransform panel;private CanvasGroup group;private float time;private bool closing;private Action finished;
        private void Awake(){group=gameObject.AddComponent<CanvasGroup>();group.alpha=0;}
        public void Close(Action complete){closing=true;time=0;finished=complete;group.blocksRaycasts=true;}
        private void Update()
        {
            time+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(time/(closing?.14f:.22f));float e=1-Mathf.Pow(1-t,3);
            group.alpha=closing?1-e:e;float s=closing?Mathf.Lerp(1,.98f,e):Mathf.Lerp(.95f,1,e);panel.localScale=Vector3.one*s;
            if(closing&&t>=1){enabled=false;finished?.Invoke();}
        }
    }
}
#endif
