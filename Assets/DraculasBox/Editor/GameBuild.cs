using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DraculasBox.Editor
{
    public static class GameBuild
    {
        public static string Status {get;private set;}="idle";
        public static string Schedule()
        {
            if(Status=="building"||Status=="scheduled")throw new InvalidOperationException("Build already running.");
            Status="scheduled";EditorApplication.update+=RunScheduled;EditorApplication.QueuePlayerLoopUpdate();return Status;
        }
        private static void RunScheduled()
        {EditorApplication.update-=RunScheduled;try{BuildWindows();}catch{Status="failed";throw;}}
        [MenuItem("Tools/Dracula's Box/Build Windows game")]
        public static void BuildWindows()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("Stop Play Mode and resolve compilation before building.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("An open scene has unsaved changes.");
            var assets=AssetDatabase.LoadAssetAtPath<Runtime.GameAssets>(GameBuilder.AssetsPath);
            if(assets==null||assets.pipeline==null)throw new InvalidOperationException("Prepare the game scene first.");
            string output=Path.GetFullPath("DraculasBox/Builds/Windows"),evidence=Path.GetFullPath("DraculasBox/Evidence/Build");
            Directory.CreateDirectory(output);Directory.CreateDirectory(evidence);
            var priorDefault=GraphicsSettings.defaultRenderPipeline;
            int priorQuality=QualitySettings.GetQualityLevel();
            var qualityPipelines=Enumerable.Range(0,QualitySettings.names.Length).Select(QualitySettings.GetRenderPipelineAssetAt).ToArray();
            string product=PlayerSettings.productName;
            int width=PlayerSettings.defaultScreenWidth,height=PlayerSettings.defaultScreenHeight;
            bool native=PlayerSettings.defaultIsNativeResolution,resizable=PlayerSettings.resizableWindow;
            var fullscreen=PlayerSettings.fullScreenMode;
            var graph=GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>();
            var compatibility=typeof(RenderGraphSettings).GetField("m_EnableRenderCompatibilityMode",BindingFlags.Instance|BindingFlags.NonPublic);
            if(graph==null||compatibility==null)throw new InvalidOperationException("The URP build adapter requires review for this editor version.");
            bool priorCompatibility=(bool)compatibility.GetValue(graph);
            // BuildPlayer can serialize imported assets and shared settings. Preserve the current
            // working copies, including unrelated edits, rather than restoring files from Git.
            var paths=new[]{"t:UniversalRenderPipelineAsset","t:UniversalRenderPipelineGlobalSettings","t:ScriptableRendererData","t:SpriteAtlas","t:TMP_FontAsset"}
                .SelectMany(filter=>AssetDatabase.FindAssets(filter,new[]{"Assets"})).Select(AssetDatabase.GUIDToAssetPath).Distinct().ToList();
            paths.AddRange(Resources.FindObjectsOfTypeAll<UnityEngine.Object>().Where(EditorUtility.IsDirty).Select(AssetDatabase.GetAssetPath).Where(p=>p.StartsWith("Assets/")&&File.Exists(p)));
            paths.AddRange(Directory.GetFiles("ProjectSettings","*.asset"));
            paths.AddRange(new[]{"ProjectSettings/ProjectSettings.asset","ProjectSettings/GraphicsSettings.asset","ProjectSettings/QualitySettings.asset","ProjectSettings/UnityConnectSettings.asset",
                "Assets/Resources/PerformanceTestRunInfo.json","Assets/Resources/PerformanceTestRunInfo.json.meta","Assets/Resources/PerformanceTestRunSettings.json","Assets/Resources/PerformanceTestRunSettings.json.meta"});
            var before=paths.Distinct().ToDictionary(p=>p,p=>File.Exists(p)?File.ReadAllBytes(p):null);
            string backupRoot=Path.Combine(evidence,"Backups",DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff"));
            Directory.CreateDirectory(backupRoot);
            foreach(var item in before)if(item.Value!=null)
            {string backup=Path.Combine(backupRoot,item.Key);Directory.CreateDirectory(Path.GetDirectoryName(backup));File.WriteAllBytes(backup,item.Value);}
            File.WriteAllText(Path.Combine(backupRoot,"paths.json"),JsonUtility.ToJson(new BackupPaths{paths=before.Keys.ToArray()},true));
            var restoreErrors=new List<string>();
            BuildReport report;
            Status="building";
            try
            {
                // Unity 6.3's serialized compatibility flag can be true while its effective
                // getter is false. Match this project's existing Render Graph runtime.
                compatibility.SetValue(graph,false);GraphicsSettings.defaultRenderPipeline=assets.pipeline;
                for(int i=0;i<qualityPipelines.Length;i++){QualitySettings.SetQualityLevel(i,false);QualitySettings.renderPipeline=assets.pipeline;}
                QualitySettings.SetQualityLevel(priorQuality,false);
                PlayerSettings.productName="Dracula's Box";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
                PlayerSettings.defaultIsNativeResolution=false;PlayerSettings.resizableWindow=true;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
                report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{GameBuilder.ScenePath},locationPathName=Path.Combine(output,"DraculasBox.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            }
            catch{Status="failed";throw;}
            finally
            {
                compatibility.SetValue(graph,priorCompatibility);GraphicsSettings.defaultRenderPipeline=priorDefault;
                for(int i=0;i<qualityPipelines.Length;i++){QualitySettings.SetQualityLevel(i,false);QualitySettings.renderPipeline=qualityPipelines[i];}
                QualitySettings.SetQualityLevel(priorQuality,false);
                PlayerSettings.productName=product;PlayerSettings.defaultScreenWidth=width;PlayerSettings.defaultScreenHeight=height;
                PlayerSettings.defaultIsNativeResolution=native;PlayerSettings.resizableWindow=resizable;PlayerSettings.fullScreenMode=fullscreen;
                foreach(var item in before)
                {
                    try
                    {
                        if(item.Value==null&&File.Exists(item.Key))
                        {string archive=Path.Combine(evidence,"Generated",DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff"));Directory.CreateDirectory(archive);File.Move(item.Key,Path.Combine(archive,Path.GetFileName(item.Key)));}
                        else if(item.Value!=null&&(!File.Exists(item.Key)||!File.ReadAllBytes(item.Key).SequenceEqual(item.Value)))
                        {
                            // Unity can hold a memory map of font assets. Replacing the directory
                            // entry preserves that mapping; truncating it with WriteAllBytes fails.
                            string restore=Path.Combine(backupRoot,"restore.tmp");File.WriteAllBytes(restore,item.Value);
                            if(File.Exists(item.Key))File.Replace(restore,item.Key,null);else File.Move(restore,item.Key);
                        }
                    }
                    catch(Exception e){restoreErrors.Add(item.Key+": "+e.Message);}
                }
                File.WriteAllText(Path.Combine(backupRoot,"restore.json"),JsonUtility.ToJson(new RestoreRecord{errors=restoreErrors.ToArray()},true));
                AssetDatabase.Refresh();
            }
            var s=report.summary;Status=s.result.ToString();
            var record=new BuildRecord{result=Status,output=s.outputPath,platform=s.platform.ToString(),unity=Application.unityVersion,errors=s.totalErrors,warnings=s.totalWarnings,bytes=s.totalSize,seconds=s.totalTime.TotalSeconds,scenes=new[]{GameBuilder.ScenePath},messages=report.steps.SelectMany(step=>step.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Exception||m.type==LogType.Warning).Select(m=>m.content).ToArray()};
            File.WriteAllText(Path.Combine(evidence,"windows-build.json"),JsonUtility.ToJson(record,true));
            if(s.result!=BuildResult.Succeeded)throw new InvalidOperationException("Build failed; see DraculasBox/Evidence/Build/windows-build.json.");
            if(restoreErrors.Count>0)throw new InvalidOperationException("Build succeeded but restoring working files failed; preserved snapshots are in "+backupRoot);
            File.WriteAllText(Path.Combine(output,"开始游戏.cmd"),"@echo off\r\nstart \"\" \"%~dp0DraculasBox.exe\" -screen-fullscreen 0 -screen-width 1280 -screen-height 720\r\n");
            var chapters=assets.Catalog.chapters;
            int mainCount=chapters.Sum(c=>c.levels.Count(l=>l.main)),branchCount=chapters.Sum(c=>c.levels.Count(l=>!l.main));
            File.WriteAllText(Path.Combine(output,"README.txt"),"Dracula's Box\r\n\r\n双击 开始游戏.cmd，或运行 DraculasBox.exe。请保留整个目录。\r\n共 "+chapters.Length+" 章、"+mainCount+" 个主线关卡与 "+branchCount+" 个可选支线。支线显示红框，不影响主线推进；旧进度继续有效。\r\n方向键 / WASD：走动、推动、菜单选择；Enter / 空格：菜单确认。\r\nZ：撤销（可长按）；R：重置（可撤销）；Esc：暂停 / 返回。\r\n踏入光会死亡，可撤销恢复；入棺后自动转场到下一关。\r\n\r\n主菜单 → 关卡创作，可新建、复制、导入或导出关卡。\r\n1–8：基础画笔；9 / 0：压力板、自动门；Q / E：两种斜镜；编号选择机关连接组。\r\n左键拖动：绘制；右键：擦除；Alt+左键：取色。\r\n滚轮：缩放；中键拖动：平移；Z / Y：编辑撤销、重做。\r\nCtrl+S：保存；F5：试玩 / 返回原稿；界面可配置阳光与尺寸。\r\n自制关卡、恢复草稿与主线进度分别保存于本游戏数据目录；移除关卡可恢复。\r\n\r\n这是可玩开发构建，手感与关卡仍待体验反馈。\r\n");
        }
        [Serializable] private sealed class BuildRecord
        {public string result,output,platform,unity;public int errors,warnings;public ulong bytes;public double seconds;public string[] scenes,messages;}
        [Serializable] private sealed class BackupPaths{public string[] paths;}
        [Serializable] private sealed class RestoreRecord{public string[] errors;}
    }
}
