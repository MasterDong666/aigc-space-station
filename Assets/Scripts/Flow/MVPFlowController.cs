using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class MiniGameTaskStateChangedEvent :
    UnityEvent<MiniGameId, MiniGameTaskState>
{
}

[Serializable]
public class MiniGameCompletedEvent : UnityEvent<MiniGameId>
{
}

[Serializable]
public class RepairStageChangedEvent : UnityEvent<int>
{
}

/// <summary>
/// Owns the lightweight MVP task sequence. Runtime progress is restored from
/// MVPGameSession after scene changes so mini-games can return to the station.
/// </summary>
[DefaultExecutionOrder(-100)]
public class MVPFlowController : MonoBehaviour
{
    private static readonly MiniGameId[] DefaultTaskOrder =
    {
        MiniGameId.OrbitInspection,
        MiniGameId.EcologyDeployment,
        MiniGameId.GeneCultivation
    };

    [Header("MVP 任务顺序")]
    [SerializeField] private MiniGameId[] taskOrder =
    {
        MiniGameId.OrbitInspection,
        MiniGameId.EcologyDeployment,
        MiniGameId.GeneCultivation
    };

    [Header("流程事件")]
    [SerializeField]
    private MiniGameTaskStateChangedEvent onTaskStateChanged = new();

    [SerializeField]
    private MiniGameCompletedEvent onTaskCompleted = new();

    [SerializeField]
    private RepairStageChangedEvent onRepairStageChanged = new();

    [SerializeField]
    private UnityEvent onAllTasksCompleted = new();

    private readonly Dictionary<MiniGameId, MiniGameTaskState>
        taskStates = new();

    private bool initialized;

    public event Action<MiniGameId, MiniGameTaskState> TaskStateChanged;
    public event Action<MiniGameId> TaskCompleted;
    public event Action<int> RepairStageChanged;
    public event Action AllTasksCompleted;

    public int CompletedTaskCount { get; private set; }
    public int RepairStage => CompletedTaskCount;
    public int TaskCount => GetValidatedTaskOrder().Count;
    public bool IsFlowComplete =>
        initialized && CompletedTaskCount >= TaskCount;

    public MiniGameId? CurrentTask
    {
        get
        {
            EnsureInitialized();

            foreach (MiniGameId id in GetValidatedTaskOrder())
            {
                MiniGameTaskState state = GetTaskState(id);

                if (
                    state == MiniGameTaskState.Available ||
                    state == MiniGameTaskState.InProgress
                )
                {
                    return id;
                }
            }

            return null;
        }
    }

    private void Awake()
    {
        RestoreFlowFromSession();
    }

    private void Start()
    {
        DispatchPendingCompletion();
    }

    public MiniGameTaskState GetTaskState(MiniGameId id)
    {
        EnsureInitialized();

        return taskStates.TryGetValue(id, out MiniGameTaskState state)
            ? state
            : MiniGameTaskState.Locked;
    }

    public bool CanBeginTask(MiniGameId id)
    {
        MiniGameTaskState state = GetTaskState(id);

        return
            state == MiniGameTaskState.Available ||
            state == MiniGameTaskState.InProgress;
    }

    public bool TryBeginTask(MiniGameId id)
    {
        MiniGameTaskState state = GetTaskState(id);

        if (state == MiniGameTaskState.InProgress)
        {
            MVPGameSession.RequestMiniGame(id);
            return true;
        }

        if (state != MiniGameTaskState.Available)
        {
            return false;
        }

        SetTaskState(id, MiniGameTaskState.InProgress);
        MVPGameSession.RequestMiniGame(id);
        return true;
    }

    public bool TryCompleteTask(MiniGameId id)
    {
        MiniGameTaskState state = GetTaskState(id);

        if (
            state != MiniGameTaskState.Available &&
            state != MiniGameTaskState.InProgress
        )
        {
            return false;
        }

        SetTaskState(id, MiniGameTaskState.Completed);
        CompletedTaskCount++;
        MVPGameSession.ReportTaskCompleted(id);
        MVPGameSession.AcknowledgePendingCompletion(id);

        onTaskCompleted.Invoke(id);
        TaskCompleted?.Invoke(id);

        onRepairStageChanged.Invoke(RepairStage);
        RepairStageChanged?.Invoke(RepairStage);

        UnlockNextTaskAfter(id);

        if (IsFlowComplete)
        {
            onAllTasksCompleted.Invoke();
            AllTasksCompleted?.Invoke();
        }

        return true;
    }

    [ContextMenu("Reset MVP Flow")]
    public void ResetFlow()
    {
        MVPGameSession.ResetTaskProgress();
        RestoreFlowFromSession();
    }

    [ContextMenu("Complete Current Task (Development)")]
    public void CompleteCurrentTaskForDevelopment()
    {
        MiniGameId? currentTask = CurrentTask;

        if (!currentTask.HasValue)
        {
            Debug.Log("MVP Flow：当前没有可完成的任务。", this);
            return;
        }

        if (TryCompleteTask(currentTask.Value))
        {
            Debug.Log(
                $"MVP Flow：开发测试已完成 {currentTask.Value}，" +
                $"Earth 修复阶段为 {RepairStage}/{TaskCount}。",
                this
            );
        }
    }

    private void UnlockNextTaskAfter(MiniGameId completedId)
    {
        IReadOnlyList<MiniGameId> order = GetValidatedTaskOrder();
        int completedIndex = -1;

        for (int index = 0; index < order.Count; index++)
        {
            if (order[index] == completedId)
            {
                completedIndex = index;
                break;
            }
        }

        int nextIndex = completedIndex + 1;

        if (completedIndex >= 0 && nextIndex < order.Count)
        {
            SetTaskState(order[nextIndex], MiniGameTaskState.Available);
        }
    }

    private void RestoreFlowFromSession()
    {
        initialized = true;
        taskStates.Clear();
        CompletedTaskCount = 0;

        IReadOnlyList<MiniGameId> order = GetValidatedTaskOrder();
        bool foundFirstIncomplete = false;

        foreach (MiniGameId id in order)
        {
            if (MVPGameSession.IsTaskCompleted(id))
            {
                taskStates[id] = MiniGameTaskState.Completed;
                CompletedTaskCount++;
                continue;
            }

            taskStates[id] = foundFirstIncomplete
                ? MiniGameTaskState.Locked
                : MiniGameTaskState.Available;
            foundFirstIncomplete = true;
        }
    }

    private void DispatchPendingCompletion()
    {
        if (!MVPGameSession.TryConsumePendingCompletion(out MiniGameId id))
        {
            return;
        }

        onTaskCompleted.Invoke(id);
        TaskCompleted?.Invoke(id);

        onRepairStageChanged.Invoke(RepairStage);
        RepairStageChanged?.Invoke(RepairStage);

        if (IsFlowComplete)
        {
            onAllTasksCompleted.Invoke();
            AllTasksCompleted?.Invoke();
        }
    }

    private void SetTaskState(
        MiniGameId id,
        MiniGameTaskState newState
    )
    {
        MiniGameTaskState oldState = GetTaskState(id);

        if (oldState == newState)
        {
            return;
        }

        taskStates[id] = newState;
        onTaskStateChanged.Invoke(id, newState);
        TaskStateChanged?.Invoke(id, newState);
    }

    private void EnsureInitialized()
    {
        if (!initialized)
        {
            RestoreFlowFromSession();
        }
    }

    private IReadOnlyList<MiniGameId> GetValidatedTaskOrder()
    {
        if (taskOrder == null || taskOrder.Length == 0)
        {
            return DefaultTaskOrder;
        }

        HashSet<MiniGameId> seen = new();
        List<MiniGameId> validated = new();

        foreach (MiniGameId id in taskOrder)
        {
            if (seen.Add(id))
            {
                validated.Add(id);
            }
        }

        return validated.Count > 0 ? validated : DefaultTaskOrder;
    }
}
