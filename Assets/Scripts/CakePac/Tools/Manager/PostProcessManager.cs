using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections.Generic;

public enum PostProcessType
{
    Vignette,
    ChromaticAberration,
    Bloom,
    LensDistortion,
    // ColorAdjustments is handled separately
}

/// <summary>
/// 用于指定要修改的ColorAdjustments的特定浮点参数。
/// </summary>
public enum ColorAdjustmentParameter
{
    Contrast,
    HueShift,
    Saturation,
    PostExposure,
}

/// <summary>
/// 后处理管理器 - 控制各种后处理效果和动画
/// </summary>
public class PostProcessManager : Singleton<PostProcessManager>
{
    [BoxGroup("核心配置")]
    [Required] public Volume postProcessVolume;

    #region 后处理组件引用
    private VolumeProfile profile;
    private Vignette vignette;
    private ColorAdjustments colorAdjustments;
    private ChromaticAberration chromaticAberration;
    private LensDistortion lensDistortion;
    private Bloom bloom;
    private DepthOfField depthOfField;
    #endregion

    #region 效果配置
    [BoxGroup("通用配置")]
    [LabelText("默认过渡时间")] public float defaultTransitionTime = 0.5f;
    #endregion

    #region 运行时状态
    private Dictionary<string, float> originalValues = new Dictionary<string, float>();
    private Dictionary<string, object> originalColors = new Dictionary<string, object>();
    private Dictionary<string, Tween> activeTweens = new Dictionary<string, Tween>();
    private Tween vignetteIntensityTween;
    private Tween vignetteRecoveryTween;
    private Sequence vignettePulseSequence;
    private bool vignettePulseActive;
    private float originalVignetteIntensity;
    private bool originalVignetteIntensityOverride;
    private float vignettePulseBaseIntensity;
    private Color vignettePulseBaseColor;
    private bool vignettePulseBaseIntensityOverride;
    private bool vignettePulseBaseColorOverride;
    #endregion

    protected override void ManagedInitialize()
    {
        base.ManagedInitialize();
        InitializePostProcess();
        SubscribeToEvents();
    }

    protected override void ManagedDestroy()
    {
        base.ManagedDestroy();
        UnsubscribeFromEvents();
        StopAllTweens();
    }
    void OnValidate()
    {
        if (postProcessVolume == null) postProcessVolume = transform.GetComponentInChildren<Volume>();
    }
    private void InitializePostProcess()
    {
        if (postProcessVolume == null) postProcessVolume = transform.GetComponentInChildren<Volume>();
        if (postProcessVolume == null)
        {
            Debug.LogError("[PostProcessManager] Volume引用为空!");
            return;
        }

        profile = postProcessVolume.profile;
        if (profile == null)
        {
            Debug.LogError("[PostProcessManager] Volume Profile为空!");
            return;
        }

        // 获取后处理组件
        profile.TryGet(out vignette);
        profile.TryGet(out colorAdjustments);
        profile.TryGet(out chromaticAberration);
        profile.TryGet(out lensDistortion);
        profile.TryGet(out bloom);
        profile.TryGet(out depthOfField);

        // 暗角是可选配置；请求效果时才在运行时 Profile 副本补齐。
        originalVignetteIntensity = vignette != null ? vignette.intensity.value : 0f;
        originalVignetteIntensityOverride = vignette != null && vignette.intensity.overrideState;

        // 保存原始值
        StoreOriginalValues();
    }

    private void StoreOriginalValues()
    {
        // Store ColorAdjustments values
        if (colorAdjustments != null)
        {
            originalValues[GetParamKey(ColorAdjustmentParameter.Saturation)] = colorAdjustments.saturation.value;
            originalValues[GetParamKey(ColorAdjustmentParameter.Contrast)] = colorAdjustments.contrast.value;
            originalValues[GetParamKey(ColorAdjustmentParameter.HueShift)] = colorAdjustments.hueShift.value;
            originalValues[GetParamKey(ColorAdjustmentParameter.PostExposure)] = colorAdjustments.postExposure.value;
            originalColors[nameof(ColorAdjustments.colorFilter)] = colorAdjustments.colorFilter.value;
        }

        // Store other effects' values. Vignette keeps its color and override state separately.
        if (chromaticAberration != null)
            originalValues[nameof(ChromaticAberration)] = chromaticAberration.intensity.value;

        if (bloom != null)
            originalValues[nameof(Bloom)] = bloom.intensity.value;

        if (lensDistortion != null)
            originalValues[nameof(LensDistortion)] = lensDistortion.intensity.value;
    }

