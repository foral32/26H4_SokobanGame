#if UNITY_EDITOR && UNITY_6000_0_OR_NEWER
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 编辑器当前 SceneView 相机截图工具。
/// 截图只包含场景渲染结果，不包含 Unity 编辑器窗口、工具栏和 Gizmos。
/// </summary>
public static class DevelopmentSceneViewScreenshot
{
    #region 常量

    private const int FallbackWidth = 2560;
    private const int FallbackHeight = 1440;
    private const string FileNamePrefix = "DevSceneView_";

    #endregion

    #region 公开接口

    /// <summary>
    /// 获取项目默认开发截图目录；与 DevelopmentScreenshotCamera 使用同一项目目录规则。
    /// </summary>
    public static string GetDefaultOutputDirectory()
    {
        var projectDirectory = Directory.GetParent(Application.dataPath);
        var projectName = projectDirectory == null ? "UnityProject" : projectDirectory.Name;
        return Path.Combine(DevelopmentScreenshotCamera.DefaultBaseDirectory, projectName);
    }

    /// <summary>拍摄当前活动的 SceneView 视角并保存为 PNG。</summary>
    public static bool TryCapture(string outputDirectory, out string filePath, out string error)
    {
        filePath = null;
        error = null;

        var sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            error = "当前没有可用的 SceneView 窗口。";
            return false;
        }

        var sceneCamera = sceneView.camera;
        if (sceneCamera == null)
        {
            error = "当前 SceneView 没有可用的场景相机。";
            return false;
        }

        var width = Mathf.Max(1, sceneCamera.pixelWidth);
        var height = Mathf.Max(1, sceneCamera.pixelHeight);
        if (width <= 1 || height <= 1)
        {
            width = FallbackWidth;
            height = FallbackHeight;
        }

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            error = "开发截图目录为空。";
            return false;
        }

        RenderTexture renderTexture = null;
        Texture2D screenshot = null;
        var previousTarget = sceneCamera.targetTexture;
        var previousActive = RenderTexture.active;

        try
        {
            Directory.CreateDirectory(outputDirectory);
            filePath = CreateAvailableFilePath(outputDirectory);

            renderTexture = RenderTexture.GetTemporary(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default);
            renderTexture.name = "Development Scene View Screenshot (Temporary)";

            sceneCamera.targetTexture = renderTexture;
            sceneCamera.Render();

            RenderTexture.active = renderTexture;
            screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            screenshot.Apply(false, false);
            File.WriteAllBytes(filePath, screenshot.EncodeToPNG());
            return true;
        }
        catch (Exception exception)
        {
            filePath = null;
            error = exception.Message;
            return false;
        }
        finally
        {
            sceneCamera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;

            if (screenshot != null)
                UnityEngine.Object.DestroyImmediate(screenshot);

            if (renderTexture != null)
                RenderTexture.ReleaseTemporary(renderTexture);
        }
    }

    #endregion

    #region 文件命名

    private static string CreateAvailableFilePath(string directory)
    {
        var safeProjectName = MakeFileNameSafe(GetProjectName());
        var stem = $"{FileNamePrefix}{safeProjectName}_{DateTime.Now:yyyyMMdd_HHmm}";
        var candidate = Path.Combine(directory, stem + ".png");
        var suffix = 2;

        while (File.Exists(candidate))
        {
            candidate = Path.Combine(directory, $"{stem}_{suffix:00}.png");
            suffix++;
        }

        return candidate;
    }

    private static string GetProjectName()
    {
        var projectDirectory = Directory.GetParent(Application.dataPath);
        return projectDirectory == null ? "UnityProject" : projectDirectory.Name;
    }

    private static string MakeFileNameSafe(string value)
    {
        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
            value = value.Replace(invalidCharacter, '_');

        return value;
    }

    #endregion
}
#endif
