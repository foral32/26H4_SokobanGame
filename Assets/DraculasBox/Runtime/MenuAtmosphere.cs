using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MenuAtmosphere:MaskableGraphic
    {
        public bool titleLayout;
        private static readonly Vector2[] titlePoints={new Vector2(.097f,.90f),new Vector2(.132f,.834f),new Vector2(.928f,.91f),new Vector2(.91f,.165f),new Vector2(.89f,.124f),new Vector2(.06f,.112f)};
        private static readonly float[] titleSizes={64,43,30,40,36,25};
        private float nextMesh;
        private void Update(){if(Time.unscaledTime<nextMesh)return;nextMesh=Time.unscaledTime+.08f;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();const int steps=12;
            for(int y=0;y<=steps;y++)for(int x=0;x<=steps;x++)
            {float u=x/(float)steps,v=y/(float)steps;float shade=Mathf.Clamp01(new Vector2((u-.43f)*1.35f,(v-.57f)*1.2f).sqrMagnitude*1.8f);var c=Color.Lerp(GameThemes.Current.AtmosphereCenter,GameThemes.Current.AtmosphereEdge,shade);vh.AddVert(new Vector2(Mathf.Lerp(r.xMin,r.xMax,u),Mathf.Lerp(r.yMin,r.yMax,v)),c,Vector2.zero);}
            for(int y=0;y<steps;y++)for(int x=0;x<steps;x++){int i=y*(steps+1)+x;vh.AddTriangle(i,i+steps+1,i+steps+2);vh.AddTriangle(i,i+steps+2,i+1);}
            if(titleLayout)
            {
                for(int i=0;i<titlePoints.Length;i++)
                {
                    var p=new Vector2(Mathf.Lerp(r.xMin,r.xMax,titlePoints[i].x),Mathf.Lerp(r.yMin,r.yMax,titlePoints[i].y));
                    var c=Color.Lerp(GameThemes.Current.Secondary,GameThemes.Current.Accent,.15f);c.a=.10f;
                    var a=p-Vector2.one*titleSizes[i]*.5f;var b=p+Vector2.one*titleSizes[i]*.5f;
                    MenuTileGraphic.Line(vh,a,new Vector2(a.x,b.y),1,c);MenuTileGraphic.Line(vh,new Vector2(a.x,b.y),b,1,c);MenuTileGraphic.Line(vh,b,new Vector2(b.x,a.y),1,c);MenuTileGraphic.Line(vh,new Vector2(b.x,a.y),a,1,c);
                }
                return;
            }
            for(int i=0;i<12;i++)
            {
                float t=Time.unscaledTime*.07f+i*1.618f;float px=Mathf.Repeat(i*.371f+.07f,1),py=Mathf.Repeat(i*.613f+.16f,1);var p=new Vector2(Mathf.Lerp(r.xMin,r.xMax,px),Mathf.Lerp(r.yMin,r.yMax,py))+new Vector2(Mathf.Sin(t),Mathf.Cos(t*.73f))*12;
                float s=22+(i%4)*19;var c=GameThemes.Current.Accent;c.a=.045f+(i%3)*.012f;
                var a=p-Vector2.one*s*.5f;var b=p+Vector2.one*s*.5f;
                MenuTileGraphic.Line(vh,a,new Vector2(a.x,b.y),2,c);MenuTileGraphic.Line(vh,new Vector2(a.x,b.y),b,2,c);MenuTileGraphic.Line(vh,b,new Vector2(b.x,a.y),2,c);MenuTileGraphic.Line(vh,new Vector2(b.x,a.y),a,2,c);
            }
        }
    }
}
