using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BoxOrVoid.Core;
using TMPro;
using UnityEngine;

namespace BoxOrVoid.Study.LevelEditing
{
    public sealed partial class PlayerLevelEditor
    {
        private sealed class DialogChoice
        {public string Label;public Action Action;public bool Primary;public DialogChoice(string label,Action action,bool primary=false){Label=label;Action=action;Primary=primary;}}
        private RectTransform DialogContent=>(RectTransform)modalRoot.Find("Dialog content");
        private void Dialog(string title,string body,params DialogChoice[] choices)
            => DrawDialog(title, body, 590, choices);

        private void BeginDialog()
        {
            if(EventSystemSelectedInput())UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            EditorUi.Clear(DialogContent);dialogInput=null;dialogCancel=null;modalRoot.gameObject.SetActive(true);toast.text=tooltip.text="";hover.gameObject.SetActive(false);ghost.gameObject.SetActive(false);
        }
        private void DrawDialog(string title, string body, float height, params DialogChoice[] choices)
        {
            BeginDialog();
            float top=height*.5f;
            EditorUi.Image("Dialog border",DialogContent,null,Vector2.zero,new Vector2(620,height),new Color32(100,119,129,255));
            EditorUi.Image("Dialog panel",DialogContent,null,Vector2.zero,new Vector2(610,height-10),new Color32(52,61,65,255));
            EditorUi.Text("Heading",DialogContent,assets,title,new Vector2(0,top-60),new Vector2(520,50),32);
            EditorUi.Text("Description",DialogContent,assets,body,new Vector2(0,top-120),new Vector2(530,62),17,TextAlignmentOptions.Center,EditorUi.Muted);
            AddButton(DialogContent,"dialog-close","×",new Vector2(272,top-42),new Vector2(34,34),CloseDialog);
            float width=choices.Length>2?164:210;
            for(int i=0;i<choices.Length;i++){var choice=choices[i];AddButton(DialogContent,"dialog-choice-"+i,choice.Label,new Vector2((i-(choices.Length-1)*.5f)*(width+16),60-top),new Vector2(width,50),choice.Action,choice.Primary);}
        }
        private static bool EventSystemSelectedInput()=>UnityEngine.EventSystems.EventSystem.current!=null&&UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject!=null&&UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>()!=null;
        private void CloseDialog()
        {
            if(modalRoot==null)return;
            if(EventSystemSelectedInput())UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            modalRoot.gameObject.SetActive(false);dialogInput=null;var cancel=dialogCancel;dialogCancel=null;cancel?.Invoke();
        }
        private void Message(string title,string body)=>Dialog(title,body,new DialogChoice("返回",CloseDialog,true));
        private void RenameDialog()
        {
            Dialog("关卡名称","简洁、容易辨认，也可以稍后修改。",new DialogChoice("取消",CloseDialog),new DialogChoice("保存名称",()=>{string name=dialogInput.text;try{Session.SetName(name);CloseDialog();if(Page==CreatorPage.Library){Save();RefreshLibrary();}}catch(ArgumentException ex){Notice(ex.Message);}},true));
            dialogInput=EditorUi.Input(DialogContent,assets,"Level name",Session.Document.name,new Vector2(0,20),new Vector2(500,58));dialogInput.characterLimit=60;
        }
        private void NewDialog()
        {
            int width=9,height=7;
            Dialog("新建关卡","先选择棋盘大小，之后仍可调整。",new DialogChoice("取消",CloseDialog),new DialogChoice("开始编辑",()=>{var d=LevelDocument.New(width,height);CloseDialog();app.EnterCreatorDocument(d,false);},true));
            var choices=new List<EditorWidget>();int[] widths={7,9,13},heights={7,7,9};
            for(int i=0;i<3;i++){int n=i;var b=AddButton(DialogContent,"new-size-"+i,widths[i]+" × "+heights[i],new Vector2((i-1)*174,45),new Vector2(155,115),()=>{width=widths[n];height=heights[n];for(int j=0;j<choices.Count;j++)choices[j].Select(j==n);});b.Label.fontSize=28;choices.Add(b);b.Select(i==1);}
            AddButton(DialogContent,"templates","从内置关卡创建副本",new Vector2(0,-88),new Vector2(360,48),()=>TemplateDialog(0));
            EditorUi.Text("Note",DialogContent,assets,"内置关卡会保留原样，副本独立保存。",new Vector2(0,-139),new Vector2(520,36),14,TextAlignmentOptions.Center,EditorUi.Muted);
        }
        private void TemplateDialog(int page)
        {
            Dialog("选择关卡副本","只使用前两章已复刻的机制。",new DialogChoice("返回",NewDialog));
            var levels=assets.Data.levels;int pages=(levels.Length+7)/8;page=Mathf.Clamp(page,0,pages-1);
            for(int i=0;i<8&&page*8+i<levels.Length;i++){var source=levels[page*8+i];AddButton(DialogContent,"template-"+source.id,source.ChapterTitle+" / "+source.displayId,new Vector2((i%2-.5f)*268,105-(i/2)*67),new Vector2(248,52),()=>{var d=LevelDocument.New();d.level=assets.LevelData(source.id);d.theme=source.chapter;d.name=source.ChapterTitle+" "+source.displayId+" · 副本";CloseDialog();app.EnterCreatorDocument(d,false);});}
            int current=page;AddButton(DialogContent,"template-prev","上一页",new Vector2(-120,-180),new Vector2(130,34),()=>TemplateDialog(current-1));AddButton(DialogContent,"template-next","下一页",new Vector2(120,-180),new Vector2(130,34),()=>TemplateDialog(current+1));
        }
        private void MoreDialog()
        {
            Dialog("关卡选项",Session.Document.name,new DialogChoice("返回编辑",CloseDialog));
            string[] names={"保存  Ctrl+S","修改名称","棋盘大小","表现主题","导出关卡","返回关卡库"};
            Action[] actions={()=>{if(Save())CloseDialog();},RenameDialog,ResizeDialog,ThemeDialog,ExportDialog,()=>{CloseDialog();LeaveEditing(()=>app.OpenCreatorLibrary());}};
            for(int i=0;i<names.Length;i++)AddButton(DialogContent,"option-"+i,names[i],new Vector2((i%2-.5f)*254,82-i/2*83),new Vector2(230,62),actions[i]);
        }
        private void ThemeDialog()
        {
            Dialog("表现主题","只改变外观，关卡规则与物件保持一致。",new DialogChoice("返回",MoreDialog));
            for(int i=0;i<2;i++){int n=i;var b=AddButton(DialogContent,"theme-"+i,"",new Vector2((i-.5f)*230,15),new Vector2(195,195),()=>{Session.SetTheme(n);CloseDialog();});EditorUi.Image("Theme",b.transform,assets.Sprite(i==0?"Tut World Tile-sc":"Box World Tile"),Vector2.zero,Vector2.one*184);b.Select(Session.Document.theme==i);}
        }
        private void ToolsDialog()
        {
            Dialog("绘制工具","选一种工具，再到棋盘上操作。",new DialogChoice("返回编辑",CloseDialog));
            ToolCard(0,"choose-line","画直线","从起点拖到终点","L",new[]{"100","010","001"},()=>UseTool("line"));
            ToolCard(1,"choose-rectangle","画矩形","选边框或实心，再拖出范围","U",new[]{"111","101","111"},RectangleDialog);
            ToolCard(2,"choose-fill","填充地形","点一片相连地形，预览后填墙","F",new[]{"110","111","011"},()=>UseTool("fill"));
            ToolCard(3,"choose-pick","取样","点棋盘物件，取作当前画笔","I",new[]{"000","010","000"},()=>UseTool("pick"));
            ToolCard(4,"choose-select","框选区域","拖出选区，再复制或移动","M",new[]{"101","000","101"},()=>UseTool("select"));
            ToolCard(5,"repeat","叠箱子","每点一下，在该格加一层箱子","",null,()=>
            {
                CloseDialog();
                Session.Brush=ObjectKind.Box;
                if(Session.Tone==Tone.Gray)Session.Tone=Tone.White;
                Session.Repeat=true;
                Session.Tool=EditTool.Place;
                Refresh();
            });
        }

