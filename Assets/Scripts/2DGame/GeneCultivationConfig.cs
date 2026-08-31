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

    [Tooltip("基因树层级：0 为第一层（默认开放），必须逐层成熟上一层级后才解锁下一层级。")]
    public int layer;

    [Tooltip("是否已解锁（兼容旧字段；实际可选性由 geneTreeUnlockedLevel 与 layer 决定）")]
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

    /// <summary>画面中展示的完整生态培育时长（虚拟小时）。</summary>
    public const float MaturationHours = 72f;

    /// <summary>
    /// 比赛演示中用多少现实秒演完 72 小时培育过程。
    /// 这是视觉时间压缩，不要求玩家真实等待三天。
    /// </summary>
    public const float MaturationPreviewSeconds = 5f;

    /// <summary>优质成熟地块出现变异彩蛋的概率（可配置）。</summary>
    public const float MutationChance = 0.10f;

    /// <summary>触发变异所需的最低地块品质（0~100）。</summary>
    public const int MutationQualityThreshold = 60;

    /// <summary>适宜地块的品质加成。</summary>
    public const int QualitySuitableBonus = 20;

    public static readonly SporeType[] Spores =
    {
        new SporeType
        {
            id = "microbe",
            displayName = "微生物",
            description = "基础分解菌群，可快速适应贫瘠地表，是生态修复的第一环。",
            layer = 0,
        },
        new SporeType
        {
            id = "moss",
            displayName = "苔藓",
            description = "耐寒苔藓孢子，适合高纬度裸露岩层。",
            layer = 1,
        },
        new SporeType
        {
            id = "herb",
            displayName = "草本",
            description = "先锋草本植物，根系可固土保水。",
            layer = 2,
        },
    };

    /// <summary>某孢子是否处于已解锁层级（layer &lt;= 会话已解锁层级）。</summary>
    public static bool IsSporeSelectable(SporeType spore)
    {
        return spore.layer <= MVPGameSession.GeneTreeUnlockedLevel;
    }

    /// <summary>基因树的最高层级（由孢子数组推导）。</summary>
    public static int MaxGeneLayer
    {
        get
        {
            int max = 0;
            SporeType[] spores = Spores;
            for (int i = 0; i < spores.Length; i++)
            {
                if (spores[i].layer > max)
                {
                    max = spores[i].layer;
                }
            }

            return max;
        }
    }

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
