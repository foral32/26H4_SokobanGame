using System;
using System.Linq;
using DraculasBox.Runtime;
using TMPro;
using UnityEngine;

namespace DraculasBox.Editor
{
    internal sealed partial class CampaignWorkbench
    {
        private GameUI.ChapterMapView mapView;
        private ChapterMapLayout baseLayout;
        private string dragId;
        private float mapOffset;
        private Vector2 grab;
        private MapInsertion? aimed;
        private bool validDrop,painted,canBranch;
        private TMP_Text mapStatus;

        public bool HandleKeys(IStudyKeyboard keys)
        {
            if(overlay!=Overlay.None)return false;
            if(panel!=Panel.Outline&&panel!=Panel.Chapters)return false;
            if(dragId!=null){if(keys.Pressed(KeyCode.Escape))CancelDrag();return true;}
            if(panel==Panel.Outline&&(keys.Pressed(KeyCode.Menu)||keys.Pressed(KeyCode.F10)&&(keys.Held(KeyCode.LeftShift)||keys.Held(KeyCode.RightShift)))){ShowLevelTools(selectedId);return true;}
            bool command=keys.Held(KeyCode.LeftControl)||keys.Held(KeyCode.RightControl)||keys.Held(KeyCode.LeftCommand)||keys.Held(KeyCode.RightCommand);
            if(!command)return false;
            bool z=keys.Pressed(KeyCode.Z),redo=keys.Pressed(KeyCode.Y)||z&&(keys.Held(KeyCode.LeftShift)||keys.Held(KeyCode.RightShift));
            if(!z&&!redo)return false;
            if(redo?doc.CanRedo:doc.CanUndo)History(!redo);
            return true;
        }

