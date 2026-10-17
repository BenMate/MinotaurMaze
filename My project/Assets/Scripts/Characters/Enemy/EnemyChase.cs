
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyChase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MinotaurAI minotaur;
    [SerializeField] private Transform spawnLocation;

    [Header("Teleport Radius Check")]
    [Tooltip("The Minotaur will only teleport if he is farther than this distance from the spawn location.")]
    [SerializeField, Min(0f)] private float teleportRadius = 8f;

    [Header("Trigger Settings")]
    [SerializeField] private bool unlimitedUses = false;

    [Tooltip("Number of successful activations allowed when Unlimited Uses is off.")]
    [SerializeField, Min(1)] private int maxUses = 1;

    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = true;

    private Collider2D triggerCollider;
    private int usesRemaining;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;
        usesRemaining = maxUses;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!unlimitedUses && usesRemaining <= 0)
            return;

        PlayerController player = other.GetComponent<PlayerController>();

        if (player == null)
            player = other.GetComponentInParent<PlayerController>();

        if (player == null)
            return;

        if (minotaur == null || spawnLocation == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: Assign the Minotaur and spawn location.",
                this);
            return;
        }

        // Check the Minotaur's distance from the spawn location.
        float distanceToPlayer = Vector2.Distance(
     minotaur.transform.position,
     player.transform.position
 );

        if (showDebugMessages)
        {
            Debug.Log(
                $"EnemyChase: Minotaur-player distance = {distanceToPlayer:F2}, " +
                $"required distance = {teleportRadius:F2}"
            );
        }

        // If the Minotaur is close enough, do NOT teleport him.
        if (distanceToPlayer <= teleportRadius)
        {
            if (showDebugMessages)
            {
                Debug.Log("EnemyChase: Minotaur is too close. Teleport cancelled.");
            }

            return;
        }

        // Only reached if the Minotaur is farther away than the radius.
        bool success = minotaur.TriggerAmbush(
            player.transform,
            spawnLocation.position
        );

        if (!success)
        {
            if (showDebugMessages)
                Debug.LogWarning(
                    $"{gameObject.name}: Ambush failed. Trigger can retry.",
                    this);

            return;
        }

        if (!unlimitedUses)
        {
            usesRemaining--;

            if (usesRemaining <= 0)
                triggerCollider.enabled = false;
        }

        if (showDebugMessages)
        {
            string usesText = unlimitedUses
                ? "Unlimited uses"
                : $"Uses remaining: {usesRemaining}/{maxUses}";

            Debug.Log(
                $"{gameObject.name}: Ambush triggered. {usesText}.",
                this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnLocation == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(spawnLocation.position, teleportRadius);
    }
}
