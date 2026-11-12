using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    public enum CursorAppearance {Normal,Hover,Pressed}

    /// <summary>Presentation only: it neither consumes pointer events nor changes cursor locking.</summary>
    [DisallowMultipleComponent]
    public sealed class GameCursor:MonoBehaviour
    {
        public static readonly Vector2 Hotspot=new Vector2(3,2);
        private readonly Texture2D[] textures=new Texture2D[3];
        private readonly List<RaycastResult> hits=new List<RaycastResult>(16);
        private DraculaApp owner;
        private EventSystem events;
        private PointerEventData pointer;
        private CursorAppearance applied;
        private bool ownsCursor;
        public CursorAppearance Appearance {get;private set;}
        public Texture2D CurrentTexture=>textures[(int)Appearance];

        public void Initialize(DraculaApp app)
        {
            owner=app;
            textures[0]=Resources.Load<Texture2D>("Cursor/PointerNormal");
            textures[1]=Resources.Load<Texture2D>("Cursor/PointerHover");
            textures[2]=Resources.Load<Texture2D>("Cursor/PointerPressed");
            if(textures[0]==null||textures[1]==null||textures[2]==null)
            {Debug.LogWarning("Dracula's Box cursor artwork is missing; using the system cursor.",this);enabled=false;}
        }

        private void LateUpdate()
        {
            // Isolated checks never read hardware input or replace the user's desktop cursor.
            if(owner==null||owner.Automated)return;
            if(!PointerAvailable())
            {Appearance=CursorAppearance.Normal;RestoreSystemCursor();return;}
            Sample(Input.mousePosition,Input.GetMouseButton(0));
            if(ownsCursor&&applied==Appearance)return;
            Cursor.SetCursor(CurrentTexture,Hotspot,CursorMode.Auto);
            ownsCursor=true;applied=Appearance;
        }

        private static bool Inside(Vector2 screen)
            =>screen.x>=0&&screen.y>=0&&screen.x<Screen.width&&screen.y<Screen.height;

        private static bool PointerAvailable()
        {
            if(!Application.isPlaying||!Input.mousePresent)return false;
            #if UNITY_EDITOR
            var window=UnityEditor.EditorWindow.mouseOverWindow;
            if(!UnityEditorInternal.InternalEditorUtility.isApplicationActive||window==null||window.GetType().Name!="GameView")return false;
            #else
            if(!Application.isFocused)return false;
            #endif
            return Inside(Input.mousePosition);
        }

        private void Sample(Vector2 screen,bool held)
        {
            if(!Inside(screen)){Appearance=CursorAppearance.Normal;return;}
            if(held){Appearance=CursorAppearance.Pressed;return;}
            var current=EventSystem.current;
            if(current==null){Appearance=CursorAppearance.Normal;return;}
            if(events!=current||pointer==null){events=current;pointer=new PointerEventData(events);}
            pointer.Reset();pointer.position=screen;hits.Clear();events.RaycastAll(pointer,hits);
            // Only the frontmost graphic counts: a modal blocker must hide buttons behind it.
            var selectable=hits.Count==0?null:hits[0].gameObject.GetComponentInParent<Selectable>();
            Appearance=selectable!=null&&selectable.IsActive()&&selectable.IsInteractable()
                ?CursorAppearance.Hover:CursorAppearance.Normal;
        }

        private void RestoreSystemCursor()
        {
            if(!ownsCursor)return;
            Cursor.SetCursor(null,Vector2.zero,CursorMode.Auto);ownsCursor=false;
        }
        private void OnApplicationFocus(bool focused){if(!focused)RestoreSystemCursor();}
        private void OnApplicationPause(bool paused){if(paused)RestoreSystemCursor();}
        private void OnDisable(){Appearance=CursorAppearance.Normal;RestoreSystemCursor();}
        private void OnDestroy()=>RestoreSystemCursor();

        #if UNITY_EDITOR
        public bool OwnsHardwareCursorForCheck=>ownsCursor;
        public void SampleForCheck(Vector2 screen,bool held)
        {
            if(owner==null||!owner.Automated)throw new System.InvalidOperationException("Use an isolated cursor check.");
            Sample(screen,held);
        }
        #endif
    }
}
