using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局游戏进度：地球修复进度 + 各任务完成记录。
/// Stage 1 仅进程内状态（同一运行内防重复），暂不做磁盘存档；
/// 后续接入任务 2 / 任务 3 时复用 TryCompleteTask 即可。
/// </summary>
public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    [Header("进度设置")]
    [SerializeField] private int earthProgress = 10;

    private readonly HashSet<string> completedTasks = new HashSet<string>();

    /// <summary>地球修复进度变化时触发（参数为最新进度）。</summary>
    public event Action<int> ProgressChanged;

    public int EarthProgress => earthProgress;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        earthProgress += MVPGameSession.CompletedTaskCount * 5;
    }

    public bool IsTaskCompleted(string taskId)
    {
        return completedTasks.Contains(taskId) ||
            (TryResolveMiniGameId(taskId, out MiniGameId id) &&
             MVPGameSession.IsTaskCompleted(id));
    }

    /// <summary>
    /// 尝试完成任务并发放奖励。
    /// 同一次运行内重复完成同一任务返回 false，不重复发放奖励。
    /// </summary>
    public bool TryCompleteTask(string taskId, int reward)
    {
        if (IsTaskCompleted(taskId))
        {
            return false;
        }

        completedTasks.Add(taskId);

        if (TryResolveMiniGameId(taskId, out MiniGameId id))
        {
            MVPGameSession.ReportTaskCompleted(id);
        }

        earthProgress += reward;
        ProgressChanged?.Invoke(earthProgress);
        return true;
    }

    private static bool TryResolveMiniGameId(
        string taskId,
        out MiniGameId id
    )
    {
        if (taskId == OrbitCalibrationConfig.TaskId)
        {
            id = MiniGameId.OrbitInspection;
            return true;
        }

        if (taskId == GeneCultivationConfig.TaskId)
        {
            id = MiniGameId.GeneCultivation;
            return true;
        }

        if (taskId == EcologyNutrientConfig.TaskId)
        {
            id = MiniGameId.EcologyDeployment;
            return true;
        }

        id = default;
        return false;
    }
}
