using System;
using UnityEngine;

/// <summary>
/// Scene-facing adapter for the shared MVPGameSession progression state.
/// The authoritative value now survives station/mini-game scene changes and
/// daily task completion can be reset without losing total Earth progress.
/// </summary>
public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    public event Action<int> ProgressChanged;

    public int EarthProgress => MVPGameSession.EarthProgress;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        MVPGameSession.ProgressChanged += HandleProgressChanged;
    }

    private void OnDestroy()
    {
        MVPGameSession.ProgressChanged -= HandleProgressChanged;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool IsTaskCompleted(string taskId)
    {
        return
            TryResolveMiniGameId(taskId, out MiniGameId id) &&
            MVPGameSession.IsTaskCompleted(id);
    }

    /// <summary>
    /// Pays one reward per task in the current workday. Starting a new
    /// workday clears daily completion while preserving total progress.
    /// </summary>
    public bool TryCompleteTask(string taskId, int reward)
    {
        if (!TryResolveMiniGameId(taskId, out MiniGameId id))
        {
            return false;
        }

        return MVPGameSession.ReportTaskCompleted(id, reward);
    }

    public bool TryAwardUniqueProgress(string rewardId, int reward)
    {
        return MVPGameSession.TryAwardUniqueProgress(rewardId, reward);
    }

    private void HandleProgressChanged(int progress)
    {
        ProgressChanged?.Invoke(progress);
    }

    private static bool TryResolveMiniGameId(
        string taskId,
        out MiniGameId id
    )
    {
        if (taskId == OrbitCalibrationConfig.TaskId)
        {
            id = MiniGameId.OrbitInspection;
            return true;
        }

        if (taskId == GeneCultivationConfig.TaskId)
        {
            id = MiniGameId.GeneCultivation;
            return true;
        }

        if (taskId == EcologyNutrientConfig.TaskId)
        {
            id = MiniGameId.EcologyDeployment;
            return true;
        }

        id = default;
        return false;
    }
}
