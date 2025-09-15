using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("- Move Settings -")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("- Components -")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerInput inputs;

    void Awake()
    {
        // Auto-assign components if missing
        rb ??= GetComponent<Rigidbody2D>();
        inputs ??= GetComponent<PlayerInput>();

        if (rb == null)
            Debug.LogError("PlayerController requires a Rigidbody2D!");
        if (inputs == null)
            Debug.LogError("PlayerController requires a PlayerInput!");

        // Freeze rotation and disable gravity for top-down movement
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void FixedUpdate()
    {
        if (inputs == null) return;

        Vector2 movement = inputs.moveInputs;
        if (movement.magnitude > 1f) movement.Normalize();
        rb.linearVelocity = movement * moveSpeed;
    }
}

