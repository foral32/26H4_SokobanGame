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
            if(panel==Panel.Chapters){DrawChapters();return;}
            if(panel==Panel.Outline){DrawOutline();return;}
            UI.RefreshWorkshop();
            UI.WorkshopIcon("campaign-back",GameIcon.Back,new Vector2(-584,311),new Vector2(44,40),Back,null,true);
            Label("Campaign heading",PanelTitle(),-300,311,480,30);
            Rule("Header rule",0,278,1220);
            if(panel==Panel.Actions) DrawInspector();
            else if(panel==Panel.Review) DrawReview();
            else if(panel==Panel.Chapter) DrawChapter();
            else if(panel==Panel.Anchor||panel==Panel.Transfer) DrawChoices();
            else DrawConfirmation();
            if(!string.IsNullOrEmpty(notice))
                UI.Text("Campaign status",notice,new Vector2(0,-321),new Vector2(1190,53),18,GameThemes.IsClassic?GameUI.Plum:GameUI.WorkshopInk,TextAlignmentOptions.MidlineLeft);
        }
        private string PanelTitle()=>panel switch
        {
            Panel.Actions=>"关卡操作",Panel.Chapter=>"章节设置", Panel.Anchor=>"选择解锁关", Panel.Transfer=>"移至章节",
            Panel.Review=>"写回工程", Panel.Remove=>"移出正式目录", _=>"从工程重载"
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
        private void DrawChapter()
        {
            Label("Chapter title label","章节名称",-210,156,372);
            var field=UI.Input("Campaign chapter title",Current.title,new Vector2(10,103),new Vector2(810,46),_=>{});
            Label("Chapter order label","章节顺序",-210,25,372);
            int index=Array.IndexOf(Data.catalog.chapters,Current);
            Label("Chapter position","第 "+(index+1)+" 章",-319,-27,152,23);
            // Apply the field and the move together, so a focused rename survives a reorder.
            void Apply(int shift)
            {
                string title=field.text.Trim();if(title.Length==0){notice="请输入章节名称。";Draw();return;}
                Edit(()=>{Current.title=title;if(shift!=0)doc.MoveChapter(chapterId,shift);});
            }
            Button("chapter-earlier","提前一章",-42,-27,155,()=>Apply(-1));
            Button("chapter-later","延后一章",130,-27,155,()=>Apply(1));
            UI.SetEnabled("chapter-earlier",index>0);UI.SetEnabled("chapter-later",index+1<Data.catalog.chapters.Length);
            Button("chapter-apply","完成",323,-147,185,()=>Apply(0),true);
            Button("campaign-reload","从工程重载",-294,-250,200,()=>Switch(Panel.Discard));
            UI.FocusButton("chapter-apply");
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
            if(!anchor)Label("Transfer rule",Selected.main?"关联支线随主线一起移动。":"移入后由目标章末关解锁，可重新指定。",0,-254,900,17,true);
            Pager("targets",0,-299,choicePage,options.Count,p=>choicePage=p);
        }
        private void DrawReview()
        {
            var changes=Changes();
            Label("Review heading",changes.Count+" 项修改",-124,228,940,25);
            for(int i=reviewPage*Rows;i<Math.Min(changes.Count,(reviewPage+1)*Rows);i++)
            {
                float y=169-(i%Rows)*44;
                Label("Change "+i,changes[i],0,y,1176,20);
                Rule("Review separator "+i,0,y-23,1176);
            }
            Pager("changes",0,-207,reviewPage,changes.Count,p=>reviewPage=p);
            string problem=CampaignProject.PublicationError(Data);
            if(problem!=null)Label("Issue",problem,-101,-255,970,18,true);
            Button("campaign-publish","确认写回",505,-254,191,Publish,true);
            UI.SetEnabled("campaign-publish",problem==null&&changes.Count>0);
            if(problem!=null)
            {
                Button("campaign-fix","打开待处理关卡",-494,-207,212,()=>
                {
                    var b=Data.boards.First(level=>new EditDocument(level).Validation()!=null||!CampaignDocument.Proves(level,Data.Plan(level.id)?.route,out int count)||count!=Data.Plan(level.id).pushes);
                    selectedId=b.id;NormalizeSelection();panel=Panel.Outline;OpenBoard();
                });
            }
            UI.FocusButton(problem==null?"campaign-publish":"campaign-fix");
        }
        private void DrawConfirmation()
        {
            bool remove=panel==Panel.Remove;
            Label("Confirm subject",remove?Data.Board(selectedId).title:"放弃当前工作草稿？",0,107,790,29);
            Label("Confirm detail",remove?"从章节编排中移出。已写回的棋盘文件会保留。":"重新读取当前工程中的章节、棋盘和路线。",0,46,790,21,true);
            Button("campaign-cancel","取消",-102,-75,178,()=>Switch(Panel.Outline));
            Button("campaign-confirm",remove?"移出目录":"放弃并重载",108,-75,200,()=>
            {if(remove)Edit(()=>doc.Remove(Current,Selected));else Reload();});
            UI.FocusButton("campaign-cancel");
        }
    }
}