        private void UseTool(string id)
        {
            CloseDialog();
            Act("tool-"+id);
        }

        private void RectangleDialog()
        {
            DrawDialog("画哪种矩形？","在棋盘上从一角拖到对角，松开完成绘制。",262,
                new DialogChoice("只画边框",()=>{solidRectangle=false;UseTool("rectangle");}),
                new DialogChoice("填满内部",()=>{solidRectangle=true;UseTool("rectangle");},true));
        }

        private void ToolCard(int index,string id,string title,string instruction,string key,string[] cells,Action action)
        {
            var position=new Vector2(index%2==0?-140:140,90-index/2*109);
            var card=AddButton(DialogContent,id,title,position,new Vector2(264,96),action);
            card.Label.gameObject.SetActive(false);
            EditorUi.Text("Tool name",card.transform,assets,title,new Vector2(10,21),new Vector2(150,32),21,TextAlignmentOptions.Left);
            EditorUi.Text("Key",card.transform,assets,key,new Vector2(109,21),new Vector2(24,30),15,TextAlignmentOptions.Center,EditorUi.Muted);
            EditorUi.Text("Gesture",card.transform,assets,instruction,new Vector2(28,-18),new Vector2(188,40),14,TextAlignmentOptions.Left,EditorUi.Muted);
            if(cells==null)
            {
                var box=EditorBoardCanvas.Icon(assets,Session.Document.theme,ObjectKind.Box,Tone.White,Tone.White,true);
                for(int i=0;i<3;i++)EditorUi.Image("Box",card.transform,box,new Vector2(-99+i*5,-8+i*5),Vector2.one*28);
                return;
            }
            for(int y=0;y<3;y++)for(int x=0;x<3;x++)
                EditorUi.Image("Cell",card.transform,null,new Vector2(-108+x*13,13-y*13),Vector2.one*11,
                    cells[y][x]=='1'?EditorUi.Blue:EditorUi.Panel);
        }
        private void HelpDialog()
        {
            BeginDialog();
            var root=DialogContent;
            var paper=new Color32(224,226,221,255);var ink=new Color32(43,50,52,255);
            EditorUi.Plate(root,"Guide cover",Vector2.zero,new Vector2(776,554));
            EditorUi.Image("Book mark",root,null,new Vector2(-349,238),new Vector2(10,38),EditorUi.Blue);
            EditorUi.Text("Heading",root,assets,"操作指南",new Vector2(-214,236),new Vector2(234,48),31,TextAlignmentOptions.Left);
            EditorUi.Quiet(AddButton(root,"dialog-close","×",new Vector2(350,236),new Vector2(40,40),CloseDialog));
            for(int col=0;col<2;col++)
            {
                float x=col==0?-184:184;
                EditorUi.Image("Page",root,null,new Vector2(x,6),new Vector2(364,386),paper);
                EditorUi.Image("Page edge",root,null,new Vector2(x,-188),new Vector2(364,3),new Color32(152,162,164,255));
            }
            void Group(float x,float y,string title)
            {
                EditorUi.Image("Section mark",root,null,new Vector2(x-154,y),Vector2.one*8,EditorUi.Blue);
                EditorUi.Text("Section",root,assets,title,new Vector2(x-46,y),new Vector2(186,30),19,TextAlignmentOptions.Left,ink);
                EditorUi.Image("Rule",root,null,new Vector2(x,y-20),new Vector2(316,1),new Color32(168,180,182,255));
            }
            void Row(float x,float y,string label,string keys)
            {
                float w=Mathf.Max(40,keys.Length*8+20);
                var cap=EditorUi.Image("Keycap",root,null,new Vector2(x+157-w/2,y),new Vector2(w,23),new Color32(193,204,207,255));
                EditorUi.Text("Key",cap.transform,assets,keys,Vector2.zero,new Vector2(w-3,23),14,TextAlignmentOptions.Center,ink);
                EditorUi.Text("Action",root,assets,label,new Vector2(x-60,y),new Vector2(194,26),16,TextAlignmentOptions.Left,ink);
            }
            float l=-184,r=184;
            Group(l,168,"摆放与绘制");Group(r,168,"编辑与试玩");
            Row(l,130,"选择物件","1–6");Row(l,102,"切换物件颜色","Tab");
            Row(l,74,"摆放 / 查看 / 擦除","B / V / E");Row(l,46,"直线 / 矩形 / 填充","L / U / F");Row(l,18,"取作画笔","I");
            Row(r,130,"撤回","Z / Ctrl+Z");Row(r,102,"重做","Y / Ctrl+Y");Row(r,74,"保存关卡","Ctrl+S");
            Row(r,46,"试玩 / 返回修改","F5");Row(r,18,"取消当前操作","Esc");
            Group(l,-24,"框选与整理");Group(r,-24,"查看棋盘");
            Row(l,-62,"框选区域","M");Row(l,-90,"追加选区","Shift + 拖动");Row(l,-118,"复制 / 粘贴","Ctrl+C / Ctrl+V");Row(l,-146,"移动选区","Ctrl+X");
            Row(r,-62,"切换黑白视角","Q");Row(r,-90,"网格 / 适应画面","G / Home");Row(r,-118,"缩放","滚轮");Row(r,-146,"平移","中键 / 空格 + 拖动");
            EditorUi.Text("Mouse note",root,assets,"右键点格子，查看叠在一起的物件。",new Vector2(-167,-229),new Vector2(382,34),15,TextAlignmentOptions.Left,EditorUi.Muted);
            AddButton(root,"dialog-choice-0","返回编辑",new Vector2(276,-229),new Vector2(164,40),CloseDialog,true);
        }
        private void ProblemDialog(List<LevelProblem> problems,int index=0)
        {
            var p=problems[index];var choices=new List<DialogChoice>{new DialogChoice("返回编辑",CloseDialog),new DialogChoice(p.Cell>=0?"查看位置":p.Kind==ObjectKind.Player?"放置角色":"放置目标",()=>{CloseDialog();if(p.Cell>=0){OpenCell(p.Cell);Session.Pan=-board.Position(p.Cell);Refresh();}else{Session.Brush=p.Kind;Session.Tone=p.Tone;Session.Tool=EditTool.Place;stackOpen=false;Refresh();}},true)};
            if(problems.Count>1)choices.Add(new DialogChoice("下个问题",()=>ProblemDialog(problems,(index+1)%problems.Count)));
            Dialog(p.Title.Replace("还少一个","缺少").Replace("有点多","数量过多"),p.Detail,choices.ToArray());
            if(p.Kind!=ObjectKind.Empty)EditorUi.Image("Problem object",DialogContent,EditorBoardCanvas.Icon(assets,Session.Document.theme,p.Kind,p.Tone,Tone.White,true),new Vector2(0,0),Vector2.one*128);
            else{var mini=EditorUi.Rect("Problem preview",DialogContent,new Vector2(0,0),Vector2.one*190).gameObject.AddComponent<EditorBoardCanvas>();mini.Render(assets,Session.Document.level,Session.Document.theme,Session.ViewTone,190f/Math.Max(Session.Document.level.width,Session.Document.level.height));}
            EditorUi.Text("Problem count",DialogContent,assets,(index+1)+" / "+problems.Count+" 项待调整 · 草稿仍可保存",new Vector2(0,-160),new Vector2(540,30),14,TextAlignmentOptions.Center,EditorUi.Muted);
        }
        private void ResizeDialog()
        {
            int width=Session.Document.level.width,height=Session.Document.level.height,anchor=4;
            void Show()
            {
                var preview=Session.PreviewResize(width,height,anchor,out int cropped);
                Dialog("调整棋盘","改变保存范围，不会自动在边缘生成墙。",new DialogChoice("取消",CloseDialog),new DialogChoice("应用调整",()=>
                {
                    void Apply(){Session.Resize(width,height,anchor);stackOpen=false;CloseDialog();Refresh();}
                    if(cropped>0)Dialog("确认裁切？",cropped+" 个物件将被裁掉。调整后可用 Z 撤回。",new DialogChoice("返回预览",Show),new DialogChoice("确认裁切",Apply,true));else Apply();
                },true));
                EditorUi.Text("Width",DialogContent,assets,"宽  "+width,new Vector2(-132,115),new Vector2(100,32),20);
                EditorUi.Text("Height",DialogContent,assets,"高  "+height,new Vector2(132,115),new Vector2(100,32),20);
                AddButton(DialogContent,"width-minus","−",new Vector2(-218,115),Vector2.one*40,()=>{width=Math.Max(3,width-1);Show();});AddButton(DialogContent,"width-plus","+",new Vector2(-45,115),Vector2.one*40,()=>{width=Math.Min(64,width+1);Show();});
                AddButton(DialogContent,"height-minus","−",new Vector2(45,115),Vector2.one*40,()=>{height=Math.Max(3,height-1);Show();});AddButton(DialogContent,"height-plus","+",new Vector2(218,115),Vector2.one*40,()=>{height=Math.Min(64,height+1);Show();});
                EditorUi.Text("Anchor label",DialogContent,assets,"保留位置",new Vector2(-106,25),new Vector2(145,32),15,TextAlignmentOptions.Right,EditorUi.Muted);
                for(int i=0;i<9;i++){int n=i;var b=AddButton(DialogContent,"anchor-"+i,"",new Vector2((i%3-1)*32,54-i/3*32),new Vector2(28,28),()=>{anchor=n;Show();});b.Select(i==anchor);}
                EditorUi.Text("Anchor value",DialogContent,assets,new[]{"左上","上方","右上","左侧","中央","右侧","左下","下方","右下"}[anchor],new Vector2(105,24),new Vector2(150,32),15,TextAlignmentOptions.Left,EditorUi.Muted);
                var before=EditorUi.Rect("Before",DialogContent,new Vector2(-145,-102),Vector2.one*132).gameObject.AddComponent<EditorBoardCanvas>();var after=EditorUi.Rect("After",DialogContent,new Vector2(145,-102),Vector2.one*132).gameObject.AddComponent<EditorBoardCanvas>();
                before.Render(assets,Session.Document.level,Session.Document.theme,Session.ViewTone,122f/Math.Max(Session.Document.level.width,Session.Document.level.height));after.Render(assets,preview,Session.Document.theme,Session.ViewTone,122f/Math.Max(width,height));
                EditorUi.Text("Arrow",DialogContent,assets,"→",new Vector2(0,-102),Vector2.one*40,24);
                EditorUi.Text("Crop summary",DialogContent,assets,cropped>0?cropped+" 个物件位于裁切范围内":"全部物件均保留",new Vector2(0,-190),new Vector2(520,26),14,TextAlignmentOptions.Center,cropped>0?new Color(1,.73f,.49f):EditorUi.Muted);
                int oldW=Session.Document.level.width,oldH=Session.Document.level.height,dx=anchor%3==0?0:anchor%3==2?width-oldW:Mathf.FloorToInt((width-oldW)/2f),row=2-anchor/3,dy=row==0?0:row==2?height-oldH:Mathf.FloorToInt((height-oldH)/2f);
                for(int y=0;y<oldH;y++)for(int x=0;x<oldW;x++)if(x+dx<0||y+dy<0||x+dx>=width||y+dy>=height)EditorUi.Image("Crop",before.transform,null,before.Position(y*oldW+x),Vector2.one*before.CellSize,new Color(1,.52f,.25f,.43f));
            }
            Show();
        }
        private void LibraryOptions()
        {
            if(libraryIndex>=entries.Count)return;var row=entries[libraryIndex];
            Dialog("关卡选项",row.Document.name+(row.HasRecovery?"\n以下操作使用最新自动草稿":""),new DialogChoice("返回",CloseDialog));
            AddButton(DialogContent,"library-duplicate","创建副本",new Vector2(-135,65),new Vector2(246,60),()=>FileAction(()=>{Library.Duplicate(Library.LatestDocument(row));CloseDialog();RefreshLibrary();}));
            AddButton(DialogContent,"library-export","导出关卡",new Vector2(135,65),new Vector2(246,60),()=>FileAction(()=>ExportDocument(Library.LatestDocument(row))));
            AddButton(DialogContent,"library-rename","修改名称",new Vector2(-135,-22),new Vector2(246,60),()=>FileAction(()=>{if(Session!=null)Session.Changed-=Edited;Session=new LevelEditSession(Library.LatestDocument(row));RenameDialog();}));
            AddButton(DialogContent,"library-recycle","移入回收箱",new Vector2(135,-22),new Vector2(246,60),()=>Dialog("移入回收箱？","这份关卡仍可从回收箱恢复。",new DialogChoice("取消",CloseDialog),new DialogChoice("移入回收箱",()=>FileAction(()=>{Library.Recycle(row.Document.id);CloseDialog();RefreshLibrary();}),true)));
        }
        private void ExportDialog()=>ExportDocument(Session.Document);
        private void ExportDocument(LevelDocument d)
        {
            Dialog("导出关卡","分享后，对方可以通过“导入”打开。",new DialogChoice("取消",CloseDialog),new DialogChoice("导出",()=>FileAction(()=>{string path=Library.Export(d,CleanPath(dialogInput.text));Message("已导出",path);}),true));
            EditorUi.Text("Folder label",DialogContent,assets,"保存位置",new Vector2(-180,68),new Vector2(160,30),16,TextAlignmentOptions.Left,EditorUi.Muted);
            dialogInput=EditorUi.Input(DialogContent,assets,"Export folder",Library.ExportFolder,new Vector2(0,20),new Vector2(520,52));
            AddButton(DialogContent,"choose-export-folder","选择文件夹",new Vector2(0,-62),new Vector2(230,44),()=>ChoosePath(true,CleanPath(dialogInput.text)));
            EditorUi.Text("Non overwrite",DialogContent,assets,"已有同名文件时会自动另存一份。",new Vector2(0,-125),new Vector2(530,40),14,TextAlignmentOptions.Center,EditorUi.Muted);
        }
        internal Func<bool,string,Task<string>> PickPath = WindowsLevelPicker.PickAsync;
        internal Func<string> ClipboardText = () => GUIUtility.systemCopyBuffer;
        private bool pickerOpen;
        private string lastImportDirectory;

