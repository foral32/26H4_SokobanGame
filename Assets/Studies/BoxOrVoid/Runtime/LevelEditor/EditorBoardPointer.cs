using UnityEngine;
using UnityEngine.EventSystems;

namespace BoxOrVoid.Study.LevelEditing
{
    public sealed class EditorBoardPointer:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IPointerMoveHandler,IPointerExitHandler,IScrollHandler
    {
        private PlayerLevelEditor owner;
        public void Bind(PlayerLevelEditor value)=>owner=value;
        public void OnPointerDown(PointerEventData e){owner?.PointerDown(e);}
        public void OnPointerUp(PointerEventData e){owner?.PointerUp(e);}
        public void OnBeginDrag(PointerEventData e){}
        public void OnDrag(PointerEventData e){owner?.PointerMove(e);}
        public void OnEndDrag(PointerEventData e){owner?.PointerUp(e);}
        public void OnPointerMove(PointerEventData e){owner?.PointerMove(e);}
        public void OnPointerExit(PointerEventData e){owner?.PointerExit();}
        public void OnScroll(PointerEventData e){owner?.PointerScroll(e);}
    }
}
