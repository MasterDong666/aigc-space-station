using System;
using System.Collections.Generic;

/// <summary>剧情节点类型。</summary>
public enum BeatType
{
    StageMarker,
    Video,
    Popup,
    Narrative,
    Tutorial,
    Task,
    Ending,
}

/// <summary>
/// 统一剧情节点描述（可扩展数据结构）。
/// 视频 / 弹窗 / 教程 / 任务 / 结局的配置入口；正式内容替换只改数据不改代码。
/// </summary>
[Serializable]
public struct StoryBeat
{
    public FlowStage stage;
    public BeatType type;
    public string id;
    public string title;
    public string body;
    public string mediaLabel;
    public float durationSeconds;
}

/// <summary>空壳版剧情链清单（16 项，单一数据源）。</summary>
public static class StoryBeatCatalog
{
    public static StoryBeat[] FullChain { get; } =
    {
        Beat(FlowStage.MainMenu, BeatType.StageMarker, "main_menu", "主界面", "选择进入游戏", "FrontEnd 主菜单"),
        Beat(FlowStage.ProfileSetup, BeatType.StageMarker, "profile_setup", "档案建立", "输入修复官呼号并选择头像", "FrontEnd 档案面板"),
        Beat(FlowStage.EndingTeaserVideo, BeatType.Video, "ending_teaser", "结局引入视频", "结局预告 / 冷开场（占位：无文件时自动跳过）", "视频位：StreamingAssets/Video/ending_teaser.mp4", 4f),
        Beat(FlowStage.OpeningVideo, BeatType.Narrative, "opening", "正序视频（叙事卡）", "开场叙事：归墟、迁徙与地球重塑计划", "现有叙事卡系统", 0f),
        Beat(FlowStage.AIIntroPopup, BeatType.Popup, "ai_intro", "AI弹窗介绍", "晨曦 AI 欢迎并介绍游戏", "PopupManager 宿主", 0f),
        Beat(FlowStage.ActOneVideo, BeatType.Video, "act_one", "第一幕视频", "第一幕剧情（占位：无文件时自动跳过）", "视频位：StreamingAssets/Video/act_one.mp4", 4f),
        Beat(FlowStage.GameIntroPopup, BeatType.Popup, "game_intro", "游戏弹窗", "三项每日任务与玩法说明", "PopupManager 宿主", 0f),
        Beat(FlowStage.DailyTutorial, BeatType.Tutorial, "daily_tutorial", "每日任务新手教程", "首个任务前弹出任务简报", "现有 TaskTutorialUI", 0f),
        Beat(FlowStage.DailyGame, BeatType.Task, "daily_game", "每日任务游戏", "轨道巡检 / 营养液投放 / 基因培育", "现有三个小游戏", 0f),
        Beat(FlowStage.HolidayPopup, BeatType.Popup, "holiday_popup", "弹窗休假", "日结算弹窗（首次返航 / 普通结算 / 结局就绪）", "现有 StationDailyReturnController", 0f),
        Beat(FlowStage.HolidayVideo, BeatType.Narrative, "holiday", "休假视频（叙事卡）", "返航诺亚叙事", "现有叙事卡系统", 0f),
        Beat(FlowStage.PuzzleGame, BeatType.Task, "puzzle_game", "拼图游戏", "诺亚生物 3×3 拼图（一次性奖励）", "现有 NoahHolidayUI", 0f),
        Beat(FlowStage.FreePlay, BeatType.StageMarker, "free_play", "玩家自主游玩", "空间站自由循环与后续工作日", "现有 MVPFlowController", 0f),
        Beat(FlowStage.EndingUnlock, BeatType.StageMarker, "ending_unlock", "结局解锁", "地球修复进度达到 50", "MVPGameSession.IsEndingUnlocked", 0f),
        Beat(FlowStage.EndingChoice, BeatType.Ending, "ending_choice", "结局选择", "最终抉择二选一", "现有 EndingChoiceUI", 0f),
        Beat(FlowStage.End, BeatType.StageMarker, "end", "结束", "返回主菜单", "FrontEnd 主菜单", 0f),
    };

    public static StoryBeat Get(FlowStage stage)
    {
        for (int i = 0; i < FullChain.Length; i++)
        {
            if (FullChain[i].stage == stage)
            {
                return FullChain[i];
            }
        }

        return default;
    }

    public static string Describe(FlowStage stage)
    {
        StoryBeat beat = Get(stage);
        return stage + "(" + FlowStageInfo.GetDisplayName(stage) + ") [" + beat.type + "]";
    }

    private static StoryBeat Beat(
        FlowStage stage,
        BeatType type,
        string id,
        string title,
        string body,
        string mediaLabel,
        float durationSeconds = 0f
    )
    {
        return new StoryBeat
        {
            stage = stage,
            type = type,
            id = id,
            title = title,
            body = body,
            mediaLabel = mediaLabel,
            durationSeconds = durationSeconds,
        };
    }
}
