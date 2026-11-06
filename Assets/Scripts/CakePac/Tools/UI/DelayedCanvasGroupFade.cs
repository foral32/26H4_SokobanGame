using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;

[RequireComponent(typeof(CanvasGroup))]
public class DelayedCanvasGroupFade : ConoBehavior
{
    [Header("淡入配置")]
    [SerializeField] private bool startHidden = true;
    [SerializeField] private float showDelay = 5f; // 延迟多少秒后显示
    [SerializeField] private float fadeDuration = 0.5f; // 淡入动画持续时间
    [SerializeField] private float targetAlpha = 1f; // 目标透明度

    [Title("缩放动画")]
    [SerializeField] private bool enableScaleAnimation = false;
    [ShowIf("enableScaleAnimation")]
    [SerializeField] private float scaleAnimationDuration = 0.3f;
    [ShowIf("enableScaleAnimation")]
    [SerializeField] private float scaleMultiplier = 1.2f; // 最大缩放倍数
    [ShowIf("enableScaleAnimation")]
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    protected CanvasGroup canvasGroup;
    protected bool hasShown = false;
    protected float timer = 0f;

    protected override void ManagedInitialize()
    {
        base.ManagedInitialize();
        canvasGroup = GetComponent<CanvasGroup>();
        if (startHidden)
        {
            canvasGroup.alpha = 0f;
        }
    }

    protected virtual void Update()
    {
        if (hasShown) return;

        float currentTime = timer + Time.deltaTime;

        timer = currentTime;
        if (timer >= showDelay)
        {
            FadeIn();
        }

    }

    [Button("立即显示")]
    public virtual void FadeIn()
    {
        if (hasShown) return;
        hasShown = true;

        // 创建动画序列
        var sequence = DOTween.Sequence();

        // 添加淡入动画
        sequence.Append(canvasGroup.DOFade(targetAlpha, fadeDuration)
            .SetEase(Ease.OutQuad));

        // 如果启用了缩放动画
        if (enableScaleAnimation)
        {
            // 记录原始缩放
            Vector3 originalScale = transform.localScale;

            // 添加缩放动画
            sequence.Append(
                DOTween.Sequence()
                    .Append(transform.DOScale(originalScale * scaleMultiplier, scaleAnimationDuration * 0.5f))
                    .Append(transform.DOScale(originalScale, scaleAnimationDuration * 0.5f))
                    .SetEase(scaleEase)
            );
        }
    }

    [Button("重置状态")]
    public void Reset()
    {
        hasShown = false;
        timer = 0f;
        if (startHidden)
        {
            canvasGroup.alpha = 0f;
        }
    }
}
