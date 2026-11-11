using DraculasBox.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    public enum EditOperation {Paint,Erase,Select}
    public sealed partial class LevelEditor
    {
        private RectTransform materials,scenePanel,filesPanel;
        private ScrollRect materialScroll;
        private bool paletteCollapsed;
        public EditOperation Operation {get;private set;}
        public bool SceneOpen=>scenePanel!=null&&scenePanel.gameObject.activeSelf;
        private Brush EffectiveBrush=>Operation==EditOperation.Erase?Brush.Erase:Selected;
        private void BuildWorkshop()
        {
            var previousOperation=Operation;
            ui.Clear();ui.WorkshopTheme();brushes.Clear();pendingStamps.Clear();stroke=panning=false;previewQueued=false;
            board.EditorPaletteCollapsed(paletteCollapsed);board.Preview(Document.Data);
            ui.Image("Header rule",null,new Vector2(0,280),new Vector2(1224,1),GameUI.WorkshopLine);
            ui.Image("Tool rule",null,new Vector2(0,228),new Vector2(1224,1),GameUI.WorkshopLine);
            ui.Image("Footer rule",null,new Vector2(0,-293),new Vector2(1224,1),GameUI.WorkshopLine);
            var back=ui.EditorButton("leave-editor",app.EditingOfficial?"关卡图":"关卡库",new Vector2(-537,312),new Vector2(148,42),app.LeaveEditor);back.transform.Find("Label").GetComponent<RectTransform>().anchoredPosition=new Vector2(12,0);var backIcon=ui.Rect("Icon",new Vector2(-50,0),new Vector2(22,22),back.transform).gameObject.AddComponent<GameIconGraphic>();backIcon.icon=GameIcon.Back;backIcon.color=GameUI.WorkshopInk;backIcon.raycastTarget=false;
            titleInput=ui.Input("Level title",Document.Data.title,new Vector2(-162,312),new Vector2(510,42),v=>{Document.Rename(v);RefreshLabels();});
            titleInput.textComponent.fontSize=23;
            titleInput.restoreOriginalTextOnEscape=false;
            titleInput.textViewport.sizeDelta=new Vector2(450,36);
            titleInput.textViewport.anchoredPosition=new Vector2(-20,0);
            titleInput.textComponent.rectTransform.sizeDelta=new Vector2(450,36);
            if(app.EditingOfficial)
            {
                ui.Text("Level prefix",app.Campaign.LevelPrefix(Document.Data.id),new Vector2(-387,312),new Vector2(90,42),22,GameUI.WorkshopInk);
                titleInput.GetComponent<RectTransform>().anchoredPosition=new Vector2(-117,312);
                titleInput.GetComponent<RectTransform>().sizeDelta=new Vector2(420,42);
                titleInput.textViewport.sizeDelta=titleInput.textComponent.rectTransform.sizeDelta=new Vector2(360,36);
            }
            var titleBorder=titleInput.GetComponent<Outline>();
            var editIcon=ui.Rect("Edit icon",new Vector2(app.EditingOfficial?186:231,0),new Vector2(22,22),titleInput.transform).gameObject.AddComponent<GameIconGraphic>();
            editIcon.icon=GameIcon.Edit;editIcon.color=GameUI.Muted;editIcon.raycastTarget=false;
            titleInput.onSelect.AddListener(_=>{titleBorder.effectColor=GameUI.WorkshopAccent;editIcon.color=GameUI.WorkshopAccent;});
            titleInput.onDeselect.AddListener(_=>{titleBorder.effectColor=GameUI.WorkshopLine;editIcon.color=GameUI.Muted;});
            dirtyMark=ui.Text("Unsaved","",new Vector2(112,312),new Vector2(30,36),24,GameUI.WorkshopAccent);
            ui.EditorButton("editor-files","...",new Vector2(221,312),new Vector2(44,42),()=>ToggleFiles());
            ui.EditorButton("save","保存",new Vector2(356,312),new Vector2(156,42),()=>Save());
            ui.EditorButton("test","试玩",new Vector2(520,312),new Vector2(156,42),Play,true);
            var collapse=ui.WorkshopIcon("palette-toggle",GameIcon.Back,new Vector2(-592,254),new Vector2(36,36),()=>TogglePalette());if(paletteCollapsed)collapse.transform.Find("Icon").localRotation=Quaternion.Euler(0,0,180);
            ui.EditorButton("operation-select","选择",new Vector2(-504,254),new Vector2(98,36),()=>SetOperation(EditOperation.Select));
            ui.EditorButton("operation-paint","绘制",new Vector2(-398,254),new Vector2(98,36),()=>SetOperation(EditOperation.Paint));
            ui.EditorButton("brush-7","擦除",new Vector2(-292,254),new Vector2(98,36),()=>SetOperation(EditOperation.Erase));
            ui.Image("Tool separator",null,new Vector2(-230,254),new Vector2(1,24),GameUI.WorkshopLine);
            ui.EditorButton("shape-brush","点涂",new Vector2(-166,254),new Vector2(98,36),()=>SelectShape(PaintShape.Brush));
            ui.EditorButton("shape-rectangle","矩形",new Vector2(-60,254),new Vector2(98,36),()=>SelectShape(PaintShape.Rectangle));
            ui.EditorButton("solutions","关卡解",new Vector2(172,254),new Vector2(110,36),OpenSolutions);
            ui.EditorButton("drafts","生成草稿",new Vector2(312,254),new Vector2(138,36),()=>OpenGenerator());
            ui.EditorButton("scene","场景设置",new Vector2(526,254),new Vector2(136,36),ToggleScene);
            ui.EditorShortcut("save","Ctrl+S");ui.EditorShortcut("test","F5");
            ui.EditorShortcut("operation-select","A");ui.EditorShortcut("operation-paint","Q");ui.EditorShortcut("brush-7","E");
            ui.EditorShortcut("shape-brush","P");ui.EditorShortcut("shape-rectangle","R");
            materials=ui.Rect("Materials",new Vector2(-543,-30),new Vector2(146,512));ui.WorkshopPanel("Material surface",Vector2.zero,new Vector2(146,512),materials);
            string[] labels={"地板","墙体","箱子","玩家","棺材","四向灯","缺口","","压力板","自动门","斜镜 /","斜镜 \\"};string[] icons={"solid","Wall_inactive_gray","Box_active_white","vampire","coffin-lid","lamp","solid","","plate","door","mirror-slash","mirror-backslash"};int row=0;
            var scroll=ui.Rect("Material scroll",new Vector2(0,28),new Vector2(146,448),materials);var hit=scroll.gameObject.AddComponent<Image>();hit.color=Color.clear;materialScroll=scroll.gameObject.AddComponent<ScrollRect>();scroll.gameObject.AddComponent<RectMask2D>();var content=ui.Rect("Material content",Vector2.zero,new Vector2(146,(labels.Length-1)*51),scroll);content.anchorMin=content.anchorMax=new Vector2(.5f,1);content.pivot=new Vector2(.5f,1);content.anchoredPosition=Vector2.zero;materialScroll.viewport=scroll;materialScroll.content=content;materialScroll.horizontal=false;materialScroll.movementType=ScrollRect.MovementType.Clamped;materialScroll.scrollSensitivity=28;
            for(int i=0;i<labels.Length;i++)
            {
                if(i==7){brushes.Add(null);continue;}int tool=i;var position=new Vector2(0,-25-row++*51);
                var button=ui.BrushButton(i,labels[i],icons[i],position,()=>Select((Brush)tool),content);
                // The scroll offsets are measured from the content's top, not its center.
                var item=(RectTransform)button.transform;item.anchorMin=item.anchorMax=new Vector2(.5f,1);item.anchoredPosition=position;brushes.Add(button);
            }
            var bar=ui.Rect("Material scrollbar",new Vector2(69,28),new Vector2(8,448),materials);var scrollbar=bar.gameObject.AddComponent<Scrollbar>();var handle=ui.WorkshopPanel("Scroll handle",Vector2.zero,Vector2.zero,bar);handle.rectTransform.offsetMin=handle.rectTransform.offsetMax=Vector2.zero;handle.color=new Color(GameUI.WorkshopAccent.r,GameUI.WorkshopAccent.g,GameUI.WorkshopAccent.b,.40f);scrollbar.handleRect=handle.rectTransform;scrollbar.targetGraphic=handle;scrollbar.direction=Scrollbar.Direction.BottomToTop;materialScroll.verticalScrollbar=scrollbar;materialScroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            channelButtons.Clear();channelHeading=ui.Text("Connection group","连接组",Vector2.zero,new Vector2(112,24),16,GameUI.WorkshopInk,TextAlignmentOptions.MidlineLeft);channelHeading.rectTransform.SetParent(materials,false);channelHeading.rectTransform.anchoredPosition=new Vector2(0,-210);
            for(int i=0;i<4;i++){int c=i;channelButtons.Add(ui.EditorButton("channel-"+i,(i+1).ToString(),new Vector2(-43+i*29,-237),new Vector2(25,26),()=>SelectChannel(c),false,materials));}
            materials.gameObject.SetActive(!paletteCollapsed);
            ui.WorkshopIcon("edit-undo",GameIcon.Undo,new Vector2(-572,-321),new Vector2(60,38),Undo);
            var redo=ui.WorkshopIcon("edit-redo",GameIcon.Undo,new Vector2(-504,-321),new Vector2(60,38),Redo);redo.transform.Find("Icon").localScale=new Vector3(-1,1,1);
            ui.EditorShortcut("edit-undo","Z");ui.EditorShortcut("edit-redo","Y");
            ui.EditorButton("zoom-out","−",new Vector2(390,-321),new Vector2(38,36),()=>Zoom(.8f));
            ui.EditorButton("zoom-fit","100%",new Vector2(466,-321),new Vector2(100,36),()=>{board.ResetEditorView();RefreshLabels();});
            ui.EditorButton("zoom-in","+",new Vector2(542,-321),new Vector2(38,36),()=>Zoom(1.25f));
            status=ui.Text("Editor status","",new Vector2(-80,-321),new Vector2(640,36),18,GameUI.WorkshopAccent,TextAlignmentOptions.MidlineLeft);
            validation=ui.Text("Validation","",new Vector2(-80,-321),new Vector2(640,36),18,GameUI.WorkshopAccent,TextAlignmentOptions.MidlineLeft);
            BuildScenePanel();BuildFilesPanel();RefreshOperation();SetOperation(previousOperation);SelectChannel(Channel);RefreshLabels();
        }
        private void BuildScenePanel()
        {
            scenePanel=ui.Rect("Scene properties",new Vector2(463,59),new Vector2(262,322));ui.WorkshopPanel("Property surface",Vector2.zero,new Vector2(262,322),scenePanel);ui.WorkshopPanel("Property border",Vector2.zero,new Vector2(262,322),scenePanel,true);
            void Label(string name,string text,Vector2 p)=>ui.Text(name,text,p,new Vector2(30,36),20,GameUI.WorkshopInk).rectTransform.SetParent(scenePanel,false);
            Label("Width label","宽",new Vector2(-97,113));Label("Height label","高",new Vector2(-97,65));
            ui.EditorButton("width-minus","−",new Vector2(-52,113),new Vector2(34,34),()=>Resize(-1,0),false,scenePanel);ui.EditorButton("width-plus","+",new Vector2(98,113),new Vector2(34,34),()=>Resize(1,0),false,scenePanel);
            ui.EditorButton("height-minus","−",new Vector2(-52,65),new Vector2(34,34),()=>Resize(0,-1),false,scenePanel);ui.EditorButton("height-plus","+",new Vector2(98,65),new Vector2(34,34),()=>Resize(0,1),false,scenePanel);
            widthInput=ui.Input("Board width",Document.Data.rows[0].Length.ToString(),new Vector2(23,113),new Vector2(86,34),v=>Dimension(v,true),2);widthInput.transform.SetParent(scenePanel,false);widthInput.contentType=TMP_InputField.ContentType.IntegerNumber;
            heightInput=ui.Input("Board height",Document.Data.rows.Length.ToString(),new Vector2(23,65),new Vector2(86,34),v=>Dimension(v,false),2);heightInput.transform.SetParent(scenePanel,false);heightInput.contentType=TMP_InputField.ContentType.IntegerNumber;
            var line=ui.Image("Property rule",null,new Vector2(0,22),new Vector2(226,1),GameUI.WorkshopLine,scenePanel);
            ui.EditorButton("sun","",new Vector2(0,-16),new Vector2(226,40),()=>{FinishStroke();Document.ConfigureSun(!Document.Data.sunEnabled,Document.Data.sunFrom);RefreshDocument();},false,scenePanel);
            var label=ui.Text("Sun direction heading","日照来源",new Vector2(0,-61),new Vector2(226,28),16,GameUI.Muted,TextAlignmentOptions.MidlineLeft);label.transform.SetParent(scenePanel,false);
            for(int i=0;i<4;i++){int side=i;ui.EditorButton("sun-dir-"+i,new[]{"左","上","右","下"}[i],new Vector2(-87+i*58,-105),new Vector2(48,36),()=>{FinishStroke();Document.ConfigureSun(Document.Data.sunEnabled,(SunSide)side);RefreshDocument();},false,scenePanel);}
            scenePanel.gameObject.SetActive(false);scenePanel.gameObject.AddComponent<WorkshopPopover>();
        }
        private void BuildFilesPanel()
        {
            int count=app.EditingOfficial?2:3;var panelSize=new Vector2(178,count*46+10);
            filesPanel=ui.Rect("Editor files",new Vector2(221,285-panelSize.y*.5f),panelSize);ui.WorkshopPanel("File surface",Vector2.zero,panelSize,filesPanel);ui.WorkshopPanel("File border",Vector2.zero,panelSize,filesPanel,true);
            float y=(count-1)*23;
            if(!app.EditingOfficial){ui.EditorButton("save-copy","另存副本",new Vector2(0,y),new Vector2(154,36),app.SaveEditorCopy,false,filesPanel);y-=46;}
            ui.EditorButton("export-editor","导出",new Vector2(0,y),new Vector2(154,36),app.ExportEditor,false,filesPanel);y-=46;
            ui.EditorButton("editor-settings","设置",new Vector2(0,y),new Vector2(154,36),app.Settings,false,filesPanel);
            filesPanel.gameObject.SetActive(false);filesPanel.gameObject.AddComponent<WorkshopPopover>();
        }
        private void ToggleScene(){bool open=!SceneOpen;ClosePanels();scenePanel.gameObject.SetActive(open);RefreshLabels();}
        private void ToggleFiles(){bool open=!filesPanel.gameObject.activeSelf;ClosePanels();filesPanel.gameObject.SetActive(open);}
        public bool ClosePanels(){ui.CommitText();bool open=SceneOpen||filesPanel!=null&&filesPanel.gameObject.activeSelf;if(scenePanel!=null)scenePanel.gameObject.SetActive(false);if(filesPanel!=null)filesPanel.gameObject.SetActive(false);return open;}
        private void TogglePalette(){CancelRectangle(false);FinishStroke();ClosePanels();paletteCollapsed=!paletteCollapsed;board.EditorPaletteCollapsed(paletteCollapsed);Show();}
        public void SetOperation(EditOperation operation){ClosePanels();CancelSelectionGesture();CancelRectangle(false);FinishStroke();Operation=operation;if(operation!=EditOperation.Select)ClearSelection();RefreshShapeControls();RefreshChannelControls();RefreshOperation();}
        private void RefreshOperation(){ui.WorkshopChoice("operation-select",Operation==EditOperation.Select);ui.WorkshopChoice("operation-paint",Operation==EditOperation.Paint);ui.WorkshopChoice("brush-7",Operation==EditOperation.Erase);for(int i=0;i<brushes.Count;i++)if(brushes[i]!=null)ui.SelectBrush(brushes[i],i==(int)Selected&&Operation==EditOperation.Paint,Operation!=EditOperation.Paint);}
        public void RevealBrush(Brush brush){if(materialScroll==null)return;int row=(int)brush;if(row>7)row--;float top=row*51,bottom=top+51,offset=materialScroll.content.anchoredPosition.y;if(top<offset)offset=top;else if(bottom>offset+448)offset=bottom-448;offset=Mathf.Clamp(offset,0,materialScroll.content.rect.height-448);materialScroll.content.anchoredPosition=new Vector2(0,offset);}
    }
}
