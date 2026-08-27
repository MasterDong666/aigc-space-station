using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps the lightweight MVP task state alive while scenes change.
/// This is intentionally runtime-only: restarting Play Mode starts a new demo.
/// </summary>
public static class MVPGameSession
{
    private static readonly HashSet<MiniGameId> CompletedTasks = new();

    private static MiniGameId? requestedMiniGame;
    private static MiniGameId? pendingCompletion;
    private static bool stationIntroSeen;

    public static int CompletedTaskCount => CompletedTasks.Count;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        CompletedTasks.Clear();
        requestedMiniGame = null;
        pendingCompletion = null;
        stationIntroSeen = false;
    }

    public static bool IsTaskCompleted(MiniGameId id)
    {
        return CompletedTasks.Contains(id);
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
        bool added = CompletedTasks.Add(id);

        if (added)
        {
            pendingCompletion = id;
        }

        return added;
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

    public static void ResetTaskProgress()
    {
        CompletedTasks.Clear();
        requestedMiniGame = null;
        pendingCompletion = null;
    }
}
