using UnityEngine;

public class NPCDialogue : MonoBehaviour, IInteractable
{
    [Header("Dialogue")]
    [SerializeField] private Conversation[] conversations;

    private int conversationIndex;

    private PlayerController currentPlayer;

    public void Interact(PlayerController player)
    {
        if (DialogueManager.Instance.IsTalking)
            return;

        currentPlayer = player;

        DialogueManager.Instance.StartConversation(
            this,
            conversations[conversationIndex]);
    }

    public void ConversationFinished()
    {
        conversationIndex = Mathf.Min(
            conversationIndex + 1,
            conversations.Length - 1);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player =
            other.GetComponent<PlayerController>();

        if (player != null)
            player.SetCurrentNPC(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player =
            other.GetComponent<PlayerController>();

        if (player != null)
            player.ClearCurrentNPC(this);
    }
}