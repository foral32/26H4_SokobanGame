using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace DraculasBox.Runtime
{
    public sealed class LibraryDoubleClick:MonoBehaviour,IPointerClickHandler
    {
        public Action Open;
        public void OnPointerClick(PointerEventData data){if(data.button==PointerEventData.InputButton.Left&&data.clickCount==2)Open?.Invoke();}
    }
}
