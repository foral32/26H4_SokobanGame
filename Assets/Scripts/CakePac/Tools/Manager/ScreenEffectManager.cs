using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ScreenEffectManager : Singleton<ScreenEffectManager>
{
    [Header("Flash Effect")]
    [SerializeField] private CanvasGroup flashCanvasGroup;
    [SerializeField] private Image flashImage;

    protected override void ManagedInitialize()
    {
        base.ManagedInitialize();
        SetupUI();
    }

    private void SetupUI()
    {
        if (flashCanvasGroup == null)
        {
            // 创建Canvas
            GameObject canvasGO = new GameObject("FlashCanvas");
            canvasGO.transform.SetParent(transform);

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            canvasGO.AddComponent<CanvasScaler>();

            // 创建CanvasGroup
            flashCanvasGroup = canvasGO.AddComponent<CanvasGroup>();
            flashCanvasGroup.alpha = 0f;
            flashCanvasGroup.blocksRaycasts = false;

            // 创建Image
            GameObject imageGO = new GameObject("FlashImage");
            imageGO.transform.SetParent(canvasGO.transform, false);

            flashImage = imageGO.AddComponent<Image>();
            flashImage.color = Color.white;

            // 设置全屏
            RectTransform rect = flashImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }
    }

    #region 闪白效果
    /// <summary>
    /// 屏幕闪烁效果
    /// </summary>
    public static void Flash(float fadeInDuration = 0.1f, float maxAlpha = 1f, float hold = 0.1f,
                           float fadeOutDuration = 0.3f, Color? color = null)
    {
        Instance.DoFlash(fadeInDuration, maxAlpha, hold, fadeOutDuration, color ?? Color.white);
    }

    /// <summary>
    /// 快速闪白
    /// </summary>
    public static void QuickFlash() => Flash(0.05f, 0.8f, 0.05f, 0.2f);

    /// <summary>
    /// 伤害闪红
    /// </summary>
    public static void DamageFlash() => Flash(0.05f, 0.6f, 0.1f, 0.3f, Color.red);

    private void DoFlash(float fadeIn, float maxAlpha, float hold, float fadeOut, Color color)
    {
        flashCanvasGroup.DOKill();
        flashImage.color = color;

        DOTween.Sequence()
            .Append(flashCanvasGroup.DOFade(maxAlpha, fadeIn))
            .AppendInterval(hold)
            .Append(flashCanvasGroup.DOFade(0f, fadeOut));
    }

    private void DoFade(Color color, float fromAlpha, float toAlpha, float duration)
    {
        flashCanvasGroup.DOKill();
        flashImage.color = color;
        flashCanvasGroup.alpha = fromAlpha;
        flashCanvasGroup.DOFade(toAlpha, duration);
    }
    #endregion

    /// <summary>
    /// 停止所有效果
    /// </summary>
    public static void Stop()
    {
        if (Instance != null)
        {
            Instance.flashCanvasGroup.DOKill();
            Instance.flashCanvasGroup.alpha = 0f;
        }
    }
}