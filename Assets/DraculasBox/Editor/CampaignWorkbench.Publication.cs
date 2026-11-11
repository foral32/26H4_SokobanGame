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
        private readonly HashSet<string> excludedChanges=new();
        private float reviewScroll;
        private CampaignPublication CreatePublication()
        {
            var review=new CampaignPublication(draft.baseline,Data);
            foreach(var item in review.Items)item.selected=!excludedChanges.Contains(item.key);
            return review;
        }
        private void DrawReviewPopup()
        {
            CampaignPublication review;
            try{review=CreatePublication();}
            catch(Exception e){publicationError=e.Message;DrawCompactConfirmation("Publication review",e.Message,"campaign-review-cancel","campaign-publish","重试",Publish);return;}
            var issues=CampaignProject.PublicationIssues(review.Working,CampaignProject.ChangedProofs(review.Baseline,review.Working));
            CampaignProject.PublicationIssue? Issue(CampaignPublication.Item item)
            {
                foreach(var issue in issues)
                {
                    if((item.kind==CampaignPublication.Kind.Board||item.kind==CampaignPublication.Kind.Route||item.kind==CampaignPublication.Kind.LevelAdd)&&issue.id==item.id)return issue;
                }
                return null;
            }
            const float width=704;
            float contentHeight=review.Items.Sum(i=>Issue(i).HasValue?112:78);
            bool banner=!string.IsNullOrEmpty(publicationError);
            float height=Mathf.Clamp(contentHeight+162+(banner?64:0),282,560),viewHeight=height-162-(banner?64:0);
            var root=UI.WorkshopOverlay("Publication review",Vector2.zero,new Vector2(768,height),CloseOverlay);
            OverlayText(root,"Review heading","写回工程",new Vector2(-152,height*.5f-35),new Vector2(400,38),26);
            OverlayButton(root,"campaign-popup-close","×",new Vector2(352,height*.5f-35),new Vector2(32,32),CloseOverlay);
            int count=review.Items.Count(i=>i.selected);
            var subjects=new Dictionary<string,TMP_Text>();
            TMP_Text statusLabel=null;
            void RefreshSelection()
            {
                count=review.Items.Count(i=>i.selected);publicationError=null;
                if(statusLabel!=null)statusLabel.text="";
                foreach(var item in review.Items)
                {
                    if(item.selected)excludedChanges.Remove(item.key);else excludedChanges.Add(item.key);
                    UI.SetLabel("publish-toggle-"+item.key,item.selected?"✓":"");
                    UI.WorkshopChoice("publish-toggle-"+item.key,item.selected);
                    subjects[item.key].color=item.selected?GameUI.WorkshopInk:Muted;
                }
                UI.SetLabel("publish-select-all",count==review.Items.Count?"全不选":"全选");
                UI.SetLabel("campaign-publish","写回所选（"+count+"）");
            }
            OverlayButton(root,"publish-select-all",count==review.Items.Count?"全不选":"全选",new Vector2(220,height*.5f-35),new Vector2(92,32),()=>
            {bool select=count!=review.Items.Count;foreach(var item in review.Items)item.selected=select;RefreshSelection();});
            if(banner)statusLabel=OverlayText(root,"Publication status",publicationError.Split('\n')[0],new Vector2(0,height*.5f-86),new Vector2(width,58),18);
            var viewport=UI.Rect("Review viewport",new Vector2(0,banner?-27:5),new Vector2(width,viewHeight),root);
            viewport.gameObject.AddComponent<Image>().color=Color.clear;viewport.gameObject.AddComponent<RectMask2D>();
            var sheet=UI.Rect("Review items",Vector2.zero,new Vector2(width,Mathf.Max(contentHeight,viewHeight)),viewport);
            sheet.anchorMin=sheet.anchorMax=sheet.pivot=Vector2.up;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=sheet;scroll.horizontal=false;
            scroll.inertia=false;scroll.scrollSensitivity=40;scroll.movementType=ScrollRect.MovementType.Clamped;
            float y=0;
            foreach(var item in review.Items)
            {
                var row=item;var issue=Issue(row);float rowHeight=issue.HasValue?112:78;
                var check=OverlayButton(sheet,"publish-toggle-"+row.key,row.selected?"✓":"",new Vector2(18,y-31),new Vector2(30,30),()=>
                {reviewScroll=sheet.anchoredPosition.y;row.selected=!row.selected;RefreshSelection();},row.selected);
                subjects[row.key]=OverlayText(sheet,"Change subject "+row.key,row.title,new Vector2(330,y-23),new Vector2(550,30),21,!row.selected);
                OverlayText(sheet,"Change detail "+row.key,row.detail,new Vector2(330,y-52),new Vector2(550,27),17,true);
                if(issue.HasValue)
                {
                    var pending=issue.Value;
                    var label=OverlayText(sheet,"Change issue "+row.key,pending.message,new Vector2(328,y-83),new Vector2(546,40),17);
                    label.color=GameUI.WorkshopAccent;
                    OverlayButton(sheet,"campaign-fix-"+pending.id,"修改",new Vector2(660,y-43),new Vector2(82,34),()=>
                    {reviewScroll=sheet.anchoredPosition.y;selectedId=pending.id;NormalizeSelection();panel=Panel.Outline;OpenBoard(false,true);});
                }
                UI.Image("Change divider "+row.key,null,new Vector2(width*.5f,y-rowHeight+2),new Vector2(width,1),GameUI.WorkshopLine,sheet);
                y-=rowHeight;
            }
            foreach(RectTransform child in sheet){var p=child.anchoredPosition;child.anchorMin=child.anchorMax=Vector2.up;child.anchoredPosition=p;}
            sheet.anchoredPosition=new Vector2(0,Mathf.Clamp(reviewScroll,0,Mathf.Max(0,contentHeight-viewHeight)));
            if(contentHeight>viewHeight)
            {
                var track=UI.WorkshopPanel("Review scrollbar",new Vector2(367,banner?-27:5),new Vector2(4,viewHeight),root);
                var handle=UI.WorkshopPanel("Review scroll thumb",Vector2.zero,Vector2.zero,track.transform);handle.color=GameUI.WorkshopAccent;
                var bar=track.gameObject.AddComponent<Scrollbar>();bar.handleRect=handle.rectTransform;bar.targetGraphic=handle;
                bar.direction=Scrollbar.Direction.BottomToTop;scroll.verticalScrollbar=bar;
            }
            UI.Image("Review footer rule",null,new Vector2(0,-height*.5f+64),new Vector2(width,1),GameUI.WorkshopLine,root);
            UI.WorkshopTextAction("campaign-reload","重载工程",new Vector2(-298,-height*.5f+30),new Vector2(108,38),()=>{overlay=Overlay.Reload;Draw();},root);
            OverlayButton(root,"campaign-review-cancel","取消",new Vector2(130,-height*.5f+30),new Vector2(104,38),CloseOverlay);
            // Keep the action clickable: invalid selections must explain why.
            OverlayButton(root,"campaign-publish","写回所选（"+count+"）",new Vector2(277,-height*.5f+30),new Vector2(150,38),Publish,true);
            UI.FocusButton("campaign-review-cancel");
        }
    }
}
