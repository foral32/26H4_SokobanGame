using UnityEngine;
using DG.Tweening;

public class LifetimeScale : LifetimeBehavior

{
    [Header("缩放设置")]
    [SerializeField] private float fromScale = 0f;
    [SerializeField] private float toScale = 2f;
    public float ToScale { get => toScale; set => toScale = value; }
    [SerializeField] private bool useLocalScale = true;

    [Header("缓动设置")]
    [SerializeField] private Ease scaleEase = Ease.InOutQuad;
    private Sequence scaleSequence;

    private void Start()
    {
        // 设置初始缩放
        SetScale(fromScale);

        // 创建缩放动画序列
        scaleSequence = DOTween.Sequence();

        if (useLocalScale)
        {
            scaleSequence.Append(transform.DOScale(Vector3.one * toScale, lifeTime)
                .SetEase(scaleEase));
        }
        else
        {
            scaleSequence.Append(DOTween.To(
                () => transform.lossyScale,
                scale => transform.SetGlobalScale(scale),
                Vector3.one * toScale,
                lifeTime
            ).SetEase(scaleEase));
        }

    }

    private void SetScale(float scale)
    {
        if (useLocalScale)
        {
            transform.localScale = Vector3.one * scale;
        }
        else
        {
            transform.SetGlobalScale(Vector3.one * scale);
        }
    }

    private void OnDestroy()
    {
        scaleSequence?.Kill();
    }
}