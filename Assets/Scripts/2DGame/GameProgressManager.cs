using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局游戏进度：地球修复进度 + 各任务/成就完成记录（唯一奖励来源）。
/// 后续进度来源（妹妹拼图 / 文明遗迹扫描 / 成就 / 世代档案 / 其他剧情任务）
/// 统一通过 TryCompleteTask(taskId, reward) 发放，自动防重复领取。
/// 当前仅进程内状态（同一运行内防重复），暂不做磁盘存档。
/// </summary>
public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    /// <summary>正式结局解锁所需的修复进度（集中配置，不要在 UI 中散落硬编码）。</summary>
    public const int EndingUnlockProgress = 50;

    [Header("进度设置")]
    [SerializeField] private int earthProgress = 10;

    private readonly HashSet<string> completedTasks = new HashSet<string>();

    /// <summary>地球修复进度变化时触发（参数为最新进度）。</summary>
    public event Action<int> ProgressChanged;

    public int EarthProgress => earthProgress;

    /// <summary>正式结局是否解锁（进度达到 EndingUnlockProgress）。</summary>
    public bool IsEndingUnlocked
    {
        get { return earthProgress >= EndingUnlockProgress; }
    }

    /// <summary>【调试用】直接设置修复进度（仅测试使用，不要暴露到正式 UI）。</summary>
    public void DebugSetProgress(int value)
    {
        earthProgress = Mathf.Clamp(value, 0, 100);
        ProgressChanged?.Invoke(earthProgress);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool IsTaskCompleted(string taskId)
    {
        return completedTasks.Contains(taskId);
    }

    /// <summary>
    /// 尝试完成任务并发放奖励。
    /// 同一次运行内重复完成同一任务返回 false，不重复发放奖励。
    /// </summary>
    public bool TryCompleteTask(string taskId, int reward)
    {
        if (completedTasks.Contains(taskId))
        {
            return false;
        }

        completedTasks.Add(taskId);
        earthProgress += reward;
        ProgressChanged?.Invoke(earthProgress);
        return true;
    }
}
