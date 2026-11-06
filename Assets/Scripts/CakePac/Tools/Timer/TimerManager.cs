using System;
using System.Collections.Generic;
using Aya.Events;
using UnityEngine;
using UnityEngine.Pool;

public class TimerManager : Singleton<TimerManager>
{
    [SerializeField] private int initialPoolSize = 10;
    [SerializeField] private int maxPoolSize = 100;

    private ObjectPool<GameTimer> timerPool;
    private List<GameTimer> activeTimers = new List<GameTimer>();

    protected override void ManagedInitialize()
    {
        base.ManagedInitialize();
        InitializePool();
    }
    private void InitializePool()
    {
        timerPool = new ObjectPool<GameTimer>(
            createFunc: CreateNewTimer,
            actionOnGet: OnTimerTaken,
            actionOnRelease: OnTimerReturned,
            actionOnDestroy: OnTimerDestroyed,
            defaultCapacity: initialPoolSize,
            maxSize: maxPoolSize
        );
    }

    private GameTimer CreateNewTimer()
    {
        return new GameTimer();
    }

    private void OnTimerTaken(GameTimer timer)
    {
        activeTimers.Add(timer);
    }

    private void OnTimerReturned(GameTimer timer)
    {
        timer.InitTimer();
        activeTimers.Remove(timer);
    }

    private void OnTimerDestroyed(GameTimer timer)
    {
        activeTimers.Remove(timer);
    }

    private void Update()
    {
        UpdateActiveTimers();
    }

    private void UpdateActiveTimers()
    {
        for (int i = activeTimers.Count - 1; i >= 0; i--)
        {
            var timer = activeTimers[i];
            if (timer.TimerState == TimerState.Running)
            {
                if (timer.IsRealTime)
                {
                    timer.UpdateRealTimer();
                }
                else
                {
                    timer.UpdateTimer();
                }
            }
            else if (timer.TimerState == TimerState.Completed)
            {
                timerPool.Release(timer);
            }
        }
    }

    #region Public API

    public static GameTimer GetTimer(float time, Action action)
    {
        if (time < 0 || action == null)
        {
            Debug.LogError("Invalid timer parameters");
            return null;
        }

        var timer = Instance.timerPool.Get();
        timer.StartTimer(false, time, action);
        return timer;
    }

    public static GameTimer GetRealTimer(float time, Action action)
    {
        if (time < 0 || action == null)
        {
            Debug.LogError("Invalid timer parameters");
            return null;
        }

        var timer = Instance.timerPool.Get();
        timer.StartTimer(true, time, action);
        return timer;
    }

    public static GameTimer GetLoopTimer(float time, Action action)
    {
        if (time < 0 || action == null)
        {
            Debug.LogError("Invalid timer parameters");
            return null;
        }

        var timer = Instance.timerPool.Get();
        timer.StartTimer(false, time, action, true);
        return timer;
    }

    public static GameTimer GetRealLoopTimer(float time, Action action)
    {
        if (time < 0 || action == null)
        {
            Debug.LogError("Invalid timer parameters");
            return null;
        }

        var timer = Instance.timerPool.Get();
        timer.StartTimer(true, time, action, true);
        return timer;
    }

    public static GameTimer GetFixedFrameTimer(int frameCount, Action action)
    {
        return GetTimer(frameCount * Time.fixedDeltaTime, action);
    }

    public static void PauseTimer(GameTimer timer)
    {
        if (timer != null && Instance.activeTimers.Contains(timer))
        {
            timer.Pause();
        }
    }

    public static void ResumeTimer(GameTimer timer)
    {
        if (timer != null && Instance.activeTimers.Contains(timer))
        {
            timer.Resume();
        }
    }

    public static void CancelTimer(GameTimer timer)
    {
        if (timer != null && Instance.activeTimers.Contains(timer))
        {
            Instance.timerPool.Release(timer);
        }
    }

    public static void PauseAllTimers()
    {
        foreach (var timer in Instance.activeTimers)
        {
            timer.Pause();
        }
    }

    public static void ResumeAllTimers()
    {
        foreach (var timer in Instance.activeTimers)
        {
            timer.Resume();
        }
    }

    public static void ClearAllTimers()
    {
        foreach (var timer in Instance.activeTimers.ToArray())
        {
            Instance.timerPool.Release(timer);
        }
    }

    #endregion

    protected override void ManagedDestroy()
    {
        ClearAllTimers();
        timerPool?.Clear();
        base.ManagedDestroy();
    }
}
