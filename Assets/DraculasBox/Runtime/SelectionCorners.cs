using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SelectionCorners:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();float length=Mathf.Min(18,r.width*.16f),width=3;
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})
            {var p=new Vector2(x<0?r.xMin:r.xMax,y<0?r.yMin:r.yMax);MenuTileGraphic.Line(vh,p,p+new Vector2(-x*length,0),width,color);MenuTileGraphic.Line(vh,p,p+new Vector2(0,-y*length),width,color);}
        }
    }
}
