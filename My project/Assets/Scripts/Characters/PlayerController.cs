using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody2D))]
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


    void Awake()
    {
        // Auto-assign components if missing
        rb ??= GetComponent<Rigidbody2D>();
        inputs ??= GetComponent<PlayerInput>();
        cam ??= GetComponentInChildren<Camera>();

        if (rb == null)
            Debug.LogError("PlayerController requires a Rigidbody2D!");
        if (inputs == null)
            Debug.LogError("PlayerController requires a PlayerInput!");

        // Freeze rotation and disable gravity for top-down movement
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        //camera zoom
        float scroll = inputs.mouseWheel;
        if (scroll != 0.0f)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    void FixedUpdate()
    {
        if (inputs == null) return;

        Vector2 movement = inputs.moveInputs;
        if (movement.magnitude > 1f) movement.Normalize();
        
       // rb.linearVelocity = movement * moveSpeed;

        rb.MovePosition(rb.position + moveSpeed * Time.fixedDeltaTime * movement);
    }
}