        private async void ChoosePath(bool folder, string initialDirectory)
        {
            if (pickerOpen) return;
            pickerOpen = true;
            try
            {
                string path = await PickPath(folder, initialDirectory);
                if (!this || !IsOpen || string.IsNullOrEmpty(path)) return;
                if (folder) { if (dialogInput != null) dialogInput.text = path; }
                else OpenImportPath(path);
            }
            catch (Exception)
            {
                if (this && IsOpen && !folder) ImportPathDialog("文件窗口未能打开。请在下方粘贴文件路径。", dialogInput ? dialogInput.text : "");
                else if (this && dialogInput != null) { dialogInput.ActivateInputField(); Notice("请直接粘贴导出文件夹路径"); }
            }
            finally { pickerOpen = false; }
        }

        private static string CleanPath(string value) => (value ?? "").Trim().Trim('"').Trim();
        private void OpenImportPath(string value)
        {
            string path = CleanPath(value);
            if (Directory.Exists(path)) { ChoosePath(false,path); return; }
            if (!File.Exists(path) || !string.Equals(Path.GetExtension(path),".json",StringComparison.OrdinalIgnoreCase))
            { ImportPathDialog("请粘贴一个现有的 JSON 关卡文件路径。",path); return; }
            lastImportDirectory=Path.GetDirectoryName(path);
            try { ImportPreview(path, Library.ReadImport(path)); }
            catch (Exception ex) when (UserLevelLibrary.IsFileFailure(ex))
            {
                ImportPathDialog("无法读取这个关卡。请重新选择文件或修改路径。",path);
            }
        }

