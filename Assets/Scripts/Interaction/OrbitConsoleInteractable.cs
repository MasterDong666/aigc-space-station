using UnityEngine;
using UnityEngine.UI;

public class OrbitConsoleInteractable : MonoBehaviour, IInteractable
{
    [SerializeField]
    private string interactionPrompt =
        "操作 星际轨道控制台";

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private PlayerInteractor playerInteractor;
    [SerializeField] private MiniGameTerminalFlowLink flowLink;
    [SerializeField] private MiniGameScenePortal scenePortal;

    public string InteractionPrompt =>
        flowLink == null
            ? interactionPrompt
            : flowLink.GetInteractionPrompt(interactionPrompt);
    public bool IsOpen =>
        panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        if (playerInteractor == null)
        {
            playerInteractor = FindObjectOfType<PlayerInteractor>();
        }

        if (flowLink == null)
        {
            flowLink = GetComponent<MiniGameTerminalFlowLink>();
        }

        if (scenePortal == null)
        {
            scenePortal = GetComponent<MiniGameScenePortal>();
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
        }
    }

    public void Interact()
    {
        if (IsOpen)
        {
            return;
        }

        if (flowLink != null && !flowLink.TryBegin())
        {
            return;
        }

        if (scenePortal != null)
        {
            if (playerInteractor != null)
            {
                playerInteractor.SetPlayerControlLocked(true);
            }

            if (scenePortal.TryEnter())
            {
                return;
            }

            if (playerInteractor != null)
            {
                playerInteractor.SetPlayerControlLocked(false);
            }
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        if (playerInteractor != null)
        {
            playerInteractor.SetPlayerControlLocked(true);
        }
    }

    public void ClosePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (playerInteractor != null)
        {
            playerInteractor.SetPlayerControlLocked(false);
        }
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
        }
    }
}
