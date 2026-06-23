using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("- Move Settings -")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("- Cam Settings -")]
    [SerializeField] private float zoomSpeed = 5.0f;
    [SerializeField] private float minZoom = 1.0f;
    [SerializeField] private float maxZoom = 10.0f;

    [Header("- Components -")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerInput inputs;
    [SerializeField] private Camera cam;
    [SerializeField] private Animator animator;

    // Stores the last valid direction so idle faces correctly
    private Vector2 lastMoveDirection = Vector2.down;

    void Awake()
    {
        rb ??= GetComponent<Rigidbody2D>();
        inputs ??= GetComponent<PlayerInput>();
        cam ??= GetComponentInChildren<Camera>();
        animator ??= GetComponent<Animator>();

        if (rb == null)
            Debug.LogError("PlayerController requires a Rigidbody2D!");

        if (inputs == null)
            Debug.LogError("PlayerController requires a PlayerInput!");

        if (animator == null)
            Debug.LogError("PlayerController requires an Animator!");

        // Freeze rotation and disable gravity for top-down movement
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        // Camera zoom
        float scroll = inputs.mouseWheel;

        if (scroll != 0.0f)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(
                cam.orthographicSize,
                minZoom,
                maxZoom
            );
        }
    }

    void FixedUpdate()
    {
        if (inputs == null)
            return;

        Vector2 movement = inputs.moveInputs;

        if (movement.magnitude > 1f)
            movement.Normalize();

        UpdateAnimation(movement);

        rb.MovePosition(
            rb.position + moveSpeed * Time.fixedDeltaTime * movement
        );
    }

    void UpdateAnimation(Vector2 movement)
    {
        bool isMoving = movement.magnitude > 0.05f;
        animator.SetBool("IsMoving", isMoving);

        if (isMoving)
        {
            Vector2 dir = movement.normalized;

            // 4-direction lock with horizontal priority
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            {
                lastMoveDirection = new Vector2(Mathf.Sign(dir.x), 0f);
            }
            else
            {
                lastMoveDirection = new Vector2(0f, Mathf.Sign(dir.y));
            }
        }

        animator.SetFloat("MoveX", lastMoveDirection.x);
        animator.SetFloat("MoveY", lastMoveDirection.y);
    }
}