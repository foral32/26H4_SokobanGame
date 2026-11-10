using UnityEngine;
namespace DraculasBox.Runtime
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class WorkshopPopover:MonoBehaviour
    {
        private CanvasGroup group;private float opened;
        private void OnEnable(){group=GetComponent<CanvasGroup>();opened=Time.unscaledTime;group.alpha=0;transform.localScale=Vector3.one*.985f;}
        private void Update(){float t=1-Mathf.Pow(1-Mathf.Clamp01((Time.unscaledTime-opened)/.12f),3);group.alpha=t;transform.localScale=Vector3.one*Mathf.Lerp(.985f,1,t);}
        private void OnDisable(){if(group!=null)group.alpha=1;transform.localScale=Vector3.one;}
    }
}
