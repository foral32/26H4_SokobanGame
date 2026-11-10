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
        private enum Overlay { None,Chapter,DeleteChapter,Review,Reload }
        private Overlay overlay;
        private string overlayChapterId,chapterNameDraft,overlayError;
        private TMP_InputField chapterName;
        private Vector2 chapterPopupCenter;

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
            =>UI.EditorButton(id,text,pos,size,action,primary,parent,true);
        private void DrawOverlay()
        {
            if(overlay==Overlay.Chapter)DrawChapterPopup();
            else if(overlay==Overlay.DeleteChapter)DrawDeleteChapter();
            else if(overlay==Overlay.Review)DrawReviewPopup();
            else if(overlay==Overlay.Reload)DrawReloadPopup();
        }
        private void DrawChapterPopup()
        {
            var c=Data.Chapter(overlayChapterId);int index=Array.IndexOf(Data.catalog.chapters,c);
            var root=UI.WorkshopOverlay("Chapter tools",chapterPopupCenter,new Vector2(392,292),CloseOverlay);
            OverlayText(root,"Chapter tools heading","章节",new Vector2(-110,113),new Vector2(120,32),23);
            OverlayText(root,"Chapter position","第 "+(index+1)+" 章",new Vector2(76,113),new Vector2(100,30),18,true);
            OverlayButton(root,"campaign-popup-close","×",new Vector2(166,113),new Vector2(32,30),CloseOverlay);
            chapterName=UI.Input("Campaign chapter title",chapterNameDraft??c.title,new Vector2(0,61),new Vector2(340,44),_=>{});
            chapterName.transform.SetParent(root,false);chapterName.restoreOriginalTextOnEscape=false;
            chapterName.onSubmit.AddListener(_=>ApplyChapter(0,true));
            OverlayButton(root,"chapter-earlier","← 前移",new Vector2(-89,3),new Vector2(162,38),()=>ApplyChapter(-1,false));
            OverlayButton(root,"chapter-later","后移 →",new Vector2(89,3),new Vector2(162,38),()=>ApplyChapter(1,false));
            UI.SetEnabled("chapter-earlier",index>0);UI.SetEnabled("chapter-later",index+1<Data.catalog.chapters.Length);
            OverlayButton(root,"chapter-delete","删除章节…",new Vector2(-89,-57),new Vector2(162,38),()=>
            {
                ApplyChapter(0,false);
                if(string.IsNullOrEmpty(overlayError)){overlay=Overlay.DeleteChapter;Draw();}
            });
            UI.SetEnabled("chapter-delete",Data.catalog.chapters.Length>1);
            OverlayButton(root,"chapter-apply","完成",new Vector2(89,-57),new Vector2(162,38),()=>ApplyChapter(0,true),true);
            OverlayText(root,"Chapter tools status",overlayError??"",new Vector2(0,-112),new Vector2(340,44),18,true);
            if(Data.catalog.chapters.Length==1)UI.SetLabel("chapter-delete","至少保留一章");
            UI.FocusButton("chapter-apply");
        }
        private void DrawDeleteChapter()
        {
            DrawCompactConfirmation("Delete chapter","删除「"+Data.Chapter(overlayChapterId).title+"」？",
                "chapter-delete-cancel","chapter-delete-confirm","删除",DeleteChapter);
            UI.FocusButton("chapter-delete-cancel");
        }
        private void DrawCompactConfirmation(string name,string question,string cancel,string confirm,string action,Action apply)
        {
            var heading=UI.Text(name+" heading",question,Vector2.zero,new Vector2(444,80),26,GameUI.WorkshopInk,TextAlignmentOptions.TopLeft);
            float textHeight=heading.GetPreferredValues(question,444,1000).y;
            float height=Mathf.Max(156,textHeight+118);
            var root=UI.WorkshopOverlay(name,Vector2.zero,new Vector2(500,height),CloseOverlay);
            heading.transform.SetParent(root,false);heading.rectTransform.sizeDelta=new Vector2(444,textHeight+2);
            heading.rectTransform.anchoredPosition=new Vector2(0,height*.5f-28-textHeight*.5f);
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
        private void DrawReviewPopup()
        {
            var changes=Changes();var issues=new List<CampaignProject.PublicationIssue>();string invalid=null;
            try{issues=CampaignProject.PublicationIssues(Data);}catch(Exception e){invalid=e.Message;}
            string banner=invalid??publicationError;
            const float width=636;
            float contentHeight=changes.Count*56+(issues.Count>0?issues.Count*76+26:0)+(banner!=null?64:0);
            float height=Mathf.Clamp(contentHeight+142,238,514),viewHeight=height-142;
            var root=UI.WorkshopOverlay("Publication review",Vector2.zero,new Vector2(700,height),CloseOverlay);
            OverlayText(root,"Review heading","写回工程",new Vector2(-119,height*.5f-35),new Vector2(398,38),26);
            OverlayButton(root,"campaign-popup-close","×",new Vector2(318,height*.5f-35),new Vector2(32,32),CloseOverlay);
            var viewport=UI.Rect("Review viewport",new Vector2(0,7),new Vector2(width,viewHeight),root);
            viewport.gameObject.AddComponent<Image>().color=Color.clear;viewport.gameObject.AddComponent<RectMask2D>();
            var sheet=UI.Rect("Review items",Vector2.zero,new Vector2(width,Mathf.Max(contentHeight,viewHeight)),viewport);
            sheet.anchorMin=sheet.anchorMax=sheet.pivot=Vector2.up;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=sheet;
            scroll.horizontal=false;scroll.vertical=true;scroll.inertia=false;scroll.scrollSensitivity=36;scroll.movementType=ScrollRect.MovementType.Clamped;
            float y=-28;
            if(banner!=null){OverlayText(sheet,"Publication status",banner,new Vector2(width*.5f,y),new Vector2(width,56),19,true);y-=64;}
            if(issues.Count>0)
            {
                foreach(var issue in issues)
                {
                    var pending=issue;string id="campaign-fix-"+issue.id;float rowY=y;
                    string owner=Data.catalog.chapters.First(c=>c.levels.Any(l=>l.id==issue.id)).title;
                    OverlayText(sheet,"Pending title "+issue.id,ChapterName(owner)+" / "+issue.title,new Vector2(251,y+7),new Vector2(502,30),21);
                    OverlayText(sheet,"Pending reason "+issue.id,issue.trial?"未通关":issue.message,new Vector2(251,y-21),new Vector2(502,30),18,true);
                    OverlayButton(sheet,id,issue.trial?"试玩":"修改",new Vector2(580,y-7),new Vector2(112,38),()=>
                    {selectedId=pending.id;NormalizeSelection();panel=Panel.Outline;OpenBoard(pending.trial,true);});
                    UI.OnFocus(id,()=>
                    {
                        float top=-rowY-24,bottom=top+66,offset=sheet.anchoredPosition.y;
                        if(top<offset)offset=top;else if(bottom>offset+viewport.rect.height)offset=bottom-viewport.rect.height;
                        sheet.anchoredPosition=new Vector2(0,Mathf.Clamp(offset,0,Mathf.Max(0,sheet.rect.height-viewport.rect.height)));
                    });
                    y-=76;
                }
                UI.Image("Pending separator",null,new Vector2(width*.5f,y+14),new Vector2(width,1),GameUI.WorkshopLine,sheet);
                y-=26;
            }
            for(int i=0;i<changes.Count;i++)
            {
                var change=changes[i];
                OverlayText(sheet,"Change subject "+i,change.subject,new Vector2(223,y),new Vector2(446,48),21);
                var detail=OverlayText(sheet,"Change detail "+i,change.detail,new Vector2(560,y),new Vector2(152,48),19,true);
                detail.alignment=TextAlignmentOptions.MidlineRight;
                if(change.removed)detail.color=GameUI.WorkshopAccent;
                if(i+1<changes.Count)UI.Image("Change divider "+i,null,new Vector2(width*.5f,y-28),new Vector2(width,1),Color.Lerp(GameUI.WorkshopSurface,GameUI.WorkshopLine,.45f),sheet);
                y-=56;
            }
            // Row coordinates start at the sheet's top-left, independent of its
            // scrollable height. The shared UI helpers default to center anchors.
            foreach(RectTransform child in sheet)
            {
                var position=child.anchoredPosition;
                child.anchorMin=child.anchorMax=Vector2.up;child.anchoredPosition=position;
            }
            if(contentHeight>viewHeight)
            {
                var track=UI.WorkshopPanel("Review scrollbar",new Vector2(332,7),new Vector2(4,viewHeight),root);
                var handle=UI.WorkshopPanel("Review scroll thumb",Vector2.zero,Vector2.zero,track.transform);handle.color=GameUI.WorkshopAccent;
                var bar=track.gameObject.AddComponent<Scrollbar>();bar.handleRect=handle.rectTransform;bar.targetGraphic=handle;
                bar.direction=Scrollbar.Direction.BottomToTop;scroll.verticalScrollbar=bar;
            }
            UI.Image("Review footer rule",null,new Vector2(0,-height*.5f+68),new Vector2(width,1),GameUI.WorkshopLine,root);
            UI.WorkshopTextAction("campaign-reload","重载工程",new Vector2(-266,-height*.5f+32),new Vector2(104,38),()=>{overlay=Overlay.Reload;Draw();},root);
            OverlayButton(root,"campaign-review-cancel","取消",new Vector2(104,-height*.5f+32),new Vector2(104,38),CloseOverlay);
            OverlayButton(root,"campaign-publish","写回",new Vector2(254,-height*.5f+32),new Vector2(128,38),Publish,true);
            UI.SetEnabled("campaign-publish",invalid==null&&issues.Count==0&&changes.Count>0);
            UI.FocusButton(issues.Count>0?"campaign-fix-"+issues[0].id:invalid==null&&changes.Count>0?"campaign-publish":"campaign-review-cancel");
        }
        private void DrawReloadPopup()
        {
            DrawCompactConfirmation("Reload campaign","放弃草稿？","campaign-cancel","campaign-confirm","重载",Reload);
            UI.FocusButton("campaign-cancel");
        }
    }
}
