
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CodePuzzle : MonoBehaviour, IInteractable
{
    [Header("Shared Symbol Sprites")]
    [SerializeField] private Sprite[] symbolSprites = new Sprite[5];

    [Header("Symbol Displays")]
    [SerializeField] private SpriteRenderer[] currentSymbols = new SpriteRenderer[5];
    [SerializeField] private SpriteRenderer[] nextSymbols = new SpriteRenderer[5];

    [Header("Animation")]
    [SerializeField] private float slideDuration = 0.25f;
    [SerializeField] private float slideDistance = 1f;

    [Header("Bounce")]
    [SerializeField] private float bounceDistance = 0.08f;
    [SerializeField] private float bounceDuration = 0.12f;

    [Header("Interaction Zones")]
    [SerializeField] private Collider2D[] symbolInteractionZones = new Collider2D[5];
    [SerializeField] private Collider2D checkInteractionZone;

    [Header("Correct Combination")]
    [Tooltip("Enter five values from 1 to 5. 1 = first symbol, 5 = fifth symbol.")]
    [SerializeField] private int[] correctCombination = new int[5];

    [Header("Door")]
    [SerializeField] private GameObject closedDoor;
    [SerializeField] private GameObject openDoor;

    [Header("Dialogue")]
    [SerializeField] private string incorrectMessage = "Nothing happened...";
    [SerializeField]
    private string correctMessage =
        "You hear a loud rumble... The doors to the next floor have opened!";

    private readonly int[] currentCombination = new int[5];
    private readonly bool[] isAnimating = new bool[5];

    private readonly Vector3[] currentHomePositions = new Vector3[5];
    private readonly Vector3[] nextHomePositions = new Vector3[5];

    private readonly List<Collider2D> activeZones =
        new List<Collider2D>();

    private PlayerController currentPlayer;
    private bool puzzleSolved;

    private void Start()
    {
        for (int i = 0; i < 5; i++)
        {
            currentCombination[i] = 0;

            if (currentSymbols[i] == null || nextSymbols[i] == null)
            {
                Debug.LogError(
                    $"CodePuzzle: Missing symbol renderer at display {i + 1}.",
                    this
                );
                continue;
            }

            currentHomePositions[i] =
                currentSymbols[i].transform.localPosition;

            nextHomePositions[i] =
                nextSymbols[i].transform.localPosition;

            // Put the incoming symbol above the display.
            nextHomePositions[i] += Vector3.up * slideDistance;

            currentSymbols[i].transform.localPosition =
                currentHomePositions[i];

            nextSymbols[i].transform.localPosition =
                nextHomePositions[i];

            currentSymbols[i].sprite = symbolSprites[0];
            nextSymbols[i].sprite = null;
        }

        if (closedDoor != null)
            closedDoor.SetActive(true);

        if (openDoor != null)
            openDoor.SetActive(false);
    }

    // PLAYER ENTERS AN INTERACTION ZONE

    public void PlayerEnteredInteraction(
        PlayerController player,
        Collider2D zone)
    {
        if (puzzleSolved || player == null || zone == null)
            return;

        if (!IsPuzzleZone(zone))
            return;

        currentPlayer = player;

        if (!activeZones.Contains(zone))
            activeZones.Add(zone);

        player.SetCurrentCodePuzzle(this);
    }

    // PLAYER LEAVES AN INTERACTION ZONE

    public void PlayerExitedInteraction(
        PlayerController player,
        Collider2D zone)
    {
        if (player == null || zone == null)
            return;

        activeZones.Remove(zone);

        if (activeZones.Count == 0 && currentPlayer == player)
        {
            player.ClearCurrentCodePuzzle(this);
            currentPlayer = null;
        }
    }

    private bool IsPuzzleZone(Collider2D zone)
    {
        if (zone == checkInteractionZone)
            return true;

        for (int i = 0; i < symbolInteractionZones.Length; i++)
        {
            if (symbolInteractionZones[i] == zone)
                return true;
        }

        return false;
    }

    // PLAYER PRESSES E

    public void Interact(PlayerController player)
    {
        if (puzzleSolved || player == null)
            return;

        Collider2D nearestZone = GetNearestActiveZone(player);

        if (nearestZone == null)
            return;

        if (nearestZone == checkInteractionZone)
        {
            CheckCombination(player);
            return;
        }

        for (int i = 0; i < symbolInteractionZones.Length; i++)
        {
            if (nearestZone == symbolInteractionZones[i])
            {
                CycleSymbol(i);
                return;
            }
        }
    }

    private Collider2D GetNearestActiveZone(PlayerController player)
    {
        Collider2D nearestZone = null;
        float nearestDistance = float.MaxValue;

        for (int i = activeZones.Count - 1; i >= 0; i--)
        {
            Collider2D zone = activeZones[i];

            if (zone == null || !zone.enabled)
            {
                activeZones.RemoveAt(i);
                continue;
            }

            Vector2 closestPoint =
                zone.ClosestPoint(player.transform.position);

            float distance = Vector2.Distance(
                player.transform.position,
                closestPoint
            );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestZone = zone;
            }
        }

        return nearestZone;
    }

    // CYCLE SYMBOL

    private void CycleSymbol(int symbolIndex)
    {
        if (symbolIndex < 0 || symbolIndex >= 5)
            return;

        // Prevent another press while this symbol is animating.
        if (isAnimating[symbolIndex])
            return;

        if (symbolSprites == null || symbolSprites.Length != 5)
        {
            Debug.LogWarning(
                "CodePuzzle: Assign exactly 5 shared symbol sprites.",
                this
            );
            return;
        }

        if (currentSymbols[symbolIndex] == null ||
            nextSymbols[symbolIndex] == null)
            return;

        currentCombination[symbolIndex]++;

        if (currentCombination[symbolIndex] >= 5)
            currentCombination[symbolIndex] = 0;

        Sprite incomingSprite =
            symbolSprites[currentCombination[symbolIndex]];

        StartCoroutine(
            AnimateSymbol(symbolIndex, incomingSprite)
        );
    }

    // DOWNWARD SLIDING ANIMATION WITH BOUNCE

    private IEnumerator AnimateSymbol(
        int symbolIndex,
        Sprite incomingSprite)
    {
        isAnimating[symbolIndex] = true;

        SpriteRenderer current = currentSymbols[symbolIndex];
        SpriteRenderer next = nextSymbols[symbolIndex];

        Vector3 currentStart = currentHomePositions[symbolIndex];
        Vector3 currentEnd =
            currentStart + Vector3.down * slideDistance;

        Vector3 nextStart = nextHomePositions[symbolIndex];
        Vector3 nextEnd = currentHomePositions[symbolIndex];

        // Show the incoming sprite above the display.
        next.sprite = incomingSprite;
        next.transform.localPosition = nextStart;

        float elapsed = 0f;

        // PHASE 1: Slide the old symbol down and the new one in.
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / slideDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            current.transform.localPosition = Vector3.Lerp(
                currentStart,
                currentEnd,
                easedProgress
            );

            next.transform.localPosition = Vector3.Lerp(
                nextStart,
                nextEnd,
                easedProgress
            );

            yield return null;
        }

        // Finish the slide exactly.
        current.transform.localPosition = currentEnd;
        next.transform.localPosition = nextEnd;

        // PHASE 2: Tiny bounce downwards.
        Vector3 bounceEnd =
            nextEnd + Vector3.down * bounceDistance;

        float halfBounceDuration = Mathf.Max(0.01f, bounceDuration * 0.5f);
        elapsed = 0f;

        while (elapsed < halfBounceDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsed / halfBounceDuration
            );

            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            next.transform.localPosition = Vector3.Lerp(
                nextEnd,
                bounceEnd,
                easedProgress
            );

            yield return null;
        }

        next.transform.localPosition = bounceEnd;

        // PHASE 3: Settle back into the resting position.
        elapsed = 0f;

        while (elapsed < halfBounceDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsed / halfBounceDuration
            );

            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            next.transform.localPosition = Vector3.Lerp(
                bounceEnd,
                nextEnd,
                easedProgress
            );

            yield return null;
        }

        // Finish the animation exactly.
        next.transform.localPosition = nextEnd;

        // Move the new symbol into the permanent current renderer.
        current.sprite = next.sprite;
        current.transform.localPosition = currentHomePositions[symbolIndex];

        // Reset the incoming renderer above the display.
        next.sprite = null;
        next.transform.localPosition = nextHomePositions[symbolIndex];

        isAnimating[symbolIndex] = false;
    }

    // CHECK COMBINATION

    private void CheckCombination(PlayerController player)
    {
        for (int i = 0; i < 5; i++)
        {
            if (isAnimating[i])
                return;
        }

        if (correctCombination == null ||
            correctCombination.Length != 5)
        {
            Debug.LogError(
                "CodePuzzle: Correct Combination must contain exactly 5 values.",
                this
            );
            return;
        }

        bool correct = true;

        for (int i = 0; i < 5; i++)
        {
            if (correctCombination[i] < 1 ||
                correctCombination[i] > 5)
            {
                Debug.LogError(
                    "CodePuzzle: Combination values must be between 1 and 5.",
                    this
                );
                return;
            }

            int targetSpriteIndex = correctCombination[i] - 1;

            if (currentCombination[i] != targetSpriteIndex)
            {
                correct = false;
                break;
            }
        }

        if (correct)
        {
            SolvePuzzle(player);
        }
        else
        {
            Debug.Log("CodePuzzle: Incorrect combination.");

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartMessage(
                    incorrectMessage,
                    player
                );
            }
        }
    }

    // SOLVE PUZZLE

    private void SolvePuzzle(PlayerController player)
    {
        if (puzzleSolved)
            return;

        puzzleSolved = true;

        if (closedDoor != null)
            closedDoor.SetActive(false);

        if (openDoor != null)
            openDoor.SetActive(true);

        activeZones.Clear();

        if (currentPlayer != null)
        {
            currentPlayer.ClearCurrentCodePuzzle(this);
            currentPlayer = null;
        }

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartMessage(
                correctMessage,
                player
            );
        }

        Debug.Log("CodePuzzle: Puzzle solved!");
    }
}
