using System.Collections;
using UnityEngine;

public class HideHole : MonoBehaviour
{
    [Header("Eyes Tilemap")]
    [SerializeField] private GameObject eyesTilemap;

    [Header("AI")]
    [SerializeField] private MinotaurAI minotaur;

    private void Awake()
    {
        // Make sure the animated eyes are hidden at the start
        if (eyesTilemap != null)
            eyesTilemap.SetActive(false);
    }

    public void Interact(PlayerController player)
    {
        // Leave the hole
        if (player.IsHidden)
        {
            if (eyesTilemap != null)
                eyesTilemap.SetActive(false);

            player.HidePlayer(false);

            return;
        }

        // Begin hiding
        player.HidePlayer(true, () =>
        {
            if (eyesTilemap != null)
                eyesTilemap.SetActive(true);
        });
    }

    private IEnumerator ShowEyes()
    {
        yield return new WaitForSeconds(0.3f);

        if (eyesTilemap != null)
            eyesTilemap.SetActive(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();

        if (player != null)
            player.SetCurrentHideHole(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();

        if (player != null)
            player.ClearCurrentHideHole(this);
    }
}