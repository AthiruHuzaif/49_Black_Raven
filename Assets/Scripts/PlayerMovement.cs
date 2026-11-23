using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody2D playerRb;
    public float speed = 5f;
    private float input;

    void Awake()
    {
        playerRb.constraints |= RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        input = 0f;
        if (Keyboard.current == null) return;

        input = (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 10f : 0f)
              - (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed ? 10f : 0f);
    }

    void FixedUpdate()
    {
        playerRb.linearVelocity = new Vector2(input * speed, playerRb.linearVelocity.y);
    }
}
