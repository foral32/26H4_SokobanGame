using UnityEngine;
using UnityEngine.UI;

namespace BoxOrVoid.Study.LevelEditing
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class EditorCornerGraphic:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;float arm=Mathf.Min(r.width,r.height)*.24f,t=4;
            Add(vh,new Rect(r.xMin,r.yMax-t,arm,t));Add(vh,new Rect(r.xMax-arm,r.yMax-t,arm,t));Add(vh,new Rect(r.xMin,r.yMin,arm,t));Add(vh,new Rect(r.xMax-arm,r.yMin,arm,t));
            Add(vh,new Rect(r.xMin,r.yMin,t,arm));Add(vh,new Rect(r.xMax-t,r.yMin,t,arm));Add(vh,new Rect(r.xMin,r.yMax-arm,t,arm));Add(vh,new Rect(r.xMax-t,r.yMax-arm,t,arm));
        }
        private void Add(VertexHelper vh,Rect r){int i=vh.currentVertCount;vh.AddVert(new Vector2(r.xMin,r.yMin),color,Vector2.zero);vh.AddVert(new Vector2(r.xMax,r.yMin),color,Vector2.zero);vh.AddVert(new Vector2(r.xMax,r.yMax),color,Vector2.zero);vh.AddVert(new Vector2(r.xMin,r.yMax),color,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);}
    }
}
