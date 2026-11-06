using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using Sirenix.OdinInspector;

public class UIHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("缩放配置")]
    [SerializeField] private float hoverScale = 1.2f;
    [SerializeField] private float scaleDuration = 0.2f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    private Vector3 originalScale;
    private Tweener currentTween;

    private void Awake()
    {
        // 保存原始缩放值
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 停止当前正在进行的缩放动画
        currentTween?.Kill();
        // 播放放大动画
        currentTween = transform.DOScale(originalScale * hoverScale, scaleDuration)
            .SetEase(scaleEase);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 停止当前正在进行的缩放动画
        currentTween?.Kill();
        // 播放恢复动画
        currentTween = transform.DOScale(originalScale, scaleDuration)
            .SetEase(scaleEase);
    }

    private void OnDisable()
    {
        // 如果禁用组件时正在播放动画，立即停止并恢复原始大小
        currentTween?.Kill();
        transform.localScale = originalScale;
    }

#if UNITY_EDITOR
    [Button("测试鼠标进入效果")]
    private void TestHoverEnter()
    {
        if (Application.isPlaying)
        {
            OnPointerEnter(null);
        }
    }

    [Button("测试鼠标离开效果")]
    private void TestHoverExit()
    {
        if (Application.isPlaying)
        {
            OnPointerExit(null);
        }
    }

    [Button("重置缩放")]
    private void ResetScale()
    {
        if (Application.isPlaying)
        {
            currentTween?.Kill();
            transform.localScale = originalScale;
        }
    }
#endif
}
