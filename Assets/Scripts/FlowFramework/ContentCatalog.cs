using System;

/// <summary>任务教程数据。</summary>
[Serializable]
public struct TutorialDef
{
    public MiniGameId gameId;
    public string eyebrow;
    public string title;
    public string[] steps;
    public string symbol;
}

/// <summary>每日任务数据。</summary>
[Serializable]
public struct TaskDef
{
    public MiniGameId gameId;
    public string title;
    public string description;
    public int reward;
    public MiniGameThemeId theme;
}

/// <summary>结局数据。</summary>
[Serializable]
public struct EndingDef
{
    public FinalEndingChoice choice;
    public string title;
    public string body;
    public string textureKey;
}

/// <summary>
/// 教程 / 任务 / 结局内容数据源（与现有 UI 的硬编码文案保持一致）。
/// 供验证脚本断言、未来 UI 复用与文案单点替换；
/// 现有 TaskTutorialUI / EndingChoiceUI 的展示逻辑保持不动。
/// </summary>
public static class ContentCatalog
{
    public static TutorialDef GetTutorial(MiniGameId id)
    {
        switch (id)
        {
            case MiniGameId.OrbitInspection:
                return new TutorialDef
                {
                    gameId = id,
                    eyebrow = "ORBIT INSPECTION",
                    title = "星际轨道巡检",
                    steps = new[]
                    {
                        "观察三个参数的自动移动指针",
                        "指针进入绿色区间时点击【锁定参数】",
                        "三项全部锁定后提交运维报告",
                    },
                    symbol = "◈",
                };

            case MiniGameId.EcologyDeployment:
                return new TutorialDef
                {
                    gameId = id,
                    eyebrow = "ECOLOGY DEPLOYMENT",
                    title = "生态营养液投放",
                    steps = new[]
                    {
                        "选择污染地块并查看监测数据",
                        "选择与污染指数匹配的营养液浓度",
                        "拖拽框选投放范围并启动卫星投放",
                    },
                    symbol = "◆",
                };

            case MiniGameId.GeneCultivation:
                return new TutorialDef
                {
                    gameId = id,
                    eyebrow = "GENE CULTIVATION",
                    title = "基因孢子培育",
                    steps = new[]
                    {
                        "在基因库选择孢子并调取样本",
                        "选择适宜播种的绿色区域",
                        "释放无人机群并等待培育完成",
                    },
                    symbol = "❖",
                };

            default:
                return default;
        }
    }

    public static TaskDef GetTask(MiniGameId id)
    {
        switch (id)
        {
            case MiniGameId.OrbitInspection:
                return new TaskDef
                {
                    gameId = id,
                    title = "星际轨道巡检",
                    description = "时机锁定三参数校准",
                    reward = MVPGameSession.DefaultTaskReward,
                    theme = MiniGameThemeId.Orbit,
                };

            case MiniGameId.EcologyDeployment:
                return new TaskDef
                {
                    gameId = id,
                    title = "生态营养液投放",
                    description = "地块监测与卫星投放",
                    reward = MVPGameSession.DefaultTaskReward,
                    theme = MiniGameThemeId.Ecology,
                };

            case MiniGameId.GeneCultivation:
                return new TaskDef
                {
                    gameId = id,
                    title = "基因孢子培育",
                    description = "无人机播撒与温室收获",
                    reward = MVPGameSession.DefaultTaskReward,
                    theme = MiniGameThemeId.Gene,
                };

            default:
                return default;
        }
    }

    public static EndingDef GetEnding(FinalEndingChoice choice)
    {
        switch (choice)
        {
            case FinalEndingChoice.EndRestorationProgram:
                return new EndingDef
                {
                    choice = choice,
                    title = "终止重塑计划",
                    body = "让归墟归于寂静，人类文明留在诺亚。",
                    textureKey = "Story/EndingNoah",
                };

            case FinalEndingChoice.SacrificeForEarth:
                return new EndingDef
                {
                    choice = choice,
                    title = "为地球牺牲",
                    body = "选择留下，用最后一任修复官的坚守完成重塑。",
                    textureKey = "Story/EndingSacrifice",
                };

            default:
                return default;
        }
    }
}
