using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BoxOrVoid.Study.Editor
{
    public static class StudyBuild
    {
        public static string Status {get;private set;}="idle";
        [MenuItem("Tools/Box or Void Study/Build Windows study")]
        public static void BuildWindows()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("Stop Play Mode and resolve compilation before building.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++) if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("An open scene has unsaved changes.");
            string output=Path.GetFullPath("Studies/BoxOrVoid/Builds/Windows");Directory.CreateDirectory(output);
            string evidence=Path.GetFullPath("Studies/BoxOrVoid/Evidence/Build");Directory.CreateDirectory(evidence);
            Status="building";
            var studyPipeline=StudyBuilder.PrepareRendering();
            var priorDefault=GraphicsSettings.defaultRenderPipeline;
            int priorQuality=QualitySettings.GetQualityLevel();
            var priorQualityPipelines=Enumerable.Range(0,QualitySettings.names.Length).Select(QualitySettings.GetRenderPipelineAssetAt).ToArray();
            string priorProduct=PlayerSettings.productName;
            int priorWidth=PlayerSettings.defaultScreenWidth,priorHeight=PlayerSettings.defaultScreenHeight;
            bool priorNative=PlayerSettings.defaultIsNativeResolution,priorResizable=PlayerSettings.resizableWindow;
            var priorFullscreen=PlayerSettings.fullScreenMode;
            // Explicit scene list isolates the study without changing the host project's Build Settings.
            BuildReport report;
            // In Unity 6.3, the effective getter is false without the compatibility
            // define, but a legacy serialized true flag still rejects the build.
            // Match the existing Render Graph runtime just for this study build.
            // Unity 2022.3 only has the legacy render path, so there is nothing to toggle.
#if UNITY_6000_0_OR_NEWER
            var graph=GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>();
            var compatibility=typeof(RenderGraphSettings).GetField("m_EnableRenderCompatibilityMode",BindingFlags.Instance|BindingFlags.NonPublic);
            if(graph==null || compatibility==null) throw new InvalidOperationException("Recheck the URP build adapter for this editor version.");
            bool original=(bool)compatibility.GetValue(graph);
#endif
            var paths=AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset").Concat(AssetDatabase.FindAssets("t:UniversalRenderPipelineGlobalSettings")).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.StartsWith("Assets/")).Distinct().ToList();
            paths.AddRange(AssetDatabase.FindAssets("t:SpriteAtlas",new[]{"Assets"}).Select(AssetDatabase.GUIDToAssetPath));
            paths.AddRange(new[]{"ProjectSettings/ProjectSettings.asset","ProjectSettings/GraphicsSettings.asset","ProjectSettings/QualitySettings.asset","ProjectSettings/UnityConnectSettings.asset"});
            paths.AddRange(new[]{"Assets/Resources/PerformanceTestRunInfo.json","Assets/Resources/PerformanceTestRunSettings.json","Assets/Resources/PerformanceTestRunInfo.json.meta","Assets/Resources/PerformanceTestRunSettings.json.meta"});
            var before=paths.ToDictionary(p=>p,p=>File.Exists(p)?File.ReadAllBytes(p):null);
            try
            {
#if UNITY_6000_0_OR_NEWER
                compatibility.SetValue(graph,false);
#endif
                GraphicsSettings.defaultRenderPipeline=studyPipeline;
                for(int i=0;i<priorQualityPipelines.Length;i++) {QualitySettings.SetQualityLevel(i,false);QualitySettings.renderPipeline=studyPipeline;}
                QualitySettings.SetQualityLevel(priorQuality,false);
                PlayerSettings.productName="Box or Void Study";
                PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
                PlayerSettings.defaultIsNativeResolution=false;PlayerSettings.resizableWindow=true;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
                report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{StudyBuilder.ScenePath},locationPathName=Path.Combine(output,"BoxOrVoidStudy.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            }
            finally
            {
#if UNITY_6000_0_OR_NEWER
                compatibility.SetValue(graph,original);
#endif
                GraphicsSettings.defaultRenderPipeline=priorDefault;
                for(int i=0;i<priorQualityPipelines.Length;i++) {QualitySettings.SetQualityLevel(i,false);QualitySettings.renderPipeline=priorQualityPipelines[i];}
                QualitySettings.SetQualityLevel(priorQuality,false);
                PlayerSettings.productName=priorProduct;PlayerSettings.defaultScreenWidth=priorWidth;PlayerSettings.defaultScreenHeight=priorHeight;
                PlayerSettings.defaultIsNativeResolution=priorNative;PlayerSettings.resizableWindow=priorResizable;PlayerSettings.fullScreenMode=priorFullscreen;
                string archive=Path.Combine(evidence,"Generated",DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff"));
                foreach(var item in before)
                {
                    if(item.Value==null && File.Exists(item.Key)) {Directory.CreateDirectory(archive);File.Move(item.Key,Path.Combine(archive,Path.GetFileName(item.Key)));}
                    else if(item.Value!=null && (!File.Exists(item.Key) || !File.ReadAllBytes(item.Key).SequenceEqual(item.Value))) File.WriteAllBytes(item.Key,item.Value);
                }
                AssetDatabase.Refresh();
            }
            var summary=report.summary;Status=summary.result.ToString();
            var record=new BuildRecord{result=Status,output=summary.outputPath,platform=summary.platform.ToString(),unity=Application.unityVersion,backend=PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone).ToString(),errors=summary.totalErrors,warnings=summary.totalWarnings,bytes=summary.totalSize,seconds=summary.totalTime.TotalSeconds,scenes=new[]{StudyBuilder.ScenePath},messages=report.steps.SelectMany(step=>step.messages).Where(m=>m.type==LogType.Error || m.type==LogType.Exception || m.type==LogType.Warning).Select(m=>m.content).ToArray()};
            File.WriteAllText(Path.Combine(evidence,"windows-build.json"),JsonUtility.ToJson(record,true));
            if(summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("Study build failed; see Evidence/Build/windows-build.json");
            File.WriteAllText(Path.Combine(output,"开始游戏.cmd"),"@echo off\r\nstart \"\" \"%~dp0BoxOrVoidStudy.exe\" -screen-fullscreen 0 -screen-width 1280 -screen-height 720\r\n");
            File.WriteAllText(Path.Combine(output,"README.txt"),"Box or Void Study — 欢迎 / 箱即是空，共 21 关\r\n\r\n双击 开始游戏.cmd，以 1280×720 可调整窗口启动；也可直接运行 BoxOrVoidStudy.exe。\r\n请保留整个目录，不要只移动 exe。\r\n方向键 / WASD：移动与菜单选择；空格：切换 / 确认；Enter：菜单确认。\r\nZ：撤回（可长按）；R：重置（可撤回）；Esc：暂停 / 返回。\r\n菜单也可鼠标点击。设置页用左右键调整音量。\r\n\r\n主菜单「关卡创作」进入游戏内编辑器，支持新建、编辑、试玩、保存、导入与导出。\r\n编辑：1–6 选择物件；B/V/E 摆放/查看/擦除；Tab 切换颜色；Z 撤回，Y 重做。\r\nF5 试玩/返回编辑；Ctrl+S 保存；F1 查看完整操作指南。\r\n关卡库保存在游戏自己的 UserLevels 目录；导出时可选择文件夹，导入前提供预览。\r\n\r\n这是本地学习复刻的 Windows x64 Mono 开发构建，不是原作发行版。\r\n详见工程 Studies/BoxOrVoid/README.md 和 Review.md。\r\n");
        }
        public static string Schedule()
        {
            if(Status=="building" || Status=="scheduled") throw new InvalidOperationException("Build already running");
            Status="scheduled";EditorApplication.delayCall+=BuildWindows;return Status;
        }
        [Serializable] private sealed class BuildRecord
        {
            public string result,output,platform,unity,backend;
            public int errors,warnings;
            public ulong bytes;
            public double seconds;
            public string[] scenes,messages;
        }
    }
}
