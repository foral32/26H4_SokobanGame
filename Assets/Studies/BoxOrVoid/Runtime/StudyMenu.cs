using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BoxOrVoid.Study
{
    public sealed class StudyMenu:MonoBehaviour
    {
        private sealed class Item
        {
            public string id;
            public RectTransform rect;
            public Action activate;
            public TMP_Text label;
            public NavigationNode node;
            public Image icon,panel;
            public float pressedAt=-10;
        }
        [SerializeField] private Canvas sceneCanvas;
        [SerializeField] private RectTransform dynamicRoot;
        [SerializeField] private GameObject titleRoot;
        [SerializeField] private Image titleCursor;
        [SerializeField] private Image[] titlePanels,titleIcons;
        private readonly List<Item> items=new List<Item>();
        private StudyAssets assets;
        private PuzzleApp app;
        private Canvas canvas;
        private Image cursor;
        private TMP_Text detail;
        private int selected;
        private Vector2 lastMouse;
        private Vector2 cursorTarget;
        private CanvasGroup dynamicGroup;
        private float panelOpenedAt=-10;
        public GameObject AuthoredTitle=>titleRoot;
        public void AddCreatorEntry(Image panel,Image icon)
        {
            if(titlePanels.Length!=3)throw new InvalidOperationException("Creator menu entry is already present.");
            titlePanels=new[]{titlePanels[0],panel,titlePanels[1],titlePanels[2]};titleIcons=new[]{titleIcons[0],icon,titleIcons[1],titleIcons[2]};
        }

        public void ConfigureScene(Canvas authoredCanvas,RectTransform generatedRoot,GameObject title,Image pointer,Image[] panels,Image[] icons)
        {sceneCanvas=authoredCanvas;dynamicRoot=generatedRoot;titleRoot=title;titleCursor=pointer;titlePanels=panels;titleIcons=icons;}

        public void Initialize(StudyAssets source,Camera camera,PuzzleApp host)
        {
            assets=source;app=host;
            canvas=sceneCanvas;canvas.worldCamera=camera;dynamicGroup=dynamicRoot.GetComponent<CanvasGroup>();
        }
        public void Hide() {canvas.gameObject.SetActive(false);}
        private void Clear()
        {
            canvas.gameObject.SetActive(true);
            titleRoot.SetActive(false);dynamicGroup.alpha=1;dynamicRoot.localScale=Vector3.one;panelOpenedAt=-10;
            for(int i=dynamicRoot.childCount-1;i>=0;i--) {var child=dynamicRoot.GetChild(i).gameObject;child.SetActive(false);Destroy(child);}
            items.Clear();selected=0;detail=null;cursor=null;
        }
        public void Title()
        {
            Clear();titleRoot.SetActive(true);
            Action[] actions=titlePanels.Length==4?new Action[]{app.ShowChapters,app.OpenCreatorLibrary,app.ShowSettings,app.ExitStudy}:new Action[]{app.ShowChapters,app.ShowSettings,app.ExitStudy};string[] ids=titlePanels.Length==4?new[]{"play","creator","settings","exit"}:new[]{"play","settings","exit"};
            for(int i=0;i<titlePanels.Length;i++) items.Add(new Item{id=ids[i],rect=titlePanels[i].rectTransform,panel=titlePanels[i],icon=titleIcons[i],activate=actions[i]});
            cursor=titleCursor;RefreshSelection();cursor.rectTransform.anchoredPosition=cursorTarget;ShowSaveWarning();
        }
        public void Chapters(int chapter)
        {
            Clear();TopRight(app.Progress.CompletedCount+"/21 已解");
            for(int i=0;i<2;i++)
            {
                int c=i;bool unlocked=app.Progress.ChapterUnlocked(c);
                var sprite=assets.Sprite(c==0?"Tut World Tile-sc":unlocked?"Box World Tile":"Box World Locked");
                Button("chapter"+i,"",sprite,new Vector2((i-.5f)*205,0),new Vector2(195,195),()=>{if(app.Progress.ChapterUnlocked(c)) app.ShowLevels(c);else app.PlayCue("selectionSelectBlocked");});
                if(app.Progress.ChapterCount(c)==(c==0?8:13)) Image("Completed",assets.Sprite("Goal_complete"),new Vector2((i-.5f)*205,79),new Vector2(120,145));
            }
            selected=chapter;detail=Text("Chapter detail","",new Vector2(0,-160),new Vector2(900,42),24,TextAlignmentOptions.Center);
            Footer("方向键 / WASD 选择    空格 / Enter 确认    Esc 返回");FinishSelection(true);
        }
        public void Levels(int chapter,string current)
        {
            Clear();TopRight(app.Progress.ChapterCount(chapter)+"/"+(chapter==0?8:13)+" 已解");
            var nodes=assets.Data.navigation.Where(n=>n.chapter==chapter && (n.isReturn || !string.IsNullOrEmpty(n.level))).ToArray();
            float minX=nodes.Min(n=>n.x),maxX=nodes.Max(n=>n.x),minY=nodes.Min(n=>n.y),maxY=nodes.Max(n=>n.y);
            float step=chapter==0?130:66;
            Vector2 center=new Vector2((minX+maxX)*.5f,(minY+maxY)*.5f);
            // Explicit links retain the original directional graph, including gaps leading to bonus branches.
            foreach(var n in nodes)
            {
                foreach(string neighbor in n.neighbors??Array.Empty<string>())
                {
                    var target=nodes.FirstOrDefault(x=>x.id==neighbor);
                    if(target==null || string.CompareOrdinal(n.id,target.id)>=0) continue;
                    Vector2 a=(new Vector2(n.x,n.y)-center)*step,b=(new Vector2(target.x,target.y)-center)*step;
                    if(Vector2.Distance(a,b)<step*1.1f) continue;
                    Vector2 delta=b-a;var line=Image("Path",null,(a+b)*.5f,new Vector2(delta.magnitude,6));line.color=new Color(.45f,.47f,.49f,.6f);line.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
                }
            }
            foreach(var node in nodes)
            {
                var captured=node;string sprite=node.sprite;
                if(!node.isReturn)
                {
                    var level=assets.Level(node.level);sprite=app.Progress.IsComplete(level.id)?level.completedSprite:app.Progress.IsUnlocked(level.id)?level.unlockedSprite:level.lockedSprite;
                }
                Button(node.id,"",assets.Sprite(sprite),(new Vector2(node.x,node.y)-center)*step,new Vector2(step*.94f,step*.94f),()=>
                {
                    if(captured.isReturn) app.ShowChapters();
                    else if(app.Progress.IsUnlocked(captured.level)) app.LoadLevel(captured.level);
                    else app.PlayCue("selectionSelectBlocked");
                });
                items[items.Count-1].node=node;
                if(node.level==current) selected=items.Count-1;
            }
            detail=Text("Selection detail","",new Vector2(0,-290),new Vector2(900,42),24,TextAlignmentOptions.Center);
            Footer("方向键 / WASD 选择    空格 / Enter 进入    Esc 返回");FinishSelection(true);
        }
        public void Pause()
        {
            Clear();Dim();Text("Pause heading","暂停",new Vector2(0,190),new Vector2(500,60),44,TextAlignmentOptions.Center);
            TextButton("resume","继续",105,()=>app.Resume());TextButton("retry","重置",35,()=>app.RestartFromPause());
            TextButton("levels","返回选关",-35,()=>app.ShowLevels(app.SelectedChapter));TextButton("settings","设置",-105,()=>app.ShowSettings());
            TextButton("home","主菜单",-175,()=>app.ShowTitle());
            Footer("移动：方向键 / WASD    切换：空格    撤回：Z    重置：R    暂停：Esc");FinishSelection(false);OpenPanel();
        }
        public void Settings()
        {
            Clear();Dim();Text("Settings heading","设置",new Vector2(0,190),new Vector2(500,60),44,TextAlignmentOptions.Center);
            TextButton("music","",70,()=>AdjustVolume(.1f));TextButton("effects","",-10,()=>AdjustVolume(.1f));
            TextButton("back","返回",-130,()=>app.CloseSettings());
            Footer("上下选择    左右调整音量    Esc 返回");RefreshVolumeLabels();FinishSelection(false);OpenPanel();
        }
        private void RefreshVolumeLabels()
        {items[0].label.text="音乐     "+Mathf.RoundToInt(app.Progress.Data.musicVolume*100)+"%";items[1].label.text="音效     "+Mathf.RoundToInt(app.Progress.Data.effectsVolume*100)+"%";}
        private void AdjustVolume(float delta)
        {
            if(selected==0) app.Progress.Data.musicVolume=Mathf.Clamp01(app.Progress.Data.musicVolume+delta);
            if(selected==1) app.Progress.Data.effectsVolume=Mathf.Clamp01(app.Progress.Data.effectsVolume+delta);
            app.ApplyVolumes();RefreshVolumeLabels();app.PlayCue("uiNavigate");
        }
        public void Handle(StudyAction action)
        {
            if(items.Count==0 || app.IsTransitioning) return;
            if(action==StudyAction.Confirm) {items[selected].pressedAt=Time.unscaledTime;app.PlayCue("uiSubmit");items[selected].activate();return;}
            if(action==StudyAction.Back) {app.Back();return;}
            if((int)action>=4) return;
            if(app.Page==StudyPage.Settings && (action==StudyAction.Left || action==StudyAction.Right) && selected<2) {AdjustVolume(action==StudyAction.Left?-.1f:.1f);return;}
            int target=selected;
            if(app.Page==StudyPage.Levels)
            {
                string id=items[selected].node.neighbors[(int)action];target=items.FindIndex(i=>i.id==id);
                if(target<0) {app.PlayCue("selectionMoveBlocked");return;}
            }
            else if(app.Page==StudyPage.Title && items.Count==4)
            {
                int row=selected/2,column=selected%2;
                if(action==StudyAction.Left||action==StudyAction.Right)column=1-column;else row=1-row;
                target=row*2+column;
            }
            else
            {
                int delta=(action==StudyAction.Up || action==StudyAction.Left)?-1:1;
                target=(selected+delta+items.Count)%items.Count;
            }
            selected=target;RefreshSelection();app.PlayCue("selectionMove");
        }
        public void PollMouse()
        {
            if(items.Count==0 || !canvas.gameObject.activeSelf || app.TestInputOnly || !Application.isFocused) return;
            Vector2 mouse=Input.mousePosition;bool moved=(mouse-lastMouse).sqrMagnitude>1;lastMouse=mouse;
            if(!moved && !Input.GetMouseButtonDown(0)) return;
            for(int i=0;i<items.Count;i++) if(RectTransformUtility.RectangleContainsScreenPoint(items[i].rect,mouse,canvas.worldCamera))
            { if(selected!=i) {selected=i;RefreshSelection();app.PlayCue("selectionMove");}if(Input.GetMouseButtonDown(0)) Handle(StudyAction.Confirm);return; }
        }
        private void FinishSelection(bool square)
        {
            ShowSaveWarning();
            cursor=Image("Selection cursor",assets.Sprite(square?"Cursor":"Main Menu Pointer"),Vector2.zero,Vector2.one*90);
            RefreshSelection();cursor.rectTransform.anchoredPosition=cursorTarget;
        }
        private void ShowSaveWarning()
        {if(!string.IsNullOrEmpty(app.Progress.Warning)) Text("Save warning",app.Progress.Warning,new Vector2(0,280),new Vector2(1000,70),22,TextAlignmentOptions.Center);}
        private void OpenPanel() {panelOpenedAt=Time.unscaledTime;dynamicGroup.alpha=0;dynamicRoot.localScale=Vector3.one*.98f;}
        private void LateUpdate()
        {
            if(canvas==null || !canvas.gameObject.activeSelf) return;
            float blend=1-Mathf.Exp(-Time.unscaledDeltaTime/0.065f);
            if(cursor!=null) cursor.rectTransform.anchoredPosition=Vector2.Lerp(cursor.rectTransform.anchoredPosition,cursorTarget,blend);
            for(int i=0;i<items.Count;i++)
            {
                var item=items[i];float target=i==selected?1.045f:1;
                if(Time.unscaledTime-item.pressedAt<.1f) target=.94f;
                item.rect.localScale=Vector3.Lerp(item.rect.localScale,Vector3.one*target,blend);
            }
            float t=Mathf.Clamp01((Time.unscaledTime-panelOpenedAt)/.14f);
            dynamicGroup.alpha=t;dynamicRoot.localScale=Vector3.one*Mathf.Lerp(.98f,1,1-Mathf.Pow(1-t,3));
        }
        private void RefreshSelection()
        {
            if(cursor==null || items.Count==0) return;
            var item=items[selected];bool square=app.Page==StudyPage.Chapters || app.Page==StudyPage.Levels;
            cursorTarget=item.rect.anchoredPosition+(square?Vector2.zero:new Vector2(-item.rect.sizeDelta.x*.5f-35,0));
            cursor.rectTransform.sizeDelta=square?item.rect.sizeDelta*1.14f:new Vector2(32,32);
            cursor.rectTransform.localRotation=Quaternion.Euler(0,0,square?0:90);
            bool selectable=app.Page==StudyPage.Chapters?app.Progress.ChapterUnlocked(selected):item.node==null || item.node.isReturn || app.Progress.IsUnlocked(item.node.level);
            cursor.color=square?(selectable?new Color(.36213f,.71698f,.15557f):new Color(.59748f,.09221f,.00939f)):Color.white;
            foreach(var row in items) if(row.label!=null) row.label.color=row==item?Color.white:new Color(.68f,.7f,.72f);
            foreach(var row in items) if(row.panel!=null)
            {
                row.panel.color=row==item?new Color(.76f,.80f,.88f):new Color(.66f,.66f,.66f);
                row.icon.color=row==item?Color.white:new Color(.24f,.25f,.26f);
            }
            if(detail!=null && app.Page==StudyPage.Chapters) detail.text=selected==0?"欢迎":app.Progress.ChapterUnlocked(1)?"箱即是空":"完成「欢迎」后解锁";
            if(detail!=null && app.Page==StudyPage.Levels) detail.text=item.node.isReturn?"返回章节":assets.Level(item.node.level).ChapterTitle+"/"+assets.Level(item.node.level).displayId+(app.Progress.IsComplete(item.node.level)?"  已解":app.Progress.IsUnlocked(item.node.level)?"":"  未解锁");
        }
        private void TextButton(string id,string label,float y,Action action)
        {
            var text=Text(id,label,new Vector2(0,y),new Vector2(400,58),30,TextAlignmentOptions.Center);
            items.Add(new Item{id=id,rect=text.rectTransform,label=text,activate=action});
        }
        private void Button(string id,string label,Sprite sprite,Vector2 position,Vector2 size,Action action)
        {
            Image panel=null;
            if(app.Page==StudyPage.Title)
            {
                panel=Image(id+" circle",assets.Sprite("Main Menu Button Panel"),position,size*1.03f);
            }
            var image=Image(id,sprite,position,size);items.Add(new Item{id=id,rect=panel!=null?panel.rectTransform:image.rectTransform,activate=action,icon=image,panel=panel});
            if(app.Page==StudyPage.Title) image.rectTransform.sizeDelta=size*.62f;
            if(!string.IsNullOrEmpty(label)) Text(id+" label",label,position-new Vector2(0,size.y*.5f+22),new Vector2(220,36),23,TextAlignmentOptions.Center);
        }
        private void Dim() {var image=Image("Dim",null,Vector2.zero,new Vector2(1800,1100));image.color=new Color(.08f,.09f,.10f,.93f);}
        private void Footer(string value) {Text("Controls",value,new Vector2(0,-335),new Vector2(1200,30),18,TextAlignmentOptions.Center);}
        private void TopRight(string value) {Text("Progress",value,new Vector2(450,320),new Vector2(330,45),30,TextAlignmentOptions.TopRight);}
        private Image Image(string name,Sprite sprite,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(dynamicRoot,false);
            var image=go.GetComponent<Image>();image.sprite=sprite;image.preserveAspect=sprite!=null;image.raycastTarget=false;
            image.rectTransform.anchorMin=image.rectTransform.anchorMax=new Vector2(.5f,.5f);image.rectTransform.anchoredPosition=position;image.rectTransform.sizeDelta=size;return image;
        }
        private TMP_Text Text(string name,string value,Vector2 position,Vector2 size,float fontSize,TextAlignmentOptions alignment)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(dynamicRoot,false);
            var text=go.GetComponent<TextMeshProUGUI>();text.font=assets.Font;text.text=value;text.fontSize=fontSize;text.alignment=alignment;text.color=new Color(.8f,.82f,.84f);text.raycastTarget=false;
            text.rectTransform.anchorMin=text.rectTransform.anchorMax=new Vector2(.5f,.5f);text.rectTransform.anchoredPosition=position;text.rectTransform.sizeDelta=size;return text;
        }
    }
}
