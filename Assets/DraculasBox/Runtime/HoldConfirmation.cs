using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    public sealed class HoldConfirmation:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
    {
        public const float Duration=1.4f;
        private bool mouseHeld,completed;
        private KeyCode keyboardKey;
        private float elapsed;
        private Action confirm;
        private Action<float> changed;
        private static readonly KeyCode[] ConfirmKeys={KeyCode.Return,KeyCode.KeypadEnter,KeyCode.Space};
        public float Amount=>Mathf.Clamp01(elapsed/Duration);
        public void Configure(Action confirm,Action<float> changed){this.confirm=confirm;this.changed=changed;Cancel();}
        public void Cancel(){mouseHeld=false;keyboardKey=KeyCode.None;elapsed=0;changed?.Invoke(0);}
        public void Retry(){completed=false;Cancel();}
        public void OnPointerDown(PointerEventData e)
        {if(e.button==PointerEventData.InputButton.Left&&!completed&&GetComponent<Button>().IsInteractable()){Cancel();mouseHeld=true;}}
        public void OnPointerUp(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)Cancel();}
        public void OnPointerExit(PointerEventData e)=>Cancel();
        private void OnDisable()=>Cancel();
        private void OnApplicationFocus(bool focus){if(!focus)Cancel();}
        public void Step(IStudyKeyboard keys,bool selected,bool active)
        {
            if(completed)return;
            if(!active||!GetComponent<Button>().IsInteractable()){Cancel();return;}
            if(!mouseHeld)
            {
                if(!selected){Cancel();return;}
                foreach(var key in ConfirmKeys)if(keys.Pressed(key))keyboardKey=key;
                if(keyboardKey==KeyCode.None||!keys.Held(keyboardKey)){Cancel();return;}
            }
            elapsed+=Time.unscaledDeltaTime;changed?.Invoke(Amount);
            if(elapsed<Duration)return;
            completed=true;mouseHeld=false;keyboardKey=KeyCode.None;confirm?.Invoke();
        }
    }
}
