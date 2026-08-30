using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 剧情流程中枢（静态，跨场景存活）。
/// 只记录"当前阶段"与阶段历史，不承载玩法状态；
/// 玩法进度以 MVPGameSession 为权威，结局解锁由进度钩子驱动（幂等）。
/// </summary>
public static class GameFlowManager
{
    private static FlowStage currentStage = FlowStage.None;
    private static FlowStage previousStage = FlowStage.None;
    private static int stageIndex;
    private static bool progressHookInstalled;
    private static readonly List<FlowStage> StageHistoryList = new();

    public static FlowStage CurrentStage => currentStage;
    public static FlowStage PreviousStage => previousStage;
    public static int StageIndex => stageIndex;
    public static IReadOnlyList<FlowStage> StageHistory => StageHistoryList;

    /// <summary>(previous, current) 阶段变化事件。</summary>
    public static event Action<FlowStage, FlowStage> StageChanged;

    /// <summary>
    /// 记录阶段切换。允许重复进入同一阶段（如 DailyGame 每工作日多次），
    /// 是否幂等由调用方用 IsStageReached 控制（如 EndingUnlock）。
    /// </summary>
    public static void SetStage(FlowStage stage, string reason = null)
    {
        if (stage == currentStage && stage != FlowStage.None)
        {
            return;
        }

        previousStage = currentStage;
        currentStage = stage;
        stageIndex++;
        StageHistoryList.Add(stage);
        StageChanged?.Invoke(previousStage, currentStage);
        Debug.Log(
            "[FLOW] " + previousStage + " -> " + stage +
            (string.IsNullOrEmpty(reason) ? string.Empty : "  (" + reason + ")")
        );
    }

    public static bool IsStageReached(FlowStage stage)
    {
        return StageHistoryList.Contains(stage);
    }

    /// <summary>
    /// 安装"进度达到结局阈值 → EndingUnlock"的幂等钩子。
    /// 各场景 Bootstrap 的 Awake 各调用一次即可（内部幂等重挂）。
    /// </summary>
    public static void HookProgressListener()
    {
        ProgressManager.HookSession();

        if (progressHookInstalled)
        {
            return;
        }

        progressHookInstalled = true;
        ProgressManager.ProgressChanged += HandleProgressChanged;
    }

    private static void HandleProgressChanged(int progress)
    {
        if (
            progress >= MVPGameSession.EndingProgress &&
            !MVPGameSession.EndingCompleted &&
            !IsStageReached(FlowStage.EndingUnlock)
        )
        {
            SetStage(FlowStage.EndingUnlock, "progress threshold");
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlay()
    {
        currentStage = FlowStage.None;
        previousStage = FlowStage.None;
        stageIndex = 0;
        StageHistoryList.Clear();
        progressHookInstalled = false;
        StageChanged = null;
    }
}
