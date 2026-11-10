using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    /// <summary>The title's O is the same square vampire, drawn at UI resolution.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class VampireLogoGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var theme=GameThemes.Current;var rect=GetPixelAdjustedRect();
            float s=Mathf.Min(rect.width,rect.height);Vector2 center=rect.center;
            Vector2 P(float x,float y)=>center+new Vector2(x*s,y*s);
            void Polygon(Color ink,params Vector2[] points)=>MenuTileGraphic.Poly(vh,points,ink);
            Rect Face(Vector2 offset)=>new Rect(center.x-.405f*s+offset.x,center.y-.365f*s+offset.y,.81f*s,.81f*s);
            MenuTileGraphic.Quad(vh,Face(new Vector2(5,-7)),new Color(0,0,0,.20f));
            Polygon(theme.CapeShade,P(-.29f,-.10f),P(.29f,-.10f),P(.45f,-.442f),P(-.45f,-.442f));
            Polygon(theme.Cape,P(-.29f,-.10f),P(.29f,-.10f),P(.43f,-.414f),P(-.43f,-.414f));
            MenuTileGraphic.Quad(vh,Face(new Vector2(3,-4)),theme.TitleDepth);
            var face=Face(Vector2.zero);float b=s*.032f;
            var light=Color.Lerp(theme.TitleFace,Color.white,.76f);
            var shade=Color.Lerp(theme.TitleFace,theme.TitleDepth,.42f);
            MenuTileGraphic.Quad(vh,face,shade);
            Polygon(light,new Vector2(face.xMin,face.yMax),new Vector2(face.xMax,face.yMax),new Vector2(face.xMax-b,face.yMax-b),new Vector2(face.xMin+b,face.yMax-b));
            Polygon(Color.Lerp(theme.TitleFace,Color.white,.38f),new Vector2(face.xMin,face.yMin),new Vector2(face.xMin,face.yMax),new Vector2(face.xMin+b,face.yMax-b),new Vector2(face.xMin+b,face.yMin+b));
            MenuTileGraphic.Quad(vh,new Rect(face.x+b,face.y+b,face.width-b*2,face.height-b*2),theme.TitleFace);
            foreach(float x in new[]{-.13f,.13f})
            {
                float w=s*.052f,h=s*.15f,r=w*.5f;Vector2 eye=P(x,.05f);
                var ink=new Color32(37,32,39,255);
                MenuTileGraphic.Quad(vh,new Rect(eye.x-r,eye.y-h*.5f+r,w,h-2*r),ink);
                MenuTileGraphic.Disc(vh,eye+Vector2.up*(h*.5f-r),r,ink);
                MenuTileGraphic.Disc(vh,eye-Vector2.up*(h*.5f-r),r,ink);
            }
            foreach(float x in new[]{-.16f,.16f})
                Polygon(light,P(x-.024f,-.35f),P(x+.024f,-.35f),P(x,-.413f));
        }
    }
}
