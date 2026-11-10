using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    public sealed partial class GameUI
    {
        public static Color WorkshopGround=>GameThemes.Current.WorkshopBackground;public static Color WorkshopSurface=>GameThemes.Current.WorkshopSurface;public static Color WorkshopLine=>GameThemes.Current.WorkshopLine;public static Color WorkshopInk=>GameThemes.Current.WorkshopInk;public static Color WorkshopAccent=>GameThemes.Current.WorkshopAccent;
        private GameObject workshopPopup;
        public void WorkshopTheme(){light=true;entryDuration=.16f;app.viewCamera.backgroundColor=WorkshopGround;}
        public void RefreshWorkshop(){Clear();WorkshopTheme();entryDuration=0;pageGroup.alpha=1;pageGroup.blocksRaycasts=true;}
        public void FocusButton(string id){int index=rows.FindIndex(r=>r.id==id);if(index>=0)Focus(index);}
        public Button WorkshopListItem(string id,string title,string marker,Vector2 position,Vector2 size,bool chosen,bool branch,Action click,Action open=null)
        {
            var rect=Rect(id,position,size);
            var button=ClickTarget(rect);
            var face=WorkshopPanel("Row",Vector2.zero,size,rect);
            face.color=chosen?(GameThemes.IsClassic?new Color32(225,211,225,255):GameThemes.Current.WorkshopSelected):WorkshopGround;face.raycastTarget=false;
            float inset=!branch&&string.IsNullOrEmpty(marker)?12:60;
            if(branch)inset+=16;
            if(branch)
            {
                float x=-size.x/2+33;
                Image("Branch stem",null,new Vector2(x,5),new Vector2(2,14),WorkshopAccent,rect);
                Image("Branch arm",null,new Vector2(x+6,-1),new Vector2(14,2),WorkshopAccent,rect);
                Image("Branch node",null,new Vector2(x+14,-1),new Vector2(6,6),Plum,rect);
            }
            else if(!string.IsNullOrEmpty(marker))Label(Rect("Index",new Vector2(-size.x/2+32,0),new Vector2(48,size.y),rect),marker,16,Muted);
            float right=open==null?12:36;
            Label(Rect("Title",new Vector2((inset-right)/2,0),new Vector2(size.x-inset-right,size.y),rect),title,20,WorkshopInk,TextAlignmentOptions.MidlineLeft);
            if(chosen)Image("Selection",null,new Vector2(-size.x/2+2,0),new Vector2(3,size.y-12),WorkshopAccent,rect);
            var focus=WorkshopPanel("Focus",Vector2.zero,size,rect,true);focus.color=new Color(WorkshopAccent.r,WorkshopAccent.g,WorkshopAccent.b,0);
            var row=Register(id,button,click,focus);
            if(open!=null)row.submit=open;
            return button;
        }
        private static void Rounded(Image image,bool border=false){image.sprite=border?SurfaceSprites.PanelBorder:SurfaceSprites.Panel;image.type=UnityEngine.UI.Image.Type.Sliced;image.preserveAspect=false;}
        public Image WorkshopPanel(string name,Vector2 position,Vector2 size,Transform parent=null,bool border=false)
        {var image=Image(name,null,position,size,border?WorkshopLine:WorkshopSurface,parent);Rounded(image,border);image.raycastTarget=!border;return image;}
        public void WorkshopChoice(string id,bool selected)
        {
            if(!buttons.TryGetValue(id,out var b)||b==null)return;
            b.GetComponent<Image>().color=selected?WorkshopAccent:WorkshopLine;
            var face=b.transform.Find("Face")??b.transform.Find("Primary face");if(face!=null)face.GetComponent<Image>().color=selected?WorkshopAccent:WorkshopSurface;
            var label=b.transform.Find("Label").GetComponent<TMP_Text>();label.color=b.interactable?(selected?GameThemes.Current.WorkshopOnAccent:WorkshopInk):new Color(WorkshopInk.r,WorkshopInk.g,WorkshopInk.b,.35f);
        }
        public Button WorkshopIcon(string id,GameIcon icon,Vector2 position,Vector2 size,Action action,Transform parent=null,bool navigable=false)
        {var b=EditorButton(id,"",position,size,action,false,parent,navigable);Icon("Icon",icon,Vector2.zero,22,WorkshopInk,b.transform);return b;}
        public void WorkshopPopup(GameObject popup){workshopPopup=popup;}
        public bool CloseWorkshopPopup(){if(workshopPopup==null||!workshopPopup.activeSelf)return false;workshopPopup.SetActive(false);int more=rows.FindIndex(r=>r.id=="more-custom");if(more>=0)Focus(more);return true;}
        public RectTransform WorkshopOverlay(string name,Vector2 center,Vector2 size,Action close)
        {
            CommitText();
            foreach(var row in rows){row.button.interactable=false;row.motion.Selected=false;}
            rows.Clear();selected=0;light=true;
            var shade=Rect("Overlay backdrop",Vector2.zero,Vector2.zero);
            shade.anchorMin=Vector2.zero;shade.anchorMax=Vector2.one;shade.offsetMin=shade.offsetMax=Vector2.zero;
            var dismiss=ClickTarget(shade);dismiss.GetComponent<Image>().color=new Color(0,0,0,.32f);
            Register("campaign-overlay-outside",dismiss,close,null,navigable:false);
            var root=Rect(name,center,size);
            WorkshopPanel("Overlay shadow",new Vector2(3,-5),size+Vector2.one*4,root).color=new Color(0,0,0,.20f);
            WorkshopPanel("Overlay surface",Vector2.zero,size,root);
            WorkshopPanel("Overlay border",Vector2.zero,size,root,true);
            root.gameObject.AddComponent<WorkshopPopover>();
            return root;
        }
        public void OnFocus(string id,Action focused)
        {var row=rows.First(r=>r.id==id);row.focus=focused;}
        public Button WorkshopTextAction(string id,string value,Vector2 position,Vector2 size,Action action,Transform parent=null)
        {
            var rect=Rect(id,position,size,parent);var button=ClickTarget(rect);
            Label(Rect("Label",Vector2.zero,size,rect),value,18,Muted,TextAlignmentOptions.MidlineLeft);
            var line=Image("Focus",null,new Vector2(0,-size.y*.5f+3),new Vector2(size.x,1),new Color(WorkshopAccent.r,WorkshopAccent.g,WorkshopAccent.b,0),rect);
            Register(id,button,action,line);return button;
        }
    }
}
