using System;
using System.Collections;
using System.IO;
using System.Linq;
using DraculasBox.Core;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class RuntimeChecks
    {
        [Serializable] private sealed class ThemeProbe {public string theme;}
        private static readonly string[] ThemeIds={"classic","wine","cobalt","eclipse","porcelain"};
        private readonly System.Collections.Generic.List<string> themeReviewIssues=new System.Collections.Generic.List<string>();
        private void ThemeReview(bool value,string label){report.checks++;if(!value){themeReviewIssues.Add(label);report.errors.Add(label);}}
        private static float ThemeLuminance(Color c)
        {float Linear(float v)=>v<=.04045f?v/12.92f:Mathf.Pow((v+.055f)/1.055f,2.4f);return .2126f*Linear(c.r)+.7152f*Linear(c.g)+.0722f*Linear(c.b);}
        private static float ThemeContrast(Color a,Color b){float x=ThemeLuminance(a),y=ThemeLuminance(b);return (Mathf.Max(x,y)+.05f)/(Mathf.Min(x,y)+.05f);}
        public void BeginThemes(DraculaApp host)
        {
            app=host;app.SetTestKeyboard(this);report=new CheckReport{runtime=Application.isEditor?"Unity Editor theme checks":"Windows Player theme checks",width=Screen.width,height=Screen.height};
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--dracula-theme-checks");folder=i>=0&&i+1<args.Length&&!args[i+1].StartsWith("-")?args[i+1]:Path.GetFullPath("DraculasBox/Evidence/Themes");Directory.CreateDirectory(folder);
            #if UNITY_EDITOR
            folder=UnityEditor.SessionState.GetString("DraculasBox.ThemeEvidenceFolder",folder);Directory.CreateDirectory(folder);
            #endif
            Application.logMessageReceived+=Log;StartCoroutine(Guard(ThemeRegression()));
        }
        private static string StoredTheme(ProgressData data)=>JsonUtility.FromJson<ThemeProbe>(JsonUtility.ToJson(data)).theme;
        private IEnumerator SelectThemeForCheck(string id)
        {
            int steps=0;
            do{yield return Click("theme-next");Require(++steps<=5,"theme cycling reaches "+id);}while(StoredTheme(app.Progress.Data)!=id);
            var value=app.UI.FindButton("theme").transform.Find("Theme name").GetComponent<TMPro.TMP_Text>();
            Require(value.text==GameThemes.Resolve(id).Name,"selector displays the active name "+id);
        }
        private IEnumerator ThemeRegression()
        {
            yield return Ready();
            Require(app.UI.AuthoredTitle.transform.Find("Title logo")!=null,"title has the integrated vampire wordmark");
            yield return Tap(KeyCode.DownArrow);Require(app.UI.SelectedId=="creator","title supports vertical navigation");
            yield return Tap(KeyCode.UpArrow);Require(app.UI.SelectedId=="play","title returns upward through its vertical menu");
            #if UNITY_EDITOR
            var campaign=app.UI.FindButton("campaign");Require(campaign!=null,"official campaign editor entry remains available");
            var entry=(RectTransform)campaign.transform;
            Require(entry.anchorMin==Vector2.right&&entry.anchorMax==Vector2.right&&entry.pivot==Vector2.right&&entry.anchoredPosition==new Vector2(-28,22),"official campaign editor retains its bottom-right anchors");
            Capture("editor_title_entry");
            #else
            Require(app.UI.FindButton("campaign")==null,"player menu has no editor-only campaign entry");
            #endif
            yield return Click("play");yield return Page(GamePage.Chapters);yield return Click("back");yield return Page(GamePage.Title);
            yield return Click("creator");yield return Page(GamePage.Library);yield return Tap(KeyCode.Escape);yield return Page(GamePage.Title);
            Require(GameThemes.All.Count==5,"exactly five themes");
            string legacyRoot=Path.Combine(Application.temporaryCachePath,"DraculaThemeLegacy-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(legacyRoot);
            File.WriteAllText(Path.Combine(legacyRoot,"progress.json"),"{\"schema\":1,\"completed\":[\"c1_01\"],\"music\":0.3,\"effects\":0.6}");
            var legacy=new ProgressStore(legacyRoot);Require(legacy.Data.theme==GameThemes.DefaultThemeId&&legacy.Done("c1_01")&&Mathf.Abs(legacy.Data.music-.3f)<.001f,"old saves keep progress and use the default theme when unset");
            File.WriteAllText(Path.Combine(legacyRoot,"progress.json"),"{\"schema\":1,\"completed\":[\"c1_01\"],\"theme\":\"missing-theme\"}");
            var unknown=new ProgressStore(legacyRoot);Require(unknown.Data.theme==GameThemes.DefaultThemeId&&unknown.Done("c1_01"),"unknown theme safely falls back without clearing progress");
            yield return Click("settings");
            Require(app.UI.FindButton("theme")!=null&&app.UI.FindButton("theme-prev")!=null&&app.UI.FindButton("theme-next")!=null,"settings has one cycling theme row");
            Require(ThemeIds.All(id=>app.UI.FindButton("theme-"+id)==null),"settings hides the five-theme list");
            Require(ThemeIds.Length==5,"five requested themes");
            app.Progress.Data.music=.37f;app.Progress.Data.effects=.64f;
            foreach(var chapter in app.assets.Catalog.chapters)foreach(var level in chapter.levels)app.Progress.Complete(level.id);
            app.Progress.RememberUnlocks(app.assets.Catalog);string[] completed=app.Progress.Data.completed.ToArray();
            var thumbnailLevel=app.assets.Level("c3_03");thumbnailLevel.id=Guid.NewGuid().ToString("N");thumbnailLevel.title="主题预览";app.Library.Save(thumbnailLevel);
            foreach(string id in ThemeIds)
            {
                var stableButton=app.UI.FindButton("music");
                yield return SelectThemeForCheck(id);
                ThemeReview(ReferenceEquals(stableButton,app.UI.FindButton("music")),"theme switch keeps raycast targets stable "+id);
                Require(app.Page==GamePage.Settings&&app.UI.SelectedId=="theme","theme switch retains settings focus "+id);
                Require(StoredTheme(app.Progress.Data)==id,"active theme is saved "+id);
                var loaded=new ProgressStore(Path.GetDirectoryName(app.Library.Folder));
                Require(StoredTheme(loaded.Data)==id,"theme survives progress reload "+id);
                Require(loaded.Data.completed.SequenceEqual(completed)&&Mathf.Abs(loaded.Data.music-.37f)<.001f&&Mathf.Abs(loaded.Data.effects-.64f)<.001f,"theme preserves progress and volume "+id);
                Capture(id+"_settings");
                yield return Click("back");yield return Page(GamePage.Title);Capture(id+"_title");
                var logo=app.UI.AuthoredTitle.transform.Find("Title logo");var artwork=logo==null?null:logo.GetComponentInChildren<TitleArtworkGraphic>();
                Require(artwork!=null&&artwork.Source!=null,"approved title artwork is present "+id);
                foreach(string removed in new[]{"Sun","Lit tile","First shadow","Second shadow","Crate","Hero","Hero cape","Hero fangs","Coffin"})Require(logo.Find(removed)==null&&app.UI.AuthoredTitle.transform.Find(removed)==null,"title sample strip is absent "+id+" "+removed);
                var play=(RectTransform)app.UI.FindButton("play").transform;var creator=(RectTransform)app.UI.FindButton("creator").transform;
                Require(Mathf.Abs(play.anchoredPosition.x-creator.anchoredPosition.x)<1&&play.anchoredPosition.y>creator.anchoredPosition.y&&play.Find("Tile")==null,"approved menu uses vertical text actions "+id);
                app.ShowLevels("chapter_4");yield return Page(GamePage.Levels);Capture(id+"_selection");
                var completedTile=app.UI.FindButton("level-c4_01").transform;
                ThemeReview(ThemeContrast(completedTile.Find("Tile").GetComponent<MenuTileGraphic>().color,completedTile.Find("Number").GetComponent<TMPro.TMP_Text>().color)>=3,"completed node numbers have readable contrast "+id);
                if(id=="porcelain")
                {
                    var many=new Chapter{id="theme-long",title="主题测试",levels=app.assets.Catalog.chapters.SelectMany(c=>c.levels).Take(9).Select(l=>new ChapterLink{id=l.id,main=true}).ToArray()};
                    app.UI.Levels(many);yield return Ready();var tile=app.UI.FindButton("level-"+many.levels[0].id).transform;
                    Color face=tile.Find("Tile").GetComponent<MenuTileGraphic>().color,number=tile.Find("Number").GetComponent<TMPro.TMP_Text>().color;
                    ThemeReview(ThemeContrast(face,number)>=3,"completed porcelain numbers remain visible in paginated chapter selection");Capture("porcelain_long_selection");yield return Ready();
                }
                app.LoadLevel("c4_04");yield return Level("c4_04");Capture(id+"_playing");
                Require(LightingMatches(),"theme keeps logical light mask "+id);
                app.Act(Command.Right);yield return Ready();
                var session=app.Session;string state=session.State.Key();int undo=session.UndoCount;
                app.Pause();yield return Ready();yield return Click("settings");string other=ThemeIds[(Array.IndexOf(ThemeIds,id)+1)%ThemeIds.Length];yield return SelectThemeForCheck(other);yield return SelectThemeForCheck(id);
                Require(ReferenceEquals(session,app.Session)&&app.Session.State.Key()==state&&app.Session.UndoCount==undo,"pause theme switch preserves state and undo "+id);
                yield return Click("back");yield return Click("home");yield return Page(GamePage.Title);
                var draft=app.assets.Level("c1_02");draft.id=Guid.NewGuid().ToString("N");draft.title="主题预览";
                app.Edit(draft);yield return Page(GamePage.Editing);Capture(id+"_editor");
                var doc=app.Creator.Document;var title=app.UI.Root.Find("Level title").GetComponent<TMPro.TMP_InputField>();title.text="保留草稿";title.onEndEdit.Invoke(title.text);
                int history=doc.UndoCount;string contents=JsonUtility.ToJson(doc.Data);app.Board.ZoomAt(app.Board.Screen(doc.Data.player),1.25f);float zoom=app.Board.EditorZoom;
                yield return Click("editor-files");yield return Click("editor-settings");yield return SelectThemeForCheck(other);yield return SelectThemeForCheck(id);
                app.Library.DiscardRecovery(doc.Data.id);app.SendMessage("OnApplicationQuit",SendMessageOptions.RequireReceiver);
                var recovered=app.Library.Recovery();ThemeReview(recovered!=null&&recovered.id==doc.Data.id&&recovered.title=="保留草稿","quitting from editor settings recovers the unsaved draft "+id);
                yield return Click("back");yield return Page(GamePage.Editing);
                Require(ReferenceEquals(doc,app.Creator.Document)&&JsonUtility.ToJson(doc.Data)==contents&&doc.UndoCount==history&&Mathf.Abs(app.Board.EditorZoom-zoom)<.001f,"editor settings preserve draft history and viewport "+id);
                app.OpenLibrary();yield return Page(GamePage.Library);Canvas.ForceUpdateCanvases();yield return null;
                Require(app.UI.GetComponentsInChildren<LevelThumbnail>().Any(t=>t.canvasRenderer.GetMesh()!=null&&t.canvasRenderer.GetMesh().vertexCount>0),"library draws themed level thumbnails "+id);Capture(id+"_library");
                app.ShowTitle();yield return Page(GamePage.Title);yield return Click("settings");
            }
            yield return SelectThemeForCheck("porcelain");yield return Tap(KeyCode.RightArrow);
            Require(app.UI.SelectedId=="theme"&&StoredTheme(app.Progress.Data)=="classic","keyboard wraps forward through themes");
            yield return Tap(KeyCode.LeftArrow);Require(StoredTheme(app.Progress.Data)=="porcelain","keyboard wraps backward through themes");
            yield return Tap(KeyCode.Return);Require(StoredTheme(app.Progress.Data)=="classic","confirm advances the theme");
            yield return Click("theme-prev");Require(StoredTheme(app.Progress.Data)=="porcelain","mouse previous wraps through themes");
            yield return Click("theme-next");yield return Click("back");yield return Page(GamePage.Title);Capture("classic_roundtrip_title");
            app.OpenLibrary();yield return Page(GamePage.Library);Capture("classic_roundtrip_library");
            report.progressIsolated=app.Automated;Require(app.Automated,"theme checks use isolated progress");
            Require(themeReviewIssues.Count==0,string.Join("; ",themeReviewIssues));
        }
    }
}
