using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Stable hand-off point for independently developed mini-games.
/// A mini-game only needs to invoke Complete() once after success.
/// </summary>
public class MiniGameCompletionRelay : MonoBehaviour
{
    [SerializeField] private MiniGameTerminalFlowLink flowLink;
    [SerializeField] private UnityEvent onCompletionAccepted = new();
    [SerializeField] private UnityEvent onCompletionRejected = new();

    public MiniGameId MiniGameId =>
        flowLink == null
            ? MiniGameId.OrbitInspection
            : flowLink.MiniGameId;

    private void Awake()
    {
        if (flowLink == null)
        {
            flowLink = GetComponent<MiniGameTerminalFlowLink>();
        }
    }

    /// <summary>
    /// UnityEvent-friendly completion method for a mini-game success button.
    /// </summary>
    public void Complete()
    {
        if (TryComplete())
        {
            onCompletionAccepted.Invoke();
        }
        else
        {
            onCompletionRejected.Invoke();
        }
    }

    public bool TryComplete()
    {
        if (flowLink == null)
        {
            flowLink = GetComponent<MiniGameTerminalFlowLink>();
        }

        return
            flowLink != null &&
            flowLink.ReportCompleted();
    }
}
