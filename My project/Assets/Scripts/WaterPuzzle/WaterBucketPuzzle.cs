using UnityEngine;

public class WaterBucketPuzzle : MonoBehaviour
{
    [Header("Buckets")]
    [SerializeField] private WaterBucket bucket3L;
    [SerializeField] private WaterBucket bucket4L;
    [SerializeField] private WaterBucket bucket5L;

    [Header("Door")]
    [Tooltip("Closed door GameObject. Disabled when puzzle is solved.")]
    [SerializeField] private GameObject lockedDoor;

    [Tooltip("Open door GameObject. Enabled when puzzle is solved.")]
    [SerializeField] private GameObject openDoor;

    [Header("Solved Dialogue")]
    [SerializeField] private NPCDialogue solvedDialogue;

    [Header("HUD")]
    [SerializeField] private WaterBucketHUD bucketHUD;

    private bool puzzleSolved;

    // The actual player who picked up the bucket.
    private PlayerController currentPlayer;


    // ---------------------------------------------------------
    // PROPERTIES
    // ---------------------------------------------------------

    public WaterBucket Bucket3L => bucket3L;

    public WaterBucket Bucket4L => bucket4L;

    public WaterBucket Bucket5L => bucket5L;

    public bool PuzzleSolved => puzzleSolved;


    // ---------------------------------------------------------
    // UNITY
    // ---------------------------------------------------------

    private void Awake()
    {
        if (bucket3L != null)
            bucket3L.SetPuzzle(this);

        if (bucket4L != null)
            bucket4L.SetPuzzle(this);

        if (bucket5L != null)
            bucket5L.SetPuzzle(this);


        // Puzzle starts locked.

        if (lockedDoor != null)
            lockedDoor.SetActive(true);

        if (openDoor != null)
            openDoor.SetActive(false);
    }


    // ---------------------------------------------------------
    // PLAYER
    // ---------------------------------------------------------

    public void SetCurrentPlayer(PlayerController player)
    {
        if (player == null)
            return;

        currentPlayer = player;
    }


    // ---------------------------------------------------------
    // CHECK PUZZLE
    // ---------------------------------------------------------

    public void CheckPuzzle()
    {
        if (puzzleSolved)
            return;

        if (bucket5L == null)
            return;


        // Goal:
        //
        // 5L bucket contains exactly 4L.

        if (bucket5L.CurrentAmount == 4)
        {
            SolvePuzzle();
        }
    }


    // ---------------------------------------------------------
    // SOLVE
    // ---------------------------------------------------------

    private void SolvePuzzle()
    {
        if (puzzleSolved)
            return;

        puzzleSolved = true;

        Debug.Log(
            "[WATER PUZZLE] SOLVED! 5L bucket contains exactly 4L."
        );


        // -----------------------------------------------------
        // OPEN DOOR
        // -----------------------------------------------------

        if (lockedDoor != null)
            lockedDoor.SetActive(false);

        if (openDoor != null)
            openDoor.SetActive(true);


        // -----------------------------------------------------
        // HIDE BUCKET HUD
        // -----------------------------------------------------

        if (bucketHUD != null)
            bucketHUD.Hide();


        // -----------------------------------------------------
        // SOLVED DIALOGUE
        // -----------------------------------------------------

        if (solvedDialogue != null &&
            currentPlayer != null)
        {
            solvedDialogue.TriggerConversation(
                currentPlayer
            );
        }
    }


    // ---------------------------------------------------------
    // FILL 3L BUCKET
    // ---------------------------------------------------------

    public void Fill3LBucket()
    {
        if (puzzleSolved)
            return;

        if (bucket3L == null)
            return;

        bucket3L.SetAmount(
            bucket3L.Capacity
        );

        CheckPuzzle();
    }


    // ---------------------------------------------------------
    // POUR
    // ---------------------------------------------------------

    public void Pour(
        WaterBucket from,
        WaterBucket to)
    {
        if (puzzleSolved)
            return;

        if (from == null ||
            to == null)
            return;

        if (from == to)
            return;


        // Nothing to pour if the source is empty.

        if (from.CurrentAmount <= 0)
            return;


        // Nothing to pour if the target is full.

        if (to.CurrentAmount >= to.Capacity)
            return;


        // How much room is available?

        int availableSpace =
            to.Capacity -
            to.CurrentAmount;


        // Move only as much as will fit.

        int amountToMove =
            Mathf.Min(
                from.CurrentAmount,
                availableSpace
            );


        // Remove water from source.

        from.SetAmount(
            from.CurrentAmount -
            amountToMove
        );


        // Add water to target.

        to.SetAmount(
            to.CurrentAmount +
            amountToMove
        );


        Debug.Log(
            $"[WATER PUZZLE] Poured {amountToMove}L " +
            $"from {from.Capacity}L bucket " +
            $"into {to.Capacity}L bucket."
        );


        CheckPuzzle();
    }


    // ---------------------------------------------------------
    // EMPTY BUCKET
    // ---------------------------------------------------------

    public void Empty(WaterBucket bucket)
    {
        if (puzzleSolved)
            return;

        if (bucket == null)
            return;

        if (bucket.CurrentAmount <= 0)
            return;


        bucket.SetAmount(0);


        Debug.Log(
            $"[WATER PUZZLE] Emptied {bucket.Capacity}L bucket."
        );
    }
}