using TMPro;
using UnityEngine;

namespace DraculasBox.Runtime
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class HoldTextMotion:MonoBehaviour
    {
        private TMP_Text label;
        private RectTransform visual;
        private Vector2 origin,anchor;
        private Vector3 initialScale;
        private Quaternion initialRotation;
        private float target,progress,phase,completedAt;
        private bool animating,completing;
        private void Awake()
        {
            label=GetComponent<TMP_Text>();visual=(RectTransform)transform;
            origin=visual.anchoredPosition;initialScale=visual.localScale;initialRotation=visual.localRotation;
        }
        public void SetProgress(float value)
        {
            value=Mathf.Clamp01(value);
            if(value>0&&target==0)
            {
                label.ForceMeshUpdate();var bounds=label.textBounds;
                // Grow from the text's left edge, keeping the menu alignment and hit target stable.
                anchor=new Vector2(bounds.min.x,bounds.center.y);phase=0;completing=false;animating=true;
            }
            target=value;
        }
        public void Complete()
        {target=progress=0;completing=animating=true;completedAt=Time.unscaledTime;Apply(1.22f,Vector2.zero,0);}
        public void ResetMotion()
        {
            target=progress=phase=0;animating=completing=false;
            if(visual==null)return;visual.anchoredPosition=origin;visual.localScale=initialScale;visual.localRotation=initialRotation;
        }
        private void OnDisable()=>ResetMotion();
        private void LateUpdate()
        {
            if(!animating)return;
            if(completing)
            {
                float t=Time.unscaledTime-completedAt;
                if(t>=.5f){ResetMotion();return;}
                Apply(1+.22f*Mathf.Exp(-t*10)*Mathf.Cos(t*18),Vector2.zero,0);return;
            }
            progress=Mathf.Lerp(progress,target,1-Mathf.Exp(-Time.unscaledDeltaTime/(target>0?.035f:.055f)));
            if(target==0&&progress<.0005f){ResetMotion();return;}
            float charge=Mathf.SmoothStep(0,1,progress),tension=charge*charge;
            phase+=Time.unscaledDeltaTime*Mathf.Lerp(14,26,charge)*Mathf.PI*2;
            var shake=new Vector2(Mathf.Sin(phase),Mathf.Sin(phase*1.37f+.8f))*1.7f*tension;
            Apply(1+.18f*charge,shake,Mathf.Sin(phase*.83f)*.22f*tension);
        }
        private void Apply(float scale,Vector2 shake,float angle)
        {
            var rotation=initialRotation*Quaternion.Euler(0,0,angle);
            var fixedAnchor=initialRotation*Vector3.Scale(anchor,initialScale);
            visual.localScale=initialScale*scale;visual.localRotation=rotation;
            visual.anchoredPosition=origin+(Vector2)(fixedAnchor-rotation*Vector3.Scale(anchor,visual.localScale))+shake;
        }
    }
}
