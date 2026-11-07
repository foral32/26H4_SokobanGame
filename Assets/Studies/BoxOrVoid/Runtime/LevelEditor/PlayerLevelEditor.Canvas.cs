using System;
using System.Collections.Generic;
using System.Linq;
using BoxOrVoid.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BoxOrVoid.Study.LevelEditing
{
    public sealed partial class PlayerLevelEditor
    {
        private HashSet<int> selectionBase;
        private void Refresh()
        {
            if(Session==null||Page!=CreatorPage.Editing)return;
            var d=Session.Document.level;
            float size=Mathf.Min(58,850f/d.width,382f/d.height)*Session.Zoom;
            board.Rect.anchoredPosition=Session.Pan+new Vector2(stackOpen?-135:0,0);
            board.Render(assets,d,Session.Document.theme,Session.ViewTone,size);
            grid.rectTransform.anchoredPosition=board.Rect.anchoredPosition;grid.rectTransform.sizeDelta=board.Rect.sizeDelta;grid.Width=d.width;grid.Height=d.height;grid.CellSize=size;grid.Strong=Session.Grid;grid.Highlights.Clear();
            if(Session.HasSelection)foreach(int i in Session.SelectedIndices)grid.Highlights.Add(i);
            grid.Refresh();RefreshStatus();
            string brush=LevelValidation.Name(new CellObjectData{ObjectType=Session.Brush,Color=Session.Tone});
            string mode=Session.Tool==EditTool.Look?"格子内容":Session.Tool==EditTool.Erase?"擦除 · "+brush:Session.Tool==EditTool.Select?"区域选择":Session.Tool==EditTool.Pick?"取物件":brush;
            if(Session.Tool==EditTool.Line)mode="直线 · "+brush;
            if(Session.Tool==EditTool.Rectangle)mode=(solidRectangle?"实心矩形 · ":"矩形边框 · ")+brush;
            if(Session.Tool==EditTool.Fill)mode="填充 · "+brush;
            if(Session.Repeat)mode="叠箱子";
            if(paste!=null)mode=(movePaste?"移动选区":"粘贴选区")+" · "+paste.Width+" × "+paste.Height;
            FindText(editRoot,"Object name").text=mode;
            string hint=paste!=null?"单击放置 · Esc 取消":Session.Tool==EditTool.Look?"单击查看 · 逐层选择":Session.Tool==EditTool.Erase?"单击擦除 · 拖动连续擦除":Session.Tool==EditTool.Line?"从起点拖到终点 · 松开绘制":Session.Tool==EditTool.Rectangle?"从一角拖到对角 · 松开绘制":Session.Tool==EditTool.Fill?"单击预览填充范围":Session.Tool==EditTool.Pick?"单击取用物件 · 查看模式可选被遮挡物件":Session.Tool==EditTool.Select?"拖动框选 · Shift 追加 · Ctrl+C 复制":"单击放置 · 拖动绘制";
            if(Session.Repeat)hint="叠放模式 · 每次单击追加一层 · B 恢复普通摆放";
            if(Session.Tool==EditTool.Place&&Session.Brush==ObjectKind.Player&&d.grid.Any(c=>c.Objects.Any(o=>o.ObjectType==ObjectKind.Player&&o.Color==Session.Tone)))hint="单击移动角色 · 原位置自动清空";
            FindText(editRoot,"Operation hint").text=hint;
            editRoot.Find("Mouse hint").gameObject.SetActive(Session.Tool==EditTool.Place&&!Session.Repeat&&Session.Brush!=ObjectKind.Player&&paste==null);
            foreach(var pair in widgets)if(pair.Key.StartsWith("tool-"))pair.Value.Select(pair.Key=="tool-"+Session.Tool.ToString().ToLowerInvariant());
            widgets["tone-white"].Select(Session.Tone==Tone.White);widgets["tone-black"].Select(Session.Tone==Tone.Black);
            widgets["tone-white"].Face.color=new Color32(224,225,222,255);widgets["tone-white"].Label.color=EditorUi.Panel;
            widgets["tone-black"].Face.color=new Color32(31,33,34,255);
            var kinds=new[]{ObjectKind.Wall,ObjectKind.Box,ObjectKind.Player,ObjectKind.BoxGoal,ObjectKind.PlayerGoal,ObjectKind.Wall};
            for(int i=0;i<6;i++)
            {
                var widget=widgets["brush-"+i];Tone tone=i==5?Tone.Gray:Session.Tone==Tone.Gray?Tone.White:Session.Tone;
                widget.transform.Find("Object").GetComponent<Image>().sprite=EditorBoardCanvas.Icon(assets,Session.Document.theme,kinds[i],tone,Session.ViewTone,true);
                widget.Select(Session.Brush==kinds[i]&&(i==5?Session.Tone==Tone.Gray:Session.Tone!=Tone.Gray));
            }
            widgets["view"].Label.text=(Session.ViewTone==Tone.White?"白色视角":"黑色视角")+"  Q";widgets["grid"].Select(Session.Grid);
            stackRoot.gameObject.SetActive(stackOpen);if(stackOpen)RefreshStack();
            RefreshSelectionTools();UpdateCursor(hoverCell);RefreshGhost();
        }
        private void RefreshStack()
        {
            EditorUi.Clear(stackRoot);int cell=Session.SelectedCell;
            if(cell<0||cell>=Session.Document.level.grid.Length){stackOpen=false;stackRoot.gameObject.SetActive(false);return;}
            var objects=Session.Document.level.grid[cell].Objects;
            int pages=Mathf.Max(1,(objects.Length+3)/4);layerPage=Mathf.Clamp(layerPage,0,pages-1);
            int visible=Mathf.Min(4,objects.Length-layerPage*4);
            float h=visible==0?128:86+visible*46+(pages>1?36:0)+108;
            stackRoot.sizeDelta=new Vector2(282,h);stackRoot.anchoredPosition=new Vector2(454,185-h/2);
            EditorUi.Plate(stackRoot,"Cell stack",Vector2.zero,stackRoot.sizeDelta);
            float t=h/2;
            void Label(Transform parent,string id,string value,Vector2 pos,Vector2 size,float font,Color color,TMPro.TextAlignmentOptions align)
                =>EditorUi.Text(id,parent,assets,value,pos,size,font,align,color);
            var left=TMPro.TextAlignmentOptions.Left;var center=TMPro.TextAlignmentOptions.Center;
            EditorUi.Image("Header mark",stackRoot,null,new Vector2(-119,t-30),new Vector2(5,23),EditorUi.Blue);
            Label(stackRoot,"Heading","格子内容",new Vector2(-34,t-30),new Vector2(146,33),24,EditorUi.Ink,left);
            EditorUi.Quiet(AddButton(stackRoot,"close-stack","×",new Vector2(113,t-29),new Vector2(36,36),()=>Act("close-stack")));
            int width=Session.Document.level.width;
            Label(stackRoot,"Coordinate","坐标  "+(cell%width+1)+" , "+(cell/width+1),new Vector2(-67,t-62),new Vector2(110,22),14,EditorUi.Muted,left);
            Label(stackRoot,"Count",objects.Length+" 层",new Vector2(85,t-62),new Vector2(70,22),14,EditorUi.Muted,TMPro.TextAlignmentOptions.Right);
            if(visible==0)
            {
                Label(stackRoot,"Empty","这里还没有物件",new Vector2(0,t-98),new Vector2(240,30),16,EditorUi.Muted,center);
                return;
            }
            int top=Array.FindLastIndex(objects,o=>o.ObjectType==ObjectKind.Box||o.ObjectType==ObjectKind.Player);
            for(int n=0;n<visible;n++)
            {
                int index=objects.Length-1-layerPage*4-n;var o=objects[index];bool active=Session.SelectedLayer==index;
                var row=AddButton(stackRoot,"layer-"+index,"",new Vector2(0,t-103-n*46),new Vector2(250,42),()=>{Session.SelectedLayer=index;RefreshStack();});
                EditorUi.Quiet(row);row.Title=LevelValidation.Name(o);row.Face.rectTransform.sizeDelta=new Vector2(250,42);
                row.Face.color=active?new Color32(76,94,99,255):new Color32(57,67,70,255);
                if(active)EditorUi.Image("Selected layer",row.transform,null,new Vector2(-123,0),new Vector2(4,42),EditorUi.Green);
                EditorUi.Image("Object",row.transform,EditorBoardCanvas.Icon(assets,Session.Document.theme,o.ObjectType,o.Color,Session.ViewTone,true),new Vector2(-96,0),Vector2.one*30);
                Label(row.transform,"Name",row.Title,new Vector2(-13,0),new Vector2(114,32),17,EditorUi.Ink,left);
                string depth=o.ObjectType==ObjectKind.Wall?"底墙":o.ObjectType==ObjectKind.PlayerGoal||o.ObjectType==ObjectKind.BoxGoal?"目标":index==top?"顶层":"被遮挡";
                Label(row.transform,"Depth",depth,new Vector2(84,0),new Vector2(58,26),13,EditorUi.Muted,TMPro.TextAlignmentOptions.Right);
            }
            if(pages>1)
            {
                float y=t-92-visible*46;
                var prev=AddButton(stackRoot,"layer-prev","上一页",new Vector2(-86,y),new Vector2(76,29),()=>{layerPage--;RefreshStack();});
                var next=AddButton(stackRoot,"layer-next","下一页",new Vector2(86,y),new Vector2(76,29),()=>{layerPage++;RefreshStack();});
                EditorUi.Quiet(prev);prev.Enable(layerPage>0);EditorUi.Quiet(next);next.Enable(layerPage<pages-1);
                Label(stackRoot,"Page",(layerPage+1)+" / "+pages,new Vector2(0,y),new Vector2(56,24),13,EditorUi.Muted,center);
            }
            bool selected=Session.SelectedLayer>=0&&Session.SelectedLayer<objects.Length;
            var movable=Enumerable.Range(0,objects.Length).Where(i=>objects[i].ObjectType==ObjectKind.Box||objects[i].ObjectType==ObjectKind.Player).ToArray();
            int slot=Array.IndexOf(movable,Session.SelectedLayer);
            EditorUi.Image("Action rule",stackRoot,null,new Vector2(0,103-t),new Vector2(248,1),new Color32(88,104,109,255));
            EditorWidget ActionButton(string id,string name,float x,float fromBottom,float w,Action action,bool enabled,bool primary=false)
            {
                var b=AddButton(stackRoot,id,name,new Vector2(x,fromBottom-t),new Vector2(w,34),action,primary);
                b.Label.fontSize=15;b.transform.Find("Shadow").gameObject.SetActive(false);
                if(!primary){b.transform.Find("Border").gameObject.SetActive(false);b.Face.rectTransform.sizeDelta=new Vector2(w,34);}
                b.Enable(enabled);return b;
            }
            ActionButton("lift","上移",-85,77,78,()=>Act("lift"),slot>=0&&slot<movable.Length-1);
            ActionButton("lower","下移",0,77,78,()=>Act("lower"),slot>0);
            var remove=ActionButton("delete","移除",85,77,78,()=>Act("delete"),selected);remove.Label.color=new Color32(239,194,177,255);
            ActionButton("recolor","切换颜色",-65,33,119,()=>Act("recolor"),selected&&objects[Session.SelectedLayer].Color!=Tone.Gray);
            ActionButton("pick-layer","取作画笔",65,33,119,()=>{if(selected)Pick(objects[Session.SelectedLayer]);},selected,true);
        }
        private void RefreshSelectionTools()
        {
            selectionRoot.gameObject.SetActive(Session.HasSelection||paste!=null);if(!selectionRoot.gameObject.activeSelf)return;
            EditorUi.Clear(selectionRoot);string[] ids={"copy","cut","paste","rotate","mirror-x","mirror-y","delete"},names={"复制","移动","粘贴","旋转","水平镜像","垂直镜像","清除"};
            for(int i=0;i<ids.Length;i++){string id=ids[i];AddButton(selectionRoot,id,names[i],new Vector2((i-3)*95,0),new Vector2(88,36),()=>Act(id));}
        }
        private void UpdateCursor(int index)
        {
            cursor.gameObject.SetActive(index>=0||stackOpen);if(index<0&&stackOpen)index=Session.SelectedCell;
            if(index<0||index>=Session.Document.level.grid.Length)return;
            cursor.rectTransform.anchoredPosition=board.Rect.anchoredPosition+board.Position(index);cursor.rectTransform.sizeDelta=Vector2.one*(board.CellSize+4);
        }
        private void RefreshGhost()
        {
            hover.gameObject.SetActive(false);ghost.gameObject.SetActive(false);
            if(ModalOpen||hoverCell<0||stackOpen||panning)return;
            Vector2 pos=board.Rect.anchoredPosition+board.Position(hoverCell);
            if(paste!=null)
            {
                var data=new LevelData{width=paste.Width,height=paste.Height,grid=paste.Cells};ghost.gameObject.SetActive(true);ghost.Render(assets,data,Session.Document.theme,Session.ViewTone,board.CellSize,.55f);ghost.Rect.anchoredPosition=pos+new Vector2((paste.Width-1)*.5f,(paste.Height-1)*.5f)*board.CellSize;return;
            }
            if(Session.Tool==EditTool.Look||Session.Tool==EditTool.Select||Session.Tool==EditTool.Pick)return;
            hover.gameObject.SetActive(true);hover.rectTransform.anchoredPosition=pos;hover.rectTransform.sizeDelta=Vector2.one*board.CellSize;
            hover.sprite=Session.Tool==EditTool.Erase?null:EditorBoardCanvas.Icon(assets,Session.Document.theme,Session.Brush,Session.Tone,Session.ViewTone,true);hover.color=Session.Tool==EditTool.Erase?new Color(.85f,.52f,.35f,.25f):new Color(1,1,1,.48f);
        }
        private int CellAt(PointerEventData e)
        {RectTransformUtility.ScreenPointToLocalPointInRectangle(board.Rect,e.position,viewCamera,out var local);return board.CellAt(local);}
        private Vector2 ViewportPoint(PointerEventData e)
        {RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)board.transform.parent,e.position,viewCamera,out var local);return local;}
        public void PointerDown(PointerEventData e)
        {
            if(!CanInteract||ModalOpen||Page!=CreatorPage.Editing)return;
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);
            if(e.button==PointerEventData.InputButton.Middle||(app.EditorKeyboard?.Held(KeyCode.Space)??false)){panning=true;panOrigin=Session.Pan;pointerOrigin=ViewportPoint(e);return;}
            int cell=CellAt(e);if(cell<0)return;hoverCell=cell;
            if(e.button==PointerEventData.InputButton.Right||Session.Tool==EditTool.Look){OpenCell(cell);return;}
            if(paste!=null)
            {
                if(Session.Paste(paste,cell%Session.Document.level.width,cell/Session.Document.level.width,movePaste)){paste=null;movePaste=cutClipboard=false;}else Notice(Session.LastMessage);Refresh();return;
            }
            if(Session.Tool==EditTool.Pick){var objects=Session.Document.level.grid[cell].Objects;if(objects.Length>0)Pick(objects[objects.Length-1]);return;}
            if(Session.Tool==EditTool.Fill)
            {
                if(Session.Brush!=ObjectKind.Wall){Notice("填充用于绘制墙体，请先选择一种墙");return;}
                var area=Session.FillArea(cell);grid.Highlights.Clear();foreach(int i in area)grid.Highlights.Add(i);grid.Refresh();
                Dialog("填充这片区域？",area.Count+" 个格子 · 保留已有物件",new DialogChoice("取消",CloseDialog),new DialogChoice("填充",()=>{CloseDialog();Session.Begin();foreach(int i in area)Session.Place(i);Session.Commit();Refresh();},true));dialogCancel=Refresh;return;
            }
            drawing=true;startCell=lastCell=cell;strokeCells.Clear();stackOpen=false;
            if(Session.Tool==EditTool.Select){bool add=(app.EditorKeyboard?.Held(KeyCode.LeftShift)??false)||(app.EditorKeyboard?.Held(KeyCode.RightShift)??false);selectionBase=add&&Session.HasSelection?new HashSet<int>(Session.SelectedIndices):null;Session.Select(cell,cell,selectionBase);Refresh();return;}
            Session.Begin();if(Session.Tool==EditTool.Place||Session.Tool==EditTool.Erase)Paint(cell);Refresh();
        }
        private void Paint(int cell)
        {if(strokeCells.Add(cell))Session.Place(cell,Session.Tool==EditTool.Erase);}
        public void PointerMove(PointerEventData e)
        {
            if(!CanInteract||ModalOpen||Page!=CreatorPage.Editing)return;
            if(panning){Session.Pan=panOrigin+ViewportPoint(e)-pointerOrigin;Refresh();return;}
            bool overBoard=RectTransformUtility.RectangleContainsScreenPoint((RectTransform)board.transform.parent,e.position,viewCamera)&&!(stackOpen&&RectTransformUtility.RectangleContainsScreenPoint(stackRoot,e.position,viewCamera));
            int cell=overBoard?CellAt(e):-1;hoverCell=cell;if(cell<0){if(drawing&&(Session.Tool==EditTool.Place||Session.Tool==EditTool.Erase))lastCell=-1;PointerExit();return;}
            if(drawing)
            {
                if(Session.Tool==EditTool.Select)Session.Select(startCell,cell,selectionBase);
                else if(Session.Tool==EditTool.Place||Session.Tool==EditTool.Erase)
                {
                    if(lastCell<0)lastCell=cell;
                    int w=Session.Document.level.width,x0=lastCell%w,y0=lastCell/w,x1=cell%w,y1=cell/w,steps=Math.Max(Math.Abs(x1-x0),Math.Abs(y1-y0));
                    for(int n=0;n<=steps;n++){float t=steps==0?0:(float)n/steps;Paint(Mathf.RoundToInt(y0+(y1-y0)*t)*w+Mathf.RoundToInt(x0+(x1-x0)*t));}
                }
                lastCell=cell;Refresh();
                if(Session.Tool==EditTool.Line||Session.Tool==EditTool.Rectangle)ShapePreview(startCell,cell);
            }
            UpdateCursor(cell);RefreshGhost();
        }
        private void ShapePreview(int a,int b)
        {
            int w=Session.Document.level.width,x0=a%w,y0=a/w,x1=b%w,y1=b/w;grid.Highlights.Clear();
            if(Session.Tool==EditTool.Rectangle){for(int y=Math.Min(y0,y1);y<=Math.Max(y0,y1);y++)for(int x=Math.Min(x0,x1);x<=Math.Max(x0,x1);x++)if(solidRectangle||x==x0||x==x1||y==y0||y==y1)grid.Highlights.Add(y*w+x);}
            else{int steps=Math.Max(Math.Abs(x1-x0),Math.Abs(y1-y0));for(int n=0;n<=steps;n++){float t=steps==0?0:(float)n/steps;grid.Highlights.Add(Mathf.RoundToInt(y0+(y1-y0)*t)*w+Mathf.RoundToInt(x0+(x1-x0)*t));}}
            grid.Refresh();
        }
        public void PointerUp(PointerEventData e){if(!drawing&&!panning)return;panning=false;EndStroke();Refresh();}
        private void EndStroke()
        {
            if(!drawing)return;drawing=false;
            if(Session.Tool==EditTool.Line||Session.Tool==EditTool.Rectangle)Session.Shape(startCell,lastCell,Session.Tool==EditTool.Rectangle,solidRectangle);
            Session.Commit();if(!string.IsNullOrEmpty(Session.LastMessage))Notice(Session.LastMessage);
        }
        private void CancelGesture(){drawing=panning=false;paste=null;movePaste=cutClipboard=false;if(Session!=null)Session.Cancel();if(hover!=null)hover.gameObject.SetActive(false);}
        public void PointerExit(){hoverCell=-1;if(hover!=null)hover.gameObject.SetActive(false);if(ghost!=null)ghost.gameObject.SetActive(false);if(cursor!=null&&!stackOpen)cursor.gameObject.SetActive(false);}
        public void PointerScroll(PointerEventData e)
        {
            if(!CanInteract||ModalOpen||Page!=CreatorPage.Editing)return;
            float before=Session.Zoom;Session.Zoom=Mathf.Clamp(before*(e.scrollDelta.y>0?1.12f:.89f),.5f,5f);
            Vector2 pointer=ViewportPoint(e),shift=new Vector2(stackOpen?-135:0,0);Session.Pan=pointer-shift-(pointer-shift-Session.Pan)*(Session.Zoom/before);Refresh();
        }
        private void OpenCell(int cell)
        {Session.SelectedCell=cell;Session.SelectedLayer=Session.Document.level.grid[cell].Objects.Length-1;layerPage=0;stackOpen=true;Session.Tool=EditTool.Look;Refresh();}
        private void Pick(CellObjectData o){Session.Brush=o.ObjectType;Session.Tone=o.Color;Session.Tool=EditTool.Place;stackOpen=false;Refresh();}
        private EditorWidget AddButton(Transform root,string id,string label,Vector2 position,Vector2 size,Action action,bool primary=false)
        {var b=EditorUi.Button(root,assets,id,label,position,size,primary:primary);b.Bind(this,action);return b;}
        private void RefreshLibrary(bool read=true)
        {
            if(read)try{entries=Library.List(recycled);}catch(Exception ex)when(UserLevelLibrary.IsFileFailure(ex)){entries=new List<LibraryEntry>();Notice(ex.Message);}
            libraryIndex=Mathf.Clamp(libraryIndex,0,entries.Count);libraryPage=Mathf.Clamp(libraryPage,0,Math.Max(0,(entries.Count-1)/3));
            FindText(libraryRoot,"Heading").text=recycled?"回收箱":"我的关卡";
            FindText(libraryRoot,"Subheading").text=recycled?"选中关卡即可恢复":entries.Count>0?entries.Count+" 个关卡":"";
            var cards=libraryRoot.Find("Cards");EditorUi.Clear(cards);
            int start=libraryPage*3,count=Math.Min(3,entries.Count-start);
            for(int n=0;n<count;n++)
            {
                int i=start+n;var row=entries[i];var tile=AddButton(cards,"level-"+i,"",new Vector2((n-count*.5f)*218,0),new Vector2(200,200),()=>{libraryIndex=i;RefreshLibrary(false);});tile.Select(i==libraryIndex);
                var mini=EditorUi.Rect("Preview",tile.transform,Vector2.zero,Vector2.one*160).gameObject.AddComponent<EditorBoardCanvas>();mini.Render(assets,row.Document.level,row.Document.theme,Tone.White,160f/Math.Max(row.Document.level.width,row.Document.level.height));
                if(row.Document.level.grid.All(c=>c.Objects.Length==0))EditorUi.Text("Empty draft",tile.transform,assets,"空白草稿",Vector2.zero,new Vector2(170,40),18,TMPro.TextAlignmentOptions.Center,EditorUi.Muted);
                if(row.Document.IsPlayedThrough)EditorUi.Image("Complete",tile.transform,assets.Sprite("Goal_complete"),new Vector2(65,75),Vector2.one*58);
            }
            if(!recycled){var add=AddButton(cards,"new","＋",new Vector2(count*109,0),new Vector2(200,200),()=>{libraryIndex=entries.Count;NewDialog();});add.Label.fontSize=72;add.Select(libraryIndex==entries.Count);EditorUi.Text("New label",add.transform,assets,"新建关卡",new Vector2(0,-62),new Vector2(190,40),23);}
            bool selected=libraryIndex<entries.Count;var current=selected?entries[libraryIndex]:null;
            FindText(libraryRoot,"Selected name").text=selected?current.Document.name:recycled?"回收箱是空的":"";
            FindText(libraryRoot,"Selected status").text=selected?current.HasRecovery?"有未保存的自动草稿":current.FromBackup?current.Warning:current.Document.IsPlayedThrough?"已玩通":"草稿 · "+current.Document.level.width+" × "+current.Document.level.height:recycled?"移入回收箱的关卡可以在这里恢复":"从空白棋盘或内置关卡副本开始";
            widgets["open"].Label.text=recycled?"恢复关卡":"继续编辑";
            widgets["open"].gameObject.SetActive(selected);
            widgets["open"].Enable(selected);
            FindText(libraryRoot,"Controls").text=recycled?"方向键选择    Enter 恢复":entries.Count==0?"Enter / N 新建    Ctrl+V 粘贴关卡路径":"方向键选择    Enter 打开    N 新建    Ctrl+V 粘贴关卡路径";
            widgets["library-prev"].gameObject.SetActive(entries.Count>3);
            widgets["library-next"].gameObject.SetActive(entries.Count>3);
            widgets["level-options"].gameObject.SetActive(selected&&!recycled);
            widgets["library-prev"].Enable(libraryPage>0);widgets["library-next"].Enable((libraryPage+1)*3<entries.Count);
            if(!string.IsNullOrEmpty(Library.LastWarning))Notice(Library.LastWarning);
        }
        private void SelectLibrary(int delta){libraryIndex=(libraryIndex+delta+entries.Count+1)%(entries.Count+1);if(libraryIndex<entries.Count)libraryPage=libraryIndex/3;RefreshLibrary(false);}
        private void OpenSelected()
        {
            if(libraryIndex>=entries.Count){if(!recycled)NewDialog();return;}
            var row=entries[libraryIndex];
            if(recycled){try{Library.Restore(row.Document.id);RefreshLibrary();Notice("关卡已恢复");}catch(Exception ex)when(UserLevelLibrary.IsFileFailure(ex)){Message("恢复未完成",ex.Message);}return;}
            void Open(bool recovery){try{var d=Library.Load(row.Document.id,recovery);CloseDialog();app.EnterCreatorDocument(d,!recovery);}catch(Exception ex)when(UserLevelLibrary.IsFileFailure(ex)){Message("暂时无法打开",ex.Message);}}
            if(row.HasRecovery)Dialog("发现未保存的修改",row.Document.name,new DialogChoice("恢复草稿",()=>Open(true),true),new DialogChoice("打开已保存版本",()=>Open(false)),new DialogChoice("取消",CloseDialog));else Open(false);
        }
    }
}
