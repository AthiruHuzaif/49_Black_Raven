using UnityEngine;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

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

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveInput;
    private bool jumpRequested;
    private ContactFilter2D groundFilter;
    private readonly Collider2D[] groundHits = new Collider2D[8];

    public bool IsGrounded { get { return CheckGrounded(); } }

    private Vector2 Velocity
    {
        get
        {
#if UNITY_6000_0_OR_NEWER
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

    private void Update()
    {
        moveInput = ReadMoveInput();

        if (ReadJumpPressed()) jumpRequested = true;

        if (flipSprite && spriteRenderer != null && Mathf.Abs(moveInput.x) > 0.01f)
        {
            spriteRenderer.flipX = moveInput.x < 0f;
        }
    }

    private void FixedUpdate()
    {
        bool grounded = CheckGrounded();

        if (grounded || airControl)
        {
            float targetSpeed = moveInput.x * moveSpeed;
            float newSpeed = acceleration <= 0f
                ? targetSpeed
                : Mathf.MoveTowards(Velocity.x, targetSpeed, acceleration * Time.fixedDeltaTime);
            Velocity = new Vector2(newSpeed, Velocity.y);
        }

        if (jumpRequested && grounded && Velocity.y <= 0.01f)
        {
            Velocity = new Vector2(Velocity.x, GetJumpSpeed());
        }

        jumpRequested = false;
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

    private static Vector2 ReadMoveInput()
    {
#if ENABLE_LEGACY_INPUT_MANAGER || !ENABLE_INPUT_SYSTEM
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#else
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return Vector2.zero;

        float x = 0f;
        float y = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;
        return new Vector2(x, y);
#endif
    }

    private static bool ReadJumpPressed()
    {
#if ENABLE_LEGACY_INPUT_MANAGER || !ENABLE_INPUT_SYSTEM
        return Input.GetButtonDown("Jump");
#else
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
#endif
    }
}