using System;
using UnityEngine;

/// <summary>一块污染地块的定义。</summary>
[Serializable]
public struct PollutionZone
{
    public string id;

    public string displayName;

    [Tooltip("污染指数 0~100")]
    public float pollutionIndex;

    [Tooltip("土壤活性 0~100")]
    public float soilActivity;

    [Tooltip("监测终端给出的修复建议文案")]
    public string recommendation;

    [Tooltip("地块色块颜色（十六进制）")]
    public string colorHex;
}

/// <summary>营养液浓度档位定义。</summary>
[Serializable]
public struct NutrientTier
{
    public string id;

    public string displayName;

    [Tooltip("适用污染值上限（MVP 测试规则）")]
    public float maxPollution;
}

/// <summary>
/// 任务2【分区生态营养液精准投放】参数集中配置。
/// 所有数值与判断规则只在此定义，界面与逻辑均从这里读取。
/// </summary>
public static class EcologyNutrientConfig
{
    public const string TaskId = "EcologyNutrient";

    /// <summary>奖励值集中配置（最终数值待整体数值设计后统一调整）。</summary>
    public const int Reward = 5;

    /// <summary>卫星投放动画时长（策划原定 30 秒，演示版压缩；正式版改这里即可）。</summary>
    public const float DeliveryDuration = 4f;

    /// <summary>最小投放范围 = 地块面积 × 该比例。</summary>
    public const float MinSelectionRatio = 0.3f;

    /// <summary>土壤活性提升系数（MVP 公式：活性提升 = 污染指数 × 系数）。</summary>
    public const float ActivityRecoveryFactor = 0.28f;

    public static readonly PollutionZone[] Zones =
    {
        new PollutionZone
        {
            id = "heavy",
            displayName = "重度污染区",
            pollutionIndex = 82f,
            soilActivity = 18f,
            recommendation = "建议进行高强度修复",
            colorHex = "#7A2E2E",
        },
        new PollutionZone
        {
            id = "acid",
            displayName = "酸化土",
            pollutionIndex = 58f,
            soilActivity = 45f,
            recommendation = "建议进行中强度修复",
            colorHex = "#8A6A3A",
        },
        new PollutionZone
        {
            id = "radiation",
            displayName = "辐射区",
            pollutionIndex = 95f,
            soilActivity = 10f,
            recommendation = "建议进行特调修复",
            colorHex = "#5A2E7A",
        },
        new PollutionZone
        {
            id = "desert",
            displayName = "普通荒漠",
            pollutionIndex = 20f,
            soilActivity = 70f,
            recommendation = "建议进行基础修复",
            colorHex = "#8A7A4A",
        },
    };

    public static readonly NutrientTier[] Tiers =
    {
        new NutrientTier { id = "low", displayName = "低", maxPollution = 25f },
        new NutrientTier { id = "medium", displayName = "中", maxPollution = 50f },
        new NutrientTier { id = "high", displayName = "高", maxPollution = 75f },
        new NutrientTier { id = "special", displayName = "特调", maxPollution = 100f },
    };

    /// <summary>
    /// 根据污染指数推荐浓度档位。
    /// MVP 测试规则（0~25 低 / 26~50 中 / 51~75 高 / 76~100 特调），非最终策划数值。
    /// </summary>
    public static NutrientTier GetRecommendedTier(float pollutionIndex)
    {
        for (int i = 0; i < Tiers.Length; i++)
        {
            if (pollutionIndex <= Tiers[i].maxPollution)
            {
                return Tiers[i];
            }
        }

        return Tiers[Tiers.Length - 1];
    }

    /// <summary>把十六进制色值解析为 Color（配置驱动地块颜色）。</summary>
    public static Color ParseColor(string hex, float alpha = 1f)
    {
        Color color;
        ColorUtility.TryParseHtmlString(hex, out color);
        color.a = alpha;
        return color;
    }
}
