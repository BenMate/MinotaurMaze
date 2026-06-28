using UnityEngine;

public class HideHole : MonoBehaviour
{
    [SerializeField] private MinotaurAI minotaur;

    public void Interact(PlayerController player)
    {
        //tries to exit hole
        if (player.IsHidden)
        {
            player.HidePlayer(false);
            return;
        }

        //player tried to hide while beind chased
        if (minotaur != null && !minotaur.CanPlayerNotHide())
            return;

        //player is now hidden
        player.HidePlayer(true);

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();

        if (player != null) player.SetCurrentHideHole(this);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();

        if (player != null) player.ClearCurrentHideHole(this);
    }
}