using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameNarrativeRoute
{
    None = 0,
    FirstReturnToNoah = 1,
    FinalChoice = 2
}

public enum FinalEndingChoice
{
    None = 0,
    EndRestorationProgram = 1,
    SacrificeForEarth = 2
}

/// <summary>
/// Runtime session shared by the front end, station and 2D mini-games.
/// Daily completion is separate from lifetime progress so tasks can be
/// repeated on a later workday without paying twice during the same day.
/// </summary>
public static class MVPGameSession
{
    public const int InitialEarthProgress = 10;
    public const int EndingProgress = 50;
    public const int DefaultTaskReward = 5;

    private static readonly HashSet<MiniGameId> CompletedToday = new();
    private static readonly Dictionary<MiniGameId, int> CompletionHistory =
        new();
    private static readonly HashSet<MiniGameId> TutorialsSeen = new();
    private static readonly HashSet<string> UniqueProgressRewards = new();

    private static MiniGameId? requestedMiniGame;
    private static MiniGameId? pendingCompletion;
    private static GameNarrativeRoute requestedNarrative;
    private static bool stationIntroSeen;
    private static bool openingCompleted;
    private static bool firstReturnStarted;
    private static bool firstReturnCompleted;
    private static bool biodiversityPuzzleCompleted;
    private static bool endingCompleted;
    private static FinalEndingChoice endingChoice;
    private static string playerName = "修复官";
    private static string avatarId = "RESTORER_A";
    private static int earthProgress = InitialEarthProgress;
    private static int workday = 1;
    private static int task1ManualStreak;
    private static bool task1LogUnlocked;
    private static int geneTreeUnlockedLevel;
    private static readonly List<GenePlotData> genePlots = new();

    public static event Action<int> ProgressChanged;
    public static event Action<int> WorkdayChanged;

