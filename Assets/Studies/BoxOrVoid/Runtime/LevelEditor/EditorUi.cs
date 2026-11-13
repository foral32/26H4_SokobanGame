using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BoxOrVoid.Study.LevelEditing
{
    /// <summary>Shared authoring primitives; fixed controls are created once in the saved scene.</summary>
    public static class EditorUi
    {
        public static readonly Color Background=new Color32(58,61,62,255),Panel=new Color32(48,55,58,255),Ink=new Color32(235,239,237,255),Muted=new Color32(180,194,201,255),Green=new Color32(118,189,56,255),Blue=new Color32(124,159,207,255);
        public static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;return r;}
        public static Image Image(string name,Transform parent,Sprite sprite,Vector2 position,Vector2 size,Color? color=null)
        {var r=Rect(name,parent,position,size);var image=r.gameObject.AddComponent<Image>();image.sprite=sprite;image.color=color??Color.white;image.preserveAspect=sprite!=null;image.raycastTarget=false;return image;}
        public static TMP_Text Text(string name,Transform parent,StudyAssets assets,string value,Vector2 position,Vector2 size,float fontSize=20,TextAlignmentOptions align=TextAlignmentOptions.Center,Color? color=null)
        {
            var r=Rect(name,parent,position,size);var text=r.gameObject.AddComponent<TextMeshProUGUI>();text.font=fontSize>=28?assets.Font:assets.EditorBodyFont;text.text=value;text.fontSize=fontSize;text.alignment=align;text.color=color??Ink;text.raycastTarget=false;text.richText=false;text.overflowMode=TextOverflowModes.Ellipsis;text.enableWordWrapping=true;return text;
        }
        public static EditorWidget Button(Transform parent,StudyAssets assets,string id,string label,Vector2 position,Vector2 size,string icon=null,bool round=false,bool primary=false,string shortcut=null)
        {
            var r=Rect(id,parent,position,size);var hit=r.gameObject.AddComponent<Image>();hit.color=Color.clear;
            r.gameObject.AddComponent<CanvasGroup>();
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=hit;button.transition=Selectable.Transition.None;var nav=button.navigation;nav.mode=Navigation.Mode.None;button.navigation=nav;
            var w=r.gameObject.AddComponent<EditorWidget>();w.Id=id;w.Title=label;w.Shortcut=shortcut??EditorShortcuts.Hint(id);w.Button=button;
            float face=round?size.x:0;
            if(round)
            {
                Image("Shadow",r,assets.Sprite("Main Menu Button Panel"),new Vector2(3,-4),new Vector2(face,face),new Color(0,0,0,.32f));
                w.Face=Image("Face",r,assets.Sprite("Main Menu Button Panel"),Vector2.zero,new Vector2(face,face),primary?Blue:Muted);
                var ir=Rect("Icon",r,Vector2.zero,new Vector2(face*.52f,face*.52f));var graphic=ir.gameObject.AddComponent<EditorIconGraphic>();graphic.Icon=icon??id;graphic.color=primary?Ink:Panel;graphic.raycastTarget=false;
                w.Label=Text("Label",r,assets,label,new Vector2(0,-face*.5f-17),new Vector2(124,30),primary?20:16);
                if(!string.IsNullOrEmpty(w.Shortcut))Text("Key",r,assets,w.Shortcut,new Vector2(0,-face*.5f-39),new Vector2(128,20),12,TextAlignmentOptions.Center,Muted);
            }
            else
            {
                Image("Shadow",r,null,new Vector2(4,-5),size,new Color(0,0,0,.25f));
                Image("Border",r,null,Vector2.zero,size,new Color32(97,114,123,255));
                w.Face=Image("Face",r,null,Vector2.zero,size-new Vector2(6,6),primary?Blue:new Color32(65,76,82,255));
                w.Label=Text("Label",r,assets,label,Vector2.zero,size-new Vector2(size.x<60?4:16,4),18,TextAlignmentOptions.Center,primary?Panel:Ink);
            }
            var select=Rect("Selected",r,Vector2.zero,size+new Vector2(12,12));w.Selection=select.gameObject.AddComponent<EditorCornerGraphic>();w.Selection.color=Green;w.Selection.raycastTarget=false;w.Selection.enabled=false;
            return w;
        }
        public static void Quiet(EditorWidget widget)
        {widget.transform.Find("Border").gameObject.SetActive(false);widget.transform.Find("Shadow").gameObject.SetActive(false);widget.Face.color=Color.clear;widget.Label.color=Muted;}
        // A shallow bevel and a single cast shadow echo the board's extruded wall tiles.
        public static void Plate(Transform parent,string name,Vector2 position,Vector2 size)
        {
            Image(name+" shadow",parent,null,position+new Vector2(5,-6),size,new Color(0,0,0,.30f));
            Image(name,parent,null,position,size,Panel).raycastTarget=true;
            Image("Top bevel",parent,null,position+new Vector2(0,(size.y-3)/2),new Vector2(size.x,3),new Color32(132,150,156,255));
            Image("Left bevel",parent,null,position+new Vector2((3-size.x)/2,0),new Vector2(3,size.y),new Color32(100,118,125,255));
        }
        public static TMP_InputField Input(Transform parent,StudyAssets assets,string name,string value,Vector2 position,Vector2 size)
        {
            var r=Rect(name,parent,position,size);var image=r.gameObject.AddComponent<Image>();image.color=new Color32(35,43,47,255);
            var input=r.gameObject.AddComponent<TMP_InputField>();input.targetGraphic=image;
            var viewport=Rect("Text viewport",r,Vector2.zero,size-new Vector2(24,8));viewport.gameObject.AddComponent<RectMask2D>();
            var text=Text("Text",viewport,assets,"",Vector2.zero,size-new Vector2(24,8),21,TextAlignmentOptions.MidlineLeft);text.overflowMode=TextOverflowModes.Overflow;
            input.textViewport=viewport;input.textComponent=(TextMeshProUGUI)text;input.text=value;input.caretColor=Ink;input.customCaretColor=true;input.selectionColor=new Color(.5f,.7f,.4f,.5f);input.lineType=TMP_InputField.LineType.SingleLine;return input;
        }
        public static void Clear(Transform root)
        {for(int i=root.childCount-1;i>=0;i--){var go=root.GetChild(i).gameObject;go.SetActive(false);if(Application.isPlaying)UnityEngine.Object.Destroy(go);else UnityEngine.Object.DestroyImmediate(go);}}
    }
}
