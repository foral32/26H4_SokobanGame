using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.Rendering.Universal;

namespace BoxOrVoid.Study.Editor
{
    public static class StudyBuilder
    {
        [Serializable] private sealed class SpriteImportEntry { public string name; public float pixelsPerUnit,pivotX,pivotY; }
        [Serializable] private sealed class SpriteImportData { public SpriteImportEntry[] sprites; }
        public const string Root="Assets/Studies/BoxOrVoid";
        public const string ScenePath=Root+"/Scenes/BoxOrVoid.unity";
        public const string CatalogPath=Root+"/Content/StudyAssets.asset";

        public static UniversalRenderPipelineAsset PrepareRendering()
        {
            string folder=Root+"/Settings";Directory.CreateDirectory(folder);
            var renderer=AssetDatabase.LoadAssetAtPath<Renderer2DData>(folder+"/StudyRenderer.asset");
            if(renderer==null) {renderer=ScriptableObject.CreateInstance<Renderer2DData>();AssetDatabase.CreateAsset(renderer,folder+"/StudyRenderer.asset");}
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(folder+"/StudyPipeline.asset");
            if(pipeline==null) {pipeline=UniversalRenderPipelineAsset.Create(renderer);pipeline.supportsHDR=false;pipeline.msaaSampleCount=1;AssetDatabase.CreateAsset(pipeline,folder+"/StudyPipeline.asset");}
            var source=AssetDatabase.LoadAssetAtPath<StudyAssets>(CatalogPath);
            if(source!=null) {source.ConfigureRendering(pipeline);EditorUtility.SetDirty(source);}
            AssetDatabase.SaveAssetIfDirty(renderer);AssetDatabase.SaveAssetIfDirty(pipeline);if(source!=null) AssetDatabase.SaveAssetIfDirty(source);return pipeline;
        }

        [MenuItem("Tools/Box or Void Study/Prepare shared scene")]
        public static void Prepare()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop this study's Play Mode before preparing assets.");
            for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("An open scene has unsaved changes. No scenes were replaced.");
            var settings=JsonUtility.FromJson<SpriteImportData>(File.ReadAllText(Root+"/Content/SpriteImport.json"));
            foreach(string path in Directory.GetFiles(Root+"/Art/Sprites","*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                var setting=settings.sprites.First(s=>s.name==Path.GetFileNameWithoutExtension(path));
                var pivot=new Vector2(setting.pivotX,setting.pivotY);
                if(importer.textureType==TextureImporterType.Sprite && Mathf.Approximately(importer.spritePixelsPerUnit,setting.pixelsPerUnit) && importer.spritePivot==pivot && importer.textureCompression==TextureImporterCompression.Uncompressed) continue;
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=setting.pixelsPerUnit;importer.spritePivot=pivot;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
                var textureSettings=new TextureImporterSettings();importer.ReadTextureSettings(textureSettings);
                textureSettings.spriteAlignment=(int)SpriteAlignment.Custom;textureSettings.spritePivot=pivot;importer.SetTextureSettings(textureSettings);
                importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
                importer.SaveAndReimport();
            }
            string fontPath=Root+"/Art/Fonts/Noto Sans SC - Black SDF.asset";
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if(font==null)
            {
                var source=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Art/Fonts/NotoSansSC-Black.ttf");
                font=TMP_FontAsset.CreateFontAsset(source,64,6,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic);
                font.name="Noto Sans SC - Black SDF";AssetDatabase.CreateAsset(font,fontPath);
                AssetDatabase.AddObjectToAsset(font.material,font);
                foreach(var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture,font);
                font.TryAddCharacters("欢迎箱即是空逆转空间撤回重置开始继续设置返回主菜单章节关卡完成已解音量音乐音效退出方向键移动暂停选择前两章学习复刻黑白玩家箱子目标0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz/：，。←↑→↓");
                EditorUtility.SetDirty(font);
            }
            var assets=AssetDatabase.LoadAssetAtPath<StudyAssets>(CatalogPath);
            if(assets==null) { assets=ScriptableObject.CreateInstance<StudyAssets>();AssetDatabase.CreateAsset(assets,CatalogPath); }
            var levels=Directory.GetFiles(Root+"/Content/Levels","*.json").Select(p=>AssetDatabase.LoadAssetAtPath<TextAsset>(p.Replace('\\','/'))).ToArray();
            var sprites=Directory.GetFiles(Root+"/Art/Sprites","*.png").Select(p=>AssetDatabase.LoadAssetAtPath<Sprite>(p.Replace('\\','/'))).ToArray();
            var clips=AssetDatabase.FindAssets("t:AudioClip",new[]{Root+"/Audio"}).Select(g=>AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            Directory.CreateDirectory(Root+"/Art/Materials");
            string materialPath=Root+"/Art/Materials/StudySprite.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null) { material=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));AssetDatabase.CreateAsset(material,materialPath); }
            var replays=Directory.GetFiles(Root+"/Content/Replays","*.json").Select(p=>AssetDatabase.LoadAssetAtPath<TextAsset>(p.Replace('\\','/'))).ToArray();
            assets.Configure(AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Content/Catalog.json"),levels,sprites,clips,font,material,replays);
            assets.ConfigureRendering(PrepareRendering());
            EditorUtility.SetDirty(assets);assets.Initialize();
            foreach(var level in assets.Data.levels) assets.Definition(level.id);
            Directory.CreateDirectory(Root+"/Scenes");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cameraObject=new GameObject("Study Camera",typeof(Camera),typeof(AudioListener));
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.16f,.17f);cameraObject.transform.position=new Vector3(0,0,-10);cameraObject.tag="MainCamera";
            var app=new GameObject("Box or Void Study").AddComponent<PuzzleApp>();app.Configure(assets,camera);
            StudyPresentationBuilder.Create(app,assets,camera);
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssetIfDirty(assets);AssetDatabase.SaveAssetIfDirty(font);AssetDatabase.SaveAssetIfDirty(material);
            Debug.Log("BoxOrVoid study: 21 data levels, one shared scene prepared.");
        }

        public static string Capture(string filename)
        {
            if(!EditorApplication.isPlaying) throw new InvalidOperationException("Capture requires the running study.");
            var app=UnityEngine.Object.FindObjectOfType<PuzzleApp>();
            if(app==null || app.Board==null) throw new InvalidOperationException("Study is not running.");
            string folder=Path.GetFullPath("Studies/BoxOrVoid/Evidence/Runtime");Directory.CreateDirectory(folder);
            string target=Path.Combine(folder,Path.GetFileName(filename));
            var camera=app.Board.Camera;var prior=camera.targetTexture;var priorActive=RenderTexture.active;float aspect=camera.aspect;
            var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                camera.aspect=16f/9f;camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(target,image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=prior;camera.aspect=aspect;RenderTexture.active=priorActive;
                UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
            }
            return target;
        }
    }
}
