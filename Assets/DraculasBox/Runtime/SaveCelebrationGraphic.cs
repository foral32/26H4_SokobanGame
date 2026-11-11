using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    // One short-lived UI mesh; paper never intercepts editor input.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SaveCelebrationGraphic:MaskableGraphic
    {
        public const float Lifetime=.9f;
        private struct Scrap
        {
            public Vector2 origin,velocity;
            public float size,aspect,angle,spin,delay,life;
            public Color ink;
        }
        private Scrap[] scraps;
        private float elapsed;
        private float lifetime=Lifetime,gravity=210,sway=5;
        private double startedAt;
        public void Initialize(float spread)
        {
            raycastTarget=false;
            var random=new System.Random(GetInstanceID());
            float Range(float low,float high)=>Mathf.Lerp(low,high,(float)random.NextDouble());
            var theme=GameThemes.Current;
            var palette=new[]{theme.Focus,theme.TitleDepth,theme.Foreground,theme.Accent};
            scraps=new Scrap[24];elapsed=0;lifetime=Lifetime;gravity=210;sway=5;startedAt=Time.realtimeSinceStartupAsDouble;
            for(int i=0;i<scraps.Length;i++)
            {
                float side=i%2==0?-1:1;
                scraps[i]=new Scrap
                {
                    origin=new Vector2(side*spread,-14),velocity=new Vector2(side*Range(85,170),Range(125,180)),
                    size=Range(3.5f,6.5f),aspect=i%3==0?2.2f:Range(.6f,1.3f),angle=Range(0,360),spin=Range(-600,600),
                    delay=Range(0,.045f),life=Range(.58f,.84f),ink=palette[i%palette.Length]
                };
            }
            SetVerticesDirty();
        }
        public void InitializeProgress(bool clearing,Vector2 spread)
        {
            raycastTarget=false;elapsed=0;lifetime=.62f;gravity=clearing?240:60;sway=1.2f;startedAt=Time.realtimeSinceStartupAsDouble;
            var random=new System.Random(GetInstanceID());
            float Range(float low,float high)=>Mathf.Lerp(low,high,(float)random.NextDouble());
            scraps=new Scrap[16];var theme=GameThemes.Current;
            for(int i=0;i<scraps.Length;i++)
            {
                float angle=(i+.5f)/scraps.Length*Mathf.PI*2;
                scraps[i]=new Scrap
                {
                    origin=new Vector2(Mathf.Cos(angle)*spread.x,Mathf.Sin(angle)*spread.y),
                    velocity=clearing?new Vector2(Range(-40,40),Range(0,20)):new Vector2(Mathf.Cos(angle)*Range(65,90),Mathf.Sin(angle)*Range(55,80)+15),
                    size=Range(3.5f,5),aspect=clearing?Range(1.7f,2.5f):Range(.8f,1.1f),angle=Range(0,360),spin=Range(-220,220),
                    delay=Range(0,.025f),life=Range(.43f,.58f),ink=i%3==0?theme.Foreground:theme.Focus
                };
            }
            SetVerticesDirty();
        }
        private void Update()
        {
            // A save may finish late in a slow frame; do not charge the effect
            // for time spent compiling or writing before it was created.
            elapsed=(float)(Time.realtimeSinceStartupAsDouble-startedAt);
            if(elapsed>=lifetime){gameObject.SetActive(false);Destroy(gameObject);return;}
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();if(scraps==null)return;
            foreach(var scrap in scraps)
            {
                float t=elapsed-scrap.delay;if(t<0||t>=scrap.life)continue;
                float age=t/scrap.life;
                var center=scrap.origin+scrap.velocity*t+Vector2.down*(gravity*t*t);
                center.x+=Mathf.Sin(t*17+scrap.angle)*t*sway;
                float angle=(scrap.angle+scrap.spin*t)*Mathf.Deg2Rad;
                float size=scrap.size*Mathf.Clamp01(t/.035f);
                var right=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*size*.5f;
                var up=new Vector2(-Mathf.Sin(angle),Mathf.Cos(angle))*size*.5f*scrap.aspect*(.35f+.65f*Mathf.Abs(Mathf.Cos(t*13+scrap.angle)));
                var ink=scrap.ink;ink.a*=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.6f,1,age));
                int at=mesh.currentVertCount;
                mesh.AddVert(center-right-up,ink,Vector2.zero);mesh.AddVert(center-right+up,ink,Vector2.zero);
                mesh.AddVert(center+right+up,ink,Vector2.zero);mesh.AddVert(center+right-up,ink,Vector2.zero);
                mesh.AddTriangle(at,at+1,at+2);mesh.AddTriangle(at,at+2,at+3);
            }
        }
    }
}
