using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    public enum GameIcon {Play,Create,Settings,Exit,Back,Undo,Reset,Pause,Lock,Check,Sun,Moon,Arrow,Edit}
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GameIconGraphic : MaskableGraphic
    {
        public GameIcon icon;
        private VertexHelper mesh;private float size;private Vector2 center;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();mesh=vh;var r=GetPixelAdjustedRect();size=Mathf.Min(r.width,r.height);center=r.center;
            switch(icon)
            {
                case GameIcon.Play:Polygon(new Vector2(-.22f,-.35f),new Vector2(-.22f,.35f),new Vector2(.37f,0));break;
                case GameIcon.Create:
                    for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)if(x!=1||y!=-1)Box(x*.24f-.075f,y*.24f-.075f,.15f,.15f);
                    Stroke(new Vector2(.10f,-.36f),new Vector2(.40f,-.06f),.075f);break;
                case GameIcon.Edit:
                    Polygon(new Vector2(-.25f,-.11f),new Vector2(-.11f,-.25f),new Vector2(.27f,.13f),new Vector2(.13f,.27f));
                    Polygon(new Vector2(-.35f,-.35f),new Vector2(-.29f,-.15f),new Vector2(-.15f,-.29f));
                    Polygon(new Vector2(.18f,.32f),new Vector2(.32f,.18f),new Vector2(.41f,.27f),new Vector2(.27f,.41f));break;
                case GameIcon.Settings:
                    Ring(.20f,.05f);for(int i=0;i<8;i++){float t=i*Mathf.PI/4;var dir=new Vector2(Mathf.Cos(t),Mathf.Sin(t));Stroke(dir*.24f,dir*.39f,.10f);}break;
                case GameIcon.Exit:
                    Path(new[]{new Vector2(-.05f,.34f),new Vector2(-.34f,.34f),new Vector2(-.34f,-.34f),new Vector2(-.05f,-.34f)});Arrow(new Vector2(-.08f,0),new Vector2(.38f,0));break;
                case GameIcon.Back:Arrow(new Vector2(.34f,0),new Vector2(-.34f,0));break;
                case GameIcon.Arrow:Arrow(new Vector2(-.34f,0),new Vector2(.34f,0));break;
                case GameIcon.Undo:
                    Path(new[]{new Vector2(-.30f,.20f),new Vector2(.05f,.20f),new Vector2(.29f,.05f),new Vector2(.29f,-.16f),new Vector2(.12f,-.31f),new Vector2(-.15f,-.31f)});Path(new[]{new Vector2(-.12f,.39f),new Vector2(-.32f,.20f),new Vector2(-.12f,.01f)});break;
                case GameIcon.Reset:
                    Arc(.31f,.04f,Mathf.PI*.70f,Mathf.PI*2.48f);Path(new[]{new Vector2(-.37f,.07f),new Vector2(-.22f,.29f),new Vector2(-.01f,.10f)});break;
                case GameIcon.Pause:Box(-.25f,-.34f,.15f,.68f);Box(.10f,-.34f,.15f,.68f);break;
                case GameIcon.Lock:
                    Path(new[]{new Vector2(-.20f,.02f),new Vector2(-.20f,.20f),new Vector2(-.12f,.33f),new Vector2(.12f,.33f),new Vector2(.20f,.20f),new Vector2(.20f,.02f)});
                    Box(-.29f,-.35f,.58f,.41f);MenuTileGraphic.Disc(mesh,Point(new Vector2(0,-.12f)),size*.045f,new Color(.18f,.22f,.28f,color.a));break;
                case GameIcon.Check:Path(new[]{new Vector2(-.34f,-.02f),new Vector2(-.08f,-.26f),new Vector2(.36f,.30f)},.10f);break;
                case GameIcon.Sun:
                    MenuTileGraphic.Disc(mesh,center,size*.18f,color);for(int i=0;i<8;i++){float t=i*Mathf.PI/4;var d=new Vector2(Mathf.Cos(t),Mathf.Sin(t));Stroke(d*.29f,d*.39f,.045f);}break;
                case GameIcon.Moon:
                    for(int i=0;i<36;i++){float a=-Mathf.PI*.5f+i*Mathf.PI/36,b=a+Mathf.PI/36;Polygon(new Vector2(0,-.34f),new Vector2(Mathf.Cos(a)*-.32f,Mathf.Sin(a)*.34f),new Vector2(Mathf.Cos(b)*-.32f,Mathf.Sin(b)*.34f));}break;
            }
        }
        private Vector2 Point(Vector2 p)=>center+p*size;
        private void Stroke(Vector2 a,Vector2 b,float width=.065f)=>MenuTileGraphic.Line(mesh,Point(a),Point(b),width*size,color);
        private void Path(Vector2[] points,float width=.065f){for(int i=1;i<points.Length;i++)Stroke(points[i-1],points[i],width);}
        private void Polygon(params Vector2[] points){var p=new Vector2[points.Length];for(int i=0;i<p.Length;i++)p[i]=Point(points[i]);MenuTileGraphic.Poly(mesh,p,color);}
        private void Box(float x,float y,float w,float h)=>MenuTileGraphic.Quad(mesh,new Rect(center.x+x*size,center.y+y*size,w*size,h*size),color);
        private void Ring(float radius,float width)=>Arc(radius,width,0,Mathf.PI*2);
        private void Arc(float radius,float width,float start,float end){for(int i=0;i<48;i++){float a=Mathf.Lerp(start,end,i/48f),b=Mathf.Lerp(start,end,(i+1)/48f);Stroke(new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,width);}}
        private void Arrow(Vector2 a,Vector2 b){Stroke(a,b);var v=(b-a).normalized;var n=new Vector2(-v.y,v.x);Path(new[]{b-v*.16f+n*.16f,b,b-v*.16f-n*.16f});}
    }
}
