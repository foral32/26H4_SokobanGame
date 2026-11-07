using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BoxOrVoid.Study.Editor
{
    /// <summary>One-time authoring/migration. The saved objects are also the actual runtime title UI.</summary>
    public static class StudyPresentationBuilder
    {
        [MenuItem("Tools/Box or Void Study/Author scene presentation")]
        public static void AuthorCurrentScene()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!=StudyBuilder.ScenePath || scene.isDirty)
                throw new InvalidOperationException("Open the saved BoxOrVoid scene in Edit Mode before authoring presentation.");
            var app=UnityEngine.Object.FindFirstObjectByType<PuzzleApp>();
            if(app.GetComponent<StudyMenu>()!=null) throw new InvalidOperationException("This scene already has authored presentation; edit its objects directly.");
            var source=AssetDatabase.LoadAssetAtPath<StudyAssets>(StudyBuilder.CatalogPath);
            Create(app,source,UnityEngine.Object.FindFirstObjectByType<Camera>());
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        public static void Create(PuzzleApp app,StudyAssets source,Camera camera)
        {
            source.Initialize();var menu=app.gameObject.AddComponent<StudyMenu>();
            var canvas=Canvas("Menu Canvas",app.transform,camera,300);
            var title=Rect("Main Menu",canvas.transform);
            var background=Image("Background",title,source.Sprite("Main Menu Background"),Vector2.zero,Vector2.zero);
            Stretch(background.rectTransform);background.preserveAspect=false;
            var outlines=Rect("Drifting Outlines",title);
            for(int i=0;i<15;i++)
            {
                var outline=Image("Outline "+(i+1),outlines,source.Sprite("Particle_Box_Uncolored"),new Vector2(((i%5+.5f)/5-.5f)*1420,((i/5+.5f)/3-.5f)*800),Vector2.one*(45+(i*37)%90));
                outline.color=new Color(.2863f,.2196f,.2353f,.5f);
            }
            outlines.gameObject.AddComponent<TitleOutlineMotion>();
            Image("Logo",title,source.Sprite("title_upscaled_withChineseName"),new Vector2(0,145),new Vector2(760,290));
            string[] names={"Play","Settings","Exit"},sprites={"Play_active","Settings_active","Return_active"};
            float[] sizes={80,68,68},ys={-100,-200,-295};var panels=new Image[3];var icons=new Image[3];
            for(int i=0;i<3;i++)
            {
                panels[i]=Image(names[i]+" Button",title,source.Sprite("Main Menu Button Panel"),new Vector2(0,ys[i]),Vector2.one*sizes[i]*1.03f);
                panels[i].color=i==0?new Color(.76f,.80f,.88f):new Color(.66f,.66f,.66f);
                icons[i]=Image("Icon",panels[i].transform,source.Sprite(sprites[i]),Vector2.zero,Vector2.one*sizes[i]*.62f);
                icons[i].color=i==0?Color.white:new Color(.24f,.25f,.26f);
            }
            var pointer=Image("Selection Pointer",title,source.Sprite("Main Menu Pointer"),new Vector2(-76.2f,-100),Vector2.one*32);
            pointer.rectTransform.localRotation=Quaternion.Euler(0,0,90);
            var controls=new GameObject("Controls",typeof(RectTransform),typeof(TextMeshProUGUI));controls.transform.SetParent(title,false);
            var text=controls.GetComponent<TextMeshProUGUI>();text.font=source.Font;text.text="方向键选择 · 空格确认";text.fontSize=18;text.alignment=TextAlignmentOptions.Right;text.color=new Color(.8f,.82f,.84f);text.raycastTarget=false;
            text.rectTransform.anchoredPosition=new Vector2(420,-330);text.rectTransform.sizeDelta=new Vector2(350,32);
            var generated=Rect("Dynamic Pages",canvas.transform);generated.gameObject.AddComponent<CanvasGroup>();
            menu.ConfigureScene(canvas,generated,title.gameObject,pointer,panels,icons);
            var overlay=Canvas("Transition Canvas",app.transform,camera,1000);
            var aperture=Rect("Circular Mask",overlay.transform).gameObject.AddComponent<StudyIrisGraphic>();
            aperture.color=new Color(33f/255,33f/255,33f/255,1);aperture.enabled=false;
            var transition=overlay.gameObject.AddComponent<StudyTransition>();transition.Configure(aperture);
            app.ConfigurePresentation(menu,transition);
        }
        private static Canvas Canvas(string name,Transform parent,Camera camera,int order)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));go.transform.SetParent(parent,false);
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder=order;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;return canvas;
        }
        private static RectTransform Rect(string name,Transform parent)
        {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;Stretch(rect);return rect;}
        private static void Stretch(RectTransform rect)
        {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        private static Image Image(string name,Transform parent,Sprite sprite,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
            var image=go.GetComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;
            image.rectTransform.anchoredPosition=position;image.rectTransform.sizeDelta=size;return image;
        }
    }
}
