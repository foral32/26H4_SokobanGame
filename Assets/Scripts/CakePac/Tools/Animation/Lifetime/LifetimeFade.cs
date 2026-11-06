using UnityEngine;
using System.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System;

public class LifetimeFade : MonoBehaviour
{
  [Header("淡入淡出设置")]
  [SerializeField] private float fadeInDuration = 1f;
  [SerializeField] private float fadeInTargetApha = 1f;
  [SerializeField] private float fadeOutDuration = 1f;

  [SerializeField] private float lifeTime = -1f;  // -1表示永久存在

  [Header("缓动设置")]
  [SerializeField] private Ease fadeInEase = Ease.OutQuad;
  [SerializeField] private Ease fadeOutEase = Ease.InQuad;

  [Header("缩放动效设置")]
  [SerializeField] private bool enableScale_Start = true;
  [SerializeField] private float fadeInFromScale = 0.5f;
  [SerializeField] private float specifiedTargetScale = 1f;
  private Vector3 TargetScale => specifiedTargetScale != 1f ? specifiedTargetScale * Vector3.one : originalScale;
  [SerializeField] private bool enableScale_End = true;
  [SerializeField] private float fadeOutToScale = 0f;
  [SerializeField] private bool useLocalScale = true;

  private List<IFadeable> fadeTargets = new List<IFadeable>();
  private bool isInitialized = false;
  private Vector3 originalScale;
  private Sequence fadeSequence;
  private System.Threading.CancellationTokenSource cts;

  private void Awake()
  {
     cts = new System.Threading.CancellationTokenSource();
    originalScale = useLocalScale ? transform.localScale : transform.lossyScale;
    CollectFadeTargets();
  }

  private async void Start()
  {
    if (lifeTime <= 0) return;
    try
    {
        if (fadeInDuration > 0)
            await FadeIn(cts.Token);
        await Task.Delay(Mathf.RoundToInt(lifeTime * 1000), cts.Token);
        if (fadeOutDuration > 0)
            await FadeOut(cts.Token);
        if (this != null) Destroy(gameObject);
    }
    catch (OperationCanceledException)
    {
        // The object was destroyed while its lifetime animation was running.
    }
  }


  void OnDestroy()
  {
    cts?.Cancel();
    cts?.Dispose();
    fadeSequence?.Kill();
    fadeSequence = null;
    // 清理淡入淡出目标列表
    fadeTargets.Clear();
  }


  private void CollectFadeTargets()
  {
    if (isInitialized) return;
    // 收集所有支持淡入淡出的组件
    foreach (var renderer in GetComponentsInChildren<Renderer>())
    {
      fadeTargets.Add(new RendererFadeTarget(renderer));
    }
    foreach (var group in GetComponentsInChildren<CanvasGroup>())
    {
      fadeTargets.Add(new CanvasGroupFadeTarget(group));
    }
    foreach (var image in GetComponentsInChildren<UnityEngine.UI.Image>())
    {
      fadeTargets.Add(new ImageFadeTarget(image));
    }

    isInitialized = true;
  }

  public async Task FadeIn(System.Threading.CancellationToken token)
  {
    fadeSequence = DOTween.Sequence();
    foreach (var target in fadeTargets)
    {
      if (target == null) continue;
      target.SetAlpha(0f);
      fadeSequence.Join(target.FadeTo(fadeInTargetApha, fadeInDuration).SetEase(fadeInEase));
    }
    if (enableScale_Start)
    {
      Vector3 startScale = originalScale * fadeInFromScale;
      if (useLocalScale)
      {
        transform.localScale = startScale;
        fadeSequence.Join(transform.DOScale(TargetScale, fadeInDuration).SetEase(fadeInEase));
      }
      else
      {
        transform.SetGlobalScale(startScale);
        fadeSequence.Join(DOTween.To(
            () => transform.lossyScale,
            scale => transform.SetGlobalScale(scale),
            TargetScale,
            fadeInDuration
          ).SetEase(fadeInEase));
      }
    }
    await fadeSequence.AsyncWaitForCompletion();
    token.ThrowIfCancellationRequested();
  }

  public async Task FadeOut(System.Threading.CancellationToken token)
  {
    fadeSequence = DOTween.Sequence();
    foreach (var target in fadeTargets)
    {
      if (target == null) continue;
      fadeSequence.Join(target.FadeTo(0f, fadeOutDuration).SetEase(fadeOutEase));
    }
    if (enableScale_End)
    {
      Vector3 endScale = originalScale * fadeOutToScale;
      if (useLocalScale)
      {
        fadeSequence.Join(transform.DOScale(endScale, fadeOutDuration).SetEase(fadeOutEase));
      }
      else
      {
        fadeSequence.Join(DOTween.To(
            () => transform.lossyScale,
            scale => transform.SetGlobalScale(scale),
            endScale,
            fadeOutDuration
          ).SetEase(fadeOutEase));
      }
    }
    await fadeSequence.AsyncWaitForCompletion();
    token.ThrowIfCancellationRequested();
  }
}
