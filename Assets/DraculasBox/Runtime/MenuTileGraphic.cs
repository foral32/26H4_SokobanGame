using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    /// <summary>Geometric menu pieces use the same bevel and cast shadow as the board.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MenuTileGraphic : MaskableGraphic
    {
        public bool round;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();
            if(round)
            {
                Disc(vh,r.center+new Vector2(4,-6),r.width*.5f,new Color(0,0,0,.30f));
                Disc(vh,r.center,r.width*.5f,color);
                return;
            }
            Quad(vh,new Rect(r.x+5,r.y-6,r.width,r.height),new Color(0,0,0,.28f));
            Quad(vh,r,Shade(color,.65f));
            float b=Mathf.Min(8,r.width*.06f);
            Poly(vh,new[]{new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,r.yMax),new Vector2(r.xMax-b,r.yMax-b),new Vector2(r.xMin+b,r.yMax-b)},Shade(color,1.18f));
            Poly(vh,new[]{new Vector2(r.xMin,r.yMin),new Vector2(r.xMin,r.yMax),new Vector2(r.xMin+b,r.yMax-b),new Vector2(r.xMin+b,r.yMin+b)},Shade(color,1.05f));
            Quad(vh,new Rect(r.x+b,r.y+b,r.width-2*b,r.height-2*b),color);
        }
        private static Color Shade(Color c,float v)=>new Color(c.r*v,c.g*v,c.b*v,c.a);
        internal static void Quad(VertexHelper vh,Rect r,Color c)
        {Poly(vh,new[]{new Vector2(r.xMin,r.yMin),new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,r.yMax),new Vector2(r.xMax,r.yMin)},c);}
        internal static void Poly(VertexHelper vh,Vector2[] points,Color c)
        {int start=vh.currentVertCount;foreach(var p in points)vh.AddVert(p,c,Vector2.zero);for(int i=1;i<points.Length-1;i++)vh.AddTriangle(start,start+i,start+i+1);}
        internal static void Disc(VertexHelper vh,Vector2 center,float radius,Color c)
        {int start=vh.currentVertCount;vh.AddVert(center,c,Vector2.zero);for(int i=0;i<=64;i++){float t=i*Mathf.PI/32;vh.AddVert(center+new Vector2(Mathf.Cos(t),Mathf.Sin(t))*radius,c,Vector2.zero);if(i>0)vh.AddTriangle(start,start+i,start+i+1);}}
        internal static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c)
        {var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;Poly(vh,new[]{a-n,a+n,b+n,b-n},c);}
    }
}
