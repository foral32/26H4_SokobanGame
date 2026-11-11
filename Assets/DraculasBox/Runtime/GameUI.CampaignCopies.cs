using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    public sealed partial class GameUI
    {
        public void CampaignCopyPicker(Action<string> copy,Action close)
        {
            CloseWorkshopPopup();
            var root=WorkshopOverlay("Campaign copy dialog",Vector2.zero,new Vector2(1032,548),close);
            Label(Rect("Copy heading",new Vector2(0,230),new Vector2(966,42),root),"复制正式关卡",28,WorkshopInk,TextAlignmentOptions.MidlineLeft);
            DialogClose("copy-close",new Vector2(480,230),root,close);
            var chapters=assets.Catalog.chapters;int chapterIndex=0,pageIndex=0;string chosen=null;
            var viewport=Rect("Copy chapters",new Vector2(-375,4),new Vector2(218,376),root);
            viewport.gameObject.AddComponent<Image>().color=Color.clear;viewport.gameObject.AddComponent<RectMask2D>();
            var sheet=Rect("Chapter list",Vector2.zero,new Vector2(214,Mathf.Max(376,chapters.Length*52)),viewport);
            sheet.anchorMin=sheet.anchorMax=sheet.pivot=new Vector2(.5f,1);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=sheet;
            scroll.horizontal=false;scroll.vertical=true;scroll.scrollSensitivity=35;scroll.movementType=ScrollRect.MovementType.Clamped;
            Image("Copy divider",null,new Vector2(-249,4),new Vector2(1,372),WorkshopLine,root);
            var tiles=new Button[6];var thumbs=new LevelThumbnail[6];var names=new TMP_Text[6];var borders=new Image[6];
            var ids=new string[6];
            for(int slot=0;slot<6;slot++)
            {
                int index=slot;var at=new Vector2(-112+(slot%3)*238,109-(slot/3)*178);
                var tile=Rect("copy-level-"+slot,at,new Vector2(218,156),root);tiles[slot]=ClickTarget(tile);
                var surface=WorkshopPanel("Card",Vector2.zero,new Vector2(218,156),tile);surface.color=GameThemes.Current.WorkshopBackground;surface.raycastTarget=false;
                WorkshopPanel("Board well",new Vector2(0,16),new Vector2(206,112),tile).color=BoardView.SafeColor;
                thumbs[slot]=Thumbnail("Level",null,new Vector2(0,16),new Vector2(200,108),tile);
                names[slot]=Label(Rect("Name",new Vector2(0,-58),new Vector2(198,32),tile),"",18,WorkshopInk);
                borders[slot]=WorkshopPanel("Selected",Vector2.zero,new Vector2(218,156),tile,true);borders[slot].color=Color.clear;
                var focus=WorkshopPanel("Focus",Vector2.zero,new Vector2(224,162),tile,true);focus.color=new Color(WorkshopAccent.r,WorkshopAccent.g,WorkshopAccent.b,0);focus.raycastTarget=false;
                Register(tile.name,tiles[slot],()=>{chosen=ids[index];Select();},focus);
            }
            var paging=Label(Rect("Copy page",new Vector2(126,-178),new Vector2(100,30),root),"",17,Muted);
            WorkshopIcon("copy-prev",GameIcon.Back,new Vector2(42,-178),new Vector2(38,32),()=>{pageIndex--;Refresh();},root,true);
            WorkshopIcon("copy-next",GameIcon.Arrow,new Vector2(210,-178),new Vector2(38,32),()=>{pageIndex++;Refresh();},root,true);
            EditorButton("copy-cancel","取消",new Vector2(-420,-231),new Vector2(126,42),close,false,root,true);
            EditorButton("copy-confirm","复制并编辑",new Vector2(390,-231),new Vector2(186,42),()=>{if(chosen!=null)copy(chosen);},true,root,true);
            for(int i=0;i<chapters.Length;i++)
            {
                int index=i;var chapter=chapters[i];
                var button=EditorButton("copy-chapter-"+chapter.id,chapter.title,new Vector2(0,-26-i*52),new Vector2(208,44),()=>{chapterIndex=index;pageIndex=0;Refresh();},false,sheet,true);
                var rect=(RectTransform)button.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);
                button.GetComponentInChildren<TMP_Text>().fontSize=18;
                OnFocus(button.name,()=>{float offset=sheet.anchoredPosition.y,top=index*52;if(top<offset)offset=top;else if(top+52>offset+376)offset=top+52-376;sheet.anchoredPosition=new Vector2(0,Mathf.Clamp(offset,0,sheet.rect.height-376));});
            }
            void Select()
            {
                for(int i=0;i<6;i++)borders[i].color=ids[i]!=null&&ids[i]==chosen?WorkshopAccent:Color.clear;
                SetEnabled("copy-confirm",chosen!=null);
            }
            void Refresh()
            {
                chosen=null;var chapter=chapters[chapterIndex];var layout=new ChapterMapLayout(chapter);int pages=Mathf.Max(1,(chapter.levels.Length+5)/6);
                pageIndex=Mathf.Clamp(pageIndex,0,pages-1);
                for(int i=0;i<6;i++)
                {
                    int index=pageIndex*6+i;bool visible=index<chapter.levels.Length;tiles[i].gameObject.SetActive(visible);ids[i]=visible?chapter.levels[index].id:null;
                    if(!visible)continue;var data=assets.Level(ids[i]);thumbs[i].Set(data);names[i].text=CampaignLabels.Name(assets.Catalog,ids[i],data.title);
                }
                for(int i=0;i<chapters.Length;i++)WorkshopChoice("copy-chapter-"+chapters[i].id,i==chapterIndex);
                paging.text=pages>1?(pageIndex+1)+" / "+pages:"";
                SetEnabled("copy-prev",pageIndex>0);SetEnabled("copy-next",pageIndex+1<pages);Select();
                FocusButton("copy-level-0");
            }
            Refresh();
        }
    }
}