    #region 事件订阅
    private void SubscribeToEvents()
    {
        // 这里可以订阅各种游戏事件
    }

    private void UnsubscribeFromEvents()
    {
        // 取消事件订阅
    }
    #endregion

    #region 公共接口

    /// <summary>
    /// 应用一个独立的后处理效果 (Vignette, Bloom, etc.)
    /// </summary>
    public void ApplyEffect(PostProcessType type, float intensity, float duration = -1, bool autoRecovery = true, float recoveryDelay_AfterApply = 0, float recoveryDuration = 0.3f)
    {
        if (duration < 0) duration = defaultTransitionTime;
        if (type == PostProcessType.Vignette)
        {
            ApplyVignetteIntensity(intensity, duration, autoRecovery,
                recoveryDelay_AfterApply, recoveryDuration);
            return;
        }

        string tweenKey = type.ToString();
        StopTween(tweenKey);

        Tween tween = null;
        switch (type)
        {
            case PostProcessType.ChromaticAberration:
                if (chromaticAberration != null)
                    tween = DOTween.To(() => chromaticAberration.intensity.value, x => chromaticAberration.intensity.value = x, intensity, duration);
                break;
            case PostProcessType.Bloom:
                if (bloom != null)
                    tween = DOTween.To(() => bloom.intensity.value, x => bloom.intensity.value = x, intensity, duration);
                break;
            case PostProcessType.LensDistortion:
                if (lensDistortion != null)
                    tween = DOTween.To(() => lensDistortion.intensity.value, x => lensDistortion.intensity.value = x, intensity, duration);
                break;
        }

        if (tween != null)
        {
            activeTweens[tweenKey] = tween;
            if (autoRecovery)
            {
                DOVirtual.DelayedCall(duration + recoveryDelay_AfterApply,
                    () => ResetEffect(type, recoveryDuration));
            }
        }
    }

    /// <summary>
    /// 重置一个独立的后处理效果
    /// </summary>
    public void ResetEffect(PostProcessType type, float duration = -1)
    {
        if (duration < 0) duration = defaultTransitionTime;
        if (type == PostProcessType.Vignette)
        {
            ResetVignetteIntensity(duration);
            return;
        }

        string key = type.ToString();
        if (!originalValues.ContainsKey(key)) return;

        float originalValue = originalValues[key];
        Tween tween = null;

        switch (type)
        {
            case PostProcessType.ChromaticAberration:
                if (chromaticAberration != null)
                    tween = DOTween.To(() => chromaticAberration.intensity.value, x => chromaticAberration.intensity.value = x, originalValue, duration);
                break;
            case PostProcessType.Bloom:
                if (bloom != null)
                    tween = DOTween.To(() => bloom.intensity.value, x => bloom.intensity.value = x, originalValue, duration);
                break;
            case PostProcessType.LensDistortion:
                if (lensDistortion != null)
                    tween = DOTween.To(() => lensDistortion.intensity.value, x => lensDistortion.intensity.value = x, originalValue, duration);
                break;
        }
        if (tween != null) activeTweens[key] = tween;
    }

    private bool EnsureVignette()
    {
        if (vignette != null) return true;
        if (profile == null) return false;
        if (!profile.TryGet(out vignette))
        {
            // InitializePostProcess 使用 Volume.profile 实例，不修改 sharedProfile 资产。
            vignette = profile.Add<Vignette>(false);
            vignette.intensity.value = 0f;
        }
        originalVignetteIntensity = vignette.intensity.value;
        originalVignetteIntensityOverride = vignette.intensity.overrideState;
        return true;
    }

    /// <summary>播放一次临时暗角脉冲；结束后恢复调用前的强度、颜色和覆盖状态。</summary>
    public void PlayVignettePulse(Color color, float peakIntensity, float fadeInDuration,
        float holdDuration, float fadeOutDuration)
    {
        if (!EnsureVignette()) return;

        if (vignettePulseActive)
        {
            vignettePulseSequence?.Kill();
        }
        else
        {
            StopVignetteRecovery();
            StopVignetteIntensityTween();
            vignettePulseBaseIntensity = vignette.intensity.value;
            vignettePulseBaseColor = vignette.color.value;
            vignettePulseBaseIntensityOverride = vignette.intensity.overrideState;
            vignettePulseBaseColorOverride = vignette.color.overrideState;
        }

        vignette.intensity.overrideState = true;
        vignette.color.overrideState = true;
        vignettePulseActive = true;

        float fadeIn = Mathf.Max(0f, fadeInDuration);
        float hold = Mathf.Max(0f, holdDuration);
        float fadeOut = Mathf.Max(0f, fadeOutDuration);
        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Join(DOTween.To(() => vignette.intensity.value,
            value => vignette.intensity.value = value, Mathf.Clamp01(peakIntensity), fadeIn));
        sequence.Join(DOTween.To(() => vignette.color.value,
            value => vignette.color.value = value, color, fadeIn));
        sequence.AppendInterval(hold);
        sequence.Append(DOTween.To(() => vignette.intensity.value,
            value => vignette.intensity.value = value, vignettePulseBaseIntensity, fadeOut));
        sequence.Join(DOTween.To(() => vignette.color.value,
            value => vignette.color.value = value, vignettePulseBaseColor, fadeOut));
        sequence.OnComplete(() => CompleteVignettePulse(sequence));
        vignettePulseSequence = sequence;
    }

    private void ApplyVignetteIntensity(float intensity, float duration, bool autoRecovery,
        float recoveryDelay, float recoveryDuration)
    {
        if (!EnsureVignette()) return;

        CancelVignettePulseAndRestore();
        StopVignetteRecovery();
        StopVignetteIntensityTween();
        vignette.intensity.overrideState = true;
        vignetteIntensityTween = DOTween.To(() => vignette.intensity.value,
            value => vignette.intensity.value = value, intensity, Mathf.Max(0f, duration));

        if (!autoRecovery) return;

        Tween scheduledRecovery = null;
        scheduledRecovery = DOVirtual.DelayedCall(
            Mathf.Max(0f, duration) + Mathf.Max(0f, recoveryDelay),
            () =>
            {
                if (vignetteRecoveryTween == scheduledRecovery) vignetteRecoveryTween = null;
                ResetVignetteIntensity(recoveryDuration);
            });
        vignetteRecoveryTween = scheduledRecovery;
    }

    private void ResetVignetteIntensity(float duration)
    {
        if (vignette == null) return;

        CancelVignettePulseAndRestore();
        StopVignetteRecovery();
        StopVignetteIntensityTween();
        vignetteIntensityTween = DOTween.To(() => vignette.intensity.value,
            value => vignette.intensity.value = value, originalVignetteIntensity, Mathf.Max(0f, duration))
            .OnComplete(() => { if (vignette != null) vignette.intensity.overrideState = originalVignetteIntensityOverride; });
    }

    private void CompleteVignettePulse(Sequence sequence)
    {
        if (!vignettePulseActive || vignettePulseSequence != sequence || vignette == null) return;
        RestoreVignettePulseBase();
        vignettePulseSequence = null;
        vignettePulseActive = false;
    }

    private void CancelVignettePulseAndRestore()
    {
        if (!vignettePulseActive) return;
        Sequence sequence = vignettePulseSequence;
        vignettePulseSequence = null;
        sequence?.Kill();
        RestoreVignettePulseBase();
        vignettePulseActive = false;
    }

    private void RestoreVignettePulseBase()
    {
        if (vignette == null) return;
        vignette.intensity.value = vignettePulseBaseIntensity;
        vignette.color.value = vignettePulseBaseColor;
        vignette.intensity.overrideState = vignettePulseBaseIntensityOverride;
        vignette.color.overrideState = vignettePulseBaseColorOverride;
    }

    private void StopVignetteIntensityTween()
    {
        vignetteIntensityTween?.Kill();
        vignetteIntensityTween = null;
    }

    private void StopVignetteRecovery()
    {
        vignetteRecoveryTween?.Kill();
        vignetteRecoveryTween = null;
    }

    private void StopVignetteTransitions()
    {
        StopVignetteRecovery();
        CancelVignettePulseAndRestore();
        StopVignetteIntensityTween();
    }

