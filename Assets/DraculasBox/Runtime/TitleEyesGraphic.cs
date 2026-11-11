using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TitleEyesGraphic:MaskableGraphic
    {
        private GameUI owner;private TitleArtworkGraphic artwork;
        private Vector2 gaze;private double nextBlink,blinkAt=-10,lastUpdate;
        private System.Random random;
        private bool automated,pointerInitialized,pointerMode;private Vector2 lastPointer;private string lastSelection;
        private Vector2? checkPointer;
        public Vector2 Gaze=>gaze;
        public float Openness {get;private set;}=1;
        public void Initialize(GameUI ui,TitleArtworkGraphic source,bool isolated=false){owner=ui;artwork=source;automated=isolated;raycastTarget=false;Restart();}
        public void SetPointerForCheck(Vector2? screen){if(automated)checkPointer=screen;}
        private void Restart(){gaze=Vector2.zero;Openness=1;random=new System.Random(GetInstanceID());lastUpdate=Time.realtimeSinceStartupAsDouble;nextBlink=lastUpdate+2.3;blinkAt=-10;pointerInitialized=pointerMode=false;lastSelection=null;SetVerticesDirty();}
        protected override void OnEnable(){base.OnEnable();Restart();}
        private void Update()
        {
            if(owner==null||artwork==null||!Application.isPlaying)return;
            double now=Time.realtimeSinceStartupAsDouble;float dt=Mathf.Clamp((float)(now-lastUpdate),0,.1f);lastUpdate=now;
            if(now>=nextBlink){blinkAt=now;nextBlink=now+3.1+random.NextDouble()*2.5;}
            float phase=(float)((now-blinkAt)/.17);Openness=phase>=0&&phase<1?1-.95f*Mathf.Sin(phase*Mathf.PI):1;
            Vector2 aim=Vector2.zero,point=Vector2.zero;
            string selected=owner.SelectedId;bool hasPoint=false;
            bool pointerAvailable=TryPointer(out var screen);
            bool moved=pointerAvailable&&(!pointerInitialized||(screen-lastPointer).sqrMagnitude>1);
            if(moved)pointerMode=true;
            else if(selected!=lastSelection)pointerMode=false;
            if(pointerAvailable){lastPointer=screen;pointerInitialized=true;}
            lastSelection=selected;
            if(pointerMode&&pointerAvailable)
                hasPoint=RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,screen,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out point);
            else
            {
                var target=owner.FindButton(selected??"");
                if(target!=null){var rect=(RectTransform)target.transform;point=rectTransform.InverseTransformPoint(rect.TransformPoint(rect.rect.center));hasPoint=true;}
            }
            if(hasPoint&&artwork.EyeBounds(out var a,out var b))
            {
                var delta=point-(a.center+b.center)*.5f;
                aim=new Vector2(Mathf.Clamp(delta.x*.035f,-6,6),Mathf.Clamp(delta.y*.025f,-5.5f,5.5f));
            }
            gaze=Vector2.Lerp(gaze,aim,1-Mathf.Exp(-dt/.085f));SetVerticesDirty();
        }
        private bool TryPointer(out Vector2 screen)
        {
            screen=Vector2.zero;
            if(automated){if(!checkPointer.HasValue)return false;screen=checkPointer.Value;}
            else
            {
                bool available=Application.isFocused;
                #if UNITY_EDITOR
                var hovered=UnityEditor.EditorWindow.mouseOverWindow;
                available|=hovered!=null&&hovered.GetType().Name=="GameView";
                #endif
                if(!available||!Input.mousePresent)return false;
                screen=Input.mousePosition;
            }
            return screen.x>=0&&screen.y>=0&&screen.x<Screen.width&&screen.y<Screen.height;
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();if(!Application.isPlaying||artwork==null||!artwork.EyeBounds(out var left,out var right))return;
            DrawEye(mesh,left);DrawEye(mesh,right);
        }
        private void DrawEye(VertexHelper mesh,Rect original)
        {
            var center=original.center+gaze;float w=original.width,h=Mathf.Max(1.5f,original.height*Openness),r=Mathf.Min(w,h)*.5f;
            Color ink=new Color32(31,29,31,255);
            if(h>=w)
            {
                MenuTileGraphic.Quad(mesh,new Rect(center.x-w*.5f,center.y-h*.5f+r,w,Mathf.Max(0,h-2*r)),ink);
                MenuTileGraphic.Disc(mesh,center+Vector2.up*(h*.5f-r),r,ink);MenuTileGraphic.Disc(mesh,center-Vector2.up*(h*.5f-r),r,ink);
            }
            else
            {
                MenuTileGraphic.Quad(mesh,new Rect(center.x-w*.5f+r,center.y-h*.5f,Mathf.Max(0,w-2*r),h),ink);
                MenuTileGraphic.Disc(mesh,center+Vector2.right*(w*.5f-r),r,ink);MenuTileGraphic.Disc(mesh,center-Vector2.right*(w*.5f-r),r,ink);
            }
        }
    }
}
