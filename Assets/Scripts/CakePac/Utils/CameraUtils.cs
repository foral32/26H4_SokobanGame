using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pixelplacement;
using UnityEngine;

public enum Direction { Up, Right, Down, Left }
public static class CameraUtils
{
    public static bool IsInCameraBound(Camera cam, Vector3 pos, out Direction outDirection)
    {
        Vector3 pointOnScreen = Camera.main.WorldToScreenPoint(pos);
        pointOnScreen.x /= Screen.width;
        pointOnScreen.y /= Screen.height;

        bool isOutsideH = pointOnScreen.x < 0 || pointOnScreen.x > 1;
        bool isOutsideV = pointOnScreen.y < 0 || pointOnScreen.y > 1;

        outDirection = Direction.Up;

        if (!isOutsideH && !isOutsideV)
            return true;

        if (isOutsideH)
        {
            if (pointOnScreen.x < 0.5f)
                outDirection = Direction.Left;
            else
                outDirection = Direction.Right;
        }
        else if (isOutsideV)
        {
            if (pointOnScreen.y < 0.5f)
                outDirection = Direction.Down;
            else
                outDirection = Direction.Up;
        }

        return false;
    }

    public static void ShakeCamera(Camera cam, float magnitude = 0.05f, float duration = 0.5f, float delay = 0f)
    {
        if (cam == null) return;
        Tween.Shake(cam.transform, new Vector3(0f, 0f, cam.transform.position.z), Vector2.one * magnitude, duration, delay);
    }

    /// <summary>
    /// 顿帧效果
    /// </summary>
    /// <param name="timeScale">顿帧期间的时间流速 (0-1之间，越小越明显)</param>
    /// <param name="duration">顿帧持续时间（秒）</param>
    /// <param name="frameCount">或者指定顿帧持续的帧数，如果大于0则忽略duration参数</param>
    public static async void FreezeFrame(float timeScale = 0.1f, float duration = 0.1f, int frameCount = 0)
    {
        // 取消之前的顿帧效果
        freezeCts?.Cancel();
        freezeCts = new CancellationTokenSource();

        try
        {
            await FreezeFrameAsync(timeScale, duration, frameCount, freezeCts.Token);
        }
        catch (System.OperationCanceledException)
        {
            // 被取消是正常情况，恢复时间流速
            Time.timeScale = 1f;
        }
    }
    #region 私有
    /// <summary>
    /// 异步顿帧方法
    /// </summary>
    private static async UniTask FreezeFrameAsync(float timeScale = 0.1f, float duration = 0.1f, int frameCount = 0, CancellationToken cancellationToken = default)
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = timeScale;

        try
        {
            if (frameCount > 0)
            {
                // 按帧数计算
                for (int i = 0; i < frameCount; i++)
                {
                    await UniTask.Yield(cancellationToken);
                }
            }
            else
            {
                // 按时间计算（使用unscaledTime以免受timeScale影响）
                await UniTask.Delay(System.TimeSpan.FromSeconds(duration), ignoreTimeScale: true, cancellationToken: cancellationToken);
            }
        }
        finally
        {
            // 无论如何都要恢复时间流速
            Time.timeScale = originalTimeScale;
        }
    }
    private static CancellationTokenSource freezeCts;
    /// <summary>
    /// 立即恢复正常时间流速
    /// </summary>
    public static void RestoreTimeScale()
    {
        freezeCts?.Cancel();
        Time.timeScale = 1f;
    }
    #endregion
}