    /// <summary>
    /// 应用一个ColorAdjustments的浮点参数效果
    /// </summary>
    public void ApplyColorAdjustment(ColorAdjustmentParameter param, float intensity, float duration = -1, bool autoRecovery = true, float recoveryDelay_AfterApply = 0, float recoveryDuration = 0.3f)
    {
        if (colorAdjustments == null) return;
        if (duration < 0) duration = defaultTransitionTime;

        string tweenKey = GetParamKey(param);
        StopTween(tweenKey);

        Tween tween = null;
        switch (param)
        {
            case ColorAdjustmentParameter.Contrast:
                tween = DOTween.To(() => colorAdjustments.contrast.value, x => colorAdjustments.contrast.value = x, intensity, duration);
                break;
            case ColorAdjustmentParameter.HueShift:
                tween = DOTween.To(() => colorAdjustments.hueShift.value, x => colorAdjustments.hueShift.value = x, intensity, duration);
                break;
            case ColorAdjustmentParameter.Saturation:
                tween = DOTween.To(() => colorAdjustments.saturation.value, x => colorAdjustments.saturation.value = x, intensity, duration);
                break;
            case ColorAdjustmentParameter.PostExposure:
                tween = DOTween.To(() => colorAdjustments.postExposure.value, x => colorAdjustments.postExposure.value = x, intensity, duration);
                break;
        }

        if (tween != null)
        {
            activeTweens[tweenKey] = tween;
            if (autoRecovery)
            {
                DOVirtual.DelayedCall(duration + recoveryDelay_AfterApply, () => ResetColorAdjustment(param, recoveryDuration));
            }
        }
    }

    /// <summary>
    /// 应用颜色滤镜效果
    /// </summary>
    public void ApplyColorFilter(Color targetColor, float duration = -1, bool autoRecovery = true, float recoveryDelay_AfterApply = 0, float recoveryDuration = 0.3f)
    {
        if (colorAdjustments == null) return;
        if (duration < 0) duration = defaultTransitionTime;

        string tweenKey = nameof(ColorAdjustments.colorFilter);
        StopTween(tweenKey);
        Debug.Log("Applying Color Filter to " + targetColor.ToString());
        var tween = DOTween.To(() => colorAdjustments.colorFilter.value, x => colorAdjustments.colorFilter.value = x, targetColor, duration);
        activeTweens[tweenKey] = tween;

        if (autoRecovery)
        {
            DOVirtual.DelayedCall(duration + recoveryDelay_AfterApply, () => ResetColorFilter(recoveryDuration));
        }
    }

    /// <summary>
    /// 重置指定的ColorAdjustments浮点参数
    /// </summary>
    public void ResetColorAdjustment(ColorAdjustmentParameter param, float duration = -1)
    {
        if (colorAdjustments == null) return;
        if (duration < 0) duration = defaultTransitionTime;

        string key = GetParamKey(param);
        if (!originalValues.ContainsKey(key)) return;

        float originalValue = originalValues[key];
        Tween tween = null;

        switch (param)
        {
            case ColorAdjustmentParameter.Contrast:
                tween = DOTween.To(() => colorAdjustments.contrast.value, x => colorAdjustments.contrast.value = x, originalValue, duration);
                break;
            case ColorAdjustmentParameter.HueShift:
                tween = DOTween.To(() => colorAdjustments.hueShift.value, x => colorAdjustments.hueShift.value = x, originalValue, duration);
                break;
            case ColorAdjustmentParameter.Saturation:
                tween = DOTween.To(() => colorAdjustments.saturation.value, x => colorAdjustments.saturation.value = x, originalValue, duration);
                break;
            case ColorAdjustmentParameter.PostExposure:
                tween = DOTween.To(() => colorAdjustments.postExposure.value, x => colorAdjustments.postExposure.value = x, originalValue, duration);
                break;
        }
        if (tween != null) activeTweens[key] = tween;
    }

    /// <summary>
    /// 重置颜色滤镜
    /// </summary>
    public void ResetColorFilter(float duration = -1)
    {
        if (colorAdjustments == null) return;
        if (duration < 0) duration = defaultTransitionTime;
        string key = nameof(ColorAdjustments.colorFilter);
        if (!originalColors.ContainsKey(key)) return;

        var originalColor = (Color)originalColors[key];
        var tween = DOTween.To(() => colorAdjustments.colorFilter.value, x => colorAdjustments.colorFilter.value = x, originalColor, duration);
        activeTweens[key] = tween;
    }

