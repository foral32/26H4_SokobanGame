using UnityEngine;
using UnityEngine.UI;

namespace BoxOrVoid.Study.LevelEditing
{
    public static class PlayerEditorLayout
    {
        public static void Create(PlayerLevelEditor editor,StudyAssets assets,Camera camera)
        {
            var root=EditorUi.Rect("Level Editor Canvas",editor.transform,Vector2.zero,Vector2.zero);
            var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder=450;
            var scaler=root.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;root.gameObject.AddComponent<GraphicRaycaster>();
            var background=EditorUi.Image("Background",root,null,Vector2.zero,new Vector2(4000,2400),EditorUi.Background);
            for(int i=0;i<12;i++)EditorUi.Image("Outline "+i,background.transform,assets.Sprite("Particle_Box_Uncolored"),new Vector2(((i*317)%1160)-580,((i*139)%620)-310),Vector2.one*(35+i*7),new Color(.5f,.55f,.58f,.045f));
            var stage=EditorUi.Rect("Player UI",root,Vector2.zero,new Vector2(1280,720));
            var edit=EditorUi.Rect("Create",stage,Vector2.zero,new Vector2(1280,720));
            EditorUi.Button(edit,assets,"back","返回",new Vector2(-570,290),Vector2.one*52,"back",true);
            EditorUi.Text("Mode",edit,assets,"关卡创作",new Vector2(-395,294),new Vector2(260,44),30,TMPro.TextAlignmentOptions.Left);
            var title=EditorUi.Button(edit,assets,"rename","新的一关",new Vector2(-400,252),new Vector2(250,34));title.Face.color=Color.clear;title.transform.Find("Border").gameObject.SetActive(false);title.transform.Find("Shadow").gameObject.SetActive(false);title.Label.alignment=TMPro.TextAlignmentOptions.Left;title.Label.color=EditorUi.Muted;
            EditorUi.Text("Save state",edit,assets,"未保存",new Vector2(352,282),new Vector2(130,30),14,TMPro.TextAlignmentOptions.Right,EditorUi.Muted);
            EditorUi.Button(edit,assets,"more","选项",new Vector2(466,290),Vector2.one*52,"dots",true);
            EditorUi.Button(edit,assets,"play","试玩",new Vector2(565,280),Vector2.one*72,"play",true,true);
            EditorUi.Button(edit,assets,"tool-place","摆放",new Vector2(-565,110),Vector2.one*48,"place",true);
            EditorUi.Button(edit,assets,"tool-look","查看",new Vector2(-565,0),Vector2.one*48,"look",true);
            EditorUi.Button(edit,assets,"tool-erase","擦除",new Vector2(-565,-110),Vector2.one*48,"erase",true);
            var viewport=EditorUi.Rect("Board viewport",edit,new Vector2(0,43),new Vector2(1000,406));viewport.gameObject.AddComponent<RectMask2D>();
            var board=EditorUi.Rect("Board",viewport,Vector2.zero,new Vector2(490,382)).gameObject.AddComponent<EditorBoardCanvas>();
            var grid=EditorUi.Rect("Grid",viewport,Vector2.zero,new Vector2(490,382)).gameObject.AddComponent<EditorGridGraphic>();grid.raycastTarget=false;
            var ghost=EditorUi.Rect("Placement preview",viewport,Vector2.zero,new Vector2(54,54)).gameObject.AddComponent<EditorBoardCanvas>();
            var hover=EditorUi.Image("Hover object",viewport,null,Vector2.zero,Vector2.one*54,new Color(1,1,1,.5f));hover.gameObject.SetActive(false);
            var cursor=EditorUi.Rect("Cell cursor",viewport,Vector2.zero,Vector2.one*54).gameObject.AddComponent<EditorCornerGraphic>();cursor.color=EditorUi.Green;cursor.raycastTarget=false;cursor.gameObject.SetActive(false);
            var hit=EditorUi.Image("Board input",viewport,null,Vector2.zero,new Vector2(1000,406),Color.clear);hit.raycastTarget=true;hit.gameObject.AddComponent<EditorBoardPointer>();
            EditorUi.Text("Object name",edit,assets,"白箱子",new Vector2(0,-185),new Vector2(700,30),21);
            EditorUi.Text("Operation hint",edit,assets,"单击放置 · 拖动绘制",new Vector2(0,-214),new Vector2(860,26),14,TMPro.TextAlignmentOptions.Center,EditorUi.Muted);
            var mouse=EditorUi.Rect("Mouse hint",edit,new Vector2(-136,-214),new Vector2(17,22)).gameObject.AddComponent<EditorIconGraphic>();mouse.Icon="mouse";mouse.color=EditorUi.Muted;mouse.raycastTarget=false;
            var palette=EditorUi.Rect("Object palette",edit,new Vector2(0,-275),new Vector2(800,100));
            EditorUi.Text("Color label",palette,assets,"颜色  Tab",new Vector2(-344,16),new Vector2(100,24),13,TMPro.TextAlignmentOptions.Center,EditorUi.Muted);
            EditorUi.Button(palette,assets,"tone-white","白",new Vector2(-364,-16),new Vector2(32,32),shortcut:"Tab");
            EditorUi.Button(palette,assets,"tone-black","黑",new Vector2(-322,-16),new Vector2(32,32),shortcut:"Tab");
            string[] labels={"墙","箱子","角色","箱子目标","角色目标","灰墙"};
            for(int i=0;i<6;i++)
            {
                var b=EditorUi.Button(palette,assets,"brush-"+i,labels[i],new Vector2(-240+i*80,0),new Vector2(66,66));b.Label.gameObject.SetActive(false);
                EditorUi.Image("Object",b.transform,null,Vector2.zero,Vector2.one*57);
                EditorUi.Text("Name",b.transform,assets,labels[i],new Vector2(0,-48),new Vector2(100,24),14);
                EditorUi.Text("Key",b.transform,assets,(i+1).ToString(),new Vector2(0,-68),new Vector2(100,18),11,TMPro.TextAlignmentOptions.Center,EditorUi.Muted);
            }
            EditorUi.Button(palette,assets,"tools","工具",new Vector2(292,0),Vector2.one*44,"tools",true);
            var undo=EditorUi.Button(edit,assets,"undo","撤回 Z",new Vector2(-572,-277),Vector2.one*40,"undo",true,shortcut:"Z / Ctrl+Z");undo.transform.Find("Key").gameObject.SetActive(false);undo.Label.fontSize=14;
            var redo=EditorUi.Button(edit,assets,"redo","重做 Y",new Vector2(-504,-277),Vector2.one*40,"redo",true,shortcut:"Y / Ctrl+Y");redo.transform.Find("Key").gameObject.SetActive(false);redo.Label.fontSize=14;
            EditorUi.Quiet(EditorUi.Button(edit,assets,"help","操作指南 F1",new Vector2(-548,-342),new Vector2(124,25),shortcut:"F1"));
            EditorUi.Quiet(EditorUi.Button(edit,assets,"view","白色视角  Q",new Vector2(522,-273),new Vector2(160,36)));
            EditorUi.Quiet(EditorUi.Button(edit,assets,"grid","网格  G",new Vector2(482,-322),new Vector2(80,28)));
            EditorUi.Quiet(EditorUi.Button(edit,assets,"fit","适配",new Vector2(570,-322),new Vector2(80,28)));
            var stack=EditorUi.Rect("Cell contents",edit,new Vector2(454,-20),new Vector2(280,410));stack.gameObject.SetActive(false);
            var selection=EditorUi.Rect("Selection actions",edit,new Vector2(0,248),new Vector2(700,42));selection.gameObject.SetActive(false);
            var library=EditorUi.Rect("My levels",stage,Vector2.zero,new Vector2(1280,720));
            EditorUi.Text("Heading",library,assets,"我的关卡",new Vector2(0,260),new Vector2(680,86),42);
            EditorUi.Text("Subheading",library,assets,"从一个想法开始",new Vector2(0,209),new Vector2(680,36),18,TMPro.TextAlignmentOptions.Center,EditorUi.Muted);
            EditorUi.Button(library,assets,"home","返回",new Vector2(-570,290),Vector2.one*48,"back",true,shortcut:"Esc");
            EditorUi.Button(library,assets,"import","导入",new Vector2(492,290),Vector2.one*44,"import",true);
            EditorUi.Button(library,assets,"recycle","回收箱",new Vector2(568,290),Vector2.one*44,"undo",true);
            EditorUi.Rect("Cards",library,new Vector2(0,22),new Vector2(1100,220));
            EditorUi.Text("Selected name",library,assets,"创建第一关",new Vector2(0,-160),new Vector2(850,48),28);
            EditorUi.Text("Selected status",library,assets,"",new Vector2(0,-198),new Vector2(900,36),16,TMPro.TextAlignmentOptions.Center,EditorUi.Muted);
            EditorUi.Button(library,assets,"open","继续编辑",new Vector2(-88,-266),new Vector2(180,56),primary:true);
            EditorUi.Button(library,assets,"level-options","关卡选项",new Vector2(113,-266),new Vector2(150,56));
            EditorUi.Button(library,assets,"library-prev","上一页",new Vector2(-510,-190),new Vector2(100,36));
            EditorUi.Button(library,assets,"library-next","下一页",new Vector2(510,-190),new Vector2(100,36));
            EditorUi.Text("Controls",library,assets,"方向键选择    Enter 编辑    N 新建",new Vector2(0,-335),new Vector2(900,30),14,TMPro.TextAlignmentOptions.Center,EditorUi.Muted);
            var play=EditorUi.Rect("Playtest",stage,Vector2.zero,new Vector2(1280,720));
            EditorUi.Text("Play title",play,assets,"试玩",new Vector2(-440,300),new Vector2(320,50),30,TMPro.TextAlignmentOptions.Left);
            EditorUi.Button(play,assets,"return-edit","返回编辑   F5 / Esc",new Vector2(480,296),new Vector2(250,54),primary:true);
            EditorUi.Text("Play controls",play,assets,"方向键 / WASD 移动    空格 切换    Z 撤回    R 重置",new Vector2(0,-334),new Vector2(1000,34),17,TMPro.TextAlignmentOptions.Center,EditorUi.Muted);
            var modal=EditorUi.Rect("Dialog layer",stage,Vector2.zero,new Vector2(1280,720));var shade=EditorUi.Image("Dim",modal,null,Vector2.zero,new Vector2(4000,2400),new Color(.08f,.1f,.11f,.84f));shade.raycastTarget=true;
            EditorUi.Rect("Dialog content",modal,Vector2.zero,new Vector2(620,590));modal.gameObject.SetActive(false);
            var toast=EditorUi.Text("Toast",stage,assets,"",new Vector2(0,223),new Vector2(860,36),17,TMPro.TextAlignmentOptions.Center,EditorUi.Green);
            var tooltip=EditorUi.Text("Tooltip",stage,assets,"",new Vector2(0,-350),new Vector2(740,20),12,TMPro.TextAlignmentOptions.Center,EditorUi.Muted);
            editor.Configure(assets,camera,canvas,background.gameObject,edit,library,play,modal,board,grid,ghost,hover,cursor,stack,selection,toast,tooltip);
            edit.gameObject.SetActive(true);library.gameObject.SetActive(false);play.gameObject.SetActive(false);canvas.gameObject.SetActive(false);
        }
    }
}
