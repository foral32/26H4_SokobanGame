using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine;

public abstract class SineWaveTransform : ManagedBehaviour
{
    [SerializeField] protected bool Active = true; // 是否启用正弦波动画
    [SerializeField] protected bool ResetOnDisable = true; // 是否在禁用时重置位置
    [SerializeField, ShowIf(nameof(ResetOnDisable))] protected float resetTweenDuration = 0.1f;

    [Header("Sine Wave Settings")]
    public Vector3 magnitude = default;
    public Vector2 sinRemapTo = new Vector2(-1, 1f);
    public float speed = default;
    [Header("Random Time Offset")]
    public float timeOffset = default;
    public bool useRandomTimeOffset = false;
    public Vector2 randomTimeOffsetRange = new Vector2(0f, 1f);

    protected Vector3 originalMagnitude;
    private float actualTimeOffset;

    protected override void ManagedInitialize()
    {
        base.ManagedInitialize();
        originalMagnitude = GetMagnitude();

        // 初始化实际使用的时间偏移
        if (useRandomTimeOffset)
        {
            actualTimeOffset = Random.Range(randomTimeOffsetRange.x, randomTimeOffsetRange.y);
        }
        else
        {
            actualTimeOffset = timeOffset;
        }
    }

    public override void ManagedUpdate()
    {
        if (!Active) return; // 如果不启用正弦波动画，直接返回
        float sine = Mathf.Sin(Time.time * speed + actualTimeOffset);
        // sine映射到sinRemapTo范围内
        float clampedSine = Mathf.Lerp(sinRemapTo.x, sinRemapTo.y, (sine + 1f) / 2f);
        float x = originalMagnitude.x + (clampedSine * magnitude.x);
        float y = originalMagnitude.y + (clampedSine * magnitude.y);
        float z = originalMagnitude.z + (clampedSine * magnitude.z);
        ApplyTransformation(new Vector3(x, y, z));
    }

    protected abstract Vector3 GetMagnitude();
    protected abstract void ApplyTransformation(Vector3 value);

    public virtual void SetActive(bool active)
    {
        Active = active;
        if (!active)
        {
            // 恢复到原始值，tween一下
            if (ResetOnDisable)
            {
                Vector3 current = GetMagnitude();
                Vector3 target = originalMagnitude;
                float duration = resetTweenDuration;

                // 使用DOTween进行重置动画
                if (duration > 0)
                {
                    DOTween.Kill(this, "SineWaveTransformReset");
                    DOTween.To(() => current, v =>
                    {
                        if (this == null || gameObject == null) return;
                        ApplyTransformation(v);
                    }, target, duration)
                    .SetEase(DG.Tweening.Ease.OutQuad)
                    .SetId("SineWaveTransformReset");
                }
                else
                {
                    if (this != null && gameObject != null)
                        ApplyTransformation(target); // 直接应用目标值
                }
            }
        }
    }

    // DOTween版本无需协程重置
    protected virtual void OnDestroy()
    {
        DOTween.Kill(this, "SineWaveTransformReset");
    }
}
