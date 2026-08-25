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

    public string InteractionPrompt => interactionPrompt;
    public bool IsOpen =>
        panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        if (playerInteractor == null)
        {
            playerInteractor = FindObjectOfType<PlayerInteractor>();
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
