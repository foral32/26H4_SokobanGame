#if UNITY_EDITOR && UNITY_6000_0_OR_NEWER
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// CakePac 主工具栏扩展（Unity 6 MainToolbarElement API）。
/// </summary>
[InitializeOnLoad]
public static class ToolBarExtender
{
    const string ScenesPath = "CakePac/Scenes";
    const string LogPath = "CakePac/Log";
    const string ScreenshotPath = "CakePac/Screenshot";

    static ToolBarExtender()
    {
        ObjectFactory.componentWasAdded -= OnComponentWasAdded;
        ObjectFactory.componentWasAdded += OnComponentWasAdded;
        Debug.Log("[ToolBarExtender] Toolbar elements registered (Unity 6 API)");
    }

    static void OnComponentWasAdded(Component component)
    {
        if (!(component is DevelopmentScreenshotCamera addedCamera))
            return;

        EditorApplication.delayCall += () => RejectDuplicateScreenshotCamera(addedCamera);
    }

    static void RejectDuplicateScreenshotCamera(DevelopmentScreenshotCamera addedCamera)
    {
        if (addedCamera == null)
            return;

        var screenshotCameras = FindLoadedScreenshotCameras();
        if (screenshotCameras.Length <= 1)
            return;

        const string message =
            "Only one DevelopmentScreenshotCamera may exist across all loaded scenes. " +
            "The newly added component has been removed.";
        Debug.LogError($"[Development Screenshot] {message}", addedCamera);
        Undo.DestroyObjectImmediate(addedCamera);
        EditorUtility.DisplayDialog("Duplicate Screenshot Camera", message, "OK");
    }

    [MainToolbarElement(ScenesPath, defaultDockPosition = MainToolbarDockPosition.Right)]
    public static MainToolbarElement CreateScenesDropdown()
    {
        var content = new MainToolbarContent("Scenes", null, "Switch scene from Build Settings");
        return new MainToolbarDropdown(content, ShowSceneMenu);
    }

    [MainToolbarElement(LogPath, defaultDockPosition = MainToolbarDockPosition.Right)]
    public static MainToolbarElement CreateLogToggle()
    {
        bool logOn = GameUtils.EnableLog;
        var content = new MainToolbarContent(
            logOn ? "Log ON" : "Log OFF",
            null,
            $"GameUtils Log: {(logOn ? "Enabled" : "Disabled")} - Click to toggle");
        return new MainToolbarToggle(content, logOn, SetLogEnabled);
    }

    [MainToolbarElement(ScreenshotPath, defaultDockPosition = MainToolbarDockPosition.Right)]
    public static MainToolbarElement CreateScreenshotDropdown()
    {
        var content = new MainToolbarContent(
            "Screenshot",
            null,
            "Capture the DevelopmentScreenshotCamera or current Scene view");
        return new MainToolbarDropdown(content, ShowScreenshotMenu);
    }

    static void ShowScreenshotMenu(Rect dropDownRect)
    {
        var menu = new GenericMenu();
        var screenshotCameras = FindLoadedScreenshotCameras();

        // SceneView 截图不依赖 DevelopmentScreenshotCamera，始终允许从当前场景视角拍摄。
        menu.AddItem(
            new GUIContent("Capture Current Scene View and Save"),
            false,
            CaptureCurrentSceneView);
        menu.AddSeparator("");

        if (screenshotCameras.Length == 1)
        {
            var screenshotCamera = screenshotCameras[0];
            menu.AddItem(
                new GUIContent("Locate and Enable DevCamera"),
                false,
                () => LocateAndEnableDevelopmentCamera(screenshotCamera));
            menu.AddItem(new GUIContent("Capture and Save"), false, () => CaptureScreenshot(screenshotCamera));
            menu.AddItem(new GUIContent("Open Screenshot Folder"), false, () => OpenScreenshotFolder(screenshotCamera));
        }
        else
        {
            menu.AddDisabledItem(new GUIContent("Locate and Enable DevCamera"));
            menu.AddDisabledItem(new GUIContent("Capture and Save"));
            menu.AddDisabledItem(new GUIContent("Open Screenshot Folder"));
            menu.AddSeparator("");

            var message = screenshotCameras.Length == 0
                ? "No screenshot component in loaded scenes"
                : $"Configuration error: {screenshotCameras.Length} screenshot components";
            menu.AddDisabledItem(new GUIContent(message));
        }

        menu.DropDown(dropDownRect);
    }

    #region SceneView截图

    static void CaptureCurrentSceneView()
    {
        var screenshotCameras = FindLoadedScreenshotCameras();
        var outputDirectory = screenshotCameras.Length == 1
            ? screenshotCameras[0].OutputDirectory
            : DevelopmentSceneViewScreenshot.GetDefaultOutputDirectory();

        if (DevelopmentSceneViewScreenshot.TryCapture(outputDirectory, out var filePath, out var error))
        {
            Debug.Log($"[Scene View Screenshot] Saved: {filePath}");
            return;
        }

        Debug.LogError($"[Scene View Screenshot] Capture failed: {error}");
        EditorUtility.DisplayDialog("Scene View Screenshot Failed", error, "OK");
    }

    #endregion

    #region DevCamera定位与启用

    /// <summary>
    /// 选中并启用唯一的开发截图相机，方便在 Hierarchy 中快速定位和调整视角。
    /// </summary>
    static void LocateAndEnableDevelopmentCamera(DevelopmentScreenshotCamera screenshotCamera)
    {
        if (!IsStillUnique(screenshotCamera))
            return;

        var targetCamera = screenshotCamera.GetComponent<Camera>();
        if (targetCamera == null)
        {
            const string message = "DevelopmentScreenshotCamera has no Camera component.";
            Debug.LogError($"[Development Screenshot] {message}", screenshotCamera);
            EditorUtility.DisplayDialog("Camera Configuration Error", message, "OK");
            return;
        }

        var cameraStateChanged = !targetCamera.enabled;
        // 编辑模式下记录 Undo，启用状态才能被可靠地撤销，并保留到场景序列化数据中。
        if (cameraStateChanged && !Application.isPlaying)
            Undo.RecordObject(targetCamera, "Enable Development Screenshot Camera");

        targetCamera.enabled = true;

        if (cameraStateChanged && !Application.isPlaying)
        {
            EditorUtility.SetDirty(targetCamera);
            if (screenshotCamera.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(screenshotCamera.gameObject.scene);
        }

        Selection.activeGameObject = screenshotCamera.gameObject;
        EditorGUIUtility.PingObject(screenshotCamera.gameObject);

        Debug.Log($"[Development Screenshot] Selected and enabled camera: {targetCamera.name}", screenshotCamera);
    }

    #endregion

    static DevelopmentScreenshotCamera[] FindLoadedScreenshotCameras()
    {
        return Enumerable.Range(0, SceneManager.sceneCount)
            .Select(SceneManager.GetSceneAt)
            .Where(scene => scene.isLoaded)
            .SelectMany(scene => scene.GetRootGameObjects())
            .SelectMany(root => root.GetComponentsInChildren<DevelopmentScreenshotCamera>(true))
            .ToArray();
    }

    static void CaptureScreenshot(DevelopmentScreenshotCamera screenshotCamera)
    {
        if (!IsStillUnique(screenshotCamera))
            return;

        if (screenshotCamera.TryCapture(out var filePath, out var error))
        {
            Debug.Log($"[Development Screenshot] Saved: {filePath}", screenshotCamera);
            return;
        }

        Debug.LogError($"[Development Screenshot] Capture failed: {error}", screenshotCamera);
        EditorUtility.DisplayDialog("Screenshot Failed", error, "OK");
    }

    static void OpenScreenshotFolder(DevelopmentScreenshotCamera screenshotCamera)
    {
        if (!IsStillUnique(screenshotCamera))
            return;

        try
        {
            Directory.CreateDirectory(screenshotCamera.OutputDirectory);
            OpenDirectoryInExplorer(screenshotCamera.OutputDirectory);
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[Development Screenshot] Could not open folder: {exception.Message}", screenshotCamera);
            EditorUtility.DisplayDialog("Open Folder Failed", exception.Message, "OK");
        }
    }

    static void OpenDirectoryInExplorer(string directory)
    {
        if (Application.platform != RuntimePlatform.WindowsEditor)
        {
            EditorUtility.RevealInFinder(directory);
            return;
        }

        var escapedDirectory = directory.Replace("\"", string.Empty);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{escapedDirectory}\"",
            UseShellExecute = true
        });
    }

    static bool IsStillUnique(DevelopmentScreenshotCamera expectedCamera)
    {
        var screenshotCameras = FindLoadedScreenshotCameras();
        if (screenshotCameras.Length == 1 && screenshotCameras[0] == expectedCamera)
            return true;

        var message = screenshotCameras.Length == 0
            ? "No DevelopmentScreenshotCamera exists in the loaded scenes."
            : $"Expected exactly one DevelopmentScreenshotCamera, but found {screenshotCameras.Length}.";
        Debug.LogError($"[Development Screenshot] {message}");
        EditorUtility.DisplayDialog("Screenshot Configuration Error", message, "OK");
        return false;
    }

    static void ShowSceneMenu(Rect dropDownRect)
    {
        var menu = new GenericMenu();
        var buildScenes = EditorBuildSettings.scenes;
        var currentScenePath = EditorSceneManager.GetActiveScene().path;

        if (buildScenes.Length == 0)
        {
            menu.AddDisabledItem(new GUIContent("No scenes in Build Settings"));
        }
        else
        {
            for (int i = 0; i < buildScenes.Length; i++)
            {
                var scene = buildScenes[i];
                if (scene.enabled)
                {
                    var scenePath = scene.path;
                    var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                    var displayName = $"{i}: {sceneName}";
                    var isCurrent = scenePath == currentScenePath;
                    if (isCurrent)
                        displayName = "● " + displayName;

                    menu.AddItem(new GUIContent(displayName), isCurrent, () => SwitchToScene(scenePath));
                }
                else
                {
                    var sceneName = Path.GetFileNameWithoutExtension(scene.path);
                    menu.AddDisabledItem(new GUIContent($"{i}: {sceneName} (Disabled)"));
                }
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Open Build Settings"), false, () =>
            {
                EditorWindow.GetWindow(typeof(BuildPlayerWindow));
            });
        }

        menu.DropDown(dropDownRect);
    }

    static void SwitchToScene(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath))
        {
            Debug.LogWarning("Scene path is empty!");
            return;
        }

        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            var choice = EditorUtility.DisplayDialogComplex(
                "Save Scene?",
                "Current scene has unsaved changes. Do you want to save before switching?",
                "Save and Switch",
                "Don't Save",
                "Cancel");

            switch (choice)
            {
                case 0:
                    EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    break;
                case 1:
                    break;
                case 2:
                    return;
            }
        }

        EditorSceneManager.OpenScene(scenePath);
    }

    static void SetLogEnabled(bool value)
    {
        if (GameUtils.EnableLog != value)
            GameUtils.ToggleLog();
        MainToolbar.Refresh(LogPath);
    }
}
#endif
