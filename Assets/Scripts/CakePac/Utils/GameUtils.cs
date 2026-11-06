using UnityEngine;

public class GameUtils
{
    #region 日志开关
    public static bool EnableLog { get; private set; } = true;

    /// <summary>切换全局游戏日志；所有 GameUtils 日志接口都会读取该开关。</summary>
    public static void ToggleLog()
    {
        EnableLog = !EnableLog;
    }
    #endregion

    #region 日志接口
    public static void WTF(string msg)
    {
        if (!EnableLog) return;
        Debug.LogFormat($"日志内容:<color=#ff0000> --->   {msg}   <--- </color>");
    }

    public static void Warn(string msg)
    {
        if (!EnableLog) return;
        Debug.LogFormat($"日志内容:<color=#ffff00> --->   {msg}   <--- </color>");
    }

    public static void Log(string msg)
    {
        if (!EnableLog) return;
        Debug.LogFormat($" {msg}");
    }

    public static void EnsureValid(params object[] args)
    {
        if (!EnableLog) return;

        foreach (var arg in args)
        {
            if (arg == null)
            {
                Debug.LogFormat($"日志内容:<color=#ffff00> --->  非空Ensure判定失败！  <--- </color>");
                return;
            }
        }
    }
    #endregion
}
