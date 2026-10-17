
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SafeZone : MonoBehaviour
{
    [Header("Safe Zone Settings")]
    [SerializeField] private bool showDebugMessages = true;

    private Collider2D zoneCollider;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();
        zoneCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();

        if (player == null)
            player = other.GetComponentInParent<PlayerController>();

        if (player == null)
            return;

        MinotaurAI[] minotaurs = FindObjectsByType<MinotaurAI>(
            FindObjectsSortMode.None
        );

        foreach (MinotaurAI minotaur in minotaurs)
        {
            minotaur.EnterSafeZone(this, player);
        }

        if (showDebugMessages)
            Debug.Log($"{gameObject.name}: Player entered safe zone.");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();

        if (player == null)
            player = other.GetComponentInParent<PlayerController>();

        if (player == null)
            return;

        MinotaurAI[] minotaurs = FindObjectsByType<MinotaurAI>(
            FindObjectsSortMode.None
        );

        foreach (MinotaurAI minotaur in minotaurs)
        {
            minotaur.ExitSafeZone(this, player);
        }

        if (showDebugMessages)
            Debug.Log($"{gameObject.name}: Player left safe zone.");
    }
}
