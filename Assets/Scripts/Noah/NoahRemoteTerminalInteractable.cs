using UnityEngine;

/// <summary>
/// Thin physical entry point for the Noah remote narrative. The controller is
/// intentionally separate so replacing this terminal never touches the UI.
/// </summary>
public class NoahRemoteTerminalInteractable : MonoBehaviour, IInteractable
{
    [SerializeField]
    private string interactionPrompt = "连接 诺亚远程通讯";

    [SerializeField]
    private NoahRemoteCommunicationController controller;

    public string InteractionPrompt => interactionPrompt;

    public void Interact()
    {
        if (controller == null)
        {
            controller = FindObjectOfType<
                NoahRemoteCommunicationController
            >();
        }

        if (controller != null)
        {
            controller.OpenTransmission();
        }
    }

    public void Configure(
        NoahRemoteCommunicationController communicationController,
        string prompt
    )
    {
        controller = communicationController;
        interactionPrompt = prompt;
    }
}
