using UnityEngine;

public class WaterWell : MonoBehaviour, IInteractable
{
    [Header("Puzzle")]
    [SerializeField] private WaterBucketPuzzle puzzle;


    // ---------------------------------------------------------
    // INTERACTION
    // ---------------------------------------------------------

    public void Interact(PlayerController player)
    {
        if (puzzle == null)
            return;

        if (!puzzle.HasBucket)
            return;

        WaterBucket bucket =
            puzzle.Bucket3L;

        if (bucket == null)
            return;


        if (bucket.CurrentAmount >=
            bucket.Capacity)
        {
            Debug.Log(
                "[WATER PUZZLE] " +
                "3L bucket is already full."
            );

            return;
        }


        puzzle.Fill3LBucket();


        Debug.Log(
            "[WATER PUZZLE] " +
            "3L bucket filled at the well."
        );
    }


    // ---------------------------------------------------------
    // PLAYER ENTERS WELL
    // ---------------------------------------------------------

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        PlayerController player =
            other.GetComponent<PlayerController>();

        if (player != null)
        {
            player.SetCurrentWaterInteractable(
                this
            );
        }
    }


    // ---------------------------------------------------------
    // PLAYER LEAVES WELL
    // ---------------------------------------------------------

    private void OnTriggerExit2D(
        Collider2D other)
    {
        PlayerController player =
            other.GetComponent<PlayerController>();

        if (player != null)
        {
            player.ClearCurrentWaterInteractable(
                this
            );
        }
    }
}