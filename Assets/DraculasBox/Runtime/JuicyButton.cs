using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

namespace DraculasBox.Runtime
{
    public sealed class JuicyButton:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler
    {
        public bool Selected;public Action FocusRequested;private bool hover,pressed;private float pulse=-10,rejected=-10;private Vector2 origin;
        [SerializeField] private Graphic focusGraphic;
        public void Configure(Graphic graphic){focusGraphic=graphic;}
        public void Pulse(){pulse=Time.unscaledTime;}
        public void Reject(){rejected=Time.unscaledTime;origin=((RectTransform)transform).anchoredPosition;}
        public void OnPointerEnter(PointerEventData e){hover=true;FocusRequested?.Invoke();}
        public void OnPointerExit(PointerEventData e){hover=pressed=false;}
        public void OnPointerDown(PointerEventData e){pressed=true;}
        public void OnPointerUp(PointerEventData e){pressed=false;}
        private void Update()
        {
            float q=Time.unscaledTime-pulse;float tap=q<.18f?Mathf.Sin(q/.18f*Mathf.PI)*.075f:0;
            float target=(pressed?.94f:Selected||hover?1.045f:1)-tap;
            transform.localScale=Vector3.Lerp(transform.localScale,Vector3.one*target,1-Mathf.Exp(-Time.unscaledDeltaTime/.045f));
            float rejectTime=Time.unscaledTime-rejected;if(rejectTime<.22f)((RectTransform)transform).anchoredPosition=origin+Vector2.right*Mathf.Sin(rejectTime*83)*(1-rejectTime/.22f)*4;else if(rejected>0){((RectTransform)transform).anchoredPosition=origin;rejected=-10;}
            if(focusGraphic!=null){var c=focusGraphic.color;c.a=Mathf.Lerp(c.a,Selected?1:0,1-Mathf.Exp(-Time.unscaledDeltaTime/.055f));focusGraphic.color=c;}
        }
        private void OnDisable(){hover=pressed=false;transform.localScale=Vector3.one;}
    }
}
