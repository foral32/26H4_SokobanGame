using UnityEngine;
using UnityEngine.UI;

namespace BoxOrVoid.Study.LevelEditing
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class EditorIconGraphic:MaskableGraphic
    {
        public string Icon="plus";
        protected override void OnPopulateMesh(VertexHelper v)
        {
            v.Clear();
            switch(Icon)
            {
                case "play":Path(v,7,4,26,16,7,28,7,4);break;
                case "back":Path(v,18,6,7,16,18,26);Path(v,7,16,28,16);break;
                case "undo":Path(v,11,5,3,12,11,19);Path(v,3,12,20,12,26,17,26,23,22,28,15,28);break;
                case "redo":Path(v,21,5,29,12,21,19);Path(v,29,12,12,12,6,17,6,23,10,28,17,28);break;
                case "look":Path(v,2,16,9,8,23,8,30,16,23,24,9,24,2,16);Circle(v,16,16,4);break;
                case "erase":Path(v,3,19,17,4,29,16,17,28,12,28,3,19);Path(v,10,12,22,23);break;
                case "dots":Circle(v,6,16,2);Circle(v,16,16,2);Circle(v,26,16,2);break;
                case "tools":Box(v,3,3,10,10);Box(v,19,3,10,10);Box(v,3,19,10,10);Box(v,19,19,10,10);break;
                case "place":case "pencil":Path(v,5,27,8,18,24,3,29,8,14,24,5,27);Path(v,20,7,25,12);break;
                case "save":Box(v,5,3,22,26);Box(v,10,3,12,9);Box(v,10,19,12,10);break;
                case "import":Path(v,16,2,16,23,8,15);Path(v,16,23,24,15);Path(v,3,21,3,29,29,29,29,21);break;
                case "select":case "rectangle":Box(v,4,4,24,24);break;
                case "line":Path(v,4,28,28,4);Circle(v,4,28,2);Circle(v,28,4,2);break;
                case "switch":Path(v,3,10,28,10,21,3);Path(v,29,22,4,22,11,29);break;
                case "help":Path(v,9,8,12,3,21,3,25,8,25,12,16,20,16,23);Circle(v,16,29,1.5f);break;
                case "close":Path(v,7,7,25,25);Path(v,7,25,25,7);break;
                case "mouse":Path(v,8,3,24,3,27,8,27,24,23,29,9,29,5,24,5,8,8,3);Path(v,16,3,16,13);Path(v,5,13,27,13);break;
                default:Path(v,16,3,16,29);Path(v,3,16,29,16);break;
            }
        }
        private Vector2 P(float x,float y){var r=rectTransform.rect;return new Vector2(r.xMin+x/32*r.width,r.yMax-y/32*r.height);}
        private void Path(VertexHelper v,params float[] xy){for(int i=2;i<xy.Length;i+=2)Line(v,P(xy[i-2],xy[i-1]),P(xy[i],xy[i+1]),rectTransform.rect.width*.075f);}
        private void Box(VertexHelper v,float x,float y,float w,float h)=>Path(v,x,y,x+w,y,x+w,y+h,x,y+h,x,y);
        private void Circle(VertexHelper v,float x,float y,float radius){for(int i=0;i<20;i++){float a=i*Mathf.PI*2/20,b=(i+1)*Mathf.PI*2/20;Line(v,P(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius),P(x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius),rectTransform.rect.width*.06f);}}
        private void Line(VertexHelper v,Vector2 a,Vector2 b,float width){Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int i=v.currentVertCount;v.AddVert(a+n,color,Vector2.zero);v.AddVert(a-n,color,Vector2.zero);v.AddVert(b-n,color,Vector2.zero);v.AddVert(b+n,color,Vector2.zero);v.AddTriangle(i,i+1,i+2);v.AddTriangle(i,i+2,i+3);}
    }
}
