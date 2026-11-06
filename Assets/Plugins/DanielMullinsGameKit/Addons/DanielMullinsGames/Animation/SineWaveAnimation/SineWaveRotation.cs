using UnityEngine;
using DG.Tweening;

public class SineWaveRotation : SineWaveTransform
{
    private Vector3 _lastRotation;

    protected override void ManagedInitialize()
    {
        base.ManagedInitialize();
        _lastRotation = transform.localEulerAngles;
    }

    protected override Vector3 GetMagnitude()
    {
        return transform.localEulerAngles;
    }

    protected override void ApplyTransformation(Vector3 value)
    {
        transform.localEulerAngles = value;
        _lastRotation = value;
    }

    public override void SetActive(bool active)
    {
        if (!active && ResetOnDisable)
        {
            // 特殊处理旋转重置
            Vector3 currentRotation = transform.localEulerAngles;
            Vector3 targetRotation = originalMagnitude;

            // 确保使用最短路径旋转
            Vector3 shortestRotation = new Vector3(
                Mathf.DeltaAngle(currentRotation.x, targetRotation.x),
                Mathf.DeltaAngle(currentRotation.y, targetRotation.y),
                Mathf.DeltaAngle(currentRotation.z, targetRotation.z)
            );

            // 计算目标角度
            Vector3 finalRotation = currentRotation + shortestRotation;

            // 使用DOTween执行旋转
            if (resetTweenDuration > 0)
            {
                DOTween.Kill(this, "SineWaveRotationReset");
                transform.DOLocalRotate(finalRotation, resetTweenDuration)
                    .SetEase(Ease.OutQuad)
                    .SetId("SineWaveRotationReset");
            }
            else
            {
                transform.localEulerAngles = finalRotation;
            }
        }

        Active = active;
    }

    protected override void OnDestroy()
    {
        DOTween.Kill(this, "SineWaveRotationReset");
        base.OnDestroy();
    }
}
