using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Renders this Camera to a PNG for development records.
/// Only one instance should exist across all loaded scenes.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("CakePac/Tools/Development Screenshot Camera")]
[ExecuteAlways]
public sealed class DevelopmentScreenshotCamera : MonoBehaviour
{
    public static string DefaultBaseDirectory
    {
        get
        {
            string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string root = string.IsNullOrWhiteSpace(pictures) ? Application.persistentDataPath : pictures;
            return Path.Combine(root, "Unity Screenshots");
        }
    }

    [Header("Output")]
    [SerializeField, Min(1)] private int outputWidth = 2560;
    [SerializeField, Min(1)] private int outputHeight = 1440;
    [SerializeField, Tooltip("Leave empty to use the cross-project default Pictures/Unity Screenshots directory.")]
    private string baseDirectory = "";

    public int OutputWidth => outputWidth;
    public int OutputHeight => outputHeight;
    public string OutputDirectory => Path.Combine(ResolvedBaseDirectory, ProjectName);

    private string ResolvedBaseDirectory =>
        string.IsNullOrWhiteSpace(baseDirectory) ? DefaultBaseDirectory : baseDirectory.Trim();

    private static string ProjectName
    {
        get
        {
            var projectDirectory = Directory.GetParent(Application.dataPath);
            return projectDirectory == null ? "UnityProject" : projectDirectory.Name;
        }
    }

    /// <summary>
    /// Captures this Camera at the configured resolution and saves it as a PNG.
    /// </summary>
    public bool TryCapture(out string filePath, out string error)
    {
        filePath = null;
        error = null;

        var targetCamera = GetComponent<Camera>();
        if (targetCamera == null)
        {
            error = "DevelopmentScreenshotCamera requires a Camera component.";
            return false;
        }

        if (outputWidth < 1 || outputHeight < 1)
        {
            error = $"Invalid screenshot resolution: {outputWidth} x {outputHeight}.";
            return false;
        }

        RenderTexture renderTexture = null;
        Texture2D screenshot = null;
        var previousTarget = targetCamera.targetTexture;
        var previousActive = RenderTexture.active;

        try
        {
            Directory.CreateDirectory(OutputDirectory);
            filePath = CreateAvailableFilePath(OutputDirectory);

            renderTexture = RenderTexture.GetTemporary(
                outputWidth,
                outputHeight,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default);

            renderTexture.name = "Development Screenshot (Temporary)";
            targetCamera.targetTexture = renderTexture;
            targetCamera.Render();

            RenderTexture.active = renderTexture;
            screenshot = new Texture2D(outputWidth, outputHeight, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, outputWidth, outputHeight), 0, 0, false);
            screenshot.Apply(false, false);

            File.WriteAllBytes(filePath, screenshot.EncodeToPNG());
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            filePath = null;
            return false;
        }
        finally
        {
            targetCamera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;

            if (screenshot != null)
            {
                if (Application.isPlaying)
                    Destroy(screenshot);
                else
                    DestroyImmediate(screenshot);
            }

            if (renderTexture != null)
                RenderTexture.ReleaseTemporary(renderTexture);
        }
    }

    private static string CreateAvailableFilePath(string directory)
    {
        var safeProjectName = MakeFileNameSafe(ProjectName);
        var stem = $"DevScene_{safeProjectName}_{DateTime.Now:yyyyMMdd_HHmm}";
        var candidate = Path.Combine(directory, stem + ".png");
        var suffix = 2;

        while (File.Exists(candidate))
        {
            candidate = Path.Combine(directory, $"{stem}_{suffix:00}.png");
            suffix++;
        }

        return candidate;
    }

    private static string MakeFileNameSafe(string value)
    {
        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
            value = value.Replace(invalidCharacter, '_');

        return value;
    }

    private void OnValidate()
    {
        outputWidth = Mathf.Max(1, outputWidth);
        outputHeight = Mathf.Max(1, outputHeight);
        DisableGameOutputCamera();
    }

    private void OnEnable()
    {
        DisableGameOutputCamera();
    }

    private void DisableGameOutputCamera()
    {
        var outputCamera = GetComponent<Camera>();
        if (outputCamera != null)
            outputCamera.enabled = false;
    }
}
