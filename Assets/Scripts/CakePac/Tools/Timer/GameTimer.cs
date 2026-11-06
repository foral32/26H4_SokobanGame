using System;
using UnityEngine;

public enum TimerState
{
    Idle,        // 空闲状态
    Running,     // 运行中
    Paused,      // 已暂停
    Completed    // 已完成
}

public class GameTimer
{
    private float timer;
    private float maxTime;
    private Action timerAction;
    private float pausedTimeRemaining;

    public bool IsRealTime { get; private set; }
    public TimerState TimerState { get; private set; }
    public bool IsLoop { get; private set; }

    // 添加进度属性
    public float Progress => 1 - (timer / maxTime);
    public float TimeRemaining => timer;

    public void StartTimer(bool isRealTime, float time, Action action, bool isLoop = false)
    {
        if (time < 0)
        {
            Debug.LogError("Timer time cannot be negative");
            return;
        }

        IsRealTime = isRealTime;
        timer = time;
        maxTime = time;
        timerAction = action;
        TimerState = TimerState.Running;
        IsLoop = isLoop;
    }

    public void Pause()
    {
        if (TimerState == TimerState.Running)
        {
            pausedTimeRemaining = timer;
            TimerState = TimerState.Paused;
        }
    }

    public void Resume()
    {
        if (TimerState == TimerState.Paused)
        {
            timer = pausedTimeRemaining;
            TimerState = TimerState.Running;
        }
    }

    public void UpdateTimer()
    {
        if (TimerState != TimerState.Running) return;

        timer -= Time.deltaTime;
        CheckComplete();
    }

    public void UpdateRealTimer()
    {
        if (TimerState != TimerState.Running) return;

        timer -= Time.unscaledDeltaTime;
        CheckComplete();
    }

    private void CheckComplete()
    {
        if (timer <= 0)
        {
            timerAction?.Invoke();
            if (IsLoop)
            {
                timer = maxTime;
            }
            else
            {
                TimerState = TimerState.Completed;
            }
        }
    }

    public void InitTimer()
    {
        timer = 0;
        maxTime = 0;
        timerAction = null;
        TimerState = TimerState.Idle;
        IsRealTime = false;
        IsLoop = false;
        pausedTimeRemaining = 0;
    }
}
