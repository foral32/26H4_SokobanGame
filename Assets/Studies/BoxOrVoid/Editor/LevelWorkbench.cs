using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BoxOrVoid.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BoxOrVoid.Study.Editor
{
    /// <summary>Edits original-compatible JSON copies; never creates a scene for a level.</summary>
    public sealed class LevelWorkbench:EditorWindow
    {
        private TextAsset source;
        private LevelData data;
        private Vector2 scroll;
        private int selected,chapter;
        private ObjectKind kind=ObjectKind.Box;
        private Tone tone=Tone.White;
        private readonly Stack<string> undo=new Stack<string>();
        private string message="选择 Content/Levels 下的 JSON，或载入一个工作副本。";
        public string CurrentJson=>data==null?null:JsonUtility.ToJson(data);
        public string Message=>message;
        public string LastSavedPath {get;private set;}

        [MenuItem("Tools/Box or Void Study/Level data workbench")]
        public static void Open() {GetWindow<LevelWorkbench>("Box or Void 关卡数据");}

        private void OnGUI()
        {
            EditorGUILayout.LabelField("关卡数据工作台",EditorStyles.boldLabel);
            source=(TextAsset)EditorGUILayout.ObjectField("JSON 文件",source,typeof(TextAsset),false);
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("载入") && source!=null) LoadJson(source.text);
            if(GUILayout.Button("撤销编辑")) UndoEdit();
            if(GUILayout.Button("打开共享场景")) OpenSharedScene();
            EditorGUILayout.EndHorizontal();
            if(data==null) {EditorGUILayout.HelpBox(message,MessageType.Info);return;}
            EditorGUILayout.LabelField(data.width+" × "+data.height+"  ·  y 向上  ·  每格从底到顶排列对象");
            scroll=EditorGUILayout.BeginScrollView(scroll);
            for(int y=data.height-1;y>=0;y--)
            {
                EditorGUILayout.BeginHorizontal();
                for(int x=0;x<data.width;x++)
                {
                    int index=y*data.width+x;var objects=data.grid[index].Objects??Array.Empty<CellObjectData>();
                    var top=objects.LastOrDefault();Color old=GUI.backgroundColor;
                    GUI.backgroundColor=index==selected?new Color(.45f,.9f,.3f):top==null?new Color(.4f,.4f,.4f):top.Color==Tone.White?Color.white:top.Color==Tone.Black?new Color(.25f,.25f,.25f):new Color(.55f,.62f,.66f);
                    if(GUILayout.Button(CellLabel(objects),GUILayout.Width(44),GUILayout.Height(38))) selected=index;
                    GUI.backgroundColor=old;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            selected=Mathf.Clamp(selected,0,data.grid.Length-1);
            EditorGUILayout.LabelField("格子 ("+selected%data.width+", "+selected/data.width+")",EditorStyles.boldLabel);
            var layers=data.grid[selected].Objects??Array.Empty<CellObjectData>();
            for(int i=0;i<layers.Length;i++)
            {
                EditorGUILayout.BeginHorizontal();EditorGUILayout.LabelField(i+": "+layers[i].Color+" / "+layers[i].ObjectType);
                if(GUILayout.Button("删除层",GUILayout.Width(70))) {RemoveLayer(selected,i);GUIUtility.ExitGUI();}
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal();kind=(ObjectKind)EditorGUILayout.EnumPopup(kind);tone=(Tone)EditorGUILayout.EnumPopup(tone);
            if(GUILayout.Button("添加顶层")) AddLayer(selected,kind,tone);
            EditorGUILayout.EndHorizontal();
            chapter=EditorGUILayout.Popup("表现主题",chapter,new[]{"欢迎","箱即是空"});
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("检查数据")) Validate();
            if(GUILayout.Button("保存工作副本")) SaveCopy();
            if(GUILayout.Button("在当前 Play Mode 试跑")) Preview();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(message,MessageType.Info);
        }
        public bool LoadJson(string json)
        {
            try {var parsed=JsonUtility.FromJson<LevelData>(json);PuzzleDefinition.FromData(parsed);data=parsed;selected=0;undo.Clear();LastSavedPath=null;message="已载入。保存会写入 Content/Drafts，保留原始 21 关。";return true;}
            catch(Exception ex) {message=ex.Message;return false;}
        }
        private void Remember() {undo.Push(JsonUtility.ToJson(data));}
        public void AddLayer(int cell,ObjectKind objectKind,Tone objectTone)
        {Remember();data.grid[cell].Objects=(data.grid[cell].Objects??Array.Empty<CellObjectData>()).Concat(new[]{new CellObjectData{ObjectType=objectKind,Color=objectTone}}).ToArray();}
        public void RemoveLayer(int cell,int layer)
        {Remember();data.grid[cell].Objects=data.grid[cell].Objects.Where((v,n)=>n!=layer).ToArray();}
        public void UndoEdit() {if(undo.Count>0) {data=JsonUtility.FromJson<LevelData>(undo.Pop());message="已撤销编辑。";}}
        public bool Validate()
        {try {PuzzleDefinition.FromData(data);message="数据结构有效；通关性需通过实际试跑或解法回放检查。";return true;}catch(Exception ex) {message=ex.Message;return false;}}
        public void SaveCopy()
        {
            if(!Validate()) return;
            string folder=StudyBuilder.Root+"/Content/Drafts";Directory.CreateDirectory(folder);
            string filename=(source==null?"custom":source.name)+"_study.json";
            string path=LastSavedPath??AssetDatabase.GenerateUniqueAssetPath(folder+"/"+filename);File.WriteAllText(path,JsonUtility.ToJson(data,true));LastSavedPath=path;AssetDatabase.Refresh();message="已保存工作副本："+path;
        }
        public void Preview()
        {
            if(!Validate()) return;
            var app=UnityEngine.Object.FindObjectOfType<PuzzleApp>();
            if(!EditorApplication.isPlaying || app==null) {message="请在共享 BoxOrVoid 场景进入 Play Mode，再点击试跑。";return;}
            app.LoadCustom(JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(data)),chapter);message="已将工作台数据交给同一玩法场景试跑；没有保存或生成场景。";
        }
        private void OpenSharedScene()
        {
            if(EditorApplication.isPlaying) {message="请先结束当前 Play Mode。";return;}
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++) if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
            {message="当前场景有未保存修改，请先处理。";return;}
            EditorSceneManager.OpenScene(StudyBuilder.ScenePath);
        }
        private static string CellLabel(CellObjectData[] objects)
        {
            if(objects.Length==0) return "·";
            var top=objects[objects.Length-1];string label=top.ObjectType==ObjectKind.Wall?"墙":top.ObjectType==ObjectKind.Box?"箱":top.ObjectType==ObjectKind.Player?"人":top.ObjectType==ObjectKind.PlayerGoal?"人○":top.ObjectType==ObjectKind.BoxGoal?"箱○":"·";
            return label+(objects.Length>1?"\n+"+(objects.Length-1):"");
        }
    }
}
