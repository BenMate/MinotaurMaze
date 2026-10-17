
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TeleportDoor : MonoBehaviour, IInteractable
{
    [Header("Destination")]
    [SerializeField] private Transform destination;

    [Header("Door Settings")]
    [SerializeField] private bool isLocked = false;

    [Header("Screen Fade")]
    [SerializeField] private CanvasGroup screenFade;
    [SerializeField, Min(0f)] private float fadeDuration = 0.25f;

    private bool isTeleporting;

    public bool IsLocked => isLocked;
    public bool IsTeleporting => isTeleporting;

    private void Awake()
    {
        Collider2D doorCollider = GetComponent<Collider2D>();
        doorCollider.isTrigger = true;

        if (screenFade != null)
        {
            screenFade.alpha = 0f;
            screenFade.blocksRaycasts = false;
            screenFade.interactable = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();

        if (player == null)
            player = other.GetComponentInParent<PlayerController>();

        if (player != null)
            player.SetCurrentTeleportDoor(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();

        if (player == null)
            player = other.GetComponentInParent<PlayerController>();

        if (player != null)
            player.ClearCurrentTeleportDoor(this);
    }

    public void Interact(PlayerController player)
    {
        if (player == null || isTeleporting)
            return;

        if (isLocked)
        {
            Debug.Log($"{gameObject.name} is locked.");
            return;
        }

        if (destination == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: No destination has been assigned.",
                this
            );
            return;
        }

        if (screenFade == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: Assign a CanvasGroup for the screen fade.",
                this
            );
            return;
        }

        StartCoroutine(TeleportPlayer(player));
    }

    private IEnumerator TeleportPlayer(PlayerController player)
    {
        isTeleporting = true;

        player.SetMovementLocked(true);

        screenFade.blocksRaycasts = true;

        // Fade out to black.
        yield return StartCoroutine(FadeScreen(0f, 1f));

        // Move the player to the destination.
        player.TeleportTo(destination.position);

        // Allow a physics update at the new position before fading in.
        yield return new WaitForFixedUpdate();

        // Fade back into the scene.
        yield return StartCoroutine(FadeScreen(1f, 0f));

        screenFade.blocksRaycasts = false;

        player.SetMovementLocked(false);

        isTeleporting = false;
    }

    private IEnumerator FadeScreen(float startAlpha, float targetAlpha)
    {
        if (fadeDuration <= 0f)
        {
            screenFade.alpha = targetAlpha;
            yield break;
        }

        float elapsed = 0f;
        screenFade.alpha = startAlpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            screenFade.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                Mathf.Clamp01(elapsed / fadeDuration)
            );

            yield return null;
        }

        screenFade.alpha = targetAlpha;
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
    }
}
