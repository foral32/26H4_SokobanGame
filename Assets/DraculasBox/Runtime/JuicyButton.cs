using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

namespace DraculasBox.Runtime
{
    public sealed class JuicyButton:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler
    {
        public bool Selected;public Action FocusRequested;private bool hover,pressed;private float pulse=-10,rejected=-10;private Vector2 origin;
        public bool ScaleOnPress {get;set;}=true;
        [SerializeField] private Graphic focusGraphic;
        public void Configure(Graphic graphic){focusGraphic=graphic;}
        public void Pulse(){pulse=Time.unscaledTime;}
        private float conquered=-10;
        public void Conquer(){conquered=Time.unscaledTime;}
        public void Reject(){rejected=Time.unscaledTime;origin=((RectTransform)transform).anchoredPosition;}
        public void OnPointerEnter(PointerEventData e){hover=true;FocusRequested?.Invoke();}
        public void OnPointerExit(PointerEventData e){hover=pressed=false;}
        public void OnPointerDown(PointerEventData e){pressed=true;}
        public void OnPointerUp(PointerEventData e){pressed=false;}
        private void Update()
        {
            float q=Time.unscaledTime-pulse;float tap=q<.18f?Mathf.Sin(q/.18f*Mathf.PI)*.075f:0;
            float target=(pressed&&ScaleOnPress?.94f:hover?1.08f:Selected?1.045f:1)-tap;
            float clearTime=Time.unscaledTime-conquered;var stretch=Vector3.one;
            if(clearTime<.62f)
            {
                float wave=clearTime<.085f?-Mathf.Sin(clearTime/.085f*Mathf.PI)*.16f:Mathf.Sin((clearTime-.085f)*18)*Mathf.Exp(-(clearTime-.085f)*7)*.28f;
                stretch=new Vector3(1-wave*.48f,1+wave,1);
            }
            transform.localScale=Vector3.Lerp(transform.localScale,stretch*target,1-Mathf.Exp(-Time.unscaledDeltaTime/(clearTime<.62f?.035f:.045f)));
            float rejectTime=Time.unscaledTime-rejected;if(rejectTime<.22f)((RectTransform)transform).anchoredPosition=origin+Vector2.right*Mathf.Sin(rejectTime*83)*(1-rejectTime/.22f)*4;else if(rejected>0){((RectTransform)transform).anchoredPosition=origin;rejected=-10;}
            if(focusGraphic!=null){var c=focusGraphic.color;c.a=Mathf.Lerp(c.a,Selected?1:0,1-Mathf.Exp(-Time.unscaledDeltaTime/.055f));focusGraphic.color=c;}
        }
        private void OnDisable(){hover=pressed=false;transform.localScale=Vector3.one;}
    }
}
