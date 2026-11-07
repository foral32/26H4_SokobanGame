using UnityEngine;
using UnityEngine.UI;

namespace BoxOrVoid.Study
{
    /// <summary>A circular aperture in canvas units, so it stays round at every aspect ratio.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StudyIrisGraphic:MaskableGraphic
    {
        private float aperture=1.1f;
        public float Aperture {get=>aperture;set {aperture=Mathf.Clamp(value,0,1.1f);SetVerticesDirty();}}
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();if(aperture>=1.0999f) return;
            var rect=rectTransform.rect;var center=rect.center;
            float diagonal=rect.size.magnitude*.5f,radius=diagonal*aperture;
            const int segments=192;
            Color32 opaque=color,transparent=new Color(color.r,color.g,color.b,0);
            if(radius<.01f)
            {
                mesh.AddVert(new Vector2(rect.xMin,rect.yMin),opaque,Vector2.zero);
                mesh.AddVert(new Vector2(rect.xMin,rect.yMax),opaque,Vector2.zero);
                mesh.AddVert(new Vector2(rect.xMax,rect.yMax),opaque,Vector2.zero);
                mesh.AddVert(new Vector2(rect.xMax,rect.yMin),opaque,Vector2.zero);
                mesh.AddTriangle(0,1,2);mesh.AddTriangle(0,2,3);return;
            }
            float feather=1.2f/(canvas!=null?canvas.scaleFactor:1);
            for(int i=0;i<=segments;i++)
            {
                float angle=i*Mathf.PI*2/segments;var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                mesh.AddVert(center+direction*Mathf.Max(0,radius-feather),transparent,Vector2.zero);
                mesh.AddVert(center+direction*radius,opaque,Vector2.zero);
                mesh.AddVert(center+direction*(diagonal*2+10),opaque,Vector2.zero);
                if(i==0) continue;
                int a=(i-1)*3,b=i*3;
                mesh.AddTriangle(a,b,b+1);mesh.AddTriangle(a,b+1,a+1);
                mesh.AddTriangle(a+1,b+1,b+2);mesh.AddTriangle(a+1,b+2,a+2);
            }
        }
    }
}