        private void ImportPathDialog(string message = "", string value = "")
        {
            BeginDialog();
            var root = DialogContent;
            EditorUi.Plate(root,"Import panel",Vector2.zero,new Vector2(600,350));
            EditorUi.Image("Header mark",root,null,new Vector2(-269,130),new Vector2(7,30),EditorUi.Blue);
            EditorUi.Text("Heading",root,assets,"导入关卡",new Vector2(-138,130),new Vector2(222,44),30,TextAlignmentOptions.Left);
            EditorUi.Quiet(AddButton(root,"dialog-close","×",new Vector2(261,132),new Vector2(38,38),CloseDialog));

            var badge = EditorUi.Image("File tile",root,null,new Vector2(-243,50),new Vector2(58,64),EditorUi.Ink);
            EditorUi.Image("File stripe",badge.transform,null,new Vector2(0,28),new Vector2(58,8),EditorUi.Blue);
            EditorUi.Text("Extension",badge.transform,assets,".json",Vector2.zero,new Vector2(55,34),18,TextAlignmentOptions.Center,EditorUi.Panel);
            EditorUi.Text("File type",root,assets,"JSON 关卡文件",new Vector2(-74,70),new Vector2(240,32),22,TextAlignmentOptions.Left);
            EditorUi.Text("File source",root,assets,"可使用编辑器导出的关卡或布局文件。",new Vector2(-44,38),new Vector2(300,28),15,TextAlignmentOptions.Left,EditorUi.Muted);
            AddButton(root,"import-browse","选择本地文件",new Vector2(197,53),new Vector2(148,46),
                ()=>ChoosePath(false,lastImportDirectory ?? Library.ExportFolder),true);

            EditorUi.Image("Rule",root,null,new Vector2(0,-8),new Vector2(542,1),new Color32(87,103,110,255));
            EditorUi.Text("Path label",root,assets,"或粘贴文件路径",new Vector2(-151,-39),new Vector2(240,28),16,TextAlignmentOptions.Left);
            dialogInput = EditorUi.Input(root,assets,"Import path",value,new Vector2(-63,-79),new Vector2(416,44));
            var path = dialogInput;
            var preview = AddButton(root,"import-preview","预览关卡",new Vector2(215,-79),new Vector2(112,44),()=>OpenImportPath(path.text));
            preview.Enable(!string.IsNullOrWhiteSpace(value));
            path.onValueChanged.AddListener(text=>preview.Enable(!string.IsNullOrWhiteSpace(text)));
            path.onSubmit.AddListener(text=>{if(CanInteract && !path.wasCanceled && !string.IsNullOrWhiteSpace(text)) OpenImportPath(text);});
            EditorUi.Text("Import hint",root,assets,string.IsNullOrEmpty(message)?"先预览棋盘，确认后加入「我的关卡」。":message,
                new Vector2(0,-132),new Vector2(542,40),15,TextAlignmentOptions.Left,
                string.IsNullOrEmpty(message)?EditorUi.Muted:new Color32(239,194,177,255));
        }
        private void ImportPreview(string path, LevelDocument d)
        {
                int issues=LevelValidation.ForPlay(d).Count;
                Dialog("导入预览",d.name,new DialogChoice("重新选择",()=>ImportPathDialog(value:path)),new DialogChoice("加入我的关卡",()=>FileAction(()=>{var added=Library.Import(d);CloseDialog();if(Page==CreatorPage.Editing)LeaveEditing(()=>app.EnterCreatorDocument(added,true));else app.EnterCreatorDocument(added,true);}),true));
                var preview=EditorUi.Rect("Import preview",DialogContent,new Vector2(0,10),Vector2.one*220).gameObject.AddComponent<EditorBoardCanvas>();preview.Render(assets,d.level,d.theme,Tone.White,210f/Math.Max(d.level.width,d.level.height));
                EditorUi.Text("Import note",DialogContent,assets,(issues>0?issues+" 项需要调整，将作为草稿导入":"可以进入试玩")+"\n同名关卡会另存副本，不覆盖已有内容。",new Vector2(0,-150),new Vector2(530,65),16,TextAlignmentOptions.Center,EditorUi.Muted);
        }
        private void FileAction(Action action)
        {try{action();}catch(Exception ex)when(UserLevelLibrary.IsFileFailure(ex)){Message("操作未完成",ex.Message+"\n已有内容保持不变。");}}
    }
}
