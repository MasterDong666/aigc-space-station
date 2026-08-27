using System;
using UnityEngine;

/// <summary>单个校准参数的取值范围定义（Slider 全程 0~100）。</summary>
[Serializable]
public struct CalibrationParam
{
    [Tooltip("显示名称")]
    public string displayName;

    [Tooltip("正确范围下限")]
    public float minValue;

    [Tooltip("正确范围上限")]
    public float maxValue;

    public bool IsInRange(float value)
    {
        return value >= minValue && value <= maxValue;
    }

    public string RangeText
    {
        get { return string.Format("正确范围 {0:0} ~ {1:0}", minValue, maxValue); }
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
        },
        new CalibrationParam
        {
            displayName = "粒子稳定值",
            minValue = 65f,
            maxValue = 80f,
        },
        new CalibrationParam
        {
            displayName = "输送倾角",
            minValue = 25f,
            maxValue = 45f,
        },
    };
}
