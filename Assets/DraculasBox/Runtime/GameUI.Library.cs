using System;
using System.Collections.Generic;
using System.Linq;
using DraculasBox.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    public sealed partial class GameUI
    {
        public const int LibraryPageSize=6;
        private List<LevelData> libraryData;
        private RectTransform libraryActions,libraryMore;
        private readonly Dictionary<string,Graphic> librarySelected=new Dictionary<string,Graphic>();
        private readonly Dictionary<string,Vector2> libraryPositions=new Dictionary<string,Vector2>();
        public void Library(List<LevelData> list,int page,int invalid,bool recover)
        {
            Clear();WorkshopTheme();libraryData=list;librarySelected.Clear();libraryPositions.Clear();
            WorkshopIcon("back",GameIcon.Back,new Vector2(-579,312),new Vector2(46,42),app.ShowTitle,null,true);
            Text("Library heading","我的关卡",new Vector2(-370,312),new Vector2(320,46),30,WorkshopInk,TextAlignmentOptions.MidlineLeft);
            EditorButton("library-generate","生成草稿",new Vector2(65,312),new Vector2(138,42),()=>app.Creator.OpenGenerator(true),false,null,true);
            EditorButton("new","新建",new Vector2(208,312),new Vector2(112,42),app.EditNew,false,null,true);
            EditorButton("import","导入",new Vector2(336,312),new Vector2(112,42),app.OpenImport,false,null,true);
            EditorButton("template","复制主线",new Vector2(514,312),new Vector2(196,42),app.CopyCampaign,false,null,true);
            Image("Library rule",null,new Vector2(0,280),new Vector2(1224,1),WorkshopLine);
            var visible=list.Skip(page*LibraryPageSize).Take(LibraryPageSize).ToArray();
            foreach(var data in visible)
            {
                int index=Array.IndexOf(visible,data);var pos=new Vector2((index%3-1)*408,141-index/3*272);libraryPositions[data.id]=pos;
                var rect=Rect("custom-"+data.id,pos,new Vector2(376,220));var button=ClickTarget(rect);
                var surface=WorkshopPanel("Preview surface",new Vector2(0,22),new Vector2(366,176),rect);surface.color=BoardView.SafeColor;surface.raycastTarget=false;
                Thumbnail("Layout",data,new Vector2(0,22),new Vector2(356,166),rect);
                Label(Rect("Draft title",new Vector2(0,-84),new Vector2(366,34),rect),data.title,23,WorkshopInk,TextAlignmentOptions.MidlineLeft);
                var selectedFrame=WorkshopPanel("Selected",new Vector2(0,22),new Vector2(372,182),rect,true);selectedFrame.color=Color.clear;librarySelected[data.id]=selectedFrame;
                var focus=WorkshopPanel("Focus",new Vector2(0,22),new Vector2(380,190),rect,true);focus.color=new Color(WorkshopLine.r,WorkshopLine.g,WorkshopLine.b,0);focus.raycastTarget=false;
                var row=Register("custom-"+data.id,button,()=>app.SelectLibrary(data.id),focus);
                row.submit=()=>{app.SelectLibrary(data.id);app.EditSelectedCustom();};
                button.gameObject.AddComponent<LibraryDoubleClick>().Open=()=>{if(app.Page==GamePage.Library&&!app.Transitioning){app.SelectLibrary(data.id);app.EditSelectedCustom();}};
            }
            if(list.Count>0)
            {
                libraryActions=Rect("Selected actions",Vector2.zero,new Vector2(376,40));
                EditorButton("edit-custom","编辑",new Vector2(-126,0),new Vector2(108,38),app.EditSelectedCustom,false,libraryActions,true);
                WorkshopChoice("edit-custom",true);
                EditorButton("more-custom","...",new Vector2(-40,0),new Vector2(44,38),()=>{bool open=!libraryMore.gameObject.activeSelf;libraryMore.gameObject.SetActive(open);if(open){int first=rows.FindIndex(r=>r.id=="duplicate-custom");Focus(first);}},false,libraryActions,true);
                libraryMore=Rect("Library actions popup",new Vector2(38,70),new Vector2(282,60),libraryActions);WorkshopPanel("Action surface",Vector2.zero,new Vector2(282,60),libraryMore);WorkshopPanel("Action border",Vector2.zero,new Vector2(282,60),libraryMore,true);
                EditorButton("duplicate-custom","复制",new Vector2(-90,0),new Vector2(80,36),app.DuplicateCustom,false,libraryMore,true);
                EditorButton("export-custom","导出",Vector2.zero,new Vector2(80,36),app.ExportCustom,false,libraryMore,true);
                EditorButton("remove-custom","移除",new Vector2(90,0),new Vector2(80,36),app.ArchiveCustom,false,libraryMore,true);
                libraryMore.gameObject.SetActive(false);libraryMore.gameObject.AddComponent<WorkshopPopover>();WorkshopPopup(libraryMore.gameObject);
                if(!visible.Any(d=>d.id==app.LibrarySelection))app.LibrarySelection=visible[0].id;
                RefreshLibrarySelection(app.LibrarySelection);
            }
            else Text("Empty library","",new Vector2(0,0),new Vector2(500,45),24,Muted);
            if(invalid>0)EditorButton("invalid-files",invalid+" 个文件无法读取",new Vector2(-400,-321),new Vector2(300,34),app.ShowLibraryIssues,false,null,true);
            if(page>0)WorkshopIcon("prev-page",GameIcon.Back,new Vector2(-100,-321),new Vector2(42,34),()=>app.LibraryPage(page-1),null,true);
            if((page+1)*LibraryPageSize<list.Count)WorkshopIcon("next-page",GameIcon.Arrow,new Vector2(100,-321),new Vector2(42,34),()=>app.LibraryPage(page+1),null,true);
            if(list.Count>LibraryPageSize)Text("Library page",(page+1)+" / "+((list.Count+LibraryPageSize-1)/LibraryPageSize),new Vector2(0,-321),new Vector2(100,36),19,Muted);
            if(recover)EditorButton("recover","恢复草稿",new Vector2(-91,312),new Vector2(136,42),app.RecoverDraft,false,null,true);
            if(app.CanRestoreArchive)EditorButton("undo-remove","恢复移除",new Vector2(508,-321),new Vector2(176,36),app.RestoreArchive,false,null,true);
            int selected=rows.FindIndex(r=>r.id=="custom-"+app.LibrarySelection);Focus(Mathf.Max(0,selected));
        }
        public void RefreshLibrarySelection(string id)
        {
            if(libraryData==null||!libraryPositions.TryGetValue(id,out var position))return;
            foreach(var item in librarySelected)item.Value.color=item.Key==id?WorkshopAccent:Color.clear;
            libraryActions.anchoredPosition=position+new Vector2(0,-128);libraryMore.gameObject.SetActive(false);
        }
    }
}