    /// <summary>
    /// 重置所有效果
    /// </summary>
    public void ResetAllEffects(float duration = -1)
    {
        if (duration < 0) duration = defaultTransitionTime;
        StopAllTweens();

        // 重置所有ColorAdjustments参数
        ResetColorFilter(duration);
        foreach (ColorAdjustmentParameter param in System.Enum.GetValues(typeof(ColorAdjustmentParameter)))
        {
            ResetColorAdjustment(param, duration);
        }

        // 重置其他独立效果
        foreach (PostProcessType type in System.Enum.GetValues(typeof(PostProcessType)))
        {
            ResetEffect(type, duration);
        }
    }
    #endregion

    #region 工具方法
    private string GetParamKey(ColorAdjustmentParameter param) => $"{nameof(ColorAdjustments)}_{param}";

    private void StopTween(string key)
    {
        if (activeTweens.ContainsKey(key))
        {
            activeTweens[key]?.Kill();
            activeTweens.Remove(key);
        }
    }

    private void StopAllTweens()
    {
        StopVignetteTransitions();
        foreach (var tween in activeTweens.Values)
            tween?.Kill();
        activeTweens.Clear();
    }
    #endregion

    #region 编辑器调试
    [FoldoutGroup("编辑器调试/Color Adjustments")]
    [Button("测试 ColorFilter (蓝色)")]
    private void EditorTestColorFilter()
    {
        if (!Application.isPlaying) return;
        ApplyColorFilter(Color.blue, 0.5f, true, 1f, 0.5f);
    }

    [FoldoutGroup("编辑器调试/Color Adjustments")]
    [Button("测试 Saturation (饱和度)")]
    private void EditorTestSaturation()
    {
        if (!Application.isPlaying) return;
        ApplyColorAdjustment(ColorAdjustmentParameter.Saturation, -100f, 0.5f, true, 1f, 0.5f);
    }

    [FoldoutGroup("编辑器调试/Color Adjustments")]
    [Button("测试 Contrast (对比度)")]
    private void EditorTestContrast()
    {
        if (!Application.isPlaying) return;
        ApplyColorAdjustment(ColorAdjustmentParameter.Contrast, 50f, 0.5f, true, 1f, 0.5f);
    }

    [FoldoutGroup("编辑器调试/Color Adjustments")]
    [Button("测试 HueShift (色相)")]
    private void EditorTestHueShift()
    {
        if (!Application.isPlaying) return;
        ApplyColorAdjustment(ColorAdjustmentParameter.HueShift, 90f, 0.5f, true, 1f, 0.5f);
    }

    [FoldoutGroup("编辑器调试/Other Effects")]
    [Button("测试 Vignette 效果")]
    private void EditorTestVignetteEffect()
    {
        if (!Application.isPlaying) return;
        ApplyEffect(PostProcessType.Vignette, 0.6f, 0.3f, true, 0.5f, 0.5f);
    }

    [FoldoutGroup("编辑器调试/Other Effects")]
    [Button("测试 Chromatic Aberration 效果")]
    private void EditorTestChromaticAberrationEffect()
    {
        if (!Application.isPlaying) return;
        ApplyEffect(PostProcessType.ChromaticAberration, 1f, 0.1f, true, 0.1f, 0.3f);
    }

    [FoldoutGroup("编辑器调试/Other Effects")]
    [Button("测试死亡效果 (饱和度+暗角)")]
    private void EditorTestDeathEffect()
    {
        if (!Application.isPlaying) return;
        ApplyColorAdjustment(ColorAdjustmentParameter.Saturation, -100f, 2f, false);
        ApplyEffect(PostProcessType.Vignette, 0.8f, 2f, false);
    }

    [FoldoutGroup("编辑器调试")]
    [Button("重置所有效果", ButtonSizes.Large), GUIColor(0.2f, 0.8f, 0.2f)]
    private void EditorResetAllEffects()
    {
        if (!Application.isPlaying) return;
        ResetAllEffects();
    }
    #endregion
}
