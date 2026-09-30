using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaterBucketHUD : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image bucketImage;
    [SerializeField] private TMP_Text amountText;

    [Header("Bucket Sprites")]
    [Tooltip("Sprite 0 = empty, 1 = 1L, 2 = 2L, 3 = 3L")]
    [SerializeField] private Sprite[] bucketSprites;

    private WaterBucket carriedBucket;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (carriedBucket == null)
            return;

        UpdateHUD();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        carriedBucket = null;
    }

    public void Show(WaterBucket bucket)
    {
        carriedBucket = bucket;

        gameObject.SetActive(true);

        UpdateHUD();
    }

    private void UpdateHUD()
    {
        if (carriedBucket == null)
            return;

        // Display: 0L, 1L, 2L, or 3L
        if (amountText != null)
        {
            amountText.text =
                $"{carriedBucket.CurrentAmount}L";
        }

        // Change bucket image to match current amount.
        if (bucketImage != null &&
            bucketSprites != null &&
            carriedBucket.CurrentAmount >= 0 &&
            carriedBucket.CurrentAmount < bucketSprites.Length)
        {
            Sprite sprite =
                bucketSprites[carriedBucket.CurrentAmount];

            if (sprite != null)
                bucketImage.sprite = sprite;
        }
    }
}