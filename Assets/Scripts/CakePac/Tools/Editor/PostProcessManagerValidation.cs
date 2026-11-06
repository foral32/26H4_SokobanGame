using System;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class PostProcessManagerValidation
{
    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int checks;
    [MenuItem("Tools/Rendering/验证暗角配置兼容")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在编辑模式验证");
        checks = 0;
        Test(false);
        Test(true);
        Debug.Log("VIGNETTE_COMPAT_PASSED checks=" + checks);
    }
    private static void Test(bool configured)
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var shared = ScriptableObject.CreateInstance<VolumeProfile>();
        if (configured)
        {
            var original = shared.Add<Vignette>(false);
            original.intensity.value = .23f; original.intensity.overrideState = true;
            original.color.value = Color.blue; original.color.overrideState = false;
        }
        var errors = new List<string>();
        Application.LogCallback capture = (m, s, t) =>
        { if (t == LogType.Error || t == LogType.Exception || t == LogType.Assert) errors.Add(m); };
        Application.logMessageReceived += capture;
        Volume volume = null; PostProcessManager manager = null;
        try
        {
            var root = new GameObject("VignetteCompatibility", typeof(Volume));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            volume = root.GetComponent<Volume>(); volume.sharedProfile = shared;
            manager = root.AddComponent<PostProcessManager>(); manager.postProcessVolume = volume;
            Call(manager, "InitializePostProcess");
            Check(errors.Count == 0, "initialization accepts " + (configured ? "configured" : "missing") + " Vignette");
            Check(volume.profile != shared, "uses runtime profile clone");
            Check(volume.profile.TryGet<Vignette>(out _) == configured, "initialization does not add optional effect");
            manager.ResetEffect(PostProcessType.Vignette, 0f);
            Check(volume.profile.TryGet<Vignette>(out _) == configured, "reset without request does not add an effect");
            manager.PlayVignettePulse(Color.red, .8f, .1f, .1f, .1f);
            Check(volume.profile.TryGet<Vignette>(out var vignette), "first request resolves runtime Vignette");
            Check(vignette.intensity.overrideState && vignette.color.overrideState, "pulse enables color and intensity overrides");
            var pulse = (Sequence)typeof(PostProcessManager).GetField("vignettePulseSequence", Flags).GetValue(manager);
            pulse.Goto(.1f);
            Check(Mathf.Abs(vignette.intensity.value - .8f) < .001f && vignette.color.value == Color.red, "pulse reaches requested peak");
            pulse.Complete(true);
            float baseline = configured ? .23f : 0f;
            Color originalColor = configured ? Color.blue : Color.black;
            Check(Mathf.Abs(vignette.intensity.value - baseline) < .001f, "pulse restores baseline intensity");
            Check(vignette.intensity.overrideState == configured && !vignette.color.overrideState, "pulse restores original override states");
            Check(vignette.color.value == originalColor, "pulse restores original color");
            manager.PlayVignettePulse(Color.green, .7f, .1f, .1f, .1f);
            manager.PlayVignettePulse(Color.red, .6f, .1f, .1f, .1f);
            ((Sequence)typeof(PostProcessManager).GetField("vignettePulseSequence", Flags).GetValue(manager)).Complete(true);
            Check(Mathf.Abs(vignette.intensity.value - baseline) < .001f, "repeated pulse preserves original baseline");
            manager.ApplyEffect(PostProcessType.Vignette, .65f, .1f, false);
            ((Tween)typeof(PostProcessManager).GetField("vignetteIntensityTween", Flags).GetValue(manager)).Complete(true);
            Check(vignette.intensity.overrideState && Mathf.Abs(vignette.intensity.value - .65f) < .001f, "regular ApplyEffect enables missing intensity override");
            manager.ResetEffect(PostProcessType.Vignette, .1f);
            ((Tween)typeof(PostProcessManager).GetField("vignetteIntensityTween", Flags).GetValue(manager)).Complete(true);
            Check(Mathf.Abs(vignette.intensity.value - baseline) < .001f && vignette.intensity.overrideState == configured, "ResetEffect restores original intensity and override");
            Check(shared.TryGet<Vignette>(out var authored) == configured, "shared profile component list is untouched");
            if (configured) Check(authored.intensity.value == .23f && authored.color.value == Color.blue && authored.intensity.overrideState && !authored.color.overrideState, "authored settings remain unchanged");
            Check(errors.Count == 0, "no compatibility errors during effect requests");
        }
        finally
        {
            if (manager) Call(manager, "StopAllTweens");
            Application.logMessageReceived -= capture;
            if (volume && volume.HasInstantiatedProfile()) UnityEngine.Object.DestroyImmediate(volume.profile);
            EditorSceneManager.ClosePreviewScene(scene);
            foreach (var component in shared.components) if (component) UnityEngine.Object.DestroyImmediate(component);
            UnityEngine.Object.DestroyImmediate(shared);
        }
    }
    private static void Call(PostProcessManager manager, string method)
    { typeof(PostProcessManager).GetMethod(method, Flags).Invoke(manager, null); }
    private static void Check(bool ok, string name)
    { if (!ok) throw new Exception("VIGNETTE_COMPAT_FAILED: " + name); checks++; Debug.Log("VIGNETTE_COMPAT_CHECK: " + name); }
}
