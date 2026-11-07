using System;
using System.IO;
using BoxOrVoid.Study.LevelEditing;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace BoxOrVoid.Study.Editor
{
    public static class PlayerEditorBuilder
    {
        [MenuItem("Tools/Box or Void Study/Author player level editor")]
        public static void Author()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=StudyBuilder.ScenePath||scene.isDirty)throw new InvalidOperationException("Open the saved study scene in Edit Mode.");
            var app=UnityEngine.Object.FindFirstObjectByType<PuzzleApp>();var existing=app.GetComponent<PlayerLevelEditor>();if(existing!=null)throw new InvalidOperationException("Editor UI already exists. Edit the saved objects or use RebuildUi for an intentional layout update.");
            var source=AssetDatabase.LoadAssetAtPath<StudyAssets>(StudyBuilder.CatalogPath);source.Initialize();PrepareFont(source);
            var editor=app.gameObject.AddComponent<PlayerLevelEditor>();PlayerEditorLayout.Create(editor,source,UnityEngine.Object.FindFirstObjectByType<Camera>());
            AuthorEntry(app.GetComponent<StudyMenu>(),source);
            EditorUtility.SetDirty(source);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        private static void PrepareFont(StudyAssets source)
        {
            string path=StudyBuilder.Root+"/Art/Fonts/EditorBodyFont.asset";var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if(font==null)
            {
                var face=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/Cn/思源黑体SourceHanSansCN-Medium.ttf");
                if(face==null)throw new InvalidOperationException("The existing Source Han Sans font is unavailable.");
                font=TMP_FontAsset.CreateFontAsset(face,48,6,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic);font.name="Creator Body";font.isMultiAtlasTexturesEnabled=true;
                AssetDatabase.CreateAsset(font,path);AssetDatabase.AddObjectToAsset(font.material,font);foreach(var texture in font.atlasTextures)AssetDatabase.AddObjectToAsset(texture,font);
            }
            string characters="";foreach(string file in Directory.GetFiles(StudyBuilder.Root+"/Runtime/LevelEditor","*.cs"))characters+=File.ReadAllText(file);
            font.TryAddCharacters(characters);EditorUtility.SetDirty(font);source.ConfigureEditorFont(font);
        }
        private static void AuthorEntry(StudyMenu menu,StudyAssets source)
        {
            var root=menu.AuthoredTitle.transform;
            root.Find("Play Button").GetComponent<RectTransform>().anchoredPosition=new Vector2(-92,-112);
            root.Find("Settings Button").GetComponent<RectTransform>().anchoredPosition=new Vector2(-92,-250);
            root.Find("Exit Button").GetComponent<RectTransform>().anchoredPosition=new Vector2(92,-250);
            var panel=EditorUi.Image("Creator Button",root,source.Sprite("Main Menu Button Panel"),new Vector2(92,-112),Vector2.one*82,new Color(.66f,.66f,.66f));
            var mark=AssetDatabase.LoadAssetAtPath<Sprite>(StudyBuilder.Root+"/Art/CreatorMark.png");
            var icon=EditorUi.Image("Icon",panel.transform,mark,Vector2.zero,Vector2.one*42,new Color(.24f,.25f,.26f));
            // The face occupies the top-left 256² of the original 276 × 281 shadowed sprite.
            foreach(string name in new[]{"Play","Creator","Settings","Exit"})
            {
                var button=(RectTransform)root.Find(name+" Button");var glyph=(RectTransform)button.Find("Icon");
                var faceOffset=new Vector2(-10,12.5f)*(button.sizeDelta.x/281f);
                var inkOffset=name=="Play"?new Vector2(-1.8f,0):name=="Exit"?new Vector2(1,0):Vector2.zero;
                glyph.anchoredPosition=faceOffset+inkOffset;
            }
            EditorUi.Text("Play label",root,source,"开始游戏",new Vector2(-92,-174),new Vector2(170,36),20);
            EditorUi.Text("Creator label",root,source,"关卡创作",new Vector2(92,-174),new Vector2(170,36),20);
            EditorUi.Text("Settings label",root,source,"设置",new Vector2(-92,-302),new Vector2(140,30),17);
            EditorUi.Text("Exit label",root,source,"退出",new Vector2(92,-302),new Vector2(140,30),17);
            menu.AddCreatorEntry(panel,icon);EditorUtility.SetDirty(menu);
        }
        public static void RebuildUi()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=StudyBuilder.ScenePath||scene.isDirty)throw new InvalidOperationException("Expected saved study scene.");
            var app=UnityEngine.Object.FindFirstObjectByType<PuzzleApp>();var editor=app.GetComponent<PlayerLevelEditor>();if(editor==null)throw new InvalidOperationException("No editor exists.");
            var source=AssetDatabase.LoadAssetAtPath<StudyAssets>(StudyBuilder.CatalogPath);PrepareFont(source);
            var root=app.transform.Find("Level Editor Canvas");if(root!=null)UnityEngine.Object.DestroyImmediate(root.gameObject);
            PlayerEditorLayout.Create(editor,source,UnityEngine.Object.FindFirstObjectByType<Camera>());app.TestInputOnly=false;EditorUtility.SetDirty(app);EditorUtility.SetDirty(source);EditorUtility.SetDirty(editor);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
    }
}
