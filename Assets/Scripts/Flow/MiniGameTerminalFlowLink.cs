using UnityEngine;

/// <summary>
/// Connects one physical station terminal to the scene-level MVP flow.
/// The terminal remains reusable and does not know about other mini-games.
/// </summary>
public class MiniGameTerminalFlowLink : MonoBehaviour
{
    [SerializeField] private MiniGameId miniGameId;
    [SerializeField] private MVPFlowController flowController;

    [Header("不可用状态提示")]
    [SerializeField] private string lockedPrompt = "终端尚未授权";
    [SerializeField] private string completedPrompt = "任务已完成";

    public MiniGameId MiniGameId => miniGameId;
    public MVPFlowController FlowController => flowController;

    public MiniGameTaskState State =>
        flowController == null
            ? MiniGameTaskState.Available
            : flowController.GetTaskState(miniGameId);

    public bool CanBegin =>
        flowController == null ||
        flowController.CanBeginTask(miniGameId);

    private void Awake()
    {
        ResolveFlowController();
    }

    public string GetInteractionPrompt(string availablePrompt)
    {
        return State switch
        {
            MiniGameTaskState.Locked => lockedPrompt,
            MiniGameTaskState.Completed => completedPrompt,
            _ => availablePrompt
        };
    }

    public bool TryBegin()
    {
        ResolveFlowController();

        return
            flowController == null ||
            flowController.TryBeginTask(miniGameId);
    }

    public bool ReportCompleted()
    {
        ResolveFlowController();

        return
            flowController != null &&
            flowController.TryCompleteTask(miniGameId);
    }

    public void Configure(
        MiniGameId id,
        MVPFlowController controller
    )
    {
        miniGameId = id;
        flowController = controller;
    }

    private void ResolveFlowController()
    {
        if (flowController == null)
        {
            flowController = FindObjectOfType<MVPFlowController>();
        }
    }
}
