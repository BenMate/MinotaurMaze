using UnityEngine;
using TMPro;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private RectTransform continueArrow;

    [Header("Typewriter")]
    [SerializeField] private float lettersPerSecond = 35f;

    private Coroutine typingCoroutine;
    private Coroutine arrowCoroutine;
    private Coroutine movementCoroutine;

    private bool isTyping;
    private bool skipTyping;

    private Vector2 arrowStartPos;

    private NPCDialogue.Conversation currentConversation;
    private NPCDialogue currentNPC;
    private PlayerController currentPlayer;

    private int currentLine;

    public bool IsTalking { get; private set; }


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        Instance = this;

        dialoguePanel.SetActive(false);

        arrowStartPos = continueArrow.anchoredPosition;

        continueArrow.gameObject.SetActive(false);
    }


    // =========================================================
    // START CONVERSATION
    // =========================================================

    public void StartConversation(
        NPCDialogue npc,
        NPCDialogue.Conversation conversation,
        PlayerController player)
    {
        if (conversation == null ||
            conversation.lines == null ||
            conversation.lines.Length == 0)
        {
            return;
        }

        currentNPC = npc;
        currentConversation = conversation;
        currentPlayer = player;

        currentLine = 0;

        IsTalking = true;

        if (currentPlayer != null)
        {
            currentPlayer.SetMovementLocked(true);
        }

        ShowCurrentElement();
    }


    // =========================================================
    // SHOW CURRENT ELEMENT
    // =========================================================

    private void ShowCurrentElement()
    {
        Debug.Log(
        $"[DIALOGUE] ShowCurrentElement | " +
        $"NPC: {currentNPC?.name} | " +
        $"Line: {currentLine} | " +
        $"IsTalking: {IsTalking}"
    );

        if (currentConversation == null ||
            currentConversation.lines == null)
        {
            FinishConversation();
            return;
        }


        // -----------------------------------------------------
        // END OF CONVERSATION
        // -----------------------------------------------------

        if (currentLine >= currentConversation.lines.Length)
        {
            FinishConversation();
            return;
        }


        NPCDialogue.DialogueLine line =
            currentConversation.lines[currentLine];


        // -----------------------------------------------------
        // BLANK ELEMENT
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(line.text))
        {
            dialoguePanel.SetActive(false);
            continueArrow.gameObject.SetActive(false);

            // If this element moves,
            // perform the movement automatically.
            if (line.moveAfter)
            {
                StartMovement(line);
                return;
            }

            // If it doesn't move but Auto Next is enabled,
            // simply continue.
            if (line.autoNext)
            {
                currentLine++;

                ShowCurrentElement();

                return;
            }

            // Otherwise wait for E.
            return;
        }


        // -----------------------------------------------------
        // ELEMENT HAS TEXT
        // -----------------------------------------------------
        Debug.Log(
    $"[DIALOGUE] SHOWING TEXT | " +
    $"Line: {currentLine} | " +
    $"Text: \"{line.text}\""
);
        dialoguePanel.SetActive(true);

        StartTyping();
    }


    // =========================================================
    // TYPEWRITER
    // =========================================================

    private void StartTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (arrowCoroutine != null)
        {
            StopCoroutine(arrowCoroutine);
            arrowCoroutine = null;
        }

        continueArrow.gameObject.SetActive(false);

        continueArrow.anchoredPosition =
            arrowStartPos;

        typingCoroutine =
            StartCoroutine(TypeLine());
    }


    // =========================================================
    // TYPE LINE
    // =========================================================

    private IEnumerator TypeLine()
    {
        isTyping = true;
        skipTyping = false;

        dialogueText.text = "";

        string line =
            currentConversation.lines[currentLine].text;


        foreach (char letter in line)
        {
            if (skipTyping)
            {
                dialogueText.text = line;
                break;
            }

            dialogueText.text += letter;

            yield return new WaitForSeconds(
                1f / lettersPerSecond
            );
        }


        isTyping = false;

        typingCoroutine = null;


        NPCDialogue.DialogueLine dialogueLine =
            currentConversation.lines[currentLine];


        // -----------------------------------------------------
        // AUTO NEXT
        // -----------------------------------------------------

        if (dialogueLine.autoNext)
        {
            continueArrow.gameObject.SetActive(false);

            yield return null;

            AdvanceLine();

            yield break;
        }


        // -----------------------------------------------------
        // NORMAL DIALOGUE
        // -----------------------------------------------------

        continueArrow.gameObject.SetActive(true);

        arrowCoroutine =
            StartCoroutine(BounceArrow());
    }


    // =========================================================
    // BOUNCE ARROW
    // =========================================================

    private IEnumerator BounceArrow()
    {
        float speed = 4f;
        float height = 6f;

        while (true)
        {
            float y =
                Mathf.Sin(Time.time * speed) * height;

            continueArrow.anchoredPosition =
                arrowStartPos +
                Vector2.up * y;

            yield return null;
        }
    }


    // =========================================================
    // PLAYER PRESSES E
    // =========================================================

    public void NextLine()
    {
        if (!IsTalking)
            return;

        // Don't allow E to interfere while NPC is walking.
        if (movementCoroutine != null)
            return;

        // If text is typing, finish the text first.
        if (isTyping)
        {
            skipTyping = true;
            return;
        }

        AdvanceLine();
    }


    // =========================================================
    // ADVANCE ELEMENT
    // =========================================================

    private void AdvanceLine()
    {
        continueArrow.gameObject.SetActive(false);

        if (arrowCoroutine != null)
        {
            StopCoroutine(arrowCoroutine);
            arrowCoroutine = null;
        }


        if (currentConversation == null ||
            currentConversation.lines == null)
        {
            FinishConversation();
            return;
        }


        NPCDialogue.DialogueLine finishedLine =
            currentConversation.lines[currentLine];


        // Move to next element.
        currentLine++;


        // -----------------------------------------------------
        // IF THE FINISHED ELEMENT HAS MOVEMENT
        // -----------------------------------------------------

        if (finishedLine.moveAfter)
        {
            StartMovement(finishedLine);
            return;
        }


        // -----------------------------------------------------
        // OTHERWISE SHOW NEXT ELEMENT
        // -----------------------------------------------------

        ShowCurrentElement();
    }


    // =========================================================
    // START MOVEMENT
    // =========================================================

    private void StartMovement(
        NPCDialogue.DialogueLine line)
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }

        movementCoroutine =
            StartCoroutine(
                RunMovement(line)
            );
    }


    // =========================================================
    // RUN MOVEMENT
    // =========================================================

    private IEnumerator RunMovement(
    NPCDialogue.DialogueLine line)
    {
        Debug.Log(
            $"[DIALOGUE] NPC MOVEMENT START | " +
            $"Line: {currentLine}"
        );

        // Hide dialogue while NPC walks.
        dialoguePanel.SetActive(false);
        continueArrow.gameObject.SetActive(false);

        // Stop the arrow animation if it is running.
        if (arrowCoroutine != null)
        {
            StopCoroutine(arrowCoroutine);
            arrowCoroutine = null;
        }

        // Let NPC completely finish walking.
        yield return StartCoroutine(
            currentNPC.RunLineAction(line)
        );

        Debug.Log(
            $"[DIALOGUE] NPC MOVEMENT FINISHED | " +
            $"Line: {currentLine}"
        );

        // Movement is finished.
        movementCoroutine = null;

        // -----------------------------------------------------
        // IMPORTANT:
        // The movement element itself is now finished.
        // Advance to the NEXT dialogue element.
        // -----------------------------------------------------

        currentLine++;

        Debug.Log(
            $"[DIALOGUE] MOVEMENT → NEXT ELEMENT | " +
            $"Next Line: {currentLine} | " +
            $"Total Lines: {currentConversation.lines.Length}"
        );

        // -----------------------------------------------------
        // SAFETY CHECK
        // -----------------------------------------------------

        if (currentConversation == null)
        {
            FinishConversation();
            yield break;
        }

        // -----------------------------------------------------
        // THERE IS ANOTHER ELEMENT
        // -----------------------------------------------------

        if (currentLine < currentConversation.lines.Length)
        {
            ShowCurrentElement();
            yield break;
        }

        // -----------------------------------------------------
        // NO MORE ELEMENTS
        // -----------------------------------------------------

        FinishConversation();
    }


    // =========================================================
    // FINISH CONVERSATION
    // =========================================================

    private void FinishConversation()
    {
        // -----------------------------------------------------
        // STOP TYPEWRITER
        // -----------------------------------------------------
        Debug.Log(
    $"[DIALOGUE] FINISH CONVERSATION | " +
    $"NPC: {currentNPC?.name}"
);
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }


        // -----------------------------------------------------
        // STOP ARROW
        // -----------------------------------------------------

        if (arrowCoroutine != null)
        {
            StopCoroutine(arrowCoroutine);
            arrowCoroutine = null;
        }


        // -----------------------------------------------------
        // DO NOT STOP THE CURRENT MOVEMENT COROUTINE
        // -----------------------------------------------------
        //
        // This is important.
        //
        // FinishConversation can be called after a movement
        // has already completed. We don't want to interrupt
        // the NPC movement coroutine from inside itself.
        //


        // -----------------------------------------------------
        // RESET STATES
        // -----------------------------------------------------

        isTyping = false;
        skipTyping = false;


        // -----------------------------------------------------
        // HIDE UI
        // -----------------------------------------------------

        dialoguePanel.SetActive(false);
        continueArrow.gameObject.SetActive(false);


        // -----------------------------------------------------
        // CONVERSATION FINISHED
        // -----------------------------------------------------

        IsTalking = false;


        // -----------------------------------------------------
        // UNLOCK PLAYER
        // -----------------------------------------------------

        if (currentPlayer != null)
        {
            Debug.Log(
    $"[DIALOGUE] UNLOCKING PLAYER | " +
    $"Player: {currentPlayer?.name}"
);

            currentPlayer.SetMovementLocked(false);
        }


        // -----------------------------------------------------
        // TELL NPC
        // -----------------------------------------------------

        if (currentNPC != null)
        {
            currentNPC.ConversationFinished();
        }


        // -----------------------------------------------------
        // CLEAR REFERENCES
        // -----------------------------------------------------

        currentPlayer = null;
        currentNPC = null;
        currentConversation = null;

        currentLine = 0;
    }
}