        private void DrawChapters()
        {
            UI.MapPage("编辑正式关卡");
            UI.MapAction("campaign-back","主菜单",new Vector2(-517,290),170,Back,GameIcon.Back);
            UI.MapAction("campaign-new-chapter","新章节",new Vector2(515,290),170,NewChapter,GameIcon.Create);
            var chapters=Data.catalog.chapters;int first=chapterPage*4,count=Math.Min(4,chapters.Length-first);
            for(int i=0;i<count;i++)
            {
                var c=chapters[first+i];int main=c.levels.Count(l=>l.main),extra=c.levels.Length-main;
                var pos=new Vector2((i-(count-1)*.5f)*294,26);
                UI.ChapterCard("select-chapter-"+c.id,pos,c,
                    main+" 主线"+(extra>0?" · "+extra+" 支线":""),()=>SelectChapter(c));
                UI.EditorButton("chapter-menu-"+c.id,"…",pos+new Vector2(116,77),new Vector2(44,36),()=>ShowChapterTools(c.id),false,null,true);
            }
            int pages=(chapters.Length+3)/4;
            if(pages>1)
            {
                UI.MapAction("chapters-prev","上一页",new Vector2(-167,-227),158,()=>{chapterPage--;Draw();},GameIcon.Back);
                UI.MapAction("chapters-next","下一页",new Vector2(167,-227),158,()=>{chapterPage++;Draw();});
                UI.SetEnabled("chapters-prev",chapterPage>0);UI.SetEnabled("chapters-next",chapterPage+1<pages);
                UI.Text("Chapter page",(chapterPage+1)+" / "+pages,new Vector2(0,-227),new Vector2(98,38),20,GameUI.Quiet);
            }
            HistoryTools();
            ReviewAction();
            UI.FocusButton("select-chapter-"+(chapters.Skip(first).Take(count).Any(c=>c.id==chapterId)?chapterId:chapters[first].id));
            if(!string.IsNullOrEmpty(notice))UI.Text("Campaign status",notice,new Vector2(0,-267),new Vector2(1080,30),18,GameUI.Quiet);
        }
        private void DrawOutline()
        {
            UI.MapPage(Current.title);
            UI.MapAction("campaign-back","返回章节",new Vector2(-506,292),198,Back,GameIcon.Back);
            UI.MapAction("campaign-new","新关卡",new Vector2(517,292),164,NewBoard,GameIcon.Create);
            mapView=new GameUI.ChapterMapView(UI,Current,true,id=>Data.Board(id).title,SelectLevel,id=>{if(dragId==null)selectedId=id;},ShowLevelTools);
            baseLayout=mapView.Layout;
            foreach(var node in Current.levels)
            {
                string id=node.id;var handle=mapView.Button(id).gameObject.AddComponent<CampaignRowDrag>();
                handle.Begin=p=>BeginDrag(id,p);handle.Move=MoveDrag;handle.End=EndDrag;handle.EdgePan=EdgePan;handle.Cancel=CancelDrag;
                handle.Context=()=>ShowLevelTools(id);
            }
            void Tool(string id,string text,float x,float width,Action click,bool on=true)
            {UI.Button(id,text,new Vector2(x,-313),new Vector2(width,42),click,on);UI.SetEnabled(id,on);}
            HistoryTools();
            Tool("campaign-chapter-settings","章节…",334,116,()=>ShowChapterTools(chapterId));
            ReviewAction();
            mapStatus=UI.Text("Campaign status",notice??"",new Vector2(0,-257),new Vector2(1120,32),17,GameUI.Quiet);
            mapView.SetOffset(mapOffset);UI.FocusButton("select-level-"+selectedId);mapView.Show(Current,baseLayout,selectedId);
        }
        private void HistoryTools()
        {
            UI.Button("campaign-undo","撤销",new Vector2(-530,-313),new Vector2(116,42),()=>History(true));
            UI.Button("campaign-redo","重做",new Vector2(-400,-313),new Vector2(116,42),()=>History(false));
            UI.EditorShortcut("campaign-undo","Ctrl+Z");UI.EditorShortcut("campaign-redo","Ctrl+Y");
            UI.SetEnabled("campaign-undo",doc.CanUndo);UI.SetEnabled("campaign-redo",doc.CanRedo);
        }
        private void ReviewAction()
        {
            UI.EditorButton("campaign-review","写回工程",new Vector2(497,-313),new Vector2(154,42),()=>
            {overlay=Overlay.Review;notice="";publicationError=null;Draw();},Dirty,null,true);
            UI.SetEnabled("campaign-review",Dirty);
        }
        private void NewBoard()
        {
            string added=null;Edit(()=>{added=doc.AddLevel(Current,Selected,false);selectedId=added;});
            if(added!=null&&string.IsNullOrEmpty(notice))OpenBoard();
        }
        private void BeginDrag(string id,Vector2 screen)
        {
            if(dragId!=null)return;
            dragId=id;aimed=null;painted=false;validDrop=false;
            var node=Current.levels.First(l=>l.id==id);
            canBranch=!node.main||baseLayout.centers.Length>1&&!Current.levels.Any(l=>!l.main&&l.requires[0]==id);
            mapView.Pointer(screen,app.viewCamera,out var press,out _);
            var offset=press-baseLayout.positions[id];grab=new Vector2(Mathf.Clamp(offset.x,-46,46),Mathf.Clamp(offset.y,-46,46));
            mapView.BeginDragPreview(id,baseLayout);
        }
        private void MoveDrag(Vector2 screen)
        {
            if(dragId==null)return;
            bool inside=mapView.Pointer(screen,app.viewCamera,out var point,out _,22);
            var center=point-grab;
            var target=inside?baseLayout.DropTarget(center,dragId,aimed,canBranch):null;
            if(!painted||!Nullable.Equals(aimed,target))
            {
                aimed=target;painted=true;validDrop=false;mapStatus.text="";
                if(target.HasValue)
                {
                    var next=JsonUtility.FromJson<Chapter>(JsonUtility.ToJson(Current));
                    var source=next.levels.First(l=>l.id==dragId);
                    try
                    {
                        bool heldMain=source.main;
                        if(CampaignDocument.Place(next,source,target.Value))
                        {
                            string anchor=target.Value.anchor;
                            var columns=anchor!=null&&heldMain?CampaignDocument.Main(Current).Select(l=>l.id==dragId?null:l.id).ToArray():null;
                            var preview=new ChapterMapLayout(next,columns,anchor,anchor==null?0:baseLayout.positions[anchor].x);
                            mapView.Show(next,preview,moving:dragId,pending:dragId,dragging:true,minimumWidth:baseLayout.width);
                            validDrop=true;
                        }
                    }
                    catch(InvalidOperationException e){mapStatus.text=e.Message;}
                }
                if(!validDrop)mapView.Show(Current,baseLayout,moving:dragId,dragging:true);
            }
            mapView.Lift(dragId,center);
            if(!validDrop)
            {
                Vector2? socket=null;
                if(inside&&canBranch&&Mathf.Abs(center.y)>45&&Mathf.Abs(center.y)<100)
                {
                    var nearby=baseLayout.DropTarget(new Vector2(center.x,Mathf.Sign(center.y)*ChapterMapLayout.BranchY),dragId);
                    if(nearby.HasValue&&nearby.Value.anchor!=dragId)
                    {
                        float x=baseLayout.positions[nearby.Value.anchor].x;
                        if(Mathf.Abs(x-center.x)<70)socket=new Vector2(x,center.y>0?60:-91);
                    }
                }
                mapView.HintBranchSocket(socket);
            }
        }
        private void EdgePan(Vector2 screen,float dt)
        {
            if(dragId==null||!mapView.Pointer(screen,app.viewCamera,out _,out float x,22))return;
            float edge=ChapterMapLayout.ViewWidth*.5f-44;
            if(Mathf.Abs(x)<=edge)return;
            float before=mapView.Offset;mapView.Pan(Mathf.Sign(x)*430*dt);
            if(Mathf.Abs(before-mapView.Offset)>.01f)MoveDrag(screen);
        }
        private void EndDrag(Vector2 screen)
        {
            if(dragId==null)return;MoveDrag(screen);
            if(!validDrop){CancelDrag();return;}
            string id=dragId;var target=aimed.Value;var previous=mapView.CapturePositions();dragId=null;
            Edit(()=>{CampaignDocument.Place(Current,Current.levels.First(l=>l.id==id),target);selectedId=id;});
            mapView?.SettleFrom(previous);
        }
        private void CancelDrag()
        {
            if(dragId==null)return;dragId=null;aimed=null;validDrop=false;
            if(mapView?.Alive==true)
            {
                mapView.EndDragPreview();mapView.Show(Current,baseLayout,selectedId);
                UI.FocusButton("select-level-"+selectedId);
                mapStatus.text="";
            }
        }
    }
}
