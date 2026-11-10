using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    public sealed partial class GameUI
    {
        private static readonly string[] RetiredTitleArt={"Sun","Lit tile","First shadow","Second shadow","Crate","Hero","Hero cape","Hero fangs","Coffin","Dracula's","Dracula's shadow","Dracula's depth","Box","Box shadow","Box depth"};
        private void BuildTitleLogo(Transform parent)
        {
            var logo=Rect("Title logo",new Vector2(0,126),new Vector2(448,230),parent);
            var artwork=Rect("Logo artwork",Vector2.zero,logo.sizeDelta,logo).gameObject.AddComponent<TitleArtworkGraphic>();
            artwork.Source=Resources.Load<Texture2D>("Title/Wordmark");artwork.raycastTarget=false;
        }
        private void TitleAction(string id,string label,int index,Action action)
        {
            var rect=Rect(id,Vector2.zero,new Vector2(320,54));var button=ClickTarget(rect);
            var cursor=Icon("Focus",GameIcon.Arrow,new Vector2(-120,0),22,new Color(Gold.r,Gold.g,Gold.b,0),rect);
            var text=Label(Rect("Label",Vector2.zero,new Vector2(270,54),rect),label,index==0?36:30,Quiet);
            Register(id,button,action,cursor,text);
        }
        private void LayoutTitleButtons(Button[] buttons)
        {
            for(int i=0;i<buttons.Length;i++)
            {
                var rect=(RectTransform)buttons[i].transform;
                rect.anchoredPosition=new Vector2(0,-80-i*56);rect.sizeDelta=new Vector2(320,54);
                foreach(string name in new[]{"Tile","Icon"})RetireTitleObject(rect.Find(name));
                var cursor=(RectTransform)rect.Find("Focus");cursor.anchoredPosition=new Vector2(-120,0);cursor.sizeDelta=Vector2.one*22;
                var label=rect.Find("Label").GetComponent<TMP_Text>();
                label.rectTransform.anchoredPosition=Vector2.zero;label.rectTransform.sizeDelta=new Vector2(270,54);
                label.fontSize=i==0?36:30;label.font=assets.bodyFont;label.alignment=TextAlignmentOptions.Midline;
            }
        }
        private void RetireTitleObject(Transform child)
        {
            if(child==null)return;child.gameObject.SetActive(false);child.SetParent(null,false);
            if(Application.isPlaying)Destroy(child.gameObject);else DestroyImmediate(child.gameObject);
        }
        private void EnsureTitleLayout()
        {
            // Upgrade old scene pages as well as the authored menu; keep its four button bindings.
            foreach(var child in authoredTitle.transform.Cast<Transform>().Where(t=>RetiredTitleArt.Contains(t.name)).ToArray())RetireTitleObject(child);
            var logo=authoredTitle.transform.Find("Title logo");
            if(logo==null||logo.Find("Logo artwork")==null){RetireTitleObject(logo);BuildTitleLogo(authoredTitle.transform);}
            var atmosphere=authoredTitle.GetComponentInChildren<MenuAtmosphere>(true);if(atmosphere!=null)atmosphere.titleLayout=true;
            LayoutTitleButtons(authoredTitleButtons);
        }
    }
}
