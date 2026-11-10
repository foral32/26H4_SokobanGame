using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BranchFrameGraphic:MaskableGraphic
    {
        public static readonly Color Red=new Color32(205,84,95,255);
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=GetPixelAdjustedRect();const float width=3;
            Quad(mesh,new Rect(r.xMin,r.yMin,r.width,width));Quad(mesh,new Rect(r.xMin,r.yMax-width,r.width,width));Quad(mesh,new Rect(r.xMin,r.yMin,width,r.height));Quad(mesh,new Rect(r.xMax-width,r.yMin,width,r.height));
        }
        private void Quad(VertexHelper mesh,Rect r){int i=mesh.currentVertCount;mesh.AddVert(new Vector3(r.xMin,r.yMin),color,Vector2.zero);mesh.AddVert(new Vector3(r.xMin,r.yMax),color,Vector2.zero);mesh.AddVert(new Vector3(r.xMax,r.yMax),color,Vector2.zero);mesh.AddVert(new Vector3(r.xMax,r.yMin),color,Vector2.zero);mesh.AddTriangle(i,i+1,i+2);mesh.AddTriangle(i,i+2,i+3);}
    }
}
