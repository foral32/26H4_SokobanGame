using System;
using System.Collections.Generic;
using System.Linq;
using DraculasBox.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Editor
{
    internal sealed partial class CampaignWorkbench
    {
        private enum Overlay { None,Chapter,DeleteChapter,Review,Reload,Level,Remove,Transfer,Anchor }
        private Overlay overlay;
        private string overlayChapterId,chapterNameDraft,overlayError;
        private TMP_InputField chapterName;
        private Vector2 chapterPopupCenter,levelPopupCenter;

        private void NewChapter()
        {
            if(!Change(()=>{chapterId=doc.AddChapter();selectedId=Current.levels[0].id;mapOffset=0;})){Draw();return;}
            ShowChapterTools(chapterId);
        }
        private void ShowChapterTools(string id)
        {
            overlayChapterId=id;chapterNameDraft=null;overlayError="";overlay=Overlay.Chapter;
            LocateChapterPopup();Draw();
        }
        private void LocateChapterPopup()
        {
            if(panel!=Panel.Chapters){chapterPopupCenter=new Vector2(268,-132);return;}
            int index=Array.FindIndex(Data.catalog.chapters,c=>c.id==overlayChapterId);
            chapterPage=index/4;
            int count=Math.Min(4,Data.catalog.chapters.Length-chapterPage*4);
            float x=(index%4-(count-1)*.5f)*294;
            chapterPopupCenter=new Vector2(Mathf.Clamp(x+104,-418,418),-38);
        }
        private void CloseOverlay()
        {
            if(overlay==Overlay.Chapter){ApplyChapter(0,true);return;}
            if(overlay==Overlay.DeleteChapter){overlay=Overlay.Chapter;Draw();return;}
            if(overlay==Overlay.Reload){overlay=Overlay.Review;Draw();return;}
            if(overlay==Overlay.Remove||overlay==Overlay.Transfer||overlay==Overlay.Anchor){overlay=Overlay.Level;overlayError="";Draw();return;}
            overlay=Overlay.None;Draw();
        }
        private void ApplyChapter(int shift,bool close)
        {
            chapterNameDraft=(chapterName==null?Data.Chapter(overlayChapterId).title:chapterName.text).Trim();
            if(chapterNameDraft.Length==0){overlayError="请输入章节名称。";Draw();return;}
            if(!Change(()=>
            {
                Data.Chapter(overlayChapterId).title=chapterNameDraft;
                if(shift!=0)doc.MoveChapter(overlayChapterId,shift);
                chapterId=overlayChapterId;
                if(!Current.levels.Any(l=>l.id==selectedId))selectedId=Current.levels.First(l=>l.main).id;
            })){overlayError=notice;Draw();return;}
            overlayError="";chapterNameDraft=null;LocateChapterPopup();
            if(close)overlay=Overlay.None;
            Draw();
        }
        private TMP_Text OverlayText(Transform parent,string name,string text,Vector2 pos,Vector2 size,float font=20,bool quiet=false)
        {
            var label=UI.Text(name,text,pos,size,font,quiet?Muted:GameUI.WorkshopInk,TextAlignmentOptions.MidlineLeft);
            label.transform.SetParent(parent,false);return label;
        }
        private Button OverlayButton(Transform parent,string id,string text,Vector2 pos,Vector2 size,Action action,bool primary=false)
        {
            var button=UI.EditorButton(id,text,pos,size,action,primary,parent,true);
            if(size.x<=40)button.transform.Find("Label").GetComponent<TMP_Text>().rectTransform.sizeDelta=size;
            return button;
        }
        private void DrawOverlay()
        {
            if(overlay==Overlay.Remove)DrawRemoveLevel();
            else if(overlay==Overlay.Transfer||overlay==Overlay.Anchor)DrawTargetPopup();
            else if(overlay==Overlay.Level)DrawLevelPopup();
            else if(overlay==Overlay.Chapter)DrawChapterPopup();
            else if(overlay==Overlay.DeleteChapter)DrawDeleteChapter();
            else if(overlay==Overlay.Review)DrawReviewPopup();
            else if(overlay==Overlay.Reload)DrawReloadPopup();
        }
        private void ShowLevelTools(string id)
        {
            if(dragId!=null)CancelDrag();selectedId=id;
            var button=mapView?.Alive==true?mapView.Button(id):null;
            var at=button==null?Vector2.zero:(Vector2)UI.Root.InverseTransformPoint(button.transform.position);
            levelPopupCenter=new Vector2(Mathf.Clamp(at.x+176,-468,468),Mathf.Clamp(at.y-92,-154,154));
            overlay=Overlay.Level;notice="";Draw();
        }
        private string DisplayName(string id)=>CampaignLabels.Name(Data.catalog,id,Data.Board(id).title);
        public string LevelPrefix(string id)=>CampaignLabels.Prefix(Data.catalog,id);
        private void DrawLevelPopup()
        {
            var root=UI.WorkshopOverlay("Level tools",levelPopupCenter,new Vector2(320,212),CloseOverlay);
            OverlayText(root,"Selected level",DisplayName(selectedId),new Vector2(-12,75),new Vector2(250,32),22);
            OverlayButton(root,"level-tools-close","×",new Vector2(133,75),new Vector2(30,28),CloseOverlay);
            OverlayButton(root,"campaign-copy","复制关卡",new Vector2(0,25),new Vector2(272,36),()=>Edit(()=>selectedId=doc.AddLevel(Current,Selected,true)));
            OverlayButton(root,"campaign-transfer","移至章节",new Vector2(0,-20),new Vector2(272,36),()=>{overlay=Overlay.Transfer;choicePage=0;overlayError="";Draw();});
            var remove=OverlayButton(root,"campaign-remove","删除关卡",new Vector2(0,-65),new Vector2(272,36),()=>{overlay=Overlay.Remove;overlayError="";Draw();});
            remove.transform.Find("Label").GetComponent<TMP_Text>().color=GameUI.WorkshopAccent;
            UI.FocusButton("campaign-copy");
        }
        private void DrawRemoveLevel()
        {
            DrawCompactConfirmation("Remove level","删除「"+DisplayName(selectedId)+"」？","campaign-cancel","campaign-confirm","删除",()=>
            {
                if(Change(()=>doc.Remove(Current,Selected))){overlay=Overlay.None;Draw();}
                else{overlayError=notice;Draw();}
            },overlayError);
            UI.FocusButton("campaign-cancel");
        }
        private void DrawTargetPopup()
        {
            bool anchor=overlay==Overlay.Anchor;
            var options=anchor?CampaignDocument.Main(Current).Where(l=>l.id!=selectedId).Select(l=>(id:l.id,name:DisplayName(l.id))).ToList()
                :Data.catalog.chapters.Where(c=>c.id!=chapterId).Select(c=>(id:c.id,name:c.title)).ToList();
            int pages=Math.Max(1,(options.Count+5)/6);choicePage=Mathf.Clamp(choicePage,0,pages-1);
            float height=136+Math.Max(1,Math.Min(6,options.Count))*46+(pages>1?44:0);
            var root=UI.WorkshopOverlay("Move level",Vector2.zero,new Vector2(520,height),CloseOverlay);
            OverlayText(root,"Move title",anchor?"选择解锁关":"移至章节",new Vector2(-12,height*.5f-32),new Vector2(440,34),25);
            OverlayButton(root,"campaign-popup-close","×",new Vector2(228,height*.5f-32),new Vector2(30,28),CloseOverlay);
            for(int i=choicePage*6;i<Math.Min(options.Count,(choicePage+1)*6);i++)
            {
                var option=options[i];
                OverlayButton(root,"target-"+option.id,option.name,new Vector2(0,height*.5f-88-i%6*46),new Vector2(448,38),()=>
                {
                    if(Change(()=>{if(anchor)doc.SetRole(Current,Selected,option.id);else doc.Transfer(Current,Selected,Data.Chapter(option.id));}))overlay=Overlay.None;
                    else overlayError=notice;
                    Draw();
                });
            }
            if(options.Count==0)OverlayText(root,"No target","没有其他章节",new Vector2(0,0),new Vector2(440,36),20,true);
            if(pages>1)
            {
                OverlayButton(root,"targets-prev","‹",new Vector2(-52,-height*.5f+70),new Vector2(42,32),()=>{choicePage--;Draw();});
                OverlayButton(root,"targets-next","›",new Vector2(52,-height*.5f+70),new Vector2(42,32),()=>{choicePage++;Draw();});
                UI.SetEnabled("targets-prev",choicePage>0);UI.SetEnabled("targets-next",choicePage+1<pages);
            }
            OverlayText(root,"Move status",overlayError??"",new Vector2(0,-height*.5f+30),new Vector2(448,46),17,true);
        }
        private void DrawChapterPopup()
        {
            var c=Data.Chapter(overlayChapterId);int index=Array.IndexOf(Data.catalog.chapters,c);
            float height=string.IsNullOrEmpty(overlayError)?202:254,top=height*.5f-42;
            var root=UI.WorkshopOverlay("Chapter tools",chapterPopupCenter,new Vector2(392,height),CloseOverlay);
            OverlayButton(root,"campaign-popup-close","×",new Vector2(166,top),new Vector2(32,32),CloseOverlay);
            chapterName=UI.Input("Campaign chapter title",chapterNameDraft??c.title,new Vector2(-22,top),new Vector2(296,44),_=>{});
            chapterName.transform.SetParent(root,false);chapterName.restoreOriginalTextOnEscape=false;
            chapterName.onSubmit.AddListener(_=>ApplyChapter(0,true));
            OverlayButton(root,"chapter-earlier","← 前移",new Vector2(-89,top-60),new Vector2(162,38),()=>ApplyChapter(-1,false));
            OverlayButton(root,"chapter-later","后移 →",new Vector2(89,top-60),new Vector2(162,38),()=>ApplyChapter(1,false));
            UI.SetEnabled("chapter-earlier",index>0);UI.SetEnabled("chapter-later",index+1<Data.catalog.chapters.Length);
            OverlayButton(root,"chapter-delete","删除章节…",new Vector2(-89,top-114),new Vector2(162,38),()=>
            {
                ApplyChapter(0,false);
                if(string.IsNullOrEmpty(overlayError)){overlay=Overlay.DeleteChapter;Draw();}
            });
            UI.SetEnabled("chapter-delete",Data.catalog.chapters.Length>1);
            OverlayButton(root,"chapter-apply","完成",new Vector2(89,top-114),new Vector2(162,38),()=>ApplyChapter(0,true),true);
            if(!string.IsNullOrEmpty(overlayError))OverlayText(root,"Chapter tools status",overlayError,new Vector2(0,-height*.5f+31),new Vector2(340,44),18,true);
            if(Data.catalog.chapters.Length==1)UI.SetLabel("chapter-delete","至少保留一章");
            UI.FocusButton("chapter-apply");
        }
        private void DrawDeleteChapter()
        {
            DrawCompactConfirmation("Delete chapter","删除「"+Data.Chapter(overlayChapterId).title+"」？",
                "chapter-delete-cancel","chapter-delete-confirm","删除",DeleteChapter);
            UI.FocusButton("chapter-delete-cancel");
        }
        private void DrawCompactConfirmation(string name,string question,string cancel,string confirm,string action,Action apply,string error=null)
        {
            var heading=UI.Text(name+" heading",question,Vector2.zero,new Vector2(444,80),26,GameUI.WorkshopInk,TextAlignmentOptions.TopLeft);
            float textHeight=heading.GetPreferredValues(question,444,1000).y;
            float height=Mathf.Max(156,textHeight+118)+(string.IsNullOrEmpty(error)?0:64);
            var root=UI.WorkshopOverlay(name,Vector2.zero,new Vector2(500,height),CloseOverlay);
            heading.transform.SetParent(root,false);heading.rectTransform.sizeDelta=new Vector2(444,textHeight+2);
            heading.rectTransform.anchoredPosition=new Vector2(0,height*.5f-28-textHeight*.5f);
            if(!string.IsNullOrEmpty(error))OverlayText(root,name+" error",error,new Vector2(0,-height*.5f+106),new Vector2(444,50),18,true);
            OverlayButton(root,cancel,"取消",new Vector2(26,-height*.5f+43),new Vector2(104,38),CloseOverlay);
            OverlayButton(root,confirm,action,new Vector2(160,-height*.5f+43),new Vector2(124,38),apply,true);
        }
        private void DeleteChapter()
        {
            string id=overlayChapterId;int index=Array.FindIndex(Data.catalog.chapters,c=>c.id==id);
            if(!Change(()=>
            {
                var removed=new HashSet<string>(Data.Chapter(id).levels.Select(l=>l.id));
                doc.RemoveChapter(id);
                if(recovery!=null&&removed.Contains(recovery.id))recovery=null;
                trialRoute=null;trialBoard=null;
                chapterId=Data.catalog.chapters[Math.Min(index,Data.catalog.chapters.Length-1)].id;
                selectedId=Current.levels.First(l=>l.main).id;
            })){overlayError=notice;overlay=Overlay.Chapter;Draw();return;}
            overlay=Overlay.None;panel=Panel.Chapters;notice="";Draw();
        }
        private void DrawReloadPopup()
        {
            DrawCompactConfirmation("Reload campaign","放弃草稿？","campaign-cancel","campaign-confirm","重载",Reload);
            UI.FocusButton("campaign-cancel");
        }
    }
}
