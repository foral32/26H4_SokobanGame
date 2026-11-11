using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    public sealed partial class GameUI
    {
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void CampaignTitleEntry(Transform parent)
        {
#if UNITY_EDITOR
            if(!Application.isPlaying||DraculaApp.CreateCampaignWorkshop==null)return;
            var old=parent.Find("campaign");
            if(old!=null){old.gameObject.SetActive(false);Destroy(old.gameObject);}
            var size=new Vector2(238,60);
            var r=Rect("campaign",Vector2.zero,size,parent);
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(1,0);r.anchoredPosition=new Vector2(-28,22);
            var b=ClickTarget(r);
            var theme=GameThemes.Current;
            var shadow=Image("Shadow",null,new Vector2(0,-4),size,new Color(0,0,0,.25f),r);Rounded(shadow);
            var face=Image("Surface",null,Vector2.zero,size,Color.Lerp(theme.Button,theme.TitleFace,.72f),r);Rounded(face);
            var line=Image("Outline",null,Vector2.zero,size,Color.Lerp(theme.TitleFace,theme.TitleDepth,.19f),r);Rounded(line,true);
            Image("Accent",null,new Vector2(-116,0),new Vector2(3,36),theme.TitleDepth,r).raycastTarget=false;
            Icon("Icon",GameIcon.Create,new Vector2(-88,0),27,theme.TitleDepth,r);
            Label(Rect("Label",new Vector2(20,0),new Vector2(178,43),r),"编辑正式关卡",24,theme.Ink);
            var focus=Rect("Focus",Vector2.zero,size+new Vector2(8,8),r).gameObject.AddComponent<SelectionCorners>();
            focus.color=new Color(Gold.r,Gold.g,Gold.b,0);focus.raycastTarget=false;
            Register("campaign",b,app.OpenCampaign,focus);
#endif
        }
        public void MapPage(string heading)
        {
            Clear();Backdrop();Text("Heading",heading,new Vector2(0,289),new Vector2(700,58),36);
            entryDuration=0;pageGroup.alpha=1;pageGroup.blocksRaycasts=true;
        }
        public void ChapterCard(string id,Vector2 pos,Chapter chapter,string count,Action open)
        {
            var row=TileAction(id,pos,176,GameThemes.Current.NodeDone,open);
            var r=row.button.transform;
            Image("Coffin","coffin-lid",Vector2.zero,new Vector2(111,130),Color.white,r);
            Label(Rect("Chapter title",new Vector2(0,-125),new Vector2(278,48),r),chapter.title,25,White);
            Label(Rect("Chapter size",new Vector2(0,-165),new Vector2(250,35),r),count,18,Quiet);
        }
        public void MapAction(string id,string text,Vector2 pos,float width,Action action,GameIcon icon=GameIcon.Arrow)
        {
            var r=Rect(id,pos,new Vector2(width,50));
            var button=ClickTarget(r);
            var at=new Vector2(-width*.5f+18,0);
            if(icon==GameIcon.Create)
                Label(Rect("Add",at,Vector2.one*30,r),"+",26,Quiet);
            else
                Icon("Direction",icon,at,22,Quiet,r);
            var title=Label(Rect("Label",new Vector2(15,0),new Vector2(width-50,48),r),text,25,Quiet,TextAlignmentOptions.MidlineLeft);
            var focus=Image("Focus",null,at+Vector2.down*19,new Vector2(18,2),new Color(Gold.r,Gold.g,Gold.b,0),r);
            Register(id,button,action,focus,title);
        }

        public sealed class ChapterMapView
        {
            private sealed class Node
            {
                public Button button;public RectTransform rect;public MenuTileGraphic face;public Graphic frame,focus,locked,done;
                public TMP_Text number,name;public CanvasGroup group;public Button more;
            }
            private readonly GameUI ui;
            private readonly bool editing;
            private readonly ScrollRect scroll;
            private readonly RectTransform viewport,sheet,wireRoot;
            private readonly Dictionary<string,Node> nodes=new();
            private readonly List<Image> lines=new();
            private readonly List<(RectTransform rect,Vector2 from,Vector2 to)> slides=new();
            private Coroutine slide;
            private readonly Image marker;
            private readonly RectTransform originGhost,dropSlot;
            private readonly CanvasGroup originAlpha;
            private readonly MenuTileGraphic originFace;
            private readonly TMP_Text originNumber;
            private int used;
            private bool changing;
            private string highlighted;
            public ChapterMapLayout Layout {get;private set;}
            public bool Alive=>sheet!=null&&sheet.gameObject.activeInHierarchy;
            public float Offset=>-sheet.anchoredPosition.x;
            public Button Button(string id)=>nodes[id].button;

            public ChapterMapView(GameUI ui,Chapter c,bool editing,Func<string,string> title,Action<string> open,Action<string> focus=null,Action<string> options=null)
            {
                this.ui=ui;this.editing=editing;
                viewport=ui.Rect("Chapter map",new Vector2(0,5),new Vector2(ChapterMapLayout.ViewWidth,ChapterMapLayout.ViewHeight));
                var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;
                viewport.gameObject.AddComponent<RectMask2D>();
                sheet=ui.Rect("Map content",Vector2.zero,viewport.sizeDelta,viewport);
                sheet.anchorMin=sheet.anchorMax=sheet.pivot=new Vector2(0,.5f);
                scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=sheet;
                scroll.horizontal=true;scroll.vertical=false;scroll.inertia=false;scroll.scrollSensitivity=42;scroll.movementType=ScrollRect.MovementType.Clamped;
                wireRoot=ui.Rect("Paths",Vector2.zero,Vector2.zero,sheet);wireRoot.anchorMin=wireRoot.anchorMax=new Vector2(0,.5f);
                if(editing)
                {
                    marker=ui.Image("Insertion point",null,Vector2.zero,Vector2.one*8,Gold,wireRoot);
                    marker.rectTransform.localRotation=Quaternion.Euler(0,0,45);
                    marker.gameObject.SetActive(false);
                    originGhost=ui.Rect("Drag origin",Vector2.zero,Vector2.one*92,sheet);originGhost.anchorMin=originGhost.anchorMax=new Vector2(0,.5f);
                    originAlpha=originGhost.gameObject.AddComponent<CanvasGroup>();originAlpha.alpha=.24f;originAlpha.blocksRaycasts=false;
                    originFace=ui.Rect("Ghost tile",Vector2.zero,Vector2.one*92,originGhost).gameObject.AddComponent<MenuTileGraphic>();originFace.raycastTarget=false;
                    originNumber=ui.Label(ui.Rect("Ghost number",Vector2.zero,new Vector2(88,62),originGhost),"",40,Quiet);
                    var rim=ui.Rect("Ghost rim",Vector2.zero,Vector2.one*104,originGhost).gameObject.AddComponent<SelectionCorners>();rim.color=Quiet;rim.raycastTarget=false;
                    originGhost.gameObject.SetActive(false);
                    dropSlot=ui.Rect("Drop destination",Vector2.zero,Vector2.one*104,sheet);dropSlot.anchorMin=dropSlot.anchorMax=new Vector2(0,.5f);
                    var target=dropSlot.gameObject.AddComponent<SelectionCorners>();target.color=Gold;target.raycastTarget=false;dropSlot.gameObject.SetActive(false);
                }
                foreach(var link in c.levels)
                {
                    string id=link.id;
                    bool available=editing||ui.app.Progress.Unlocked(link);
                    var row=ui.TileAction((editing?"select-level-":"level-")+id,Vector2.zero,92,GameThemes.Current.NodeOpen,()=>open(id),available,sheet);
                    var r=(RectTransform)row.button.transform;r.anchorMin=r.anchorMax=new Vector2(0,.5f);
                    var n=new Node{button=row.button,rect=r,face=r.Find("Tile").GetComponent<MenuTileGraphic>(),focus=r.Find("Focus").GetComponent<Graphic>(),group=r.gameObject.AddComponent<CanvasGroup>()};
                    n.frame=ui.Rect("Branch frame",Vector2.zero,Vector2.one*75,r).gameObject.AddComponent<BranchFrameGraphic>();
                    n.frame.color=BranchFrameGraphic.Red;n.frame.raycastTarget=false;
                    n.number=ui.Label(ui.Rect("Number",Vector2.zero,new Vector2(89,58),r),"",39,Ink);
                    n.name=ui.Label(ui.Rect("Name",Vector2.zero,new Vector2(132,28),r),title(id),19,White);
                    if(!available)n.locked=ui.Icon("Lock",GameIcon.Lock,Vector2.zero,32,GameThemes.Current.Lock,r);
                    if(!editing&&ui.app.Progress.Done(id))n.done=ui.Icon("Completed",GameIcon.Check,Vector2.zero,17,Gold,r);
                    nodes[id]=n;
                    if(editing&&options!=null)
                    {
                        n.more=ui.EditorButton("level-menu-"+id,"…",new Vector2(43,34),new Vector2(30,26),()=>options(id),false,r);
                        var moreLabel=n.more.transform.Find("Label").GetComponent<TMP_Text>();moreLabel.fontSize=21;moreLabel.rectTransform.sizeDelta=new Vector2(30,26);n.more.gameObject.SetActive(false);
                    }
                    row.focus=()=>{
                        if(changing)return;
                        if(editing&&highlighted!=id)
                        {
                            if(highlighted!=null&&nodes.TryGetValue(highlighted,out var old))
                            {old.face.color=GameThemes.Current.NodeOpen;old.number.color=GameThemes.Current.NodeInk;}
                            n.face.color=GameThemes.Current.NodeDone;n.number.color=GameThemes.Current.NodeDoneInk;highlighted=id;
                        }
                        if(editing)foreach(var item in nodes)if(item.Value.more!=null)item.Value.more.gameObject.SetActive(item.Key==id);
                        Reveal(id);focus?.Invoke(id);
                    };
                }
                Show(c,new ChapterMapLayout(c));
            }
            public void Pan(float delta)=>SetOffset(Offset+delta);
            public void SetOffset(float offset){sheet.anchoredPosition=new Vector2(-Mathf.Clamp(offset,0,Mathf.Max(0,sheet.rect.width-viewport.rect.width)),0);}
            public void Reveal(string id)
            {
                if(Layout==null||!Layout.positions.TryGetValue(id,out var p))return;
                if(p.x-65<Offset)SetOffset(p.x-65);
                else if(p.x+65>Offset+viewport.rect.width)SetOffset(p.x+65-viewport.rect.width);
            }
            public bool Pointer(Vector2 screen,Camera camera,out Vector2 point,out float viewX,float tolerance=0)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(sheet,screen,camera,out point);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport,screen,camera,out var local);
                var bounds=viewport.rect;bounds.xMin-=tolerance;bounds.xMax+=tolerance;bounds.yMin-=tolerance;bounds.yMax+=tolerance;
                viewX=local.x;return bounds.Contains(local);
            }
            public void BeginDragPreview(string id,ChapterMapLayout original)
            {
                if(originGhost==null)return;
                var n=nodes[id];originGhost.anchoredPosition=original.positions[id];originFace.rectTransform.sizeDelta=n.face.rectTransform.sizeDelta;
                originFace.color=GameThemes.Current.NodeOpen;originNumber.text=original.numbers[id];originNumber.fontSize=n.number.fontSize;
                originGhost.gameObject.SetActive(true);originGhost.SetAsLastSibling();
                foreach(var item in nodes)if(item.Value.more!=null)item.Value.more.gameObject.SetActive(false);
            }
            public void EndDragPreview()
            {if(originGhost!=null)originGhost.gameObject.SetActive(false);if(dropSlot!=null)dropSlot.gameObject.SetActive(false);}
            public Dictionary<string,Vector2> CapturePositions()=>nodes.ToDictionary(item=>item.Key,item=>item.Value.rect.anchoredPosition);
            private void StopSlides()
            {
                if(slide!=null){ui.StopCoroutine(slide);slide=null;}
                slides.Clear();
            }
            private void Slide(RectTransform rect,Vector2 from,Vector2 to)
            {
                if((from-to).sqrMagnitude<.25f){rect.anchoredPosition=to;return;}
                rect.anchoredPosition=from;
                slides.Add((rect,from,to));
            }
            private IEnumerator Interpolate()
            {
                float start=Time.unscaledTime;
                while(Alive)
                {
                    float t=Mathf.Clamp01((Time.unscaledTime-start)/.13f);
                    float ease=1-(1-t)*(1-t)*(1-t);
                    foreach(var item in slides)item.rect.anchoredPosition=Vector2.LerpUnclamped(item.from,item.to,ease);
                    if(t>=1)break;
                    yield return null;
                }
                slide=null;
            }
            public void SettleFrom(ChapterMapLayout previous)
                =>SettleFrom(previous.positions);
            public void SettleFrom(Dictionary<string,Vector2> previous)
            {
                StopSlides();
                foreach(var item in nodes)
                    if(previous.TryGetValue(item.Key,out var from))
                        Slide(item.Value.rect,from,Layout.positions[item.Key]);
                if(slides.Count>0)slide=ui.StartCoroutine(Interpolate());
            }
            private void Line(Vector2 a,Vector2 b,Color ink,float thickness=2)
            {
                if((b-a).sqrMagnitude<.02f)return;
                if(used==lines.Count)lines.Add(ui.Image("Path",null,Vector2.zero,Vector2.one,ink,wireRoot));
                var image=lines[used++];image.gameObject.SetActive(true);image.color=ink;
                var r=image.rectTransform;r.anchoredPosition=(a+b)*.5f;r.sizeDelta=new Vector2(Vector2.Distance(a,b),thickness);
                r.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);
            }
            public void Show(Chapter c,ChapterMapLayout layout,string selected=null,string moving=null,string pending=null,bool dragging=false,float minimumWidth=0)
            {
                StopSlides();
                Layout=layout;changing=dragging;highlighted=selected;sheet.sizeDelta=new Vector2(Mathf.Max(minimumWidth,layout.width),ChapterMapLayout.ViewHeight);SetOffset(Offset);
                var projected=pending==null?null:c.levels.First(l=>l.id==pending);
                bool insertedMain=projected!=null&&projected.main;
                string anchor=projected!=null&&!projected.main?projected.requires[0]:null;
                if(marker!=null)
                {
                    marker.gameObject.SetActive(false);
                }
                if(dropSlot!=null)
                {
                    dropSlot.gameObject.SetActive(pending!=null);
                    if(pending!=null){dropSlot.anchoredPosition=layout.positions[pending];dropSlot.sizeDelta=Vector2.one*(insertedMain?106:84);dropSlot.SetAsLastSibling();}
                }
                used=0;Line(new Vector2(layout.centers[0],0),new Vector2(layout.centers[layout.centers.Length-1],0),GameThemes.Current.Path,3);
                foreach(var lane in layout.lanes)
                {
                    float sign=lane.above?1:-1,x=layout.positions[lane.anchor].x;
                    foreach(string child in lane.siblings)
                    {
                        if(child==moving)continue;
                        Color color=child==pending?Color.Lerp(BranchFrameGraphic.Red,White,.52f):Color.Lerp(BranchFrameGraphic.Red,Night,.3f);
                        float dest=layout.positions[child].x,joint=sign*(lane.above?76:103),end=sign*(ChapterMapLayout.BranchY-40);
                        if(!lane.above)Line(new Vector2(x,-47),new Vector2(x,-54),color);
                        Line(new Vector2(x,lane.above?47:-82),new Vector2(x,joint),color);
                        Line(new Vector2(x,joint),new Vector2(dest,joint),color);
                        Line(new Vector2(dest,joint),new Vector2(dest,end),color);
                        Line(new Vector2(dest-3,end-sign*4),new Vector2(dest,end),color);
                        Line(new Vector2(dest+3,end-sign*4),new Vector2(dest,end),color);
                    }
                }
                for(int i=used;i<lines.Count;i++)lines[i].gameObject.SetActive(false);
                foreach(var link in c.levels)
                {
                    var n=nodes[link.id];var p=layout.positions[link.id];bool main=link.main,active=link.id==pending;
                    float size=main?92:70;
                    if(dragging&&link.id!=moving&&!active)Slide(n.rect,n.rect.anchoredPosition,p);
                    else n.rect.anchoredPosition=p;
                    n.rect.sizeDelta=n.face.rectTransform.sizeDelta=Vector2.one*size;
                    n.focus.rectTransform.sizeDelta=Vector2.one*(size+12);n.focus.gameObject.SetActive(!dragging);
                    n.group.alpha=link.id==moving?0:1;n.frame.gameObject.SetActive(!main);
                    if(n.more!=null){n.more.gameObject.SetActive(!dragging&&link.id==selected);((RectTransform)n.more.transform).anchoredPosition=new Vector2(size*.48f,size*.38f);}
                    bool marked=editing?link.id==selected&&!dragging:ui.app.Progress.Done(link.id);
                    n.face.color=active?(main?Gold:Color.Lerp(GameThemes.Current.NodeDone,BranchFrameGraphic.Red,.28f)):
                        marked?GameThemes.Current.NodeDone:n.locked!=null?GameThemes.Current.NodeLocked:GameThemes.Current.NodeOpen;
                    n.number.text=layout.numbers[link.id];n.number.enabled=n.locked==null;n.number.fontSize=main?42:29;
                    n.number.color=marked||active&&!main?GameThemes.Current.NodeDoneInk:GameThemes.Current.NodeInk;
                    n.name.rectTransform.anchoredPosition=new Vector2(0,main?-69:link.branchAbove?55:-54);
                    n.name.fontSize=main?19:16;
                    n.name.color=active&&main?Gold:active||link.id==anchor?Color.Lerp(BranchFrameGraphic.Red,White,.74f):main?White:Quiet;
                    if(n.done!=null)n.done.rectTransform.anchoredPosition=Vector2.one*(size*.33f);
                }
                if(slides.Count>0)slide=ui.StartCoroutine(Interpolate());
            }
            public void Lift(string id,Vector2 point)
            {
                var n=nodes[id];n.group.alpha=.92f;n.rect.anchoredPosition=point;n.rect.SetAsLastSibling();n.face.color=GameThemes.Current.NodeDone;n.number.color=GameThemes.Current.NodeDoneInk;
            }
            public void HintBranchSocket(Vector2? point)
            {
                if(marker==null)return;
                marker.gameObject.SetActive(point.HasValue);
                if(point.HasValue)
                {
                    marker.color=Color.Lerp(BranchFrameGraphic.Red,White,.53f);
                    marker.rectTransform.anchoredPosition=point.Value;
                }
            }
        }
        private void PlayableChapterMap(Chapter chapter)
        {
            MapPage(chapter.title);
            var main=chapter.levels.Where(l=>l.main).ToArray();
            Text("Chapter progress",main.Count(l=>app.Progress.Done(l.id))+" / "+main.Length,new Vector2(505,285),new Vector2(150,40),22,Quiet);
            var map=new ChapterMapView(this,chapter,false,id=>assets.Level(id).title,app.LoadLevel);
            MenuAction("back","返回章节",new Vector2(-463,-293),190,app.ShowChapters,GameIcon.Back);
            string current=chapter.levels.Any(n=>n.id==app.Progress.Data.lastLevel)?app.Progress.Data.lastLevel:chapter.levels[0].id;
            FocusButton("level-"+current);map.Reveal(current);
        }
    }
}
