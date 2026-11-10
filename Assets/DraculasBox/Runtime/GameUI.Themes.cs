using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    public sealed partial class GameUI
    {
        private TMP_Text themeName;
        private GameIconGraphic themePreviousIcon,themeNextIcon;
        private Color WorkshopPrimary=>GameThemes.IsClassic||GameThemes.Current.Id=="porcelain"?Gold:WorkshopAccent;
        private Color WorkshopPrimaryInk=>GameThemes.IsClassic?Night:GameThemes.Current.Id=="porcelain"?WorkshopInk:GameThemes.Current.WorkshopOnAccent;
        private static Color SettingsShade
        {
            get {var color=GameThemes.IsClassic?new Color(.07f,.09f,.14f,.88f):GameThemes.Current.Background;color.a=GameThemes.IsClassic?.88f:.94f;return color;}
        }
        private void ThemeChoices(float y=-70,bool compact=false)
        {
            var row=MenuAction("theme","主题",new Vector2(compact?30:0,y),compact?300:350,()=>app.CycleTheme(1));
            themeName=Label(Rect("Theme name",new Vector2(compact?35:90,0),new Vector2(compact?80:110,48),row.button.transform),GameThemes.Current.Name,25,Quiet);
            themePreviousIcon=ThemeArrow("theme-prev",new Vector2(compact?-23:29,0),-1,row.button.transform);
            themeNextIcon=ThemeArrow("theme-next",new Vector2(compact?93:151,0),1,row.button.transform);
        }
        private GameIconGraphic ThemeArrow(string id,Vector2 position,int direction,Transform parent)
        {
            var rect=Rect(id,position,new Vector2(34,46),parent);var button=ClickTarget(rect);
            var icon=Icon("Arrow",GameIcon.Arrow,Vector2.zero,17,Quiet,rect);
            if(direction<0)icon.rectTransform.localScale=new Vector3(-1,1,1);
            Register(id,button,()=>app.CycleTheme(direction),null,null,false,true,false);return icon;
        }
        public void RefreshThemeSettings(string id)
        {
            // Keep the same hit targets: a stationary mouse must not steal keyboard focus.
            app.viewCamera.backgroundColor=Night;
            var dim=content.Find("Dim");if(dim!=null)dim.GetComponent<Image>().color=SettingsShade;
            var heading=content.Find("Heading");if(heading!=null)heading.GetComponent<TMP_Text>().color=White;
            foreach(var row in rows)
            {
                if(row.label!=null)row.label.color=Quiet;
                foreach(var icon in row.button.GetComponentsInChildren<GameIconGraphic>(true))
                {
                    float alpha=icon.color.a;var color=icon.name=="Focus"?Gold:Quiet;color.a=alpha;icon.color=color;
                }
            }
            if(themeName!=null)themeName.text=GameThemes.Current.Name;
            RefreshSettings();Focus(Mathf.Max(0,rows.FindIndex(r=>r.id=="theme")));
        }
        private void RefreshThemeFocus()
        {
            if(themeName==null)return;var color=SelectedId=="theme"?White:Quiet;themeName.color=color;
            if(themePreviousIcon!=null)themePreviousIcon.color=color;if(themeNextIcon!=null)themeNextIcon.color=color;
        }
        private void ApplyTitleTheme()
        {
            var theme=GameThemes.Current;
            foreach(var text in authoredTitle.GetComponentsInChildren<TMP_Text>(true))
            {
                if(text.name.EndsWith(" shadow"))continue;
                if(text.name.EndsWith(" depth"))text.color=theme.TitleDepth;
                else if(text.name.EndsWith(" highlight"))text.color=Color.Lerp(theme.TitleFace,Color.white,.65f);
                else text.color=text.name.StartsWith("Logo ")?theme.TitleFace:theme.Secondary;
            }
            foreach(var tile in authoredTitle.GetComponentsInChildren<MenuTileGraphic>(true))tile.color=theme.Button;
            foreach(var icon in authoredTitle.GetComponentsInChildren<GameIconGraphic>(true))
            {
                float alpha=icon.color.a;var color=icon.name=="Icon"?theme.ButtonInk:theme.Focus;color.a=alpha;icon.color=color;
            }
            foreach(var artwork in authoredTitle.GetComponentsInChildren<TitleArtworkGraphic>(true))artwork.RefreshTheme();
            foreach(var atmosphere in authoredTitle.GetComponentsInChildren<MenuAtmosphere>(true))atmosphere.SetVerticesDirty();
        }
    }
}
