using UnityEngine;

public class WaterBucket : MonoBehaviour, IInteractable
{
    [Header("Bucket")]
    [SerializeField] private int capacity = 3;

    [Header("Bucket Sprites")]
    [Tooltip("Sprite 0 = empty, 1 = 1L, 2 = 2L, etc.")]
    [SerializeField] private Sprite[] bucketSprites;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private WaterBucketPuzzle puzzle;

    private int currentAmount;


    // ---------------------------------------------------------
    // PROPERTIES
    // ---------------------------------------------------------

    public int Capacity => capacity;

    public int CurrentAmount => currentAmount;


    // ---------------------------------------------------------
    // UNITY
    // ---------------------------------------------------------

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer =
                GetComponent<SpriteRenderer>();

        currentAmount = 0;

        UpdateSprite();
    }


    // ---------------------------------------------------------
    // PUZZLE REFERENCE
    // ---------------------------------------------------------

    public void SetPuzzle(
        WaterBucketPuzzle waterPuzzle)
    {
        puzzle = waterPuzzle;
    }


    // ---------------------------------------------------------
    // SET AMOUNT
    // ---------------------------------------------------------

    public void SetAmount(int amount)
    {
        currentAmount =
            Mathf.Clamp(
                amount,
                0,
                capacity
            );

        UpdateSprite();
    }


    // ---------------------------------------------------------
    // UPDATE SPRITE
    // ---------------------------------------------------------

    private void UpdateSprite()
    {
        if (spriteRenderer == null)
            return;

        if (bucketSprites == null ||
            bucketSprites.Length == 0)
            return;

        if (currentAmount < 0 ||
            currentAmount >= bucketSprites.Length)
            return;

        if (bucketSprites[currentAmount] != null)
        {
            spriteRenderer.sprite =
                bucketSprites[currentAmount];
        }
    }


    // ---------------------------------------------------------
    // INTERACTION
    // ---------------------------------------------------------

    public void Interact(PlayerController player)
    {
        if (puzzle == null)
            return;

        // This is a stationary bucket.
        //
        // The player's carried bucket is always
        // puzzle.Bucket3L.

        WaterBucket playerBucket =
            puzzle.Bucket3L;

        if (playerBucket == null)
            return;


        // -----------------------------------------------------
        // PLAYER HAS WATER
        // -----------------------------------------------------

        if (playerBucket.CurrentAmount > 0)
        {
            // Player always tries to GIVE water.
            //
            // If this bucket is full,
            // Pour() automatically does nothing.

            puzzle.Pour(
                playerBucket,
                this
            );

            return;
        }


        // -----------------------------------------------------
        // PLAYER HAS NO WATER
        // -----------------------------------------------------

        // Player takes water from this bucket.
        //
        // If this bucket is empty,
        // Pour() automatically does nothing.

        if (CurrentAmount > 0)
        {
            puzzle.Pour(
                this,
                playerBucket
            );
        }
    }


    // ---------------------------------------------------------
    // TRIGGER ENTER
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
    // TRIGGER EXIT
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