using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BoxOrVoid.Core;

namespace BoxOrVoid.Study
{
    public sealed class PuzzleHud : MonoBehaviour
    {
        private TMP_Text levelTitle, spaceText, undoText, resetText;
        private Image spaceIcon,undoIcon,resetIcon;
        private Canvas canvas;
        public void SetVisible(bool visible) {canvas.gameObject.SetActive(visible);}
        public void Initialize(StudyAssets assets,Camera camera)
        {
            var go=new GameObject("HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));go.transform.SetParent(transform,false);
            canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=2;canvas.sortingOrder=100;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            levelTitle=Label("Level",assets,new Vector2(1,1),new Vector2(-24,-20),new Vector2(340,48),TextAlignmentOptions.TopRight,32);
            spaceText=Label("Switch",assets,new Vector2(1,.5f),new Vector2(-145,4),new Vector2(210,48),TextAlignmentOptions.Center,30);spaceText.text="逆转空间";
            spaceIcon=Icon("Space",assets.Sprite("keyboard_space_icon_outline"),new Vector2(1,.5f),new Vector2(-145,72));
            undoText=Label("Undo",assets,new Vector2(1,.5f),new Vector2(-110,70),new Vector2(140,40),TextAlignmentOptions.Center,23);undoText.text="撤回";
            undoIcon=Icon("Z",assets.Sprite("keyboard_z_outline"),new Vector2(1,.5f),new Vector2(-110,130));
            resetText=Label("Reset",assets,new Vector2(1,.5f),new Vector2(-110,-168),new Vector2(140,40),TextAlignmentOptions.Center,23);resetText.text="重置";
            resetIcon=Icon("R",assets.Sprite("keyboard_r_outline"),new Vector2(1,.5f),new Vector2(-110,-110));
            spaceIcon.rectTransform.sizeDelta=new Vector2(80,80);
        }
        public void Refresh(LevelEntry level,PuzzleState state)
        {
            levelTitle.text=level.ChapterTitle+"/"+level.displayId;
            Color color=state.ActiveTone==Tone.White?new Color(.51f,.51f,.5f):new Color(.83f,.83f,.82f);
            levelTitle.color=spaceText.color=undoText.color=resetText.color=color;
            spaceIcon.color=undoIcon.color=resetIcon.color=color;
            bool showSwitch=System.Array.IndexOf(level.hintGroups,"ui_switch_spaces")>=0;
            bool showUndo=System.Array.IndexOf(level.hintGroups,"ui_undo_reset")>=0;
            spaceIcon.gameObject.SetActive(showSwitch);spaceText.gameObject.SetActive(showSwitch);
            undoIcon.gameObject.SetActive(showUndo);undoText.gameObject.SetActive(showUndo);resetIcon.gameObject.SetActive(showUndo);resetText.gameObject.SetActive(showUndo);
        }
        private TMP_Text Label(string name,StudyAssets assets,Vector2 anchor,Vector2 position,Vector2 size,TextAlignmentOptions alignment,float fontSize)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(canvas.transform,false);
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=anchor;rect.pivot=alignment==TextAlignmentOptions.TopRight?Vector2.one:new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;
            var text=go.GetComponent<TextMeshProUGUI>();text.font=assets.Font;text.fontSize=fontSize;text.alignment=alignment;text.raycastTarget=false;return text;
        }
        private Image Icon(string name,Sprite sprite,Vector2 anchor,Vector2 position)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(canvas.transform,false);
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=anchor;rect.anchoredPosition=position;rect.sizeDelta=new Vector2(48,48);
            var image=go.GetComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;return image;
        }
    }
}
