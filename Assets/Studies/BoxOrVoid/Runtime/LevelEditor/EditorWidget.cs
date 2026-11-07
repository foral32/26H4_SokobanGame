using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BoxOrVoid.Study.LevelEditing
{
    public sealed class EditorWidget:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler
    {
        public string Id,Title,Shortcut;
        public Button Button;
        public Image Face;
        public TMP_Text Label;
        public EditorCornerGraphic Selection;
        private PlayerLevelEditor owner;
        private bool hovered,pressed;
        public void Bind(PlayerLevelEditor value,System.Action action=null)
        {owner=value;Button.onClick.RemoveAllListeners();Button.onClick.AddListener(()=>{if(owner.CanInteract){owner.Cue();if(action!=null)action();else owner.Act(Id);}});}
        public void Select(bool value){Selection.enabled=value;}
        public void Enable(bool value){Button.interactable=value;var group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();group.alpha=value?1:.4f;}
        public void OnPointerEnter(PointerEventData e){hovered=true;if(owner!=null)owner.Tooltip(Title+(string.IsNullOrEmpty(Shortcut)?"":"  ["+Shortcut+"]"));}
        public void OnPointerExit(PointerEventData e){hovered=false;pressed=false;if(owner!=null)owner.Tooltip("");}
        public void OnPointerDown(PointerEventData e){pressed=true;}
        public void OnPointerUp(PointerEventData e){pressed=false;}
        private void Update(){float target=pressed?.96f:hovered&&Button.interactable?1.035f:1;transform.localScale=Vector3.Lerp(transform.localScale,Vector3.one*target,1-Mathf.Exp(-Time.unscaledDeltaTime/0.055f));}
        private void OnDisable(){hovered=pressed=false;transform.localScale=Vector3.one;}
    }
}
