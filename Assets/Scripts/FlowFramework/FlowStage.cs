using System;

/// <summary>
/// 剧情流程阶段定义（空壳版 15 节点 + 终态 End）。
/// 每个阶段的推进点见 GameFlowManager 与各场景 Bootstrap 的插桩注释。
/// </summary>
public enum FlowStage
{
    None = 0,
    MainMenu,
    ProfileSetup,
    EndingTeaserVideo,
    OpeningVideo,
    AIIntroPopup,
    ActOneVideo,
    GameIntroPopup,
    DailyTutorial,
    DailyGame,
    HolidayPopup,
    HolidayVideo,
    PuzzleGame,
    FreePlay,
    EndingUnlock,
    EndingChoice,
    End,
}

/// <summary>阶段类型分类（供调试与 UI 决策）。</summary>
public enum FlowStageType
{
    UI,
    Video,
    Popup,
    Gameplay,
    Terminal,
}

/// <summary>阶段元信息（中文名 / 类型 / 是否存档检查点）。</summary>
public static class FlowStageInfo
{
    public static FlowStageType GetType(FlowStage stage)
    {
        switch (stage)
        {
            case FlowStage.EndingTeaserVideo:
            case FlowStage.OpeningVideo:
            case FlowStage.ActOneVideo:
            case FlowStage.HolidayVideo:
                return FlowStageType.Video;

            case FlowStage.AIIntroPopup:
            case FlowStage.GameIntroPopup:
            case FlowStage.HolidayPopup:
                return FlowStageType.Popup;

            case FlowStage.DailyTutorial:
            case FlowStage.DailyGame:
            case FlowStage.PuzzleGame:
            case FlowStage.FreePlay:
                return FlowStageType.Gameplay;

            case FlowStage.End:
                return FlowStageType.Terminal;

            default:
                return FlowStageType.UI;
        }
    }

    public static string GetDisplayName(FlowStage stage)
    {
        switch (stage)
        {
            case FlowStage.MainMenu: return "主界面";
            case FlowStage.ProfileSetup: return "档案建立";
            case FlowStage.EndingTeaserVideo: return "结局引入视频";
            case FlowStage.OpeningVideo: return "正序视频";
            case FlowStage.AIIntroPopup: return "AI弹窗介绍";
            case FlowStage.ActOneVideo: return "第一幕视频";
            case FlowStage.GameIntroPopup: return "游戏弹窗";
            case FlowStage.DailyTutorial: return "每日任务新手教程";
            case FlowStage.DailyGame: return "每日任务游戏";
            case FlowStage.HolidayPopup: return "弹窗休假";
            case FlowStage.HolidayVideo: return "休假视频";
            case FlowStage.PuzzleGame: return "拼图游戏";
            case FlowStage.FreePlay: return "玩家自主游玩";
            case FlowStage.EndingUnlock: return "结局解锁";
            case FlowStage.EndingChoice: return "结局选择";
            case FlowStage.End: return "结束";
            default: return "未定义";
        }
    }

    /// <summary>是否存档检查点（发生该阶段切换时适合落盘）。</summary>
    public static bool IsCheckpoint(FlowStage stage)
    {
        return
            stage == FlowStage.MainMenu ||
            stage == FlowStage.EndingUnlock ||
            stage == FlowStage.End;
    }
}
