using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameNarrativeRoute
{
    None = 0,
    FirstReturnToNoah = 1
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
    private static string playerName = "修复官";
    private static string avatarId = "RESTORER_A";
    private static int earthProgress = InitialEarthProgress;
    private static int workday = 1;

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
        playerName = "修复官";
        avatarId = "RESTORER_A";
        earthProgress = InitialEarthProgress;
        workday = 1;
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
        firstReturnStarted = false;
        firstReturnCompleted = true;
        BeginNextWorkday();
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

    public static void ResetTaskProgress()
    {
        CompletedToday.Clear();
        requestedMiniGame = null;
        pendingCompletion = null;
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
