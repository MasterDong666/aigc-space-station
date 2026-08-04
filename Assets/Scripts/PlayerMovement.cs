using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float groundAcceleration = 24f;
    [Range(0f, 1f)]
    [SerializeField] private float airControl = 0.35f;

    [Header("跳跃与重力")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float fallGravityMultiplier = 1.8f;
    [SerializeField] private float lowJumpGravityMultiplier = 2.2f;
    [SerializeField] private float coyoteTime = 0.10f;
    [SerializeField] private float jumpBufferTime = 0.15f;

    [Header("耐力设置")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float sprintStaminaDrainPerSecond = 22f;
    [SerializeField] private float staminaRecoveryPerSecond = 18f;
    [SerializeField] private float staminaRecoveryDelay = 0.8f;
    [SerializeField] private float minimumStaminaToSprint = 1f;

    [Header("鬼跳设置")]
    [SerializeField] private bool enableBunnyHop = true;
    [SerializeField] private float bunnyHopSpeedMultiplier = 1.08f;
    [SerializeField] private float maxBunnyHopSpeed = 11.5f;
    [SerializeField] private float bunnyHopStaminaRefund = 8f;

    [Header("视角设置")]
    [Tooltip("把 Player 子物体中的 Camera 拖到这里")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float lookSensitivity = 120f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("键盘备用视角（IJKL）")]
    [SerializeField] private bool enableKeyboardLook = true;
    [SerializeField] private float keyboardLookSpeed = 90f;

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float cameraPitch;

    private float currentStamina;
    private float staminaRecoveryTimer;
    private float coyoteTimer;
    private float jumpBufferTimer;

    public float CurrentStamina => currentStamina;
    public float StaminaNormalized =>
        maxStamina <= 0f ? 0f : currentStamina / maxStamina;

    public bool IsSprinting { get; private set; }
    public bool IsGrounded => controller != null && controller.isGrounded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        currentStamina = maxStamina;

        if (playerCamera == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>();

            if (childCamera != null)
            {
                playerCamera = childCamera.transform;
            }
            else
            {
                Debug.LogError(
                    "PlayerMovement：没有找到玩家摄像机，请把 Player 子物体中的 Camera 拖到 Player Camera 槽位。",
                    this
                );
            }
        }
    }

    private void Start()
    {
        LockCursor();
    }

    private void Update()
    {
        HandleMovement();
        HandleLook();
        HandleCursor();
    }

    private void HandleMovement()
    {
        bool grounded = controller.isGrounded;

        UpdateJumpTimers(grounded);

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        Vector2 input = Vector2.ClampMagnitude(
            new Vector2(horizontalInput, verticalInput),
            1f
        );

        Vector3 inputDirection =
            transform.right * input.x +
            transform.forward * input.y;

        bool isMoving = input.sqrMagnitude > 0.01f;
        bool sprintHeld =
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);

        bool jumpRequested =
            jumpBufferTimer > 0f &&
            coyoteTimer > 0f;

        bool canUseSprint =
            currentStamina >= minimumStaminaToSprint;

        IsSprinting =
            sprintHeld &&
            isMoving &&
            canUseSprint &&
            grounded;

        bool performedBunnyHop =
            jumpRequested &&
            enableBunnyHop &&
            sprintHeld &&
            isMoving;

        UpdateHorizontalVelocity(
            inputDirection,
            input.magnitude,
            grounded,
            IsSprinting,
            performedBunnyHop
        );

        if (jumpRequested)
        {
            PerformJump(performedBunnyHop);
        }

        UpdateStamina(IsSprinting, performedBunnyHop);
        ApplyGravity(grounded);

        Vector3 finalVelocity =
            horizontalVelocity +
            Vector3.up * verticalVelocity;

        controller.Move(finalVelocity * Time.deltaTime);
    }

    private void UpdateJumpTimers(bool grounded)
    {
        if (grounded)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }
    }

    private void UpdateHorizontalVelocity(
        Vector3 inputDirection,
        float inputMagnitude,
        bool grounded,
        bool sprinting,
        bool aboutToBunnyHop
    )
    {
        if (grounded)
        {
            if (aboutToBunnyHop)
            {
                Vector3 hopDirection =
                    inputDirection.sqrMagnitude > 0.001f
                        ? inputDirection.normalized
                        : transform.forward;

                float baseSpeed = Mathf.Max(
                    horizontalVelocity.magnitude,
                    sprintSpeed
                );

                float boostedSpeed = Mathf.Min(
                    baseSpeed * bunnyHopSpeedMultiplier,
                    maxBunnyHopSpeed
                );

                horizontalVelocity =
                    hopDirection * boostedSpeed;

                return;
            }

            float targetSpeed =
                sprinting ? sprintSpeed : walkSpeed;

            Vector3 targetVelocity =
                inputDirection.normalized *
                targetSpeed *
                inputMagnitude;

            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                targetVelocity,
                groundAcceleration * Time.deltaTime
            );
        }
        else
        {
            float targetSpeed =
                Input.GetKey(KeyCode.LeftShift) ||
                Input.GetKey(KeyCode.RightShift)
                    ? sprintSpeed
                    : walkSpeed;

            Vector3 targetVelocity =
                inputDirection.normalized *
                targetSpeed *
                inputMagnitude;

            float airAcceleration =
                groundAcceleration *
                airControl;

            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                targetVelocity,
                airAcceleration * Time.deltaTime
            );
        }
    }

    private void PerformJump(bool performedBunnyHop)
    {
        verticalVelocity = Mathf.Sqrt(
            jumpHeight * -2f * gravity
        );

        jumpBufferTimer = 0f;
        coyoteTimer = 0f;

        if (performedBunnyHop)
        {
            currentStamina = Mathf.Min(
                maxStamina,
                currentStamina + bunnyHopStaminaRefund
            );

            staminaRecoveryTimer = 0f;
        }
    }

    private void ApplyGravity(bool grounded)
    {
        if (grounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
            return;
        }

        float gravityMultiplier = 1f;

        if (verticalVelocity < 0f)
        {
            gravityMultiplier = fallGravityMultiplier;
        }
        else if (
            verticalVelocity > 0f &&
            !Input.GetKey(KeyCode.Space)
        )
        {
            gravityMultiplier = lowJumpGravityMultiplier;
        }

        verticalVelocity +=
            gravity *
            gravityMultiplier *
            Time.deltaTime;
    }

    private void UpdateStamina(
        bool sprinting,
        bool performedBunnyHop
    )
    {
        if (performedBunnyHop)
        {
            return;
        }

        if (sprinting)
        {
            currentStamina -=
                sprintStaminaDrainPerSecond *
                Time.deltaTime;

            currentStamina = Mathf.Max(
                currentStamina,
                0f
            );

            staminaRecoveryTimer =
                staminaRecoveryDelay;
        }
        else
        {
            if (staminaRecoveryTimer > 0f)
            {
                staminaRecoveryTimer -=
                    Time.deltaTime;
            }
            else
            {
                currentStamina +=
                    staminaRecoveryPerSecond *
                    Time.deltaTime;

                currentStamina = Mathf.Min(
                    currentStamina,
                    maxStamina
                );
            }
        }
    }

    private void HandleLook()
    {
        if (playerCamera == null)
        {
            return;
        }

        float horizontalLook =
            Input.GetAxis("Mouse X") *
            lookSensitivity *
            Time.deltaTime;

        float verticalLook =
            Input.GetAxis("Mouse Y") *
            lookSensitivity *
            Time.deltaTime;

        if (enableKeyboardLook)
        {
            float keyboardHorizontal = 0f;
            float keyboardVertical = 0f;

            if (Input.GetKey(KeyCode.J))
            {
                keyboardHorizontal -= 1f;
            }

            if (Input.GetKey(KeyCode.L))
            {
                keyboardHorizontal += 1f;
            }

            if (Input.GetKey(KeyCode.I))
            {
                keyboardVertical += 1f;
            }

            if (Input.GetKey(KeyCode.K))
            {
                keyboardVertical -= 1f;
            }

            horizontalLook +=
                keyboardHorizontal *
                keyboardLookSpeed *
                Time.deltaTime;

            verticalLook +=
                keyboardVertical *
                keyboardLookSpeed *
                Time.deltaTime;
        }

        transform.Rotate(
            Vector3.up * horizontalLook
        );

        cameraPitch -= verticalLook;
        cameraPitch = Mathf.Clamp(
            cameraPitch,
            -maxLookAngle,
            maxLookAngle
        );

        playerCamera.localRotation =
            Quaternion.Euler(
                cameraPitch,
                0f,
                0f
            );
    }

    private void HandleCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible = true;
        }

        if (
            Input.GetMouseButtonDown(0) &&
            Cursor.lockState !=
            CursorLockMode.Locked
        )
        {
            LockCursor();
        }
    }

    private static void LockCursor()
    {
        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;
    }

    private void OnValidate()
    {
        walkSpeed = Mathf.Max(0f, walkSpeed);
        sprintSpeed = Mathf.Max(walkSpeed, sprintSpeed);
        maxStamina = Mathf.Max(1f, maxStamina);

        jumpHeight = Mathf.Max(0.1f, jumpHeight);
        gravity = Mathf.Min(-0.1f, gravity);
        fallGravityMultiplier = Mathf.Max(
            1f,
            fallGravityMultiplier
        );

        lowJumpGravityMultiplier = Mathf.Max(
            1f,
            lowJumpGravityMultiplier
        );

        bunnyHopSpeedMultiplier = Mathf.Max(
            1f,
            bunnyHopSpeedMultiplier
        );

        maxBunnyHopSpeed = Mathf.Max(
            sprintSpeed,
            maxBunnyHopSpeed
        );
    }
}
