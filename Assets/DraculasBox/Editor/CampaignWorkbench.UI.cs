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
                if(panel==Panel.Actions)DrawInspector();
                else if(panel==Panel.Anchor||panel==Panel.Transfer)DrawChoices();
                else DrawConfirmation();
                if(!string.IsNullOrEmpty(notice))
                    UI.Text("Campaign status",notice,new Vector2(0,-321),new Vector2(1190,53),18,GameThemes.IsClassic?GameUI.Plum:GameUI.WorkshopInk,TextAlignmentOptions.MidlineLeft);
            }
            DrawOverlay();
        }
        private string PanelTitle()=>panel switch
        {
            Panel.Actions=>"关卡操作",Panel.Anchor=>"选择解锁关",Panel.Transfer=>"移至章节",
            Panel.Remove=>"移出正式目录",_=>"从工程重载"
        };
        private void DrawInspector()
        {
            var level=Data.Board(selectedId);var node=Selected;
            Label("Selected title",level.title,-311,196,526,30);
            UI.WorkshopPanel("Board preview",new Vector2(-307,0),new Vector2(524,302)).color=BoardView.SafeColor;
            UI.Thumbnail("Campaign board",level,new Vector2(-307,0),new Vector2(494,278));
            Label("Board size",level.rows[0].Length+" × "+level.rows.Length,-517,-188,118,17,true);
            string route=Data.Plan(level.id)?.route;
            Label("Recorded route",string.IsNullOrEmpty(route)?"待记录通关路线":"通关路线  "+route.Length+" 步",-196,-188,300,17,true);
            Button("campaign-edit","编辑棋盘",218,-52,236,()=>OpenBoard(),true);
            Button("campaign-test","试玩",478,-52,242,()=>OpenBoard(true));
            UI.SetEnabled("campaign-test",new EditDocument(level).Validation()==null);
            Rule("Inspector rule",350,-94,498);
            Label("Type","路线",145,-130,90,19,true);
            Button("campaign-main","主线",257,-130,106,()=>Edit(()=>doc.SetRole(Current,Selected,null)));
            Button("campaign-branch","支线",374,-130,106,()=>Switch(Panel.Anchor));
            UI.WorkshopChoice("campaign-main",node.main);UI.WorkshopChoice("campaign-branch",!node.main);
            var peers=CampaignDocument.Peers(Current,node);int index=peers.IndexOf(node);
            Button("campaign-up","上移",481,-130,82,()=>Edit(()=>doc.MoveLevel(Current,Selected,-1)));
            Button("campaign-down","下移",562,-130,78,()=>Edit(()=>doc.MoveLevel(Current,Selected,1)));
            UI.SetEnabled("campaign-up",index>0);UI.SetEnabled("campaign-down",index+1<peers.Count);
            Label("Unlock label","解锁",145,-181,90,19,true);
            if(node.main)
            {
                string prerequisite=index==0?"进入本章时":Data.Board(peers[index-1].id).title+" 通关后";
                Label("Main prerequisite",prerequisite,400,-181,392,19);
            }
            else Button("campaign-anchor",Data.Board(node.requires[0]).title+" 通关后",401,-181,394,()=>Switch(Panel.Anchor));
            Button("campaign-copy","复制关卡",177,-253,155,()=>Edit(()=>selectedId=doc.AddLevel(Current,Selected,true)));
            Button("campaign-transfer","移至章节",350,-253,162,()=>Switch(Panel.Transfer));
            UI.SetEnabled("campaign-transfer",Data.catalog.chapters.Length>1);
            Button("campaign-remove","移出目录",525,-253,150,()=>Switch(Panel.Remove));
        }
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
