using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BoxOrVoid.Study.LevelEditing
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class EditorGridGraphic:MaskableGraphic
    {
        public int Width=9,Height=7;
        public float CellSize=52;
        public bool Strong;
        public readonly HashSet<int> Highlights=new HashSet<int>();
        public void Refresh(){SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper v)
        {
            v.Clear();var r=rectTransform.rect;Color line=new Color(.7f,.8f,.85f,Strong?.20f:.035f);
            for(int x=0;x<=Width;x++)Quad(v,new Rect(r.xMin+x*CellSize-.5f,r.yMin,1,r.height),line);
            for(int y=0;y<=Height;y++)Quad(v,new Rect(r.xMin,r.yMin+y*CellSize-.5f,r.width,1),line);
            foreach(int i in Highlights)Quad(v,new Rect(r.xMin+(i%Width)*CellSize,r.yMin+(i/Width)*CellSize,CellSize,CellSize),new Color(.55f,.85f,.28f,.23f));
        }
        private static void Quad(VertexHelper v,Rect r,Color c){int i=v.currentVertCount;v.AddVert(new Vector2(r.xMin,r.yMin),c,Vector2.zero);v.AddVert(new Vector2(r.xMax,r.yMin),c,Vector2.zero);v.AddVert(new Vector2(r.xMax,r.yMax),c,Vector2.zero);v.AddVert(new Vector2(r.xMin,r.yMax),c,Vector2.zero);v.AddTriangle(i,i+1,i+2);v.AddTriangle(i,i+2,i+3);}
    }
}
