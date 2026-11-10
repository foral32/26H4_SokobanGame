using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    public sealed partial class GameUI:MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform content;
        [SerializeField] private GameObject authoredTitle;
        [SerializeField] private Button[] authoredTitleButtons;
        private GameAssets assets;private DraculaApp app;
        private readonly List<Row> rows=new List<Row>();
        private readonly Dictionary<string,Button> buttons=new Dictionary<string,Button>();
        private sealed class Row
        {
            public string id;public Button button;public JuicyButton motion;public Image border;
            public TMP_Text label;public Action focus,submit;public bool compact,enabled;
        }
        private int selected;private float opened,entryDuration;
        private CanvasGroup pageGroup;private bool light;
        private TMP_Text musicValue,effectsValue;private Image[][] volumeMarks;
        public RectTransform Root=>content;
        public GameObject AuthoredTitle=>authoredTitle;
        public int SortingOrder=>canvas.sortingOrder;
        public string SelectedId=>rows.Count==0?null:rows[selected].id;
        public bool IsEntering=>Application.isPlaying&&Time.unscaledTime-opened<entryDuration;
        public static Color Ink=>GameThemes.Current.Ink;public static Color Muted=>GameThemes.Current.WorkshopMuted;public static Color Plum=>GameThemes.Current.TitleDepth;public static Color Paper=>GameThemes.Current.ObjectLight;
        public static Color Night=>GameThemes.Current.Background;public static Color White=>GameThemes.Current.Foreground;public static Color Quiet=>GameThemes.Current.Secondary;public static Color Gold=>GameThemes.Current.Focus;public static Color Lilac=>GameThemes.Current.Accent;
        private Color Foreground=>light?WorkshopInk:White;
        private Color Secondary=>light?Muted:Quiet;
        public void Configure(Canvas c,RectTransform r){canvas=c;content=r;}
        public void ConfigureTitle(GameObject root,Button[] buttons){authoredTitle=root;authoredTitleButtons=buttons;}
        public void Initialize(GameAssets a,DraculaApp host,Camera camera)
        {assets=a;app=host;canvas.worldCamera=camera;canvas.sortingOrder=100;}
        public void Clear()
        {
            if(authoredTitle!=null)authoredTitle.SetActive(false);
            for(int i=content.childCount-1;i>=0;i--)
            {var child=content.GetChild(i).gameObject;child.SetActive(false);if(Application.isPlaying)Destroy(child);else DestroyImmediate(child);}
            rows.Clear();buttons.Clear();selected=0;musicValue=effectsValue=themeName=null;themePreviousIcon=themeNextIcon=null;volumeMarks=null;
            light=false;
            if(app!=null&&app.viewCamera!=null)app.viewCamera.backgroundColor=light?new Color32(213,219,226,255):Night;
            opened=Time.unscaledTime;entryDuration=light?0:.20f;pageGroup=Group(content);
        }
        private CanvasGroup Group(RectTransform root)
        {var group=root.GetComponent<CanvasGroup>();if(group==null)group=root.gameObject.AddComponent<CanvasGroup>();group.alpha=Application.isPlaying&&entryDuration>0?0:1;group.blocksRaycasts=true;return group;}
        private void Update()
        {
            if(pageGroup==null)return;
            float t=entryDuration<=0?1:Mathf.Clamp01((Time.unscaledTime-opened)/entryDuration);
            pageGroup.alpha=1-Mathf.Pow(1-t,3);pageGroup.blocksRaycasts=t>=.80f;
            ((RectTransform)pageGroup.transform).anchoredPosition=new Vector2(0,8*Mathf.Pow(1-t,3));
        }
        public RectTransform Rect(string name,Vector2 position,Vector2 size,Transform parent=null)
        {var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent??content,false);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;return r;}
        public Image Image(string name,string sprite,Vector2 position,Vector2 size,Color color,Transform parent=null)
        {var r=Rect(name,position,size,parent);var image=r.gameObject.AddComponent<Image>();image.sprite=string.IsNullOrEmpty(sprite)?null:assets.Sprite(sprite);image.preserveAspect=image.sprite!=null;image.color=color;image.raycastTarget=false;return image;}
        public TMP_Text Text(string name,string value,Vector2 position,Vector2 size,float fontSize=22,Color? color=null,TextAlignmentOptions align=TextAlignmentOptions.Center)
        {return Label(Rect(name,position,size),value,fontSize,color??Foreground,align);}
        private TMP_Text Label(RectTransform rect,string value,float size,Color color,TextAlignmentOptions align=TextAlignmentOptions.Center)
        {
            var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.font=size>=34?assets.font:assets.bodyFont;text.text=value;text.fontSize=size;text.color=color;text.alignment=align;
            text.richText=false;text.raycastTarget=false;text.overflowMode=size>=34?TextOverflowModes.Overflow:TextOverflowModes.Ellipsis;return text;
        }
        private GameIconGraphic Icon(string name,GameIcon icon,Vector2 position,float size,Color color,Transform parent=null)
        {var g=Rect(name,position,Vector2.one*size,parent).gameObject.AddComponent<GameIconGraphic>();g.icon=icon;g.color=color;g.raycastTarget=false;return g;}
        private Button ClickTarget(RectTransform rect)
        {
            var hit=rect.gameObject.AddComponent<Image>();hit.color=Color.clear;
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=hit;button.transition=Selectable.Transition.None;
            var nav=button.navigation;nav.mode=Navigation.Mode.None;button.navigation=nav;return button;
        }
        private Row Register(string id,Button button,Action action,Graphic focusGraphic,TMP_Text label=null,bool compact=false,bool enabled=true,bool navigable=true)
        {
            var motion=button.GetComponent<JuicyButton>();if(motion==null)motion=button.gameObject.AddComponent<JuicyButton>();motion.Configure(focusGraphic);
            var row=new Row{id=id,button=button,motion=motion,label=label,compact=compact,border=button.GetComponent<Image>(),enabled=enabled};
            buttons[id]=button;
            if(navigable)rows.Add(row);
            motion.FocusRequested=()=>{if(app==null||app.Transitioning)return;int index=rows.IndexOf(row);if(index>=0&&index!=selected){Focus(index);app.Cue("uiNavigate");}};
            button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>
            {
                if(app==null||app.Transitioning)return;
                int index=rows.IndexOf(row);if(index>=0)Focus(index);
                if(!row.enabled||!button.interactable){motion.Reject();app.Cue("click_002");return;}
                motion.Pulse();app.Cue("uiSubmit");action();
            });return row;
        }
        public Button Button(string id,string label,Vector2 position,Vector2 size,Action action,bool enabled=true,bool navigable=true)
        {
            var rect=Rect(id,position,size);var b=ClickTarget(rect);
            b.GetComponent<Image>().color=light?WorkshopLine:GameThemes.Current.Border;
            Image("Face",null,Vector2.zero,size-new Vector2(3,3),light?WorkshopSurface:GameThemes.Current.Panel,rect);
            var text=Label(Rect("Label",Vector2.zero,size-new Vector2(12,6),rect),label,20,enabled?Foreground:Secondary);
            Register(id,b,action,null,text,true,enabled,navigable);return b;
        }
        private Row MenuAction(string id,string label,Vector2 position,float width,Action action,GameIcon icon=GameIcon.Arrow)
        {
            var rect=Rect(id,position,new Vector2(width,50));var b=ClickTarget(rect);
            var cursor=Icon("Focus",icon,new Vector2(-width*.5f+18,0),25,new Color(Gold.r,Gold.g,Gold.b,0),rect);
            var text=Label(Rect("Label",new Vector2(15,0),new Vector2(width-50,48),rect),label,25,Quiet,TextAlignmentOptions.MidlineLeft);
            return Register(id,b,action,cursor,text);
        }
        private Row RoundAction(string id,string label,Vector2 position,float size,GameIcon icon,Action action)
        {
            var rect=Rect(id,position,Vector2.one*size);var b=ClickTarget(rect);
            var tile=Rect("Tile",Vector2.zero,Vector2.one*size,rect).gameObject.AddComponent<MenuTileGraphic>();tile.round=true;tile.color=GameThemes.Current.Button;tile.raycastTarget=false;
            Icon("Icon",icon,Vector2.zero,size*.50f,GameThemes.Current.ButtonInk,rect);
            var text=Label(Rect("Label",new Vector2(0,-size*.5f-24),new Vector2(140,36),rect),label,20,Quiet);
            var cursor=Icon("Focus",GameIcon.Arrow,new Vector2(-size*.5f-22,0),24,new Color(Gold.r,Gold.g,Gold.b,0),rect);
            return Register(id,b,action,cursor,text);
        }
        private Row TileAction(string id,Vector2 position,float size,Color face,Action action,bool enabled=true,Transform parent=null)
        {
            var rect=Rect(id,position,Vector2.one*size,parent);var b=ClickTarget(rect);
            var tile=Rect("Tile",Vector2.zero,Vector2.one*size,rect).gameObject.AddComponent<MenuTileGraphic>();tile.color=face;tile.raycastTarget=false;
            var corners=Rect("Focus",Vector2.zero,Vector2.one*(size+12),rect).gameObject.AddComponent<SelectionCorners>();corners.color=new Color(Gold.r,Gold.g,Gold.b,0);corners.raycastTarget=false;
            return Register(id,b,action,corners,null,false,enabled);
        }
        public TMP_InputField Input(string name,string value,Vector2 position,Vector2 size,Action<string> changed,int maxLength=32)
        {
            var rect=Rect(name,position,size);
            // TMP creates its caret, selection renderer and text callbacks in OnEnable.
            // Bind the complete text hierarchy before that first activation.
            rect.gameObject.SetActive(false);
            var bg=rect.gameObject.AddComponent<Image>();bg.color=light?WorkshopSurface:GameThemes.Current.Panel;if(light)Rounded(bg);
            var outline=rect.gameObject.AddComponent<Outline>();outline.effectColor=light?WorkshopLine:new Color(Quiet.r,Quiet.g,Quiet.b,.40f);outline.effectDistance=Vector2.one;
            var input=rect.gameObject.AddComponent<TMP_InputField>();input.targetGraphic=bg;
            var viewport=Rect("Viewport",Vector2.zero,size-new Vector2(20,6),rect);viewport.gameObject.AddComponent<RectMask2D>();
            var text=Label(Rect("Text",Vector2.zero,size-new Vector2(20,6),viewport),value,20,light?WorkshopInk:White,TextAlignmentOptions.MidlineLeft);
            text.overflowMode=TextOverflowModes.Overflow;input.textViewport=viewport;input.textComponent=text;input.text=value;input.characterLimit=maxLength;var accent=light?WorkshopAccent:Gold;input.selectionColor=new Color(accent.r,accent.g,accent.b,.30f);input.customCaretColor=true;input.caretColor=accent;input.caretWidth=2;input.onEndEdit.AddListener(changed.Invoke);
            rect.gameObject.SetActive(true);
            return input;
        }
        private void Backdrop()
        {
            var rect=Rect("Night atmosphere",Vector2.zero,Vector2.zero);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var atmosphere=rect.gameObject.AddComponent<MenuAtmosphere>();atmosphere.raycastTarget=false;
        }
        public void Handle(StudyAction action)
        {
            if(IsEntering)return;
            if(action==StudyAction.Back){app.Back();return;}if(rows.Count==0)return;if(!rows[selected].button.gameObject.activeInHierarchy){int visible=rows.FindIndex(r=>r.button.gameObject.activeInHierarchy&&r.button.interactable);if(visible<0)return;Focus(visible);}
            if(action==StudyAction.Confirm){if(rows[selected].button.interactable&&rows[selected].enabled&&rows[selected].submit!=null)rows[selected].submit();else rows[selected].button.onClick.Invoke();return;}
            if((int)action>3)return;
            if(app.Page==GamePage.Settings&&selected<2&&(action==StudyAction.Left||action==StudyAction.Right)){app.AdjustVolume(selected,action==StudyAction.Left?-.1f:.1f);return;}
            if(app.Page==GamePage.Settings&&SelectedId=="theme"&&(action==StudyAction.Left||action==StudyAction.Right)){app.CycleTheme(action==StudyAction.Left?-1:1);return;}
            if(app.Page==GamePage.Title&&(action==StudyAction.Up||action==StudyAction.Down))
            {
                int next=Mathf.Clamp(selected+(action==StudyAction.Down?1:-1),0,rows.Count-1);
                if(next!=selected){Focus(next);app.Cue("uiNavigate");}
                return;
            }
            Vector2 direction=action==StudyAction.Up?Vector2.up:action==StudyAction.Down?Vector2.down:action==StudyAction.Left?Vector2.left:Vector2.right;
            int best=-1;float score=float.MaxValue;
            for(int i=0;i<rows.Count;i++)
            {if(i==selected||!rows[i].button.interactable||!rows[i].button.gameObject.activeInHierarchy)continue;var delta=(Vector2)content.InverseTransformPoint(rows[i].button.transform.position)-(Vector2)content.InverseTransformPoint(rows[selected].button.transform.position);float forward=Vector2.Dot(delta,direction);if(forward<=5)continue;float value=forward+Mathf.Abs(Vector2.Dot(delta,new Vector2(-direction.y,direction.x)))*4;if(value<score){best=i;score=value;}}
            if(best>=0){Focus(best);app.Cue("uiNavigate");}
        }
        private void Focus(int index)
        {
            if(index<0||index>=rows.Count)return;selected=index;
            for(int i=0;i<rows.Count;i++)
            {
                var row=rows[i];row.motion.Selected=i==selected;
                if(row.compact)row.border.color=i==selected?(light?WorkshopAccent:Lilac):(light?WorkshopLine:GameThemes.Current.Border);
                else if(row.label!=null)row.label.color=!row.enabled||!row.button.interactable?new Color(Quiet.r,Quiet.g,Quiet.b,.4f):i==selected?White:Quiet;
            }
            rows[selected].focus?.Invoke();RefreshThemeFocus();
        }
        public Button FindButton(string id)=>buttons.TryGetValue(id,out var button)&&button!=null&&button.gameObject.activeInHierarchy?button:null;
        public void HoverForCheck(string id)
        {
            var button=FindButton(id);if(button==null)throw new InvalidOperationException("Missing visible button "+id);
            ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);
        }
        public void ClickForCheck(string id,int clickCount=1)
        {
            var button=FindButton(id);if(button==null)throw new InvalidOperationException("Missing visible button "+id);
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,button.transform.position),clickCount=clickCount};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            if(!hits.Any(h=>h.gameObject==button.gameObject))throw new InvalidOperationException("Button not raycast visible "+id);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
    }
}
