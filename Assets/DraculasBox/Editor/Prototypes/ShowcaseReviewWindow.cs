// Editor-only review canvas. It never creates DraculaApp or edits the game scene/save.
using System;
using System.Collections.Generic;
using System.IO;
using DraculasBox.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace DraculasBox.Editor.Prototypes
{
    public sealed class ShowcaseReviewWindow:EditorWindow
    {
        private ShowcaseReviewCanvas preview;
        private double lastRepaint;
        [MenuItem("Tools/Dracula's Box/Prototypes/作品演示")]
        public static void Open()=>GetWindow<ShowcaseReviewWindow>("作品演示 · 原型");
        private void OnEnable(){minSize=new Vector2(800,480);EditorApplication.update+=Refresh;}
        private void OnDisable(){EditorApplication.update-=Refresh;preview?.Dispose();preview=null;}
        private void Refresh()
        {if(!EditorApplication.isPlayingOrWillChangePlaymode&&EditorApplication.timeSinceStartup-lastRepaint>1.0/30){lastRepaint=EditorApplication.timeSinceStartup;Repaint();}}
        private void OnGUI()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode){EditorGUILayout.HelpBox("退出 Play 后查看原型。",MessageType.Info);return;}
            if(preview==null)preview=new ShowcaseReviewCanvas();
            float scale=Mathf.Min(position.width/1280,position.height/720);
            var area=new Rect((position.width-1280*scale)*.5f,(position.height-720*scale)*.5f,1280*scale,720*scale);
            var point=(Event.current.mousePosition-area.position)/scale;
            preview.Pointer=area.Contains(Event.current.mousePosition)?point:new Vector2(-100,-100);
            double now=EditorApplication.timeSinceStartup;
            if(Event.current.type==EventType.MouseDown&&Event.current.button==0&&area.Contains(Event.current.mousePosition))
            {preview.Click(point,now);Event.current.Use();}
            if(Event.current.type==EventType.KeyDown){preview.Key(Event.current.keyCode,now);Event.current.Use();}
            if(Event.current.type==EventType.Repaint){preview.Render(now);GUI.DrawTexture(area,preview.Target,ScaleMode.StretchToFill,false);}
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class NotebookEdgeGraphic:MaskableGraphic
    {
        private static void Poly(VertexHelper mesh,Color color,params Vector2[] p)
        {int start=mesh.currentVertCount;foreach(var v in p)mesh.AddVert(v,color,Vector2.zero);for(int i=1;i<p.Length-1;i++)mesh.AddTriangle(start,start+i,start+i+1);}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;float l=r.xMin,t=r.yMax,b=r.yMin,rr=r.xMax;
            Poly(vh,new Color(0,0,0,.24f),new Vector2(l+2,b-5),new Vector2(l+2,t-9),new Vector2(l+10,t-3),new Vector2(rr,t-3),new Vector2(rr,b-5));
            Poly(vh,color,new Vector2(l,b+10),new Vector2(l,t-10),new Vector2(l+10,t),new Vector2(rr,t),new Vector2(rr,b),new Vector2(l+10,b));
            var hi=new Color(1,.97f,.88f,.7f);
            Poly(vh,hi,new Vector2(l+10,t),new Vector2(rr,t),new Vector2(rr,t-2),new Vector2(l+10,t-2));
            var shade=new Color(.56f,.37f,.39f,.22f);
            Poly(vh,shade,new Vector2(l+10,b),new Vector2(rr,b),new Vector2(rr,b+3),new Vector2(l+10,b+3));
            // A slim plum strip and short stitch marks give the attached tab its notebook character.
            Poly(vh,new Color(.55f,.21f,.3f,.85f),new Vector2(l+10,b+10),new Vector2(l+10,t-10),new Vector2(l+14,t-10),new Vector2(l+14,b+10));
            for(int i=0;i<7;i++){float y=b+13+i*11;Poly(vh,new Color(.57f,.37f,.35f,.3f),new Vector2(rr-20,y),new Vector2(rr-20,y+4),new Vector2(rr-19,y+4),new Vector2(rr-19,y));}
        }
    }

    public sealed class ShowcaseReviewCanvas:IDisposable
    {
        public const string Output="DraculasBox/Reviews/Showcase-20261111-r2";
        private const string Stills="Assets/DraculasBox/Showcase/Editor/ReferenceStills/";
        [Serializable] private sealed class Page {public string id,title,guide;public string[] images;public float[] crop;}
        [Serializable] private sealed class Content {public Page[] TD,LD;}
        private sealed class Hit {public Rect rect;public Action<double> click;public Graphic graphic;public Color idle;}
        private readonly Scene scene;
        private readonly GameObject root;
        private readonly Camera camera;
        private readonly RectTransform canvasRoot;
        private readonly TMP_FontAsset titleFont,bodyFont;
        private readonly Content content;
        private readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
        private readonly List<Hit> hits=new List<Hit>();
        private readonly GameTheme theme=GameThemes.Resolve("wine");
        private readonly int[] index={0,0};
        private RectTransform entry,overlay,pageRoot;
        private CanvasGroup overlayAlpha,pageAlpha;
        private RawImage frameImage;
        private GameIconGraphic playIcon;
        private GameObject mediaTools;
        private bool open,expanded,playing,closing;
        private int tab,frame;
        private double openedAt=-10,pageAt=-10,frameAt,closeAt;
        private float slideSign=1;
        public Vector2 Pointer {get;set;}=new Vector2(-100,-100);
        public RenderTexture Target {get;}
        private Page Current=>(tab==0?content.TD:content.LD)[index[tab]];
        private int Count=>(tab==0?content.TD:content.LD).Length;
        public ShowcaseReviewCanvas()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Preview requires Edit Mode.");
            var assets=AssetDatabase.LoadAssetAtPath<GameAssets>(GameBuilder.AssetsPath);
            // Own the font atlases as well as the font asset: cloning a dynamic TMP asset
            // would still share its atlas with the production asset.
            titleFont=TMP_FontAsset.CreateFontAsset(assets.font.sourceFontFile);titleFont.hideFlags=HideFlags.HideAndDontSave;
            bodyFont=TMP_FontAsset.CreateFontAsset(assets.bodyFont.sourceFontFile);bodyFont.hideFlags=HideFlags.HideAndDontSave;
            content=JsonUtility.FromJson<Content>(File.ReadAllText("DraculasBox/Prototypes/Showcase/content.json"));
            scene=EditorSceneManager.NewPreviewScene();
            root=new GameObject("Showcase review - isolated"){hideFlags=HideFlags.HideAndDontSave};SceneManager.MoveGameObjectToScene(root,scene);
            Target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32){hideFlags=HideFlags.HideAndDontSave};Target.Create();
            var cameraGo=new GameObject("Review camera");cameraGo.transform.SetParent(root.transform,false);
            camera=cameraGo.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.scene=scene;
            camera.transform.position=new Vector3(0,0,-10);camera.orthographicSize=5;camera.aspect=1280f/720;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=theme.Background;camera.nearClipPlane=.1f;camera.farClipPlane=100;camera.targetTexture=Target;
            var lightGo=new GameObject("Preview light");lightGo.transform.SetParent(root.transform,false);lightGo.AddComponent<Light>().type=LightType.Directional;
            var ui=new GameObject("Native game UI review",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));ui.transform.SetParent(root.transform,false);
            var canvas=ui.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=5;
            canvas.additionalShaderChannels=AdditionalCanvasShaderChannels.TexCoord1|AdditionalCanvasShaderChannels.Normal|AdditionalCanvasShaderChannels.Tangent;
            var scaler=ui.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            canvasRoot=ui.GetComponent<RectTransform>();
            ImageRect("Menu",new Rect(0,0,1280,720),"menu.png",canvasRoot);
            BuildEntry();
        }
        private RectTransform Rect(string name,Rect bounds,Transform parent)
        {
            var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(bounds.x,-bounds.y);r.sizeDelta=bounds.size;return r;
        }
        private TMP_Text Label(string value,Rect bounds,float size,Color ink,Transform parent,bool title=false)
        {
            var text=Rect("Text",bounds,parent).gameObject.AddComponent<TextMeshProUGUI>();text.font=title?titleFont:bodyFont;text.fontSize=size;text.color=ink;text.text=value;text.alignment=TextAlignmentOptions.Center;text.richText=true;text.raycastTarget=false;text.overflowMode=TextOverflowModes.Overflow;return text;
        }
        private Image Fill(string name,Rect bounds,Color ink,Transform parent)
        {var i=Rect(name,bounds,parent).gameObject.AddComponent<Image>();i.color=ink;i.raycastTarget=false;return i;}
        private Image Soft(string name,Rect bounds,Color ink,Transform parent,bool shadow=false)
        {var i=Fill(name,bounds,ink,parent);i.sprite=shadow?SurfaceSprites.SoftCardShadow:SurfaceSprites.SoftCard;i.type=Image.Type.Sliced;return i;}
        private Texture2D Texture(string name)
        {
            if(textures.TryGetValue(name,out var found))return found;
            var t=new Texture2D(2,2,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear};t.LoadImage(File.ReadAllBytes(Stills+name));textures.Add(name,t);return t;
        }
        private RawImage ImageRect(string name,Rect bounds,string file,Transform parent)
        {var i=Rect(name,bounds,parent).gameObject.AddComponent<RawImage>();i.texture=Texture(file);i.raycastTarget=false;return i;}
        private GameIconGraphic Icon(GameIcon icon,Rect bounds,Color ink,Transform parent)
        {var g=Rect(icon.ToString(),bounds,parent).gameObject.AddComponent<GameIconGraphic>();g.icon=icon;g.color=ink;g.raycastTarget=false;return g;}
        private void ClickArea(Rect rect,Action<double> action,Graphic graphic=null)
        {hits.Add(new Hit{rect=rect,click=action,graphic=graphic,idle=graphic==null?Color.white:graphic.color});}
        private void BuildEntry()
        {
            entry=Rect("Notebook tab - anchored to right edge",new Rect(0,0,196,98),canvasRoot);
            entry.pivot=new Vector2(1,.5f);entry.anchoredPosition=new Vector2(1294,-354);
            var paper=Rect("Paper",new Rect(0,0,196,98),entry);paper.anchorMin=paper.anchorMax=new Vector2(0,1);
            paper.gameObject.AddComponent<NotebookEdgeGraphic>().color=theme.ObjectTop;
            Label("点我进行",new Rect(30,16,128,26),17,theme.Ink,entry);
            Label("演示",new Rect(30,40,128,40),29,theme.Ink,entry,true);
        }
        public void Open(double now)
        {
            if(open)return;open=true;closing=false;openedAt=now;BuildOverlay(now);
        }
        private void BuildOverlay(double now)
        {
            if(overlay!=null)Object.DestroyImmediate(overlay.gameObject);hits.Clear();
            overlay=Rect("Guide overlay",new Rect(0,0,1280,720),canvasRoot);overlayAlpha=overlay.gameObject.AddComponent<CanvasGroup>();
            Fill("Game dim",new Rect(0,0,1280,720),new Color(.09f,.06f,.08f,.975f),overlay);
            Label("作品演示",new Rect(430,24,420,46),32,theme.Foreground,overlay,true);
            Label("技术策划求职 Demo",new Rect(430,71,420,24),15,theme.Secondary,overlay);
            var back=Icon(GameIcon.Back,new Rect(54,42,25,25),theme.Secondary,overlay);Label("返回",new Rect(82,38,64,32),20,theme.Secondary,overlay);ClickArea(new Rect(44,29,112,54),Close,back);
            for(int i=0;i<2;i++)
            {
                int chosen=i;float x=tabX(i);var label=Label(i==0?"TD":"LD",new Rect(x-44,105,88,35),24,i==tab?theme.Focus:theme.Secondary,overlay);
                if(i==tab)Fill("Tab focus",new Rect(x-17,142,34,3),theme.Focus,overlay);
                ClickArea(new Rect(x-50,99,100,48),t=>SelectTab(chosen,t),label);
            }
            BuildPage(now,false);
        }
        private float tabX(int i)=>i==0?582:698;
        public void SelectTab(int chosen,double now)
        {if(!open||closing||chosen==tab)return;tab=chosen;slideSign=chosen==0?-1:1;BuildOverlay(now);openedAt=now-1;pageAt=now;}
        public void Turn(int next,double now)
        {if(!open||closing||expanded||next<0||next>=Count||next==index[tab])return;slideSign=next>index[tab]?1:-1;index[tab]=next;BuildOverlay(now);openedAt=now-1;pageAt=now;}
        private void BuildPage(double now,bool animate)
        {
            frame=0;playing=false;frameAt=now;pageAt=animate?now:now-1;
            pageRoot=Rect("Guide page",new Rect(0,0,1280,720),overlay);pageAlpha=pageRoot.gameObject.AddComponent<CanvasGroup>();
            Soft("Image soft shadow",new Rect(258,159,764,430),new Color(0,0,0,.35f),pageRoot,true);
            Fill("Image rim",new Rect(270,163,740,418),new Color(theme.Border.r,theme.Border.g,theme.Border.b,.38f),pageRoot);
            frameImage=ImageRect("Demonstration still",new Rect(272,165,736,414),Current.images[0],pageRoot);UpdateFrame();
            // Transport only appears over the image while hovered; no persistent player toolbar.
            mediaTools=Rect("Media hover controls",new Rect(922,533,78,38),pageRoot).gameObject;
            Soft("Media tool scrim",new Rect(0,0,78,38),new Color(.1f,.07f,.09f,.82f),mediaTools.transform);
            playIcon=Icon(GameIcon.Play,new Rect(10,9,19,19),theme.Foreground,mediaTools.transform);
            Label("⤢",new Rect(41,0,31,38),25,theme.Foreground,mediaTools.transform);
            ClickArea(new Rect(922,533,38,38),TogglePlayback);ClickArea(new Rect(962,533,38,38),Expand);
            float start=640-(Count-1)*12;
            for(int i=0;i<Count;i++)
            {
                int page=i;float size=i==index[tab]?9:6;
                var dot=Rect("Page dot",new Rect(start+i*24-size*.5f,607-size*.5f,size,size),pageRoot).gameObject.AddComponent<MenuTileGraphic>();dot.round=true;dot.color=i==index[tab]?theme.Focus:new Color(theme.Secondary.r,theme.Secondary.g,theme.Secondary.b,.33f);dot.raycastTarget=false;
                ClickArea(new Rect(start+i*24-11,591,22,32),t=>Turn(page,t));
            }
            var left=Icon(GameIcon.Back,new Rect(start-61,596,22,22),index[tab]>0?theme.Secondary:new Color(theme.Secondary.r,theme.Secondary.g,theme.Secondary.b,.2f),pageRoot);
            var right=Icon(GameIcon.Arrow,new Rect(start+Count*24+15,596,22,22),index[tab]<Count-1?theme.Secondary:new Color(theme.Secondary.r,theme.Secondary.g,theme.Secondary.b,.2f),pageRoot);
            ClickArea(new Rect(start-73,583,48,48),t=>Turn(index[tab]-1,t),left);ClickArea(new Rect(start+Count*24+3,583,48,48),t=>Turn(index[tab]+1,t),right);
            Label(Current.guide,new Rect(174,634,932,70),22,theme.Foreground,pageRoot);
        }
        private void UpdateFrame()
        {
            frameImage.texture=Texture(Current.images[frame]);frameImage.uvRect=new Rect(0,0,1,1);
            if(Current.crop!=null&&Current.crop.Length==4)
            {var c=Current.crop;var t=frameImage.texture;frameImage.uvRect=new Rect(c[0]/t.width,1-(c[1]+c[3])/t.height,c[2]/t.width,c[3]/t.height);}
        }
        public void TogglePlayback(double now){if(!open||closing||expanded)return;playing=!playing;frameAt=now;if(playing&&Current.images.Length>1){frame=(frame+1)%Current.images.Length;UpdateFrame();}}
        public void Expand(double now)
        {
            if(!open||closing)return;expanded=!expanded;playing=false;
            if(!expanded){BuildOverlay(now);openedAt=now-1;return;}
            Object.DestroyImmediate(pageRoot.gameObject);hits.Clear();
            pageRoot=Rect("Expanded still",new Rect(0,0,1280,720),overlay);pageAlpha=pageRoot.gameObject.AddComponent<CanvasGroup>();
            Fill("Expanded background",new Rect(0,0,1280,720),new Color(.09f,.06f,.08f,1),pageRoot);
            frameImage=ImageRect("Expanded image",new Rect(80,48,1120,630),Current.images[frame],pageRoot);UpdateFrame();
            var back=Icon(GameIcon.Back,new Rect(23,18,23,23),theme.Foreground,pageRoot);ClickArea(new Rect(10,7,60,48),Expand,back);pageAt=now-1;
        }
        public void Close(double now)
        {if(expanded){Expand(now);return;}if(!open||closing)return;closing=true;closeAt=now;playing=false;}
        public void Click(Vector2 point,double now)
        {
            if(!open){if(new Rect(1088,293,192,122).Contains(point))Open(now);return;}
            if(closing)return;
            for(int i=hits.Count-1;i>=0;i--)if(hits[i].rect.Contains(point)){hits[i].click(now);return;}
        }
        public void Key(KeyCode key,double now)
        {
            if(key==KeyCode.Escape){Close(now);return;}
            if(!open){if(key==KeyCode.Return||key==KeyCode.Space)Open(now);return;}
            if(key==KeyCode.LeftArrow)Turn(index[tab]-1,now);else if(key==KeyCode.RightArrow)Turn(index[tab]+1,now);
            else if(key==KeyCode.Tab&&!expanded)SelectTab(1-tab,now);else if(key==KeyCode.Space)TogglePlayback(now);
        }
        public void Render(double now)
        {
            bool hover=!open&&new Rect(1088,293,192,122).Contains(Pointer);
            entry.localEulerAngles=new Vector3(0,0,hover?1.5f:3+Mathf.Sin((float)now*1.8f)*.8f);
            entry.anchoredPosition=new Vector2(hover?1282:1294,-354);
            if(open)
            {
                float p=closing?1-Mathf.Clamp01((float)(now-closeAt)/.14f):Mathf.Clamp01((float)(now-openedAt)/.23f);
                overlayAlpha.alpha=1-Mathf.Pow(1-p,3);
                if(closing&&p<=0){Object.DestroyImmediate(overlay.gameObject);overlay=null;open=closing=expanded=false;hits.Clear();}
                else
                {
                    float q=Mathf.Clamp01((float)(now-pageAt)/.15f);pageAlpha.alpha=1-Mathf.Pow(1-q,3);pageRoot.anchoredPosition=new Vector2(slideSign*10*Mathf.Pow(1-q,3),0);
                    if(!expanded)
                    {
                        mediaTools.SetActive(new Rect(272,165,736,414).Contains(Pointer));playIcon.icon=playing?GameIcon.Pause:GameIcon.Play;
                        if(playing&&now-frameAt>=2.8&&Current.images.Length>1){frameAt=now;frame=(frame+1)%Current.images.Length;UpdateFrame();}
                    }
                }
            }
            foreach(var h in hits)if(h.graphic!=null)h.graphic.color=h.rect.Contains(Pointer)?theme.Focus:h.idle;
            Canvas.ForceUpdateCanvases();
            foreach(var t in root.GetComponentsInChildren<TMP_Text>())t.ForceMeshUpdate(true);
            foreach(var g in root.GetComponentsInChildren<Graphic>()){g.SetAllDirty();g.Rebuild(CanvasUpdate.PreRender);}
            Canvas.ForceUpdateCanvases();camera.Render();
        }
        public void Save(string path,double now)
        {
            Render(now);var previous=RenderTexture.active;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try{RenderTexture.active=Target;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally{RenderTexture.active=previous;Object.DestroyImmediate(image);}
        }
        public static string Export()
        {
            var active=SceneManager.GetActiveScene();bool dirty=active.isDirty;int count=SceneManager.sceneCount;Directory.CreateDirectory(Output);
            using(var p=new ShowcaseReviewCanvas())
            {
                p.Save(Output+"/01-edge-entry.png",0);p.Pointer=new Vector2(1160,345);p.Save(Output+"/02-edge-hover.png",1);p.Pointer=new Vector2(-1,-1);p.Open(1);
                for(int t=0;t<2;t++){p.SelectTab(t,10+t*20);for(int i=0;i<p.Count;i++){p.Turn(i,11+t*20+i);p.Save(Output+"/"+(t==0?"TD-":"LD-")+(i+1).ToString("00")+".png",12+t*20+i);}}
                p.SelectTab(0,51);p.Turn(2,52);p.SelectTab(1,53);p.SelectTab(0,54);
                if(p.index[0]!=2)throw new InvalidOperationException("Tab position was lost.");
                p.Expand(55);p.Key(KeyCode.Escape,56);
                if(!p.open||p.expanded)throw new InvalidOperationException("Expanded media did not return to the guide.");
                p.Close(57);p.Render(58);
                if(p.open)throw new InvalidOperationException("Guide did not close.");
                p.Click(new Vector2(1160,350),59);
                if(!p.open||p.index[0]!=2)throw new InvalidOperationException("Edge tab did not reopen the retained page.");
            }
            if(active!=SceneManager.GetActiveScene()||active.isDirty!=dirty||count!=SceneManager.sceneCount)throw new InvalidOperationException("Scene changed during isolated preview.");
            File.WriteAllText(Output+"/render.txt","Native Unity Canvas + TextMeshPro + project GameIconGraphic and MenuTileGraphic. Edge-tab click, page memory, expansion and return checked. Play not started. Active scene and dirty state preserved.");return Path.GetFullPath(Output);
        }
        public static string ExportMotion()
        {
            string folder=Output+"/motion-frames";Directory.CreateDirectory(folder);
            using(var p=new ShowcaseReviewCanvas())
            {
                for(int i=0;i<108;i++)
                {
                    double t=i/12.0;
                    if(i==21)p.Pointer=new Vector2(1160,350);
                    if(i==30){p.Click(p.Pointer,t);p.Pointer=new Vector2(-1,-1);}
                    if(i==57)p.Key(KeyCode.RightArrow,t);
                    if(i==72)p.Key(KeyCode.Tab,t);
                    if(i==99)p.Key(KeyCode.Escape,t);
                    p.Save(folder+"/"+i.ToString("0000")+".png",t);
                }
            }
            return Path.GetFullPath(folder);
        }
        public void Dispose()
        {
            if(camera!=null)camera.targetTexture=null;
            if(Target!=null){Target.Release();Object.DestroyImmediate(Target);}
            if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);
            foreach(var t in textures.Values)Object.DestroyImmediate(t);Object.DestroyImmediate(titleFont);Object.DestroyImmediate(bodyFont);
        }
    }
}
