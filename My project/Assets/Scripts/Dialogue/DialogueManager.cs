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
    private bool isTyping;
    private bool skipTyping;
    private Vector2 arrowStartPos;
    
    private Conversation currentConversation;
    private NPCDialogue currentNPC;

    private int currentLine;

    public bool IsTalking { get; private set; }

    private void Awake()
    {
        Instance = this;

        dialoguePanel.SetActive(false);
        arrowStartPos = continueArrow.anchoredPosition;

        continueArrow.gameObject.SetActive(false);
    }

    private void StartTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeLine());

        continueArrow.gameObject.SetActive(false);

        if (arrowCoroutine != null)
        {
            StopCoroutine(arrowCoroutine);
            arrowCoroutine = null;
        }

        continueArrow.anchoredPosition = arrowStartPos;
    }

    private IEnumerator TypeLine()
    {
        isTyping = true;
        skipTyping = false;

        dialogueText.text = "";

        string line = currentConversation.lines[currentLine];

        foreach (char letter in line)
        {
            if (skipTyping)
            {
                dialogueText.text = line;
                break;
            }

            dialogueText.text += letter;

            yield return new WaitForSeconds(1f / lettersPerSecond);
        }

        isTyping = false;

        continueArrow.gameObject.SetActive(true);

        arrowCoroutine = StartCoroutine(BounceArrow());
    }

    private IEnumerator BounceArrow()
    {
        float speed = 4f;
        float height = 6f;

        while (true)
        {
            float y = Mathf.Sin(Time.time * speed) * height;

            continueArrow.anchoredPosition =
                arrowStartPos + Vector2.up * y;

            yield return null;
        }
    }

    public void StartConversation(
        NPCDialogue npc,
        Conversation conversation)
    {
        currentNPC = npc;
        currentConversation = conversation;

        currentLine = 0;

        IsTalking = true;

        dialoguePanel.SetActive(true);

        StartTyping();
    }

    public void NextLine()
    {
        if (isTyping)
        {
            skipTyping = true;
            return;
        }

        currentLine++;

        if (currentLine >= currentConversation.lines.Length)
        {
            EndConversation();
            return;
        }

        StartTyping();
    }

    void EndConversation()
    {
        dialoguePanel.SetActive(false);

        IsTalking = false;

        currentNPC.ConversationFinished();
    }
}