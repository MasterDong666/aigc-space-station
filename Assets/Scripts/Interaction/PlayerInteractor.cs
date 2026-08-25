using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("交互检测")]
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private float interactionDistance = 2.8f;
    [SerializeField] private LayerMask interactionLayers = ~0;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    [Header("引用")]
    [SerializeField] private InteractionPromptUI promptUI;
    [SerializeField] private PlayerMovement playerMovement;

    private IInteractable currentTarget;
    private bool playerControlLocked;

    public float InteractionDistance => interactionDistance;
    public bool HasTarget => currentTarget != null;
    public bool IsPlayerControlLocked => playerControlLocked;
    public string CurrentPrompt =>
        currentTarget == null
            ? string.Empty
            : currentTarget.InteractionPrompt;

    private void Awake()
    {
        if (interactionCamera == null)
        {
            interactionCamera = GetComponentInChildren<Camera>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        if (promptUI == null)
        {
            promptUI = FindObjectOfType<InteractionPromptUI>(true);
        }

        SetTarget(null);
    }

    private void Update()
    {
        if (playerControlLocked)
        {
            SetTarget(null);
            return;
        }

        UpdateTarget();

        if (
            currentTarget != null &&
            Input.GetKeyDown(interactionKey)
        )
        {
            currentTarget.Interact();
        }
    }

    public void SetPlayerControlLocked(bool locked)
    {
        playerControlLocked = locked;

        if (playerMovement != null)
        {
            playerMovement.enabled = !locked;
        }

        if (locked)
        {
            SetTarget(null);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void UpdateTarget()
    {
        if (interactionCamera == null)
        {
            SetTarget(null);
            return;
        }

        Ray ray = interactionCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (
            Physics.Raycast(
                ray,
                out RaycastHit hit,
                interactionDistance,
                interactionLayers,
                QueryTriggerInteraction.Collide
            )
        )
        {
            SetTarget(FindInteractable(hit.collider));
            return;
        }

        SetTarget(null);
    }

    private static IInteractable FindInteractable(
        Collider hitCollider
    )
    {
        MonoBehaviour[] behaviours =
            hitCollider.GetComponentsInParent<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IInteractable interactable)
            {
                return interactable;
            }
        }

        return null;
    }

    private void SetTarget(IInteractable target)
    {
        if (ReferenceEquals(currentTarget, target))
        {
            return;
        }

        currentTarget = target;

        if (promptUI == null)
        {
            return;
        }

        if (currentTarget == null)
        {
            promptUI.Hide();
        }
        else
        {
            promptUI.Show(
                "[E] " + currentTarget.InteractionPrompt
            );
        }
    }

    private void OnValidate()
    {
        interactionDistance = Mathf.Max(
            0.1f,
            interactionDistance
        );
    }
}