    public static int CompletedTaskCount => CompletedToday.Count;
    public static int EarthProgress => earthProgress;
    public static int Workday => workday;
    public static string PlayerName => playerName;
    public static string AvatarId => avatarId;
    public static bool HasPlayerProfile { get; private set; }
    public static bool OpeningCompleted => openingCompleted;
    public static bool FirstReturnStarted => firstReturnStarted;
    public static bool FirstReturnCompleted => firstReturnCompleted;
    public static bool BiodiversityPuzzleCompleted =>
        biodiversityPuzzleCompleted;
    public static bool EndingCompleted => endingCompleted;
    public static FinalEndingChoice EndingChoice => endingChoice;
    public static bool IsCurrentDayComplete =>
        CompletedToday.Count >= Enum.GetValues(typeof(MiniGameId)).Length;
    public static bool IsEndingUnlocked => earthProgress >= EndingProgress;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        CompletedToday.Clear();
        CompletionHistory.Clear();
        TutorialsSeen.Clear();
        UniqueProgressRewards.Clear();
        requestedMiniGame = null;
        pendingCompletion = null;
        requestedNarrative = GameNarrativeRoute.None;
        stationIntroSeen = false;
        openingCompleted = false;
        firstReturnStarted = false;
        firstReturnCompleted = false;
        biodiversityPuzzleCompleted = false;
        endingCompleted = false;
        endingChoice = FinalEndingChoice.None;
        playerName = "修复官";
        avatarId = "RESTORER_A";
        earthProgress = InitialEarthProgress;
        workday = 1;
        task1ManualStreak = 0;
        task1LogUnlocked = false;
        geneTreeUnlockedLevel = 0;
        genePlots.Clear();
        HasPlayerProfile = false;
        ProgressChanged = null;
        WorkdayChanged = null;
    }

    public static void SetPlayerProfile(
        string requestedName,
        string requestedAvatar
    )
    {
        playerName = string.IsNullOrWhiteSpace(requestedName)
            ? "修复官"
            : requestedName.Trim();
        avatarId = string.IsNullOrWhiteSpace(requestedAvatar)
            ? "RESTORER_A"
            : requestedAvatar.Trim();
        HasPlayerProfile = true;
    }

    public static void MarkOpeningCompleted()
    {
        openingCompleted = true;
    }

    public static int Task1ManualStreak => task1ManualStreak;
    public static bool Task1LogUnlocked => task1LogUnlocked;

    /// <summary>任务1以“完全手动”方式完成：累计手动连击天数。</summary>
    public static void RegisterTask1ManualCompletion()
    {
        task1ManualStreak++;
    }

    /// <summary>任务1以“全自动托管”方式完成：手动连击清零（托管当天不计入）。</summary>
    public static void RegisterTask1AutonomousCompletion()
    {
        task1ManualStreak = 0;
    }

    /// <summary>连续手动达到目标天数时，一次性解锁“前代修复官日志碎片”彩蛋。</summary>
    public static bool TryUnlockTask1Log()
    {
        if (task1LogUnlocked)
        {
            return false;
        }

        if (task1ManualStreak < OrbitCalibrationConfig.ManualStreakTarget)
        {
            return false;
        }

        task1LogUnlocked = true;
        return true;
    }

    // ===== 任务3：基因树层级 + 播种地块状态 =====

    public static int GeneTreeUnlockedLevel => geneTreeUnlockedLevel;

    public static IReadOnlyList<GenePlotData> GenePlots => genePlots;

    /// <summary>指定层级是否已解锁（layer 0 恒为第一层，默认开放）。</summary>
    public static bool IsGeneLayerUnlocked(int layer)
    {
        return geneTreeUnlockedLevel >= layer;
    }

    /// <summary>
    /// 当前层成熟后，一次性解锁下一层。
    /// 仅当"本次成熟的孢子恰好处于当前已解锁的最前沿层级"时才解锁，
    /// 从而保证必须逐层完成上一层培育，禁止越级。
    /// </summary>
    public static bool TryUnlockGeneTreeNextLevel(int maturedSporeLayer)
    {
        if (geneTreeUnlockedLevel >= GeneCultivationConfig.MaxGeneLayer)
        {
            return false;
        }

        // 必须成熟当前最前沿层级的孢子，才能解锁下一层（禁止越级）。
        if (maturedSporeLayer != geneTreeUnlockedLevel)
        {
            return false;
        }

        geneTreeUnlockedLevel++;
        return true;
    }

    /// <summary>播种一块地块（同一区域重复播种则覆盖并重置倒计时）。</summary>
    public static void SowGenePlot(
        string regionId,
        string sporeId,
        int quality
    )
    {
        long now = DateTime.UtcNow.Ticks;
        long matureAt = now + MatureTicks;

        for (int i = 0; i < genePlots.Count; i++)
        {
            if (genePlots[i].regionId == regionId)
            {
                genePlots[i].sporeId = sporeId;
                genePlots[i].sowedAtUtcTicks = now;
                genePlots[i].matureAtUtcTicks = matureAt;
                genePlots[i].quality = Mathf.Clamp(quality, 0, 100);
                genePlots[i].mutationTriggered = false;
                genePlots[i].mutationViewed = false;
                return;
            }
        }

        genePlots.Add(new GenePlotData
        {
            regionId = regionId,
            sporeId = sporeId,
            sowedAtUtcTicks = now,
            matureAtUtcTicks = matureAt,
            quality = Mathf.Clamp(quality, 0, 100),
            mutationTriggered = false,
            mutationViewed = false,
        });
    }

    public static GenePlotData GetGenePlot(string regionId)
    {
        for (int i = 0; i < genePlots.Count; i++)
        {
            if (genePlots[i].regionId == regionId)
            {
                return genePlots[i];
            }
        }

        return null;
    }

    /// <summary>地块是否已成熟（真实 72 小时，按 UTC 时间戳判断，退出重进仍正确）。</summary>
    public static bool IsGenePlotMature(GenePlotData plot)
    {
        if (plot == null)
        {
            return false;
        }

        long now = DateTime.UtcNow.Ticks;
        long matureAt = GetGenePlotMatureAtUtcTicks(plot);
        return now >= matureAt;
    }

    /// <summary>地块成熟的绝对时刻（UTC ticks）。优先使用存档字段，旧存档回退为 sowedAtUtcTicks + MatureTicks。</summary>
    public static long GetGenePlotMatureAtUtcTicks(GenePlotData plot)
    {
        if (plot == null)
        {
            return 0L;
        }

        return plot.matureAtUtcTicks > 0L
            ? plot.matureAtUtcTicks
            : plot.sowedAtUtcTicks + MatureTicks;
    }

    /// <summary>优质成熟地块尝试触发变异彩蛋（每块地一次）。</summary>
    public static bool TryTriggerGenePlotMutation(GenePlotData plot)
    {
        if (plot == null || plot.mutationTriggered)
        {
            return false;
        }

        if (plot.quality < GeneCultivationConfig.MutationQualityThreshold)
        {
            return false;
        }

        if (UnityEngine.Random.value >= GeneCultivationConfig.MutationChance)
        {
            return false;
        }

        plot.mutationTriggered = true;
        return true;
    }

    public static void MarkGenePlotMutationViewed(string regionId)
    {
        GenePlotData plot = GetGenePlot(regionId);
        if (plot != null)
        {
            plot.mutationViewed = true;
        }
    }

    public const long MatureTicks = 72L * 3600L * 10000000L;

    public static bool IsTaskCompleted(MiniGameId id)
    {
        return CompletedToday.Contains(id);
    }

    public static int GetTaskCompletionCount(MiniGameId id)
    {
        return CompletionHistory.TryGetValue(id, out int count) ? count : 0;
    }

    public static bool HasSeenTaskTutorial(MiniGameId id)
    {
        return TutorialsSeen.Contains(id);
    }

    public static void MarkTaskTutorialSeen(MiniGameId id)
    {
        TutorialsSeen.Add(id);
    }

    public static void RequestMiniGame(MiniGameId id)
    {
        requestedMiniGame = id;
    }

    public static bool TryConsumeRequestedMiniGame(out MiniGameId id)
    {
        if (requestedMiniGame.HasValue)
        {
            id = requestedMiniGame.Value;
            requestedMiniGame = null;
            return true;
        }

        id = default;
        return false;
    }

    public static bool ReportTaskCompleted(MiniGameId id)
    {
        return ReportTaskCompleted(id, DefaultTaskReward);
    }

    public static bool ReportTaskCompleted(MiniGameId id, int reward)
    {
        if (!CompletedToday.Add(id))
        {
            return false;
        }

        CompletionHistory[id] = GetTaskCompletionCount(id) + 1;
        pendingCompletion = id;
        AddProgress(reward);
        return true;
    }

    public static bool TryAwardUniqueProgress(string rewardId, int reward)
    {
        if (
            string.IsNullOrWhiteSpace(rewardId) ||
            !UniqueProgressRewards.Add(rewardId)
        )
        {
            return false;
        }

        AddProgress(reward);
        return true;
    }

    public static bool TryConsumePendingCompletion(out MiniGameId id)
    {
        if (pendingCompletion.HasValue)
        {
            id = pendingCompletion.Value;
            pendingCompletion = null;
            return true;
        }

        id = default;
        return false;
    }

    public static void AcknowledgePendingCompletion(MiniGameId id)
    {
        if (pendingCompletion == id)
        {
            pendingCompletion = null;
        }
    }

    public static bool TryMarkStationIntroSeen()
    {
        if (stationIntroSeen)
        {
            return false;
        }

        stationIntroSeen = true;
        return true;
    }

    public static void RequestNarrative(GameNarrativeRoute route)
    {
        requestedNarrative = route;
    }

    public static bool TryConsumeNarrative(out GameNarrativeRoute route)
    {
        route = requestedNarrative;
        requestedNarrative = GameNarrativeRoute.None;
        return route != GameNarrativeRoute.None;
    }

    public static bool TryStartFirstReturn()
    {
        if (firstReturnStarted || firstReturnCompleted)
        {
            return false;
        }

        firstReturnStarted = true;
        return true;
    }

    public static void CompleteFirstReturnAndBeginNextWorkday()
    {
        CompleteFirstReturn();
        BeginNextWorkday();
    }

    public static void CompleteFirstReturn()
    {
        firstReturnStarted = false;
        firstReturnCompleted = true;
    }

    public static void CancelFirstReturn()
    {
        firstReturnStarted = false;

        if (requestedNarrative == GameNarrativeRoute.FirstReturnToNoah)
        {
            requestedNarrative = GameNarrativeRoute.None;
        }
    }

    public static void BeginNextWorkday()
    {
        CompletedToday.Clear();
        requestedMiniGame = null;
        pendingCompletion = null;
        workday++;
        WorkdayChanged?.Invoke(workday);
    }

    public static bool CompleteBiodiversityPuzzle(int reward = 10)
    {
        if (biodiversityPuzzleCompleted)
        {
            return false;
        }

        biodiversityPuzzleCompleted = true;
        TryAwardUniqueProgress("NOAH_BIODIVERSITY_PUZZLE", reward);
        return true;
    }

    public static void CompleteEnding(FinalEndingChoice choice)
    {
        if (choice == FinalEndingChoice.None)
        {
            return;
        }

        endingChoice = choice;
        endingCompleted = true;
    }

    public static void ResetTaskProgress()
    {
        CompletedToday.Clear();
        requestedMiniGame = null;
        pendingCompletion = null;
    }

    /// <summary>导出存档数据（供 SaveManager）。不导出 transient 路由字段。</summary>
    public static void ExportState(GameSaveData data)
    {
        data.playerName = playerName;
        data.avatarId = avatarId;
        data.hasProfile = HasPlayerProfile;
        data.earthProgress = earthProgress;
        data.workday = workday;
        data.openingCompleted = openingCompleted;
        data.firstReturnStarted = firstReturnStarted;
        data.firstReturnCompleted = firstReturnCompleted;
        data.biodiversityPuzzleCompleted = biodiversityPuzzleCompleted;
        data.endingCompleted = endingCompleted;
        data.stationIntroSeen = stationIntroSeen;
        data.endingChoice = (int)endingChoice;
        data.task1ManualStreak = task1ManualStreak;
        data.task1LogUnlocked = task1LogUnlocked;
        data.geneTreeUnlockedLevel = geneTreeUnlockedLevel;

        data.genePlots.Clear();
        foreach (GenePlotData plot in genePlots)
        {
            data.genePlots.Add(new GenePlotData
            {
                regionId = plot.regionId,
                sporeId = plot.sporeId,
                sowedAtUtcTicks = plot.sowedAtUtcTicks,
                matureAtUtcTicks = plot.matureAtUtcTicks,
                quality = plot.quality,
                mutationTriggered = plot.mutationTriggered,
                mutationViewed = plot.mutationViewed,
            });
        }

        data.completedToday.Clear();
        foreach (MiniGameId id in CompletedToday)
        {
            data.completedToday.Add((int)id);
        }

        data.completionHistory.Clear();
        foreach (KeyValuePair<MiniGameId, int> pair in CompletionHistory)
        {
            data.completionHistory.Add(new IntIntPair
            {
                key = (int)pair.Key,
                value = pair.Value,
            });
        }

        data.tutorialsSeen.Clear();
        foreach (MiniGameId id in TutorialsSeen)
        {
            data.tutorialsSeen.Add((int)id);
        }

        data.uniqueProgressRewards.Clear();
        foreach (string rewardId in UniqueProgressRewards)
        {
            data.uniqueProgressRewards.Add(rewardId);
        }
    }

    /// <summary>
    /// 从存档恢复状态。跳过 transient 路由字段
    /// （requestedMiniGame / pendingCompletion / requestedNarrative），
    /// 避免读档把玩家凭空传送进小游戏或重播叙事。
    /// </summary>
    public static void ImportState(GameSaveData data)
    {
        if (data == null)
        {
            return;
        }

        playerName = string.IsNullOrWhiteSpace(data.playerName)
            ? "修复官"
            : data.playerName.Trim();
        avatarId = string.IsNullOrWhiteSpace(data.avatarId)
            ? "RESTORER_A"
            : data.avatarId.Trim();
        HasPlayerProfile = data.hasProfile;
        earthProgress = Mathf.Clamp(data.earthProgress, 0, EndingProgress);
        workday = Mathf.Max(1, data.workday);
        openingCompleted = data.openingCompleted;
        firstReturnStarted = data.firstReturnStarted;
        firstReturnCompleted = data.firstReturnCompleted;
        biodiversityPuzzleCompleted = data.biodiversityPuzzleCompleted;
        endingCompleted = data.endingCompleted;
        stationIntroSeen = data.stationIntroSeen;
        endingChoice = (FinalEndingChoice)data.endingChoice;
        task1ManualStreak = Mathf.Max(0, data.task1ManualStreak);
        task1LogUnlocked = data.task1LogUnlocked;
        geneTreeUnlockedLevel = Mathf.Clamp(
            data.geneTreeUnlockedLevel,
            0,
            GeneCultivationConfig.MaxGeneLayer
        );

        genePlots.Clear();
        if (data.genePlots != null)
        {
            foreach (GenePlotData plot in data.genePlots)
            {
                genePlots.Add(new GenePlotData
                {
                    regionId = plot.regionId ?? string.Empty,
                    sporeId = plot.sporeId ?? string.Empty,
                    sowedAtUtcTicks = plot.sowedAtUtcTicks,
                    // 旧存档无 matureAtUtcTicks（默认 0），由 GetGenePlotMatureAtUtcTicks 回退。
                    matureAtUtcTicks = plot.matureAtUtcTicks,
                    quality = Mathf.Clamp(plot.quality, 0, 100),
                    mutationTriggered = plot.mutationTriggered,
                    mutationViewed = plot.mutationViewed,
                });
            }
        }

        CompletedToday.Clear();
        if (data.completedToday != null)
        {
            foreach (int id in data.completedToday)
            {
                if (Enum.IsDefined(typeof(MiniGameId), id))
                {
                    CompletedToday.Add((MiniGameId)id);
                }
            }
        }

        CompletionHistory.Clear();
        if (data.completionHistory != null)
        {
            foreach (IntIntPair pair in data.completionHistory)
            {
                if (Enum.IsDefined(typeof(MiniGameId), pair.key))
                {
                    CompletionHistory[(MiniGameId)pair.key] = pair.value;
                }
            }
        }

        TutorialsSeen.Clear();
        if (data.tutorialsSeen != null)
        {
            foreach (int id in data.tutorialsSeen)
            {
                if (Enum.IsDefined(typeof(MiniGameId), id))
                {
                    TutorialsSeen.Add((MiniGameId)id);
                }
            }
        }

        UniqueProgressRewards.Clear();
        if (data.uniqueProgressRewards != null)
        {
            foreach (string rewardId in data.uniqueProgressRewards)
            {
                UniqueProgressRewards.Add(rewardId);
            }
        }
    }

    public static int GetRestorationStage()
    {
        if (earthProgress >= EndingProgress)
        {
            return 3;
        }

        if (earthProgress >= 40)
        {
            return 2;
        }

        if (earthProgress >= 25)
        {
            return 1;
        }

        return 0;
    }

    /// <summary>
    /// 从权威进度中扣除修复进度（下限 0，上限 EndingProgress）。
    /// 用于错误操作惩罚，真正影响进度，而非仅写日志。
    /// </summary>
    public static void DeductProgress(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int updated = Mathf.Clamp(earthProgress - amount, 0, EndingProgress);
        if (updated == earthProgress)
        {
            return;
        }

        earthProgress = updated;
        ProgressChanged?.Invoke(earthProgress);
    }

    private static void AddProgress(int reward)
    {
        int updated = Mathf.Clamp(
            earthProgress + Mathf.Max(0, reward),
            0,
            EndingProgress
        );

        if (updated == earthProgress)
        {
            return;
        }

        earthProgress = updated;
        ProgressChanged?.Invoke(earthProgress);
    }
}
