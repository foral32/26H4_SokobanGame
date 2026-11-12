using System;
using UnityEngine;
#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;
#endif

namespace DraculasBox.Runtime
{
    public sealed partial class GameUI
    {
#if UNITY_EDITOR
        [SerializeField, Min(0f)] private float showcaseShakeIntervalSeconds=4f;
#endif
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void ShowcaseEntry(Transform parent)
        {
#if UNITY_EDITOR
            var old=parent.Find("showcase");if(old!=null){old.gameObject.SetActive(false);if(Application.isPlaying)Destroy(old.gameObject);else DestroyImmediate(old.gameObject);}
            var r=Rect("showcase",Vector2.zero,new Vector2(228,80),parent);
            r.gameObject.tag="EditorOnly";
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(1,.5f);r.anchoredPosition=new Vector2(10,28);
            var button=ClickTarget(r);
            var artwork=Rect("Notebook tab",Vector2.zero,r.sizeDelta,r);
            artwork.pivot=new Vector2(1,.5f);artwork.anchoredPosition=new Vector2(r.sizeDelta.x*.5f,0);
            var graphic=artwork.gameObject.AddComponent<ShowcaseSurface>();graphic.tab=true;graphic.raycastTarget=false;
            Icon("Play",GameIcon.Play,new Vector2(-79,0),20,GameThemes.Current.TitleFace,artwork);
            Label(Rect("Label",new Vector2(22,0),new Vector2(163,52),artwork),"作品演示！",24,GameThemes.Current.TitleFace);
            var focus=Rect("Focus",Vector2.zero,new Vector2(218,72),r).gameObject.AddComponent<SelectionCorners>();focus.color=new Color(Gold.r,Gold.g,Gold.b,0);focus.raycastTarget=false;
            artwork.localRotation=Quaternion.Euler(0,0,2);
            if(Application.isPlaying)
            {
                Register("showcase",button,OpenShowcase,focus);
                var motion=r.gameObject.AddComponent<ShowcaseTabMotion>();motion.artwork=artwork;motion.shakeInterval=showcaseShakeIntervalSeconds;
                motion.viewed=()=>showcaseVisited||(!app.Automated&&UnityEditor.EditorPrefs.GetBool(ShowcaseSeenKey,false));
            }
#endif
        }
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void CloseShowcaseImmediate()
        {
#if UNITY_EDITOR
            if(showcaseRoot!=null){showcaseRoot.gameObject.SetActive(false);Destroy(showcaseRoot.gameObject);}
            RestoreShowcaseNavigation();
            StopShowcaseVideo();showcaseRoot=null;showcasePanel=null;showcaseClosing=false;
#endif
        }
#if UNITY_EDITOR
        private void OnDestroy(){RestoreShowcaseNavigation();StopShowcaseVideo();}
        [Serializable] public sealed class ShowcasePage {public string id,title,guide,clip;public string[] highlights;}
        [Serializable] private sealed class ShowcaseSection {public string id,label;public ShowcasePage[] pages;}
        [Serializable] private sealed class ShowcaseContent {public ShowcaseSection[] sections;}
        private ShowcaseContent showcaseContent;
        private readonly int[] showcaseIndices={0,0,0,0};
        private int showcaseTab;
        private bool showcaseVisited;
        private static string ShowcaseSeenKey=>"DraculasBox.ShowcaseViewed."+Application.dataPath;
        private RectTransform showcaseRoot,showcasePanel,showcaseMedia,showcaseArticle,showcaseOverview;
        private TMP_Text showcaseHeading,showcaseGuide,showcaseStatus,showcaseOverviewHeading,showcaseOverviewGuide;
        private Button showcasePrevious,showcaseNext;
        private TMP_Text[] showcaseTabs;
        private Image[] showcaseTabLines;
        private Transform showcaseDots;
        private RawImage showcaseImage;
        private VideoPlayer showcasePlayer;
        private RenderTexture showcaseTarget;
        private CanvasGroup showcaseTools;
        private GameIconGraphic showcasePlayIcon;
        private bool showcaseClosing,showcasePaused;
        private int showcaseRequest;
        private UnityEngine.EventSystems.EventSystem showcaseEvents;
        private bool showcaseNavigation;
        private void RestoreShowcaseNavigation()
        {if(showcaseEvents!=null)showcaseEvents.sendNavigationEvents=showcaseNavigation;showcaseEvents=null;}
        public bool ShowcaseOpen=>showcaseRoot!=null;
        public string ShowcasePageId=>showcaseContent==null?null:ShowcaseCurrent.id;
        public bool ShowcaseVideoReady=>showcasePlayer!=null&&showcasePlayer.isPrepared;
        private const string ShowcaseFolder="Assets/DraculasBox/Showcase/Editor/";
        private ShowcasePage[] ShowcasePages=>showcaseContent.sections[showcaseTab].pages;
        private ShowcasePage ShowcaseCurrent=>ShowcasePages[showcaseIndices[showcaseTab]];
        private string ShowcaseClip=>string.IsNullOrEmpty(ShowcaseCurrent.clip)?ShowcaseCurrent.id:ShowcaseCurrent.clip;
        public void PrepareEditorTitleEntries()
        {
            if(Application.isPlaying||authoredTitle==null)return;
            CampaignTitleEntry(authoredTitle.transform);ShowcaseEntry(authoredTitle.transform);
        }
        private TMP_Text ShowcaseText(Transform parent,string name,string value,Vector2 position,Vector2 size,float fontSize,Color color,TextAlignmentOptions align=TextAlignmentOptions.Center)
            =>Label(Rect(name,position,size,parent),value,fontSize,color,align);
        private Button ShowcaseButton(string name,Transform parent,Vector2 position,Vector2 size,Action action)
        {
            var r=Rect(name,position,size,parent);var b=ClickTarget(r);b.gameObject.AddComponent<JuicyButton>();
            b.onClick.AddListener(()=>{if(showcaseClosing)return;app.Cue("uiSubmit");action();});return b;
        }
        public void OpenShowcase()
        {
            if(app.Page!=GamePage.Title||app.Transitioning||ShowcaseOpen)return;
            try{showcaseContent=JsonUtility.FromJson<ShowcaseContent>(File.ReadAllText(ShowcaseFolder+"Pages.json"));}
            catch(Exception e){Debug.LogError("Cannot open showcase: "+e.Message);return;}
            if(showcaseContent?.sections==null||showcaseContent.sections.Length!=4){Debug.LogError("Showcase requires four sections.");return;}
            for(int i=0;i<4;i++)
            {
                if(showcaseContent.sections[i]?.pages==null||showcaseContent.sections[i].pages.Length==0){Debug.LogError("Showcase section is empty: "+i);return;}
                showcaseIndices[i]=Mathf.Clamp(showcaseIndices[i],0,showcaseContent.sections[i].pages.Length-1);
            }
            showcaseVisited=true;
            if(!app.Automated)UnityEditor.EditorPrefs.SetBool(ShowcaseSeenKey,true);
            showcaseClosing=false;
            showcaseEvents=UnityEngine.EventSystems.EventSystem.current;
            if(showcaseEvents!=null){showcaseNavigation=showcaseEvents.sendNavigationEvents;showcaseEvents.sendNavigationEvents=false;showcaseEvents.SetSelectedGameObject(null);}
            // A sibling of both authored title and dynamic pages blocks all menu raycasts.
            showcaseRoot=Rect("Showcase modal",Vector2.zero,Vector2.zero,content.parent);
            showcaseRoot.anchorMin=Vector2.zero;showcaseRoot.anchorMax=Vector2.one;showcaseRoot.offsetMin=showcaseRoot.offsetMax=Vector2.zero;
            var shade=showcaseRoot.gameObject.AddComponent<Image>();shade.color=new Color(0,0,0,.44f);shade.raycastTarget=true;
            showcasePanel=Rect("Showcase window",Vector2.zero,new Vector2(1072,618),showcaseRoot);
            var shadow=Image("Soft shadow",null,new Vector2(0,-8),new Vector2(1106,648),new Color(0,0,0,.4f),showcasePanel);shadow.sprite=SurfaceSprites.SoftCardShadow;shadow.type=UnityEngine.UI.Image.Type.Sliced;
            var frame=Rect("Cut corner frame",Vector2.zero,showcasePanel.sizeDelta,showcasePanel).gameObject.AddComponent<ShowcaseSurface>();frame.raycastTarget=true;
            ShowcaseText(showcasePanel,"Title","作品演示",new Vector2(0,307),new Vector2(210,44),29,White);
            var close=ShowcaseButton("showcase-close",showcasePanel,new Vector2(492,266),Vector2.one*46,CloseShowcase);
            ShowcaseText(close.transform,"Label","×",Vector2.zero,Vector2.one*46,32,White);
            showcaseTabs=new TMP_Text[4];showcaseTabLines=new Image[4];
            for(int i=0;i<4;i++)
            {
                int t=i;var tab=ShowcaseButton("showcase-tab-"+i,showcasePanel,new Vector2(-357+i*231,201),new Vector2(200,48),()=>ShowcaseSelectTab(t));
                showcaseTabs[i]=ShowcaseText(tab.transform,"Label",showcaseContent.sections[i].label,new Vector2(0,4),new Vector2(192,40),23,Quiet);
                showcaseTabLines[i]=Image("Underline",null,new Vector2(0,-23),new Vector2(48,3),Gold,tab.transform);
            }
            Image("Header rule",null,new Vector2(0,172),new Vector2(980,1),new Color(Quiet.r,Quiet.g,Quiet.b,.12f),showcasePanel);
            showcaseMedia=Rect("Video area",new Vector2(-192,-20),new Vector2(584,328.5f),showcasePanel);
            var well=showcaseMedia.gameObject.AddComponent<Image>();well.color=GameThemes.Current.Background;well.raycastTarget=true;
            showcaseImage=Rect("Recorded footage",Vector2.zero,showcaseMedia.sizeDelta,showcaseMedia).gameObject.AddComponent<RawImage>();showcaseImage.raycastTarget=false;
            showcaseStatus=ShowcaseText(showcaseMedia,"Media status","",Vector2.zero,new Vector2(540,50),20,Quiet);
            showcaseTools=Rect("Media controls",new Vector2(252,-136),new Vector2(64,44),showcaseMedia).gameObject.AddComponent<CanvasGroup>();
            var toolFace=showcaseTools.gameObject.AddComponent<Image>();toolFace.color=new Color(0,0,0,.45f);toolFace.sprite=SurfaceSprites.SoftCard;toolFace.type=UnityEngine.UI.Image.Type.Sliced;
            var pause=ShowcaseButton("showcase-pause",showcaseTools.transform,Vector2.zero,new Vector2(64,44),ShowcaseTogglePlayback);
            showcasePlayIcon=Icon("Icon",GameIcon.Pause,Vector2.zero,22,GameThemes.Current.TitleFace,pause.transform);
            showcaseMedia.gameObject.AddComponent<ShowcaseMediaHover>().tools=showcaseTools;
            showcaseMedia.gameObject.AddComponent<ShowcasePageMotion>();
            showcaseArticle=Rect("Page article",new Vector2(316,-20),new Vector2(328,328),showcasePanel);
            showcaseHeading=ShowcaseText(showcaseArticle,"Page heading","",new Vector2(0,123),new Vector2(328,78),29,White,TextAlignmentOptions.TopLeft);
            showcaseGuide=ShowcaseText(showcaseArticle,"Guide","",new Vector2(0,-66),new Vector2(328,280),22,Quiet,TextAlignmentOptions.TopLeft);
            showcaseGuide.richText=true;showcaseGuide.overflowMode=TextOverflowModes.Overflow;showcaseGuide.lineSpacing=7;
            showcasePrevious=ShowcaseButton("showcase-prev",showcasePanel,new Vector2(-142,-262),new Vector2(50,48),()=>ShowcaseTurn(-1));Icon("Icon",GameIcon.Back,Vector2.zero,26,White,showcasePrevious.transform);
            showcaseNext=ShowcaseButton("showcase-next",showcasePanel,new Vector2(142,-262),new Vector2(50,48),()=>ShowcaseTurn(1));Icon("Icon",GameIcon.Arrow,Vector2.zero,26,White,showcaseNext.transform);
            showcaseDots=Rect("Page dots",new Vector2(0,-262),new Vector2(220,24),showcasePanel);
            BuildShowcaseOverview();
            showcaseRoot.gameObject.AddComponent<ShowcaseWindowMotion>().panel=showcasePanel;
            RefreshShowcasePage();
        }
        private void BuildShowcaseOverview()
        {
            showcaseOverview=Rect("Project overview",new Vector2(0,-25),new Vector2(980,344),showcasePanel);
            var logo=Rect("Wordmark",new Vector2(-276,35),new Vector2(382,196),showcaseOverview).gameObject.AddComponent<TitleArtworkGraphic>();
            logo.Source=Resources.Load<Texture2D>("Title/Wordmark");logo.raycastTarget=false;
            ShowcaseText(showcaseOverview,"Author","made by vvvoidcake",new Vector2(-276,-100),new Vector2(390,35),20,Quiet);
            showcaseOverviewHeading=ShowcaseText(showcaseOverview,"Overview heading","",new Vector2(215,128),new Vector2(460,50),29,White,TextAlignmentOptions.TopLeft);
            showcaseOverviewGuide=ShowcaseText(showcaseOverview,"Overview guide","",new Vector2(215,-81),new Vector2(500,330),23,Quiet,TextAlignmentOptions.TopLeft);
            showcaseOverviewGuide.richText=true;showcaseOverviewGuide.overflowMode=TextOverflowModes.Overflow;showcaseOverviewGuide.lineSpacing=8;
        }
        public void ShowcaseSelectTab(int value)
        {if(!ShowcaseOpen||showcaseClosing||value==showcaseTab)return;showcaseTab=Mathf.Clamp(value,0,3);RefreshShowcasePage();}
        public void ShowcaseTurn(int direction)
        {
            if(!ShowcaseOpen||showcaseClosing||direction==0)return;
            int next=showcaseIndices[showcaseTab]+(direction<0?-1:1);
            if(next<0)
            {
                if(showcaseTab==0)return;
                showcaseTab--;next=ShowcasePages.Length-1;
            }
            else if(next>=ShowcasePages.Length)
            {
                if(showcaseTab==showcaseContent.sections.Length-1)return;
                showcaseTab++;next=0;
            }
            showcaseIndices[showcaseTab]=next;RefreshShowcasePage();
        }
        private static string FormattedShowcaseGuide(ShowcasePage page)
        {
            string value=page.guide??"";
            if(page.highlights==null||page.highlights.Length==0)return value;
            var terms=new System.Collections.Generic.List<string>();
            foreach(string term in page.highlights)
                if(!string.IsNullOrEmpty(term)&&!terms.Contains(term))terms.Add(term);
            if(terms.Count==0)return value;
            terms.Sort((a,b)=>b.Length.CompareTo(a.Length));
            string pattern=string.Join("|",terms.ConvertAll(System.Text.RegularExpressions.Regex.Escape));
            string hex=ColorUtility.ToHtmlStringRGB(Gold);
            return System.Text.RegularExpressions.Regex.Replace(value,pattern,m=>"<color=#"+hex+">"+m.Value+"</color>");
        }
        private void RefreshShowcasePage()
        {
            StopShowcaseVideo();showcasePaused=false;int request=++showcaseRequest;
            bool overview=showcaseTab==0;
            showcaseOverview.gameObject.SetActive(overview);showcaseMedia.gameObject.SetActive(!overview);showcaseArticle.gameObject.SetActive(!overview);
            showcasePrevious.gameObject.SetActive(!overview);showcaseNext.gameObject.SetActive(!overview);showcaseDots.gameObject.SetActive(!overview);
            for(int i=0;i<4;i++){showcaseTabs[i].color=i==showcaseTab?White:Quiet;showcaseTabLines[i].enabled=i==showcaseTab;}
            if(overview){showcaseOverviewHeading.text=ShowcaseCurrent.title;showcaseOverviewGuide.text=FormattedShowcaseGuide(ShowcaseCurrent);return;}
            showcaseMedia.GetComponent<ShowcasePageMotion>().Restart();
            showcaseHeading.text=ShowcaseCurrent.title;showcaseGuide.text=FormattedShowcaseGuide(ShowcaseCurrent);showcaseStatus.text="";showcasePlayIcon.icon=GameIcon.Pause;
            showcasePrevious.interactable=showcaseTab>0||showcaseIndices[showcaseTab]>0;
            showcaseNext.interactable=showcaseTab<showcaseContent.sections.Length-1||showcaseIndices[showcaseTab]<ShowcasePages.Length-1;
            foreach(var b in new[]{showcasePrevious,showcaseNext}){var g=b.GetComponentInChildren<GameIconGraphic>();var c=White;c.a=b.interactable?1:.2f;g.color=c;}
            for(int i=showcaseDots.childCount-1;i>=0;i--){var old=showcaseDots.GetChild(i);old.gameObject.SetActive(false);Destroy(old.gameObject);}
            for(int i=0;i<ShowcasePages.Length;i++)
            {
                int index=i;var dot=ShowcaseButton("showcase-dot-"+i,showcaseDots,new Vector2((i-(ShowcasePages.Length-1)*.5f)*24,0),new Vector2(24,24),()=>{showcaseIndices[showcaseTab]=index;RefreshShowcasePage();});
                var g=Rect("Dot",Vector2.zero,Vector2.one*(i==showcaseIndices[showcaseTab]?9:6),dot.transform).gameObject.AddComponent<ShowcaseSurface>();g.dot=true;g.raycastTarget=false;g.color=i==showcaseIndices[showcaseTab]?Gold:new Color(Quiet.r,Quiet.g,Quiet.b,.4f);
            }
            var poster=UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(ShowcaseFolder+"Clips/"+ShowcaseClip+".jpg");
            showcaseImage.texture=poster;showcaseImage.color=Color.white;
            string path=Path.GetFullPath(ShowcaseFolder+"Clips/"+ShowcaseClip+".webm");
            if(!File.Exists(path)){showcaseStatus.text="演示片段暂不可用";return;}
            showcaseTarget=new RenderTexture(1280,720,0,RenderTextureFormat.ARGB32){name="Showcase video target"};showcaseTarget.Create();
            showcasePlayer=showcaseMedia.gameObject.AddComponent<VideoPlayer>();var player=showcasePlayer;
            player.playOnAwake=false;player.source=VideoSource.Url;player.url=path;player.audioOutputMode=VideoAudioOutputMode.None;player.timeUpdateMode=VideoTimeUpdateMode.UnscaledGameTime;
            player.renderMode=VideoRenderMode.RenderTexture;player.targetTexture=showcaseTarget;player.isLooping=true;player.skipOnDrop=true;player.waitForFirstFrame=true;
            player.prepareCompleted+=p=>{if(request!=showcaseRequest||!ShowcaseOpen||showcaseClosing)return;showcaseImage.texture=showcaseTarget;if(!showcasePaused)p.Play();};
            player.errorReceived+=(p,error)=>{if(request!=showcaseRequest||!ShowcaseOpen)return;showcaseImage.texture=poster;showcaseStatus.text="片段未能播放";Debug.LogWarning("Showcase "+ShowcaseCurrent.id+": "+error);};
            player.Prepare();
        }
        public void ShowcaseTogglePlayback()
        {
            if(!ShowcaseOpen||showcaseClosing||showcaseTab==0)return;showcasePaused=!showcasePaused;
            if(showcasePlayer!=null&&showcasePlayer.isPrepared){if(showcasePaused)showcasePlayer.Pause();else showcasePlayer.Play();}
            showcasePlayIcon.icon=showcasePaused?GameIcon.Play:GameIcon.Pause;
        }
        private void StopShowcaseVideo()
        {
            showcaseRequest++;
            if(showcasePlayer!=null){showcasePlayer.Stop();showcasePlayer.targetTexture=null;Destroy(showcasePlayer);showcasePlayer=null;}
            if(showcaseImage!=null)showcaseImage.texture=null;
            if(showcaseTarget!=null){showcaseTarget.Release();Destroy(showcaseTarget);showcaseTarget=null;}
        }
        public void CloseShowcase()
        {
            if(!ShowcaseOpen||showcaseClosing)return;showcaseClosing=true;StopShowcaseVideo();
            showcaseRoot.GetComponent<ShowcaseWindowMotion>().Close(()=>{CloseShowcaseImmediate();FocusButton("showcase");});
        }
        public bool ShowcaseKeys(IStudyKeyboard keyboard)
        {
            if(!ShowcaseOpen){if(app.Page==GamePage.Title&&keyboard?.Pressed(KeyCode.F1)==true){OpenShowcase();return true;}return false;}
            if(keyboard==null||showcaseClosing)return true;
            if(keyboard.Pressed(KeyCode.Escape))CloseShowcase();
            else if(keyboard.Pressed(KeyCode.Tab))ShowcaseSelectTab((showcaseTab+1)%4);
            else if(keyboard.Pressed(KeyCode.LeftArrow)||keyboard.Pressed(KeyCode.A))ShowcaseTurn(-1);
            else if(keyboard.Pressed(KeyCode.RightArrow)||keyboard.Pressed(KeyCode.D))ShowcaseTurn(1);
            else if(keyboard.Pressed(KeyCode.Space))ShowcaseTogglePlayback();
            return true;
        }
#endif
    }
}
