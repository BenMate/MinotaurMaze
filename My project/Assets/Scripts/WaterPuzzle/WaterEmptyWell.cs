using UnityEngine;

public class WaterEmptyWell : MonoBehaviour, IInteractable
{

    [Header("Puzzle")]
    [SerializeField] private WaterBucketPuzzle puzzle;

    public void Interact(PlayerController player)
    {
        if (player == null) return;

        if (!puzzle.HasBucket)return;

        WaterBucket bucket = puzzle.Bucket3L;

        if (bucket == null) return;

        //nothing to empty
        if(bucket.CurrentAmount <= 0) return;

        //empty the entire carried bucket
        puzzle.Empty(bucket);

        Debug.Log(
            "[WATER PUZZLE] 3L bucket emptied at empty well."
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player =
            other.GetComponent<PlayerController>();

        if (player != null)
        {
            player.SetCurrentWaterInteractable(this);
        }
    }


    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player =
            other.GetComponent<PlayerController>();

        if (player != null)
        {
            player.ClearCurrentWaterInteractable(this);
        }
    }
}

