using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class LifetimeAlpha : LifetimeBehavior
{
  [Header("透明度设置")]
  [SerializeField] private float fromAlpha = 0f;
  [SerializeField] private float toAlpha = 1f;

  [Header("缓动设置")]
  [SerializeField] private Ease alphaEase = Ease.InOutQuad;

  private Sequence alphaSequence;
  private Material[] materials;
  private CanvasGroup canvasGroup;

  protected override void Awake()
  {
    base.Awake();
    // 获取需要控制的组件
    canvasGroup = GetComponent<CanvasGroup>();
    if (!canvasGroup)
    {
      var renderers = GetComponentsInChildren<Renderer>();
      var materialsList = new List<Material>();
      foreach (var renderer in renderers)
      {
        materialsList.AddRange(renderer.materials);
      }
      materials = materialsList.ToArray();
    }
  }

  private void Start()
  {
    // 设置初始透明度
    SetAlpha(fromAlpha);

    // 创建透明度动画序列
    alphaSequence = DOTween.Sequence();

    if (canvasGroup)
    {
      alphaSequence.Append(canvasGroup.DOFade(toAlpha, lifeTime)
          .SetEase(alphaEase));
    }
    else if (materials != null && materials.Length > 0)
    {
      foreach (var material in materials)
      {
        alphaSequence.Join(material.DOFade(toAlpha, lifeTime)
            .SetEase(alphaEase));
      }
    }
  }

  private void SetAlpha(float alpha)
  {
    if (canvasGroup)
    {
      canvasGroup.alpha = alpha;
    }
    else if (materials != null)
    {
      foreach (var material in materials)
      {
        var color = material.color;
        color.a = alpha;
        material.color = color;
      }
    }
  }

  private void OnDestroy()
  {
    alphaSequence?.Kill();
  }
}