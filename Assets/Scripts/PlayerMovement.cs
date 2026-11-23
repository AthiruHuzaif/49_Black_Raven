using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Horizontal move speed in meters per second.")]
    public float moveSpeed = 6f;

    [Tooltip("Acceleration towards the target speed. 0 means instant.")]
    public float acceleration = 60f;

    [Tooltip("Allow changing horizontal direction while airborne.")]
    public bool airControl = true;

    [Header("Jump")]
    [Tooltip("Jump apex height in meters. It is converted with the Rigidbody2D gravity.")]
    public float jumpHeight = 2f;

    [Header("Dash")]
    [Tooltip("Speed of the dash in meters per second.")]
    public float dashSpeed = 18f;

    [Tooltip("Duration of the dash in seconds.")]
    public float dashDuration = 0.25f;

    [Tooltip("Cooldown time between dashes in seconds.")]
    public float dashCooldown = 1.2f;

    [Tooltip("Allow dashing while airborne.")]
    public bool airDash = true;

    [Tooltip("If true, vertical velocity is preserved during the dash. If false, vertical velocity is zeroed.")]
    public bool preserveVerticalVelocity = true;

    [Header("Downslam")]
    [Tooltip("Downward speed of the downslam in meters per second.")]
    public float downslamSpeed = 30f;

    [Tooltip("Duration of the downslam in seconds.")]
    public float downslamDuration = 0.4f;

    [Tooltip("Cooldown time between downslam uses in seconds.")]
    public float downslamCooldown = 0.6f;

    [Tooltip("Minimum downward input value to trigger a downslam (0 to 1).")]
    public float downslamInputThreshold = 0.5f;

    [Tooltip("Fallback gravity used when Rigidbody2D Gravity Scale is 0.")]
    public float fallbackGravity = 9.81f;

    [Header("Ground Check")]
    [Tooltip("Optional check point at the feet. Leave empty to use the collider bottom center.")]
    public Transform groundCheck;

    [Tooltip("Radius of the ground check circle.")]
    public float groundCheckRadius = 0.15f;

    [Tooltip("Layers treated as ground.")]
    public LayerMask groundLayer = ~0;

    [Header("Visual")]
    [Tooltip("Flip the sprite horizontally to face the move direction.")]
    public bool flipSprite = true;

    [Header("Input")]
    [Tooltip("Input action for movement (Vector2). Assign from your Input Action asset.")]
    public InputActionReference moveAction;

    [Tooltip("Input action for jump (Button). Assign from your Input Action asset.")]
    public InputActionReference jumpAction;

    [Tooltip("Input action for dash (Button). Assign from your Input Action asset.")]
    public InputActionReference dashAction;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveInput;
    private bool jumpRequested;
    private bool dashRequested;
    private float dashCooldownTimer;
    private float dashTimer;
    private bool isDashing;
    private bool downslamRequested;
    private float downslamCooldownTimer;
    private float downslamTimer;
    private bool isSlamming;
    private bool wasDownPressed;
    private ContactFilter2D groundFilter;
    private readonly Collider2D[] groundHits = new Collider2D[8];

    public bool IsGrounded { get { return CheckGrounded(); } }

    /// <summary>True while a dash is currently active.</summary>
    public bool IsDashing { get { return isDashing; } }

    /// <summary>True while a downslam is currently active.</summary>
    public bool IsSlamming { get { return isSlamming; } }

    private Vector2 Velocity
    {
        get
        {
#if UNITY_6000_0_OR_NEWER //wrote this to test on older version of ubnity and return to current one to test optimization
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }
        set
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = value;
#else
            rb.velocity = value;
#endif
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rb.freezeRotation = true;
        groundFilter = new ContactFilter2D { useTriggers = false };
    }
//these are to enable playermovement to work with the new input sysstem and to disable it when not needed
    private void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();
        dashAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        jumpAction.action.Disable();
        dashAction.action.Disable();
    }
//these are the main update and fixedupdate functions that handle player input and movementt
    private void Update()
    {
        moveInput = ReadMoveInput();

        if (ReadJumpPressed()) jumpRequested = true;

        if (ReadDashPressed()) dashRequested = true;

        // Downslam input: S / Down key pressed while airborne (edge detection)
        bool downPressed = moveInput.y < -downslamInputThreshold;
        if (!wasDownPressed && downPressed && !IsGrounded)
        {
            downslamRequested = true;
        }
        wasDownPressed = downPressed;

        if (flipSprite && spriteRenderer != null && Mathf.Abs(moveInput.x) > 0.01f)
        {
            spriteRenderer.flipX = moveInput.x < 0f;
        }
    }

    private void FixedUpdate()
    {
        bool grounded = CheckGrounded();

        // Dash cooldown timer
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.fixedDeltaTime;
        }

        // Dash duration timer
        if (isDashing)
        {
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                dashCooldownTimer = dashCooldown;
            }
        }

        // Attempt to start a dash
        if (dashRequested && !isDashing && dashCooldownTimer <= 0f && (grounded || airDash))
        {
            StartDash();
        }
        dashRequested = false;

        // Downslam cooldown timer
        if (downslamCooldownTimer > 0f)
        {
            downslamCooldownTimer -= Time.fixedDeltaTime;
        }

        // Downslam duration timer
        if (isSlamming)
        {
            downslamTimer -= Time.fixedDeltaTime;
            if (downslamTimer <= 0f)
            {
                isSlamming = false;
                downslamCooldownTimer = downslamCooldown;
            }
        }

        // Attempt to start a downslam (mid-air only, not while dashing)
        if (downslamRequested && !isSlamming && !isDashing && downslamCooldownTimer <= 0f && !grounded)
        {
            isSlamming = true;
            downslamTimer = downslamDuration;
            downslamRequested = false;
        }
        downslamRequested = false;

        // Apply downslam velocity (overrides normal movement)
        if (isSlamming)
        {
            // Keep horizontal momentum if holding left/right; otherwise go straight down
            float slamX = Mathf.Abs(moveInput.x) > 0.01f ? moveInput.x * moveSpeed : 0f;
            Velocity = new Vector2(slamX, -downslamSpeed);
        }
        // Normal movement (disabled while dashing or slamming)
        else if (!isDashing && (grounded || airControl))
        {
            float targetSpeed = moveInput.x * moveSpeed;
            float newSpeed = acceleration <= 0f
                ? targetSpeed
                : Mathf.MoveTowards(Velocity.x, targetSpeed, acceleration * Time.fixedDeltaTime);
            Velocity = new Vector2(newSpeed, Velocity.y);
        }

        // Jump
        if (jumpRequested && grounded && Velocity.y <= 0.01f)
        {
            Velocity = new Vector2(Velocity.x, GetJumpSpeed());
        }

        jumpRequested = false;
    }

    ///Begins a dash in the current move direction (or facing direction if idle).
    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;

        Vector2 dashDirection = moveInput;
        if (dashDirection.magnitude < 0.01f)
        {
            // No move input: dash in the direction the sprite is facing
            dashDirection = new Vector2(spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f, 0f);
        }
        dashDirection.Normalize();

        float dashX = dashDirection.x * dashSpeed;
        float dashY = preserveVerticalVelocity ? Velocity.y : 0f;
        Velocity = new Vector2(dashX, dashY);
    }

    private float GetJumpSpeed()
    {
        float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
        if (gravity < 0.01f) gravity = Mathf.Max(0.01f, fallbackGravity);
        return Mathf.Sqrt(2f * gravity * Mathf.Max(0.01f, jumpHeight));
    }

    private bool CheckGrounded()
    {
        groundFilter.SetLayerMask(groundLayer);

        int count = Physics2D.OverlapCircle(GetCheckPosition(), groundCheckRadius, groundFilter, groundHits);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = groundHits[i];
            if (hit == null || hit == bodyCollider) continue;
            return true;
        }

        return false;
    }

    private Vector2 GetCheckPosition()
    {
        if (groundCheck != null) return groundCheck.position;

        if (bodyCollider != null)
        {
            Bounds bounds = bodyCollider.bounds;
            return new Vector2(bounds.center.x, bounds.min.y + 0.02f);
        }

        return transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Vector3 center;
        if (groundCheck != null)
        {
            center = groundCheck.position;
        }
        else
        {
            Collider2D col = GetComponent<Collider2D>();
            center = col != null
                ? new Vector3(col.bounds.center.x, col.bounds.min.y + 0.02f, 0f)
                : transform.position;
        }

        Gizmos.DrawWireSphere(center, groundCheckRadius);
    }

    private Vector2 ReadMoveInput()
    {
        return moveAction.action.ReadValue<Vector2>();
    }

    private bool ReadJumpPressed()
    {
        return jumpAction.action.WasPressedThisFrame();
    }

    private bool ReadDashPressed()
    {
        return dashAction.action.WasPressedThisFrame();
    }
}