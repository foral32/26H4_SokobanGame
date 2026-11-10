#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DraculasBox.Runtime
{
    public sealed class CampaignRowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public Action<Vector2> Begin,Move,End;
        public Action<Vector2,float> EdgePan;
        public Action Cancel,Context;
        private bool held;
        private int pointer;
        private Vector2 last;
        public void OnBeginDrag(PointerEventData e)
        {
            if(held||e.button!=PointerEventData.InputButton.Left)return;
            held=true;pointer=e.pointerId;last=e.position;e.eligibleForClick=false;
            Begin?.Invoke(e.pressPosition);Move?.Invoke(e.position);
        }
        public void OnDrag(PointerEventData e){if(held&&e.pointerId==pointer)Move?.Invoke(last=e.position);}
        public void OnEndDrag(PointerEventData e)
        {
            if(!held||e.pointerId!=pointer)return;
            held=false;e.eligibleForClick=false;End?.Invoke(e.position);
        }
        public void OnPointerClick(PointerEventData e){if(e.button==PointerEventData.InputButton.Right)Context?.Invoke();}
        private void Update(){if(held)EdgePan?.Invoke(last,Time.unscaledDeltaTime);}
        private void OnApplicationFocus(bool focused){if(!focused)Release();}
        private void OnDisable()=>Release();
        private void Release(){if(!held)return;held=false;Cancel?.Invoke();}
    }
}
#endif
