using System;
using UnityEngine;

/// <summary>一种孢子类型的静态定义。</summary>
[Serializable]
public struct SporeType
{
    [Tooltip("唯一 ID")]
    public string id;

    [Tooltip("显示名称")]
    public string displayName;

    [Tooltip("简短说明")]
    public string description;

    [Tooltip("是否已解锁（未来基因树逐级解锁的基础字段）")]
    public bool unlocked;
}

/// <summary>一块播种区域的定义。</summary>
[Serializable]
public struct SeedingRegion
{
    public string id;

    public string displayName;

    [Tooltip("当前是否适宜播种（绿色=适宜）")]
    public bool suitable;
}

/// <summary>
/// 培育阶段定义。
/// 保留"剩余小时数"概念，当前演示版用快速 UI 过场代替真实等待，
/// 未来接入真实时长系统时直接复用本结构。
/// </summary>
[Serializable]
public struct GrowthStage
{
    public string displayName;

    [Tooltip("剩余小时数（演示不真实等待）")]
    public float hoursRemaining;
}

/// <summary>
/// 任务3【基因孢子无人机播撒培育】参数集中配置。
/// 所有数值只在此定义，界面与逻辑均从这里读取，不散落硬编码。
/// </summary>
public static class GeneCultivationConfig
{
    public const string TaskId = "GeneCultivation";

    /// <summary>奖励值集中配置（与任务1惯例一致；最终数值待整体数值设计后统一调整）。</summary>
    public const int Reward = 5;

    public static readonly SporeType[] Spores =
    {
        new SporeType
        {
            id = "microbe",
            displayName = "微生物",
            description = "基础分解菌群，可快速适应贫瘠地表，是生态修复的第一环。",
            unlocked = true,
        },
        new SporeType
        {
            id = "moss",
            displayName = "苔藓",
            description = "耐寒苔藓孢子，适合高纬度裸露岩层。",
            unlocked = false,
        },
        new SporeType
        {
            id = "herb",
            displayName = "草本",
            description = "先锋草本植物，根系可固土保水。",
            unlocked = false,
        },
    };

    public static readonly SeedingRegion[] Regions =
    {
        new SeedingRegion { id = "north", displayName = "北部荒原", suitable = true },
        new SeedingRegion { id = "west", displayName = "西侧峡谷", suitable = false },
        new SeedingRegion { id = "central", displayName = "中央平原", suitable = true },
        new SeedingRegion { id = "south", displayName = "南岸盐碱地", suitable = false },
        new SeedingRegion { id = "east", displayName = "东部高坡", suitable = false },
        new SeedingRegion { id = "valley", displayName = "河谷绿洲", suitable = true },
    };

    public static readonly GrowthStage[] GrowthStages =
    {
        new GrowthStage { displayName = "播种完成", hoursRemaining = 72f },
        new GrowthStage { displayName = "生长中", hoursRemaining = 48f },
        new GrowthStage { displayName = "成熟中", hoursRemaining = 24f },
        new GrowthStage { displayName = "群落培育完成", hoursRemaining = 0f },
    };
}
