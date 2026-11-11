// Capture the production BoardView in isolated preview scenes. No alternate renderer.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DraculasBox.Core;
using DraculasBox.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace DraculasBox.Editor.Prototypes
{
    public static class SunProjectionPrototype
    {
        public const string Output="DraculasBox/Reviews/SunIntegration-20261111";
        private const int Width=1280,Height=720;
        private static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        [MenuItem("Tools/Dracula's Box/Prototypes/Render sunlight sample")]
        public static void Render()=>Run();
        public static string Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Editor is busy.");
            var original=SceneManager.GetActiveScene();bool dirty=original.isDirty;int scenes=SceneManager.sceneCount;
            string theme=GameThemes.Current.Id;var pipeline=QualitySettings.renderPipeline;
            var assets=AssetDatabase.LoadAssetAtPath<GameAssets>(GameBuilder.AssetsPath);
            string folder=Path.GetFullPath(Output);Directory.CreateDirectory(folder);var images=new List<string>();
            try
            {
                QualitySettings.renderPipeline=assets.pipeline;GameThemes.Apply("wine");
                using(var sample=new Stage(assets,StudyRoom()))
                {
                    sample.Capture(folder,"production-sun",images);var session=new Session(sample.Board.State.Level);
                    var step=session.Apply(Command.Left);sample.Board.Present(step);
                    for(int i=0;i<9;i++){sample.SampleStep(i/8f);sample.Capture(folder,"move-"+i.ToString("00"),images);}
                }
                foreach(SunSide side in Enum.GetValues(typeof(SunSide)))
                {var room=StudyRoom();room.sunFrom=side;var travel=LightField.SunTravel[(int)side];room.player=room.pieces[0].cell+travel;using(var sample=new Stage(assets,room))sample.Capture(folder,"source-"+side,images);}
                using(var sample=new Stage(assets,assets.Level("c2_04")))sample.Capture(folder,"production-window",images);
                var night=StudyRoom();night.sunEnabled=false;night.title="夜灯";
                using(var sample=new Stage(assets,night))sample.Capture(folder,"production-night",images);
            }
            finally{QualitySettings.renderPipeline=pipeline;GameThemes.Apply(theme);}
            bool clean=SceneManager.GetActiveScene()==original&&original.isDirty==dirty&&SceneManager.sceneCount==scenes;
            File.WriteAllText(Path.Combine(folder,"capture.txt"),"Native runtime BoardView, no overrides. Unity "+Application.unityVersion+"; scene preserved="+clean+Environment.NewLine+string.Join(Environment.NewLine,images));
            if(!clean)throw new InvalidOperationException("Preview modified the source scene.");return folder;
        }
        public static LevelData StudyRoom()=>new LevelData
        {
            id="sunlight-review",title="投影",rows=new[]{".#......","........","........","........"},
            player=new Cell(4,1),coffin=new Cell(2,0),sunEnabled=true,sunFrom=SunSide.Left,
            pieces=new[]{new EntityData{id="crate",kind=PieceKind.Box,cell=new Cell(3,1)},new EntityData{id="lamp",kind=PieceKind.Lamp,cell=new Cell(6,3)}}
        };
        private sealed class Stage:IDisposable
        {
            private readonly Scene scene;private readonly GameObject root;private readonly Camera camera;
            private readonly RenderTexture target;private readonly GameUI ui;
            public BoardView Board {get;}
            public Stage(GameAssets assets,LevelData data)
            {
                scene=EditorSceneManager.NewPreviewScene();
                try
                {
                    root=new GameObject("Sun production capture - temporary"){hideFlags=HideFlags.HideAndDontSave};SceneManager.MoveGameObjectToScene(root,scene);
                    target=new RenderTexture(Width,Height,24,RenderTextureFormat.ARGB32){hideFlags=HideFlags.HideAndDontSave};target.Create();
                    camera=Child("Capture camera").AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.scene=scene;
                    camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=GameThemes.Current.Background;camera.targetTexture=target;
                    camera.aspect=(float)Width/Height;camera.nearClipPlane=.1f;camera.farClipPlane=100;
                    Board=Child("Production BoardView").AddComponent<BoardView>();Board.Initialize(assets,camera);Board.Load(new Definition(data).Initial());
                    var go=new GameObject("Native HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));go.transform.SetParent(root.transform,false);
                    var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=5;canvas.sortingOrder=100;
                    canvas.additionalShaderChannels=AdditionalCanvasShaderChannels.TexCoord1|AdditionalCanvasShaderChannels.Normal|AdditionalCanvasShaderChannels.Tangent;
                    var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
                    var page=new GameObject("Pages",typeof(RectTransform)).GetComponent<RectTransform>();page.SetParent(go.transform,false);page.anchorMin=Vector2.zero;page.anchorMax=Vector2.one;page.offsetMin=page.offsetMax=Vector2.zero;
                    ui=go.AddComponent<GameUI>();ui.Configure(canvas,page);ui.Initialize(assets,null,camera);ui.Playing(data.title,false);
                }
                catch{Dispose();throw;}
            }
            private GameObject Child(string name){var go=new GameObject(name);go.transform.SetParent(root.transform,false);return go;}
            public void SampleStep(float t){typeof(BoardView).GetMethod("Refresh",Private).Invoke(Board,new object[]{t});}
            public void Capture(string folder,string name,List<string> images)
            {
                var old=RenderTexture.active;var image=new Texture2D(Width,Height,TextureFormat.RGB24,false);
                try
                {
                    Canvas.ForceUpdateCanvases();foreach(var text in ui.GetComponentsInChildren<TMPro.TMP_Text>())text.ForceMeshUpdate();
                    foreach(var graphic in ui.GetComponentsInChildren<Graphic>()){graphic.SetAllDirty();graphic.Rebuild(CanvasUpdate.PreRender);}Canvas.ForceUpdateCanvases();
                    camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,Width,Height),0,0);image.Apply();
                    File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());images.Add(name+".png");
                }
                finally{RenderTexture.active=old;Object.DestroyImmediate(image);}
            }
            public void Dispose()
            {
                if(camera!=null)camera.targetTexture=null;
                // Preview-scene MonoBehaviours do not enter the normal Play lifecycle.
                // Release the runtime renderer's owned GPU objects before closing the scene.
                if(Board!=null)typeof(BoardView).GetMethod("OnDestroy",Private).Invoke(Board,null);
                if(target!=null){target.Release();Object.DestroyImmediate(target);}if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
