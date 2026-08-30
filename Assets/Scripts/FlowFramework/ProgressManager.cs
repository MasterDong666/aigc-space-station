using System;
using UnityEngine;

/// <summary>
/// 进度门面（静态，零自有状态）。
/// 权威进度在 MVPGameSession——本类只做只读转发、事件重发与存档联动，
/// 不提供任何写进度入口（写入口只有 MVPGameSession）。
/// </summary>
public static class ProgressManager
{
    private static bool hooked;

    public static event Action<int> ProgressChanged;
    public static event Action<int> WorkdayChanged;

    public static int EarthProgress => MVPGameSession.EarthProgress;
    public static int Workday => MVPGameSession.Workday;
    public static bool IsEndingUnlocked => MVPGameSession.IsEndingUnlocked;
    public static int RestorationStage => MVPGameSession.GetRestorationStage();
    public static int CompletedTaskCount => MVPGameSession.CompletedTaskCount;
    public static bool EndingCompleted => MVPGameSession.EndingCompleted;

    /// <summary>
    /// 幂等订阅会话事件（MVPGameSession.ResetStatics 会把事件置 null，
    /// 各场景 Bootstrap 在 Awake 调用本方法即可重挂）。
    /// 每次进度变化后自动触发一次存档。
    /// </summary>
    public static void HookSession()
    {
        if (hooked)
        {
            return;
        }

        hooked = true;
        MVPGameSession.ProgressChanged += HandleProgressChanged;
        MVPGameSession.WorkdayChanged += HandleWorkdayChanged;
    }

    public static void UnhookSession()
    {
        if (!hooked)
        {
            return;
        }

        hooked = false;
        MVPGameSession.ProgressChanged -= HandleProgressChanged;
        MVPGameSession.WorkdayChanged -= HandleWorkdayChanged;
    }

    private static void HandleProgressChanged(int progress)
    {
        ProgressChanged?.Invoke(progress);
        SaveManager.TrySave();
    }

    private static void HandleWorkdayChanged(int workday)
    {
        WorkdayChanged?.Invoke(workday);
        SaveManager.TrySave();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlay()
    {
        UnhookSession();
        ProgressChanged = null;
        WorkdayChanged = null;
    }
}
