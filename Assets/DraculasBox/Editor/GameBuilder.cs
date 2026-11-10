using System;
using System.IO;
using System.Linq;
using DraculasBox.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace DraculasBox.Editor
{
    public static class GameBuilder
    {
        public const string Root="Assets/DraculasBox",ScenePath=Root+"/Scenes/DraculasBox.unity",AssetsPath=Root+"/Content/GameAssets.asset";
        [MenuItem("Tools/Dracula's Box/Prepare game scene")]
        public static void Prepare()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before preparing.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Open scene has unsaved changes; no scene was replaced.");
            GameThemes.Apply(GameThemes.DefaultThemeId);
            foreach(string path in Directory.GetFiles(Root+"/Art/Sprites","*.png"))
            {
                var p=path.Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(p);
                var setting=new TextureImporterSettings();importer.ReadTextureSettings(setting);setting.spriteAlignment=(int)SpriteAlignment.Center;setting.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(setting);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=256;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            var a=AssetDatabase.LoadAssetAtPath<GameAssets>(AssetsPath);if(a==null){a=ScriptableObject.CreateInstance<GameAssets>();AssetDatabase.CreateAsset(a,AssetsPath);}
            a.catalog=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Content/Chapters.json");
            a.replays=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Content/Replays.json");
            a.levels=CampaignProject.LevelReferences(a.Catalog,Root+"/Content");
            a.sprites=Directory.GetFiles(Root+"/Art/Sprites","*.png").Select(p=>AssetDatabase.LoadAssetAtPath<Sprite>(p.Replace('\\','/'))).ToArray();
            a.ResetSpriteCache();
            ConfigureAudio(a);
            a.font=Font("TitleFont","Assets/Studies/BoxOrVoid/Art/Fonts/NotoSansSC-Black.ttf");
            a.bodyFont=Font("BodyFont","Assets/Art/Fonts/Cn/思源黑体SourceHanSansCN-Medium.ttf");
            a.material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Settings/Sprite.mat");if(a.material==null){a.material=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));AssetDatabase.CreateAsset(a.material,Root+"/Settings/Sprite.mat");}
            var renderer=AssetDatabase.LoadAssetAtPath<Renderer2DData>(Root+"/Settings/Renderer.asset");if(renderer==null){renderer=ScriptableObject.CreateInstance<Renderer2DData>();AssetDatabase.CreateAsset(renderer,Root+"/Settings/Renderer.asset");}
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root+"/Settings/Pipeline.asset");if(pipeline==null){pipeline=UniversalRenderPipelineAsset.Create(renderer);pipeline.supportsHDR=false;pipeline.msaaSampleCount=1;AssetDatabase.CreateAsset(pipeline,Root+"/Settings/Pipeline.asset");}a.pipeline=pipeline;
            EditorUtility.SetDirty(a);AssetDatabase.SaveAssetIfDirty(a);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Game Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=5;camera.backgroundColor=new Color32(213,219,226,255);camera.clearFlags=CameraClearFlags.SolidColor;camera.transform.position=new Vector3(0,0,-10);
            var app=new GameObject("Dracula's Box").AddComponent<DraculaApp>();app.assets=a;app.viewCamera=camera;
            var canvas=Canvas("Game UI",camera,100);canvas.additionalShaderChannels=AdditionalCanvasShaderChannels.TexCoord1|AdditionalCanvasShaderChannels.Normal|AdditionalCanvasShaderChannels.Tangent;
            var content=Rect("Pages",canvas.transform);var ui=canvas.gameObject.AddComponent<GameUI>();ui.Configure(canvas,content);app.ui=ui;
            var transitionCanvas=Canvas("Transition",camera,200);var irisRect=Rect("Iris",transitionCanvas.transform);var iris=irisRect.gameObject.AddComponent<StudyIrisGraphic>();iris.color=new Color32(35,43,51,255);var trans=transitionCanvas.gameObject.AddComponent<StudyTransition>();trans.Configure(iris);trans.SetTiming(.18f,.24f);app.transition=trans;
            new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            // The same persistent page root is used in the editor and player.
            ui.Initialize(a,app,camera);ui.Title();
            var title=Rect("Main Menu",canvas.transform);
            foreach(var child in content.Cast<Transform>().ToArray())child.SetParent(title,false);
            ui.ConfigureTitle(title.gameObject,new[]{"play","creator","settings","exit"}.Select(id=>title.Find(id).GetComponent<Button>()).ToArray());
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,ScenePath);
            foreach(var asset in new UnityEngine.Object[]{a,a.font,a.bodyFont,a.material,pipeline,renderer})AssetDatabase.SaveAssetIfDirty(asset);
            Debug.Log("Dracula's Box: game scene prepared; "+a.levels.Length+" levels, editor available.");
        }
        [MenuItem("Tools/Dracula's Box/Refresh chapter content")]
        public static void RefreshContent()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before refreshing content.");
            var a=AssetDatabase.LoadAssetAtPath<GameAssets>(AssetsPath);
            if(a==null)throw new InvalidOperationException("Prepare the game assets first.");
            a.catalog=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Content/Chapters.json");
            a.replays=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Content/Replays.json");
            a.levels=CampaignProject.LevelReferences(a.Catalog,Root+"/Content");
            ConfigureAudio(a);
            a.font=Font("TitleFont","Assets/Studies/BoxOrVoid/Art/Fonts/NotoSansSC-Black.ttf");a.bodyFont=Font("BodyFont","Assets/Art/Fonts/Cn/思源黑体SourceHanSansCN-Medium.ttf");
            EditorUtility.SetDirty(a);AssetDatabase.SaveAssetIfDirty(a);
        }
        public static void ConfigureAudio(GameAssets a)
        {
            const string music=Root+"/Art/Audio/Music/";
            a.mainMusic=AssetDatabase.LoadAssetAtPath<AudioClip>(music+"HypnoticJewels.ogg");
            a.editorMusic=AssetDatabase.LoadAssetAtPath<AudioClip>(music+"PuzzleTune1B.ogg");
            a.audio=AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Studies/BoxOrVoid/Audio",Root+"/Art/Audio"})
                .Select(AssetDatabase.GUIDToAssetPath).Where(p=>!p.StartsWith(music,StringComparison.Ordinal))
                .Select(AssetDatabase.LoadAssetAtPath<AudioClip>).Where(c=>c!=null&&c.name!="Firmament_loopable"&&c.name!="Covert Affair_Loop").ToArray();
        }
        private static TMP_FontAsset Font(string name,string source)
        {
            string path=Root+"/Art/Fonts/"+name+".asset";var f=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if(f==null){var src=AssetDatabase.LoadAssetAtPath<UnityEngine.Font>(source);if(src==null)throw new InvalidOperationException("Missing source font "+source);f=TMP_FontAsset.CreateFontAsset(src,48,6,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic);f.name=name;f.isMultiAtlasTexturesEnabled=true;AssetDatabase.CreateAsset(f,path);AssetDatabase.AddObjectToAsset(f.material,f);foreach(var tex in f.atlasTextures)AssetDatabase.AddObjectToAsset(tex,f);}
            string text="Dracula’s Box − × · 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            text+=File.ReadAllText(Root+"/Content/Chapters.json");
            foreach(string file in Directory.GetFiles(Root+"/Runtime","*.cs"))text+=File.ReadAllText(file);
            foreach(string file in Directory.GetFiles(Root+"/Content/Levels","*.json"))text+=File.ReadAllText(file);
            text=new string(text.Where(c=>c>=32&&c<=126||c>=0x4e00&&c<=0x9fff||"’−×·".Contains(c)).Distinct().ToArray());f.TryAddCharacters(text,out string missing);EditorUtility.SetDirty(f);AssetDatabase.SaveAssetIfDirty(f);return f;
        }
        [MenuItem("Tools/Dracula's Box/Update menu presentation")]
        public static void UpdatePresentation()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().path!=ScenePath)throw new InvalidOperationException("Open the Dracula's Box scene outside Play Mode.");
            var ui=UnityEngine.Object.FindFirstObjectByType<GameUI>();var app=UnityEngine.Object.FindFirstObjectByType<DraculaApp>();
            if(ui==null||app==null)throw new InvalidOperationException("Game UI is missing.");GameThemes.Apply(GameThemes.DefaultThemeId);
            var a=AssetDatabase.LoadAssetAtPath<GameAssets>(AssetsPath);
            a.font=Font("TitleFont","Assets/Studies/BoxOrVoid/Art/Fonts/NotoSansSC-Black.ttf");a.bodyFont=Font("BodyFont","Assets/Art/Fonts/Cn/思源黑体SourceHanSansCN-Medium.ttf");
            EditorUtility.SetDirty(a);AssetDatabase.SaveAssetIfDirty(a);
            var old=ui.AuthoredTitle;ui.ConfigureTitle(null,null);ui.Initialize(a,app,app.viewCamera);ui.Title();
            var title=Rect("Main Menu",ui.transform);
            foreach(var child in ui.Root.Cast<Transform>().ToArray())child.SetParent(title,false);
            ui.ConfigureTitle(title.gameObject,new[]{"play","creator","settings","exit"}.Select(id=>title.Find(id).GetComponent<Button>()).ToArray());
            if(old!=null)UnityEngine.Object.DestroyImmediate(old);
            ui.GetComponent<Canvas>().additionalShaderChannels=AdditionalCanvasShaderChannels.TexCoord1|AdditionalCanvasShaderChannels.Normal|AdditionalCanvasShaderChannels.Tangent;
            app.transition.SetTiming(.18f,.24f);EditorUtility.SetDirty(app.transition);EditorUtility.SetDirty(ui);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ScenePath);
        }
        private static Canvas Canvas(string name,Camera camera,int order)
        {var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));var c=go.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=5;c.sortingOrder=order;var s=go.GetComponent<CanvasScaler>();s.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;s.referenceResolution=new Vector2(1280,720);s.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;return c;}
        private static RectTransform Rect(string name,Transform parent)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
        [MenuItem("Tools/Dracula's Box/Run background checks")]
        public static void Validate()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Editor is already playing.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Open scene has unsaved changes; no scene was replaced.");
            SessionState.SetBool("DraculasBox.Validate",true);EditorSceneManager.OpenScene(ScenePath);EditorApplication.isPlaying=true;
        }
    }
}
