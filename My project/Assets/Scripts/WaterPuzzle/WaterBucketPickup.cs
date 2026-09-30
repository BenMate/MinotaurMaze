using UnityEngine;

public class WaterBucketPickup : MonoBehaviour
{
    [Header("Puzzle")]
    [SerializeField] private WaterBucketPuzzle puzzle;

    [Header("Dialogue")]
    [SerializeField] private NPCDialogue dialogue;

    [Header("HUD")]
    [SerializeField] private WaterBucketHUD bucketHUD;

    private bool pickedUp;


    // ---------------------------------------------------------
    // UNITY
    // ---------------------------------------------------------

    private void Awake()
    {
        if (dialogue == null)
            dialogue = GetComponent<NPCDialogue>();

        if (dialogue != null)
        {
            dialogue.ConversationCompleted +=
                OnPickupDialogueFinished;
        }
    }


    private void OnDestroy()
    {
        if (dialogue != null)
        {
            dialogue.ConversationCompleted -=
                OnPickupDialogueFinished;
        }
    }


    // ---------------------------------------------------------
    // PICKUP COMPLETE
    // ---------------------------------------------------------

    private void OnPickupDialogueFinished(PlayerController player)
    {
        if (pickedUp)
            return;

        if (puzzle == null)
        {
            Debug.LogWarning(
                "[WATER PUZZLE] No WaterBucketPuzzle assigned."
            );

            return;
        }

        pickedUp = true;


        // -----------------------------------------------------
        // GIVE THE PUZZLE THE ACTUAL PLAYER
        // -----------------------------------------------------

        puzzle.SetCurrentPlayer(player);


        // -----------------------------------------------------
        // STOP THE PLAYER FROM DETECTING THIS NPC
        // -----------------------------------------------------

        if (player != null &&
            dialogue != null)
        {
            player.ClearCurrentNPC(dialogue);
        }


        // -----------------------------------------------------
        // SHOW CARRIED BUCKET HUD
        // -----------------------------------------------------

        if (bucketHUD != null)
        {
            bucketHUD.Show(
                puzzle.Bucket3L
            );
        }


        // -----------------------------------------------------
        // REMOVE PHYSICAL BUCKET
        // -----------------------------------------------------

        gameObject.SetActive(false);


        Debug.Log(
            "[WATER PUZZLE] Player picked up the 3L bucket."
        );
    }
}