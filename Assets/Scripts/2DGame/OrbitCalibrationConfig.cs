using System;
using UnityEngine;

/// <summary>
/// 单个校准参数的定义。
/// 指针在 0~100 之间自动 PingPong 移动，玩家在目标区间内点击锁定。
/// </summary>
[Serializable]
public struct CalibrationParam
{
    [Tooltip("显示名称")]
    public string displayName;

    [Tooltip("目标区间下限")]
    public float minValue;

    [Tooltip("目标区间上限")]
    public float maxValue;

    [Tooltip("指针扫描速度（数值/秒，0→100→0 循环）")]
    public float scanSpeed;

    public bool IsInRange(float value)
    {
        return value >= minValue && value <= maxValue;
    }

    public string RangeText
    {
        get { return string.Format("目标区间 {0:0} ~ {1:0}", minValue, maxValue); }
    }
}

/// <summary>
/// 星际轨道巡检任务参数集中配置。
/// 所有数值只在此定义，界面与校验逻辑均从这里读取，不散落硬编码。
/// </summary>
public static class OrbitCalibrationConfig
{
    public const string TaskId = "orbit_inspection";
    public const int Reward = 5;

    public static readonly CalibrationParam[] Params =
    {
        new CalibrationParam
        {
            displayName = "能量配比",
            minValue = 40f,
            maxValue = 60f,
            scanSpeed = 35f,
        },
        new CalibrationParam
        {
            displayName = "粒子稳定值",
            minValue = 65f,
            maxValue = 80f,
            scanSpeed = 45f,
        },
        new CalibrationParam
        {
            displayName = "输送倾角",
            minValue = 25f,
            maxValue = 45f,
            scanSpeed = 55f,
        },
    };
}
