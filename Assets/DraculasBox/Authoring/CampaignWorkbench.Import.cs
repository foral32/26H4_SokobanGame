using System;
using System.Collections.Generic;
using System.Linq;
using DraculasBox.Core;
using DraculasBox.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Editor
{
    internal sealed partial class CampaignWorkbench
    {
        private List<LevelData> importLevels;
        private string importReadError;
        private void ReadCustomLevels()
        {
            try
            {
                importLevels=app.Library.Read(out int invalid);
                importReadError=invalid>0?invalid+" 个关卡无法读取":"";
            }
            catch(Exception e){importLevels=new List<LevelData>();importReadError=e.Message;}
        }
        private void OpenCustomImport()
        {
            ReadCustomLevels();overlay=Overlay.ImportCustom;Draw();
        }
        private void DrawCustomImport()
        {
            var root=UI.WorkshopOverlay("Custom import",Vector2.zero,new Vector2(864,514),CloseOverlay);
            OverlayText(root,"Custom import title","导入自定义关卡",new Vector2(-20,215),new Vector2(768,42),27);
            OverlayButton(root,"custom-import-close","×",new Vector2(394,215),new Vector2(32,30),CloseOverlay);
            var cards=new Button[4];var thumbs=new LevelThumbnail[4];var names=new TMP_Text[4];
            int page=0;LevelData chosen=null;LevelThumbnail preview=null;TMP_Text title=null;
            for(int i=0;i<4;i++)
            {
                int slot=i;var pos=new Vector2(-302+i%2*210,99-i/2*158);
                cards[i]=OverlayButton(root,"custom-import-item-"+i,"",pos,new Vector2(198,146),()=>
                {
                    chosen=importLevels[page*4+slot].Copy();Select();
                });
                thumbs[i]=UI.Thumbnail("Custom thumbnail",null,new Vector2(0,16),new Vector2(182,100),cards[i].transform);
                names[i]=OverlayText(cards[i].transform,"Custom name","",new Vector2(0,-52),new Vector2(176,30),18);
            }
            UI.WorkshopPanel("Custom preview well",new Vector2(220,39),new Vector2(352,270),root).color=BoardView.SafeColor;
            preview=UI.Thumbnail("Custom preview",null,new Vector2(220,39),new Vector2(342,260),root);
            title=OverlayText(root,"Custom selected name","选择关卡",new Vector2(220,-120),new Vector2(352,38),22);
            title.alignment=TextAlignmentOptions.Center;
            var message=OverlayText(root,"Custom import status",importReadError,new Vector2(0,-178),new Vector2(802,32),17,true);
            var paging=OverlayText(root,"Custom import page","",new Vector2(-197,-153),new Vector2(100,30),17,true);
            paging.alignment=TextAlignmentOptions.Center;
            OverlayButton(root,"custom-import-prev","‹",new Vector2(-271,-153),new Vector2(36,30),()=>{page--;Refresh();});
            OverlayButton(root,"custom-import-next","›",new Vector2(-123,-153),new Vector2(36,30),()=>{page++;Refresh();});
            OverlayButton(root,"custom-import-cancel","取消",new Vector2(146,-218),new Vector2(126,40),CloseOverlay);
            OverlayButton(root,"custom-import-confirm","加入本章",new Vector2(314,-218),new Vector2(164,40),Confirm,true);
            void Select()
            {
                for(int i=0;i<4;i++)UI.WorkshopChoice("custom-import-item-"+i,chosen!=null&&page*4+i<importLevels.Count&&importLevels[page*4+i].id==chosen.id);
                for(int i=0;i<4;i++)names[i].color=cards[i].gameObject.activeSelf&&chosen!=null&&importLevels[page*4+i].id==chosen.id?GameThemes.Current.WorkshopOnAccent:GameUI.WorkshopInk;
                preview.Set(chosen);title.text=chosen?.title??(importLevels.Count==0?"暂无自定义关卡":"选择关卡");
                UI.SetEnabled("custom-import-confirm",chosen!=null);
            }
            void Refresh()
            {
                chosen=null;int pages=Math.Max(1,(importLevels.Count+3)/4);page=Mathf.Clamp(page,0,pages-1);
                for(int i=0;i<4;i++)
                {
                    int index=page*4+i;bool visible=index<importLevels.Count;cards[i].gameObject.SetActive(visible);
                    if(visible){thumbs[i].Set(importLevels[index]);names[i].text=importLevels[index].title;}
                }
                paging.text=pages>1?(page+1)+" / "+pages:"";
                UI.SetEnabled("custom-import-prev",page>0);UI.SetEnabled("custom-import-next",page+1<pages);
                Select();UI.FocusButton(importLevels.Count>0?"custom-import-item-0":"custom-import-close");
            }
            void Confirm()
            {
                if(chosen==null)return;
                var snapshot=chosen;
                ReadCustomLevels();
                var latest=importLevels.FirstOrDefault(b=>b.id==snapshot.id);
                if(latest==null||!latest.ContentEquals(snapshot))
                {
                    Refresh();message.text=latest==null?"关卡已移除或无法读取，请重新选择。":"关卡已更新，请重新选择。";return;
                }
                if(!Change(()=>selectedId=doc.ImportLevel(Current,Selected,snapshot)))
                {Refresh();message.text=notice;return;}
                overlay=Overlay.None;notice="";Draw();
            }
            Refresh();
        }
    }
}
