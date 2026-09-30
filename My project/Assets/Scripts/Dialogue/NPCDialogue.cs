using System;
using System.Collections;
using UnityEngine;

public class NPCDialogue : MonoBehaviour, IInteractable
{
    [Serializable]
    public class DialogueLine
    {
        [TextArea(2, 5)]
        public string text;

        [Header("After This Element")]
        [Tooltip("NPC moves to the next point on the assigned path.")]
        public bool moveAfter;

        [Tooltip("Automatically continue to the next dialogue element.")]
        public bool autoNext;
    }

    [Serializable]
    public class Conversation
    {
        public DialogueLine[] lines;
    }

    [Header("Dialogue")]
    [SerializeField] private Conversation[] conversations;

    [Header("NPC Path")]
    [SerializeField] private NPCPath npcPath;

    [Header("NPC Movement")]
    [SerializeField] private float moveSpeed = 2f;

    private int conversationIndex;
    private int currentPathPoint;

    private PlayerController currentPlayer;

    public static bool AnyEventRunning { get; private set; }

    public bool EventRunning { get; private set; }

    // Used by objects such as the bucket pickup.
    public event Action<PlayerController> ConversationCompleted;


    // ---------------------------------------------------------
    // INTERACTION
    // ---------------------------------------------------------

    public void Interact(PlayerController player)
    {
        if (DialogueManager.Instance == null)
            return;

        if (DialogueManager.Instance.IsTalking)
            return;

        if (EventRunning || AnyEventRunning)
            return;

        if (conversations == null ||
            conversations.Length == 0)
            return;

        if (conversations[conversationIndex].lines == null ||
            conversations[conversationIndex].lines.Length == 0)
            return;

        currentPlayer = player;

        DialogueManager.Instance.StartConversation(
            this,
            conversations[conversationIndex],
            player
        );
    }

    // ---------------------------------------------------------
    // Trigger
    // ---------------------------------------------------------

    public void TriggerConversation(PlayerController player)
    {
        if (DialogueManager.Instance == null)
            return;

        if (player == null)
            return;

        if (conversations == null ||
            conversations.Length == 0)
            return;

        if (conversations[0].lines == null ||
            conversations[0].lines.Length == 0)
            return;

        currentPlayer = player;

        DialogueManager.Instance.StartConversation(
            this,
            conversations[0],
            player
        );
    }

    // ---------------------------------------------------------
    // CONVERSATION FINISHED
    // ---------------------------------------------------------

    public void ConversationFinished()
    {
        // Advance to the next conversation.
        //
        // Once we reach the final conversation,
        // keep repeating that final conversation.

        conversationIndex = Mathf.Min(
            conversationIndex + 1,
            conversations.Length - 1
        );

        // Tell optional systems that the conversation has finished.
        ConversationCompleted?.Invoke(currentPlayer);
    }


    // ---------------------------------------------------------
    // RUN LINE ACTION
    // ---------------------------------------------------------

    public IEnumerator RunLineAction(DialogueLine line)
    {
        if (line == null)
            yield break;

        if (!line.moveAfter)
            yield break;

        yield return StartCoroutine(
            MoveToNextPoint()
        );
    }


    // ---------------------------------------------------------
    // MOVE TO NEXT PATH POINT
    // ---------------------------------------------------------

    private IEnumerator MoveToNextPoint()
    {
        if (npcPath == null)
            yield break;

        int nextPoint = currentPathPoint + 1;

        Transform target =
            npcPath.GetPoint(nextPoint);

        if (target == null)
            yield break;

        currentPathPoint = nextPoint;

        Rigidbody2D rb =
            GetComponent<Rigidbody2D>();

        Animator animator =
            GetComponent<Animator>();


        // -----------------------------------------------------
        // WALK
        // -----------------------------------------------------

        while (
            Vector2.Distance(
                transform.position,
                target.position
            ) > 0.05f)
        {
            Vector2 direction =
                (
                    (Vector2)target.position -
                    (Vector2)transform.position
                ).normalized;


            if (animator != null)
            {
                animator.SetBool(
                    "IsMoving",
                    true
                );


                if (Mathf.Abs(direction.x) >
                    Mathf.Abs(direction.y))
                {
                    animator.SetFloat(
                        "MoveX",
                        Mathf.Sign(direction.x)
                    );

                    animator.SetFloat(
                        "MoveY",
                        0f
                    );
                }
                else
                {
                    animator.SetFloat(
                        "MoveX",
                        0f
                    );

                    animator.SetFloat(
                        "MoveY",
                        Mathf.Sign(direction.y)
                    );
                }
            }


            Vector2 newPosition =
                Vector2.MoveTowards(
                    transform.position,
                    target.position,
                    moveSpeed * Time.deltaTime
                );


            if (rb != null)
            {
                rb.MovePosition(newPosition);
            }
            else
            {
                transform.position = newPosition;
            }


            yield return null;
        }


        // -----------------------------------------------------
        // SNAP TO POINT
        // -----------------------------------------------------

        if (rb != null)
        {
            rb.MovePosition(
                target.position
            );
        }
        else
        {
            transform.position =
                target.position;
        }


        // -----------------------------------------------------
        // RETURN TO IDLE FACING DOWN
        // -----------------------------------------------------

        if (animator != null)
        {
            animator.SetBool(
                "IsMoving",
                false
            );

            animator.SetFloat(
                "MoveX",
                0f
            );

            animator.SetFloat(
                "MoveY",
                -1f
            );
        }
    }


    // ---------------------------------------------------------
    // PLAYER DETECTION
    // ---------------------------------------------------------

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