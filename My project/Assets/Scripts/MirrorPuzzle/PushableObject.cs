
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PushableObject : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField, Min(0.1f)] private float gridSize = 1f;

    [Header("Push Settings")]
    [SerializeField, Min(0.01f)] private float pushDuration = 0.15f;

    private Rigidbody2D rb;
    private Collider2D objectCollider;

    private Vector2 startingPosition;
    private bool isMoving;
    private Coroutine moveCoroutine;

    public bool IsMoving => isMoving;
    public Vector2 StartingPosition => startingPosition;
    public float GridSize => gridSize;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        objectCollider = GetComponent<Collider2D>();

        startingPosition = rb.position;

        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    public bool TryPush(PlayerController player)
    {
        if (isMoving || player == null)
            return false;

        // Determine the direction from the player's position.
        Vector2 offset = rb.position - (Vector2)player.transform.position;

        if (offset.sqrMagnitude < 0.001f)
            return false;

        Vector2 direction;

        if (Mathf.Abs(offset.x) >= Mathf.Abs(offset.y))
            direction = new Vector2(Mathf.Sign(offset.x), 0f);
        else
            direction = new Vector2(0f, Mathf.Sign(offset.y));

        Vector2 oldPosition = rb.position;
        Vector2 targetPosition = oldPosition + direction * gridSize;

        if (!DestinationIsClear(targetPosition, player))
            return false;

        isMoving = true;

        Collider2D playerCollider = player.GetComponent<Collider2D>();

        if (playerCollider != null)
        {
            Physics2D.IgnoreCollision(
                objectCollider,
                playerCollider,
                true
            );
        }

        moveCoroutine = StartCoroutine(
            MoveObject(targetPosition, playerCollider)
        );

        player.BeginGridPush(oldPosition, direction, pushDuration);

        return true;
    }

    private bool DestinationIsClear(
        Vector2 targetPosition,
        PlayerController player)
    {
        Vector2 checkSize = Vector2.one * (gridSize * 0.9f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            targetPosition,
            checkSize,
            0f
        );

        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit == objectCollider || hit.isTrigger)
                continue;

            if (hit.transform == transform ||
                hit.transform.IsChildOf(transform))
                continue;

            if (hit.transform == player.transform ||
                hit.transform.IsChildOf(player.transform))
                continue;

            return false;
        }

        return true;
    }

    private IEnumerator MoveObject(
        Vector2 targetPosition,
        Collider2D playerCollider)
    {
        Vector2 startPosition = rb.position;
        float elapsed = 0f;

        while (elapsed < pushDuration)
        {
            elapsed += Time.fixedDeltaTime;

            float progress = Mathf.Clamp01(elapsed / pushDuration);

            rb.MovePosition(
                Vector2.Lerp(startPosition, targetPosition, progress)
            );

            yield return new WaitForFixedUpdate();
        }

        rb.position = targetPosition;
        transform.position = targetPosition;

        if (playerCollider != null)
        {
            Physics2D.IgnoreCollision(
                objectCollider,
                playerCollider,
                false
            );
        }

        isMoving = false;
        moveCoroutine = null;
    }

    public void ResetToStartingPosition()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        isMoving = false;
        rb.position = startingPosition;
        transform.position = startingPosition;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player != null)
            player.SetCurrentPushableObject(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player != null)
            player.ClearCurrentPushableObject(this);
    }
}
