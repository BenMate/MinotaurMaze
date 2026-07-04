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
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private CapsuleCollider2D cc;

    private Vector2 lastMoveDirection = Vector2.down;
    private bool movementLocked = false;

    private float speedMultiplier = 1f;

    private HideHole currentHideHole;

    public bool IsHidden { get; private set; }

    void Awake()
    {
        rb ??= GetComponent<Rigidbody2D>();
        inputs ??= GetComponent<PlayerInput>();
        cam ??= GetComponentInChildren<Camera>();
        animator ??= GetComponent<Animator>();
        spriteRenderer ??= GetComponent<SpriteRenderer>();
        cc ??= GetComponent<CapsuleCollider2D>();

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
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

        if (inputs.interactPressed && currentHideHole != null)
        {
            currentHideHole.Interact(this);
        }
    }

    void FixedUpdate()
    {
        if (inputs == null || movementLocked)
            return;

        Vector2 movement = inputs.moveInputs;

        if (movement.magnitude > 1f)
            movement.Normalize();

        UpdateAnimation(movement);

        rb.MovePosition(
            rb.position + (moveSpeed * speedMultiplier) * Time.fixedDeltaTime * movement
        );
    }

    void UpdateAnimation(Vector2 movement)
    {
        bool isMoving = movement.magnitude > 0.05f;
        animator.SetBool("IsMoving", isMoving);

        if (isMoving)
        {
            Vector2 dir = movement.normalized;

            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                lastMoveDirection = new Vector2(Mathf.Sign(dir.x), 0f);
            else
                lastMoveDirection = new Vector2(0f, Mathf.Sign(dir.y));
        }

        animator.SetFloat("MoveX", lastMoveDirection.x);
        animator.SetFloat("MoveY", lastMoveDirection.y);
    }

    public void ApplyGrabSlow()
    {
        speedMultiplier = 0.5f; // 50% slow
        animator.speed = 0.5f;
    }

    public void ClearGrabSlow()
    {
        speedMultiplier = 1f;
        animator.speed = 1f;
    }

    public void HidePlayer(bool isPlayerHiding)
    {
        IsHidden = isPlayerHiding;
        movementLocked = isPlayerHiding;

        spriteRenderer.color = isPlayerHiding ? Color.clear : Color.white;

        if (cc != null)
            cc.enabled = !isPlayerHiding;
    }

    public void SetCurrentHideHole(HideHole hole)
    {
        currentHideHole = hole;
    }

    public void ClearCurrentHideHole(HideHole hole)
    {
        if (currentHideHole == hole)
            currentHideHole = null;
    }

    public void PlayerDied()
    {
        movementLocked = true;
        spriteRenderer.color = Color.clear;
    }
}