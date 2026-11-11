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
        private static Color Muted => GameThemes.IsClassic ? new Color32(94,89,101,255) : GameThemes.Current.WorkshopMuted;
        private Button Button(string id,string text,float x,float y,float w,Action action,bool primary=false)
            => UI.EditorButton(id,text,new Vector2(x,y),new Vector2(w,40),action,primary,null,true);
        private void Label(string id,string value,float x,float y,float w,float size=20,bool quiet=false)
            => UI.Text(id,value,new Vector2(x,y),new Vector2(w,40),size,quiet?Muted:GameUI.WorkshopInk,TextAlignmentOptions.MidlineLeft);
        private void Rule(string id,float x,float y,float w,float h=1)
            => UI.Image(id,null,new Vector2(x,y),new Vector2(w,h),GameUI.WorkshopLine);
        private void Draw()
        {
            HidePeek();hoveredId=null;
            if(mapView?.Alive==true)mapOffset=mapView.Offset;
            mapView=null;
            if(panel==Panel.Chapters)DrawChapters();
            else if(panel==Panel.Outline)DrawOutline();
            else
            {
                UI.RefreshWorkshop();
                UI.WorkshopIcon("campaign-back",GameIcon.Back,new Vector2(-584,311),new Vector2(44,40),Back,null,true);
                Label("Campaign heading",PanelTitle(),-300,311,480,30);
                Rule("Header rule",0,278,1220);
                if(panel==Panel.Anchor||panel==Panel.Transfer)DrawChoices();
                else DrawConfirmation();
                if(!string.IsNullOrEmpty(notice))
                    UI.Text("Campaign status",notice,new Vector2(0,-321),new Vector2(1190,53),18,GameThemes.IsClassic?GameUI.Plum:GameUI.WorkshopInk,TextAlignmentOptions.MidlineLeft);
            }
            DrawOverlay();
        }
        private string PanelTitle()=>panel switch
        {
            Panel.Anchor=>"选择解锁关",Panel.Transfer=>"移至章节",
            Panel.Remove=>"移出正式目录",_=>"从工程重载"
        };
        private void Pager(string id,float x,float y,int page,int count,Action<int> set,int perPage=Rows)
        {
            int pages=(count+perPage-1)/perPage;if(pages<=1)return;
            Button(id+"-prev","‹",x-63,y,42,()=>{set(page-1);Draw();});
            UI.Text(id+" page",(page+1)+" / "+pages,new Vector2(x,y),new Vector2(80,32),17,Muted);
            Button(id+"-next","›",x+63,y,42,()=>{set(page+1);Draw();});
            UI.SetEnabled(id+"-prev",page>0);UI.SetEnabled(id+"-next",page+1<pages);
        }
        private void DrawChoices()
        {
            bool anchor=panel==Panel.Anchor;
            Label("Target context",Data.Board(selectedId).title,-105,223,700,25);
            var options=anchor?CampaignDocument.Main(Current).Where(l=>l.id!=selectedId).Select(l=>(id:l.id,name:Data.Board(l.id).title)).ToList()
                :Data.catalog.chapters.Where(c=>c.id!=chapterId).Select(c=>(id:c.id,name:c.title)).ToList();
            if(options.Count==0)Label("No target","本章需要保留一关主线。",-105,151,700);
            for(int i=choicePage*Rows;i<Math.Min(options.Count,(choicePage+1)*Rows);i++)
            {
                var option=options[i];
                UI.WorkshopListItem("target-"+option.id,option.name,(i+1).ToString("00"),new Vector2(0,159-(i%Rows)*48),new Vector2(900,42),
                    anchor&&!Selected.main&&Selected.requires[0]==option.id,false,()=>Edit(()=>
                    {
                        if(anchor)doc.SetRole(Current,Selected,option.id);
                        else doc.Transfer(Current,Selected,Data.Chapter(option.id));
                    }));
            }
            Pager("targets",0,-299,choicePage,options.Count,p=>choicePage=p);
        }
        private void DrawConfirmation()
        {
            bool remove=panel==Panel.Remove;
            Label("Confirm subject",remove?Data.Board(selectedId).title:"放弃当前工作草稿？",0,107,790,29);
            Button("campaign-cancel","取消",-102,-75,178,()=>Switch(Panel.Outline));
            Button("campaign-confirm",remove?"移出目录":"放弃并重载",108,-75,200,()=>
            {if(remove)Edit(()=>doc.Remove(Current,Selected));else Reload();});
            UI.FocusButton("campaign-cancel");
        }
    }
}
