using System;
using System.Collections;
using System.IO;
using System.Linq;
using DraculasBox.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

namespace DraculasBox.Editor
{
    public static partial class CampaignPlayChecks
    {
        public static void RunShowcaseChecks()
        {UnityEditor.SessionState.SetString("DraculasBox.CampaignEvidenceFolder","DraculasBox/Evidence/Showcase");Begin(ShowcaseChecks(true),"showcase-ui",false);}
        public static void RunShowcaseLayoutChecks()
        {UnityEditor.SessionState.SetString("DraculasBox.CampaignEvidenceFolder","DraculasBox/Evidence/Showcase");Begin(ShowcaseChecks(false),"showcase-layout",false);}
        public static void RunShowcaseNavigationChecks()
        {UnityEditor.SessionState.SetString("DraculasBox.CampaignEvidenceFolder","DraculasBox/Evidence/Showcase");Begin(ShowcaseNavigation(),"showcase-navigation",false);}
        private static Button DemoButton(string name)=>app.UI.transform.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name==name);
        private static IEnumerator DemoClick(string name)
        {
            yield return Ready();var button=DemoButton(name);Need(button!=null&&button.interactable,"showcase control enabled: "+name);
            var point=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(app.viewCamera,button.transform.position)};
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(point,hits);
            Need(hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==button.gameObject,"showcase control is topmost: "+name);
            ExecuteEvents.Execute(button.gameObject,point,ExecuteEvents.pointerClickHandler);yield return Hold(.3f);
        }
        private static void ShowcaseTextFits()
        {
            Canvas.ForceUpdateCanvases();
            foreach(var text in app.UI.GetComponentInParent<Canvas>().GetComponentsInChildren<TMPro.TMP_Text>())
            {
                if(text.name!="Guide"&&text.name!="Page heading"&&text.name!="Overview guide"&&text.name!="Overview heading")continue;
                text.ForceMeshUpdate();
                Need(text.preferredHeight<=text.rectTransform.rect.height+1,"showcase text fits: "+app.UI.ShowcasePageId+" / "+text.name);
            }
        }
        private static IEnumerator ShowcaseChecks(bool playback)
        {
            string progress=JsonUtility.ToJson(app.Progress.Data);string catalogue=app.assets.catalog.text;
            int files=app.Library.Read(out _).Count;bool navigation=EventSystem.current.sendNavigationEvents;
            Capture("menu-entry");yield return Click("showcase");yield return Hold(.3f);
            Need(app.UI.ShowcaseOpen&&app.Page==GamePage.Title,"opens over menu without changing page");
            Need(!EventSystem.current.sendNavigationEvents,"native Submit cannot activate a previously selected menu button");
            Need(app.UI.ShowcasePageId=="intro","first visit starts at README");
            Need(!app.UI.ShowcaseVideoReady,"README does not allocate a video player");
            Need(app.UI.GetComponentInParent<Canvas>().GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text=="made by vvvoidcake"),"README includes author credit");
            ShowcaseTextFits();Capture("readme-window");
            var play=app.UI.FindButton("play");var pe=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(app.viewCamera,play.transform.position)};
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pe,hits);
            Need(hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)!=play.gameObject,"modal blocks menu clicks");
            yield return Press(KeyCode.Return);Need(app.Page==GamePage.Title&&app.UI.ShowcaseOpen,"Enter cannot activate underlying menu");
            yield return Press(KeyCode.Space);Need(app.UI.ShowcasePageId=="intro","space on README is harmless");

            int[] lengths={0,5,6,3};string[] prefixes={"","ld0","td0","polish0"};
            for(int tab=1;tab<4;tab++)
            {
                yield return DemoClick("showcase-tab-"+tab);
                for(int i=1;i<=lengths[tab];i++)
                {
                    Need(app.UI.ShowcasePageId==prefixes[tab]+i,"section ordering "+tab+" / "+i);
                    ShowcaseTextFits();
                    yield return WaitUntilReady(()=>app.UI.ShowcaseVideoReady,"video "+app.UI.ShowcasePageId);yield return Hold(.5f);
                    var players=app.UI.GetComponentInParent<Canvas>().GetComponentsInChildren<VideoPlayer>();
                    Need(players.Length==1&&players[0].audioOutputMode==VideoAudioOutputMode.None,"one silent video player: "+app.UI.ShowcasePageId);
                    if(playback)Need(players[0].isPlaying&&players[0].frame>0,"video advances: "+app.UI.ShowcasePageId);
                    if(i==1){Capture(tab==1?"ld-window":tab==2?"td-window":"polish-window");}
                    if(playback&&tab==1&&i==1)
                    {
                        yield return Press(KeyCode.Space);Need(!players[0].isPlaying,"space pauses video");
                        yield return Press(KeyCode.Space);yield return Hold(.3f);Need(players[0].isPlaying,"space resumes video");
                    }
                    if(i<lengths[tab])yield return DemoClick("showcase-next");
                }
                Need(DemoButton("showcase-next").interactable==(tab<3),"next continues across sections and stops at book end: "+tab);
            }
            yield return Press(KeyCode.Tab);Need(app.UI.ShowcasePageId=="intro","Tab cycles from last section to README");
            Need(app.UI.GetComponentInParent<Canvas>().GetComponentsInChildren<VideoPlayer>().Length==0,"README releases previous video");
            yield return Press(KeyCode.Tab);Need(app.UI.ShowcasePageId=="ld05","design retains its reading position");
            yield return Press(KeyCode.Tab);Need(app.UI.ShowcasePageId=="td06","editor retains its reading position");
            yield return DemoClick("showcase-dot-0");Need(app.UI.ShowcasePageId=="td01","dots jump directly");
            yield return DemoClick("showcase-close");Need(!app.UI.ShowcaseOpen,"close animation returns to menu");
            Need(EventSystem.current.sendNavigationEvents==navigation,"close restores native menu navigation");
            yield return Click("showcase");yield return Hold(.3f);Need(app.UI.ShowcasePageId=="td01","reopen retains position");
            yield return Press(KeyCode.Escape);yield return Hold(.3f);Need(!app.UI.ShowcaseOpen,"Escape closes overlay");
            yield return Press(KeyCode.F1);yield return Hold(.3f);Need(app.UI.ShowcaseOpen,"F1 opens menu entry");
            app.UI.CloseShowcase();yield return Hold(.3f);
            Need(app.UI.GetComponentInParent<Canvas>().GetComponentsInChildren<VideoPlayer>().Length==0,"close releases all video players");
            Need(Resources.FindObjectsOfTypeAll<RenderTexture>().All(t=>t.name!="Showcase video target"),"close releases video texture");
            Need(JsonUtility.ToJson(app.Progress.Data)==progress&&app.assets.catalog.text==catalogue&&app.Library.Read(out _).Count==files,"viewing leaves progress, campaign and custom library unchanged");
            yield return Click("creator");yield return Page(GamePage.Library);Need(app.UI.FindButton("showcase")==null,"entry only exists on main menu");
            app.ShowTitle();yield return Page(GamePage.Title);
        }
        private static IEnumerator ShowcaseNavigation()
        {
            yield return Ready();
            string progress=JsonUtility.ToJson(app.Progress.Data),catalogue=app.assets.catalog.text;
            int files=app.Library.Read(out _).Count;
            var entry=app.UI.FindButton("campaign");
            Need(entry!=null&&entry.gameObject.CompareTag("EditorOnly"),"formal editor entry remains editor-only and clickable");
            var rect=(RectTransform)entry.transform;
            Need(rect.anchorMin==new Vector2(1,0)&&rect.pivot==new Vector2(1,0),"formal editor entry remains anchored to the lower right");
            Capture("menu-refined-entry");
            app.UI.HoverForCheck("campaign");yield return Hold(.25f);
            Need(app.UI.SelectedId=="campaign"&&entry.transform.Find("Focus").GetComponent<Graphic>().color.a>.9f,"hover visibly focuses formal editor entry");
            Capture("menu-refined-entry-hover");
            yield return Click("showcase");yield return Hold(.3f);app.UI.ShowcaseSelectTab(0);
            string[] pages={"intro","ld01","ld02","ld03","ld04","ld05","td01","td02","td03","td04","td05","td06","polish01","polish02","polish03"};
            yield return Press(KeyCode.LeftArrow);Need(app.UI.ShowcasePageId==pages[0],"left stops at first page");ShowcaseTextFits();
            for(int i=1;i<pages.Length;i++)
            {yield return Press(KeyCode.RightArrow);Need(app.UI.ShowcasePageId==pages[i],"keyboard reads continuously forward: "+pages[i]);ShowcaseTextFits();}
            yield return Press(KeyCode.RightArrow);Need(app.UI.ShowcasePageId==pages[pages.Length-1],"right stops at final page");
            for(int i=pages.Length-2;i>=0;i--)
            {yield return Press(KeyCode.LeftArrow);Need(app.UI.ShowcasePageId==pages[i],"keyboard reads continuously backward: "+pages[i]);}
            for(int i=0;i<5;i++)yield return Press(KeyCode.RightArrow);
            Need(app.UI.ShowcasePageId=="ld05","design boundary reached through keyboard");
            yield return DemoClick("showcase-next");Need(app.UI.ShowcasePageId=="td01","mouse next also crosses to next section's first page");
            yield return DemoClick("showcase-prev");Need(app.UI.ShowcasePageId=="ld05","mouse previous crosses to previous section's final page");
            Capture("showcase-section-boundary");
            yield return Press(KeyCode.Escape);yield return Hold(.3f);
            Need(!app.UI.ShowcaseOpen&&app.Page==GamePage.Title,"continuous navigation closes back to title");
            Need(JsonUtility.ToJson(app.Progress.Data)==progress&&app.assets.catalog.text==catalogue&&app.Library.Read(out _).Count==files,"navigation preserves campaign and player data");
        }
    }
}
