using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlidingTilePuzzle : MonoBehaviour, IInteractable
{
    [Header("Puzzle Sprites")]
    [Tooltip("Add the 16 sliced sprites in reading order: left to right, top to bottom.")]
    [SerializeField] private Sprite[] puzzleSprites;

    [Header("Grid")]
    [Tooltip("Distance between the centre of each tile.")]
    [SerializeField] private float tileSpacing = 1f;

    [Header("Scramble")]
    [Tooltip("Number of legal moves used to scramble the puzzle.")]
    [SerializeField] private int scrambleMoves = 150;

    [Header("Sliding")]
    [Tooltip("How quickly tiles slide into the empty space.")]
    [SerializeField] private float slideSpeed = 5f;

    [Header("Interaction")]
    [Tooltip("Extra space around the puzzle that allows the player to interact with it.")]
    [SerializeField] private float interactionPadding = 0.5f;

    [Header("Door")]
    [Tooltip("Closed door GameObject. Disabled when puzzle is solved.")]
    [SerializeField] private GameObject lockedDoor;

    [Tooltip("Open door GameObject. Enabled when puzzle is solved.")]
    [SerializeField] private GameObject openDoor;

    private const int GridSize = 4;

    private SlidingTile[,] tiles;

    private int emptyRow;
    private int emptyColumn;

    private bool isMoving;
    private bool puzzleSolved;

    private BoxCollider2D interactionCollider;

    public bool PuzzleSolved => puzzleSolved;


    // ---------------------------------------------------------
    // UNITY
    // ---------------------------------------------------------

    private void Awake()
    {
        // Puzzle starts locked.

        if (lockedDoor != null)
            lockedDoor.SetActive(true);

        if (openDoor != null)
            openDoor.SetActive(false);
    }

    private void Start()
    {
        SetupPuzzle();
    }


    // ---------------------------------------------------------
    // SETUP PUZZLE
    // ---------------------------------------------------------

    private void SetupPuzzle()
    {
        if (
            puzzleSprites == null ||
            puzzleSprites.Length != 16)
        {
            Debug.LogError(
                "[SLIDING PUZZLE] " +
                "You must assign exactly 16 sprites."
            );

            return;
        }

        tiles =
            new SlidingTile[
                GridSize,
                GridSize
            ];

        CreateTiles();

        SetupInteractionCollider();

        SetSolvedLayout();

        ScramblePuzzle();

        Debug.Log(
            "[SLIDING PUZZLE] Puzzle created and scrambled."
        );
    }


    // ---------------------------------------------------------
    // CREATE TILES
    // ---------------------------------------------------------

    private void CreateTiles()
    {
        for (int row = 0; row < GridSize; row++)
        {
            for (
                int column = 0;
                column < GridSize;
                column++)
            {
                int spriteIndex =
                    row * GridSize + column;

                GameObject tileObject =
                    new GameObject(
                        $"Tile_{row}_{column}"
                    );

                tileObject.transform.SetParent(
                    transform
                );

                tileObject.transform.position =
                    GetWorldPosition(
                        row,
                        column
                    );

                SpriteRenderer spriteRenderer =
                    tileObject.AddComponent<SpriteRenderer>();

                spriteRenderer.sprite =
                    puzzleSprites[spriteIndex];

                SlidingTile tile =
                    tileObject.AddComponent<SlidingTile>();

                tile.Setup(
                    this,
                    row,
                    column
                );

                tiles[row, column] =
                    tile;
            }
        }

        tiles[
            GridSize - 1,
            GridSize - 1
        ].gameObject.SetActive(false);

        emptyRow =
            GridSize - 1;

        emptyColumn =
            GridSize - 1;
    }


    // ---------------------------------------------------------
    // AUTOMATIC INTERACTION COLLIDER
    // ---------------------------------------------------------

    private void SetupInteractionCollider()
    {
        interactionCollider =
            GetComponent<BoxCollider2D>();

        if (interactionCollider == null)
        {
            interactionCollider =
                gameObject.AddComponent<BoxCollider2D>();
        }

        interactionCollider.isTrigger = true;

        Bounds puzzleBounds =
            new Bounds();

        bool boundsInitialized = false;

        for (int row = 0; row < GridSize; row++)
        {
            for (
                int column = 0;
                column < GridSize;
                column++)
            {
                SlidingTile tile =
                    tiles[row, column];

                if (tile == null)
                    continue;

                SpriteRenderer renderer =
                    tile.GetComponent<SpriteRenderer>();

                if (renderer == null)
                    continue;

                if (!boundsInitialized)
                {
                    puzzleBounds =
                        renderer.bounds;

                    boundsInitialized = true;
                }
                else
                {
                    puzzleBounds.Encapsulate(
                        renderer.bounds
                    );
                }
            }
        }

        if (!boundsInitialized)
            return;

        Vector3 localCenter =
            transform.InverseTransformPoint(
                puzzleBounds.center
            );

        Vector3 localSize =
            transform.InverseTransformVector(
                puzzleBounds.size
            );

        localSize.x =
            Mathf.Abs(localSize.x) +
            interactionPadding * 2f;

        localSize.y =
            Mathf.Abs(localSize.y) +
            interactionPadding * 2f;

        interactionCollider.offset =
            new Vector2(
                localCenter.x,
                localCenter.y
            );

        interactionCollider.size =
            new Vector2(
                localSize.x,
                localSize.y
            );
    }


    // ---------------------------------------------------------
    // SOLVED LAYOUT
    // ---------------------------------------------------------

    private void SetSolvedLayout()
    {
        for (int row = 0; row < GridSize; row++)
        {
            for (
                int column = 0;
                column < GridSize;
                column++)
            {
                SlidingTile tile =
                    tiles[row, column];

                if (tile == null)
                    continue;

                tile.SetGridPosition(
                    row,
                    column
                );

                tile.transform.position =
                    GetWorldPosition(
                        row,
                        column
                    );
            }
        }

        emptyRow =
            GridSize - 1;

        emptyColumn =
            GridSize - 1;
    }


    // ---------------------------------------------------------
    // SCRAMBLE
    // ---------------------------------------------------------

    private void ScramblePuzzle()
    {
        int previousEmptyRow = -1;
        int previousEmptyColumn = -1;

        for (int i = 0; i < scrambleMoves; i++)
        {
            List<Vector2Int> possibleMoves =
                GetPossibleMoves();

            if (
                possibleMoves.Count > 1 &&
                previousEmptyRow >= 0)
            {
                possibleMoves.RemoveAll(
                    move =>
                        move.x == previousEmptyRow &&
                        move.y == previousEmptyColumn
                );
            }

            if (possibleMoves.Count == 0)
                continue;

            Vector2Int selectedMove =
                possibleMoves[
                    Random.Range(
                        0,
                        possibleMoves.Count
                    )
                ];

            SlidingTile tile =
                tiles[
                    selectedMove.x,
                    selectedMove.y
                ];

            previousEmptyRow =
                emptyRow;

            previousEmptyColumn =
                emptyColumn;

            SwapTileInstantly(tile);
        }
    }


    // ---------------------------------------------------------
    // POSSIBLE MOVES
    // ---------------------------------------------------------

    private List<Vector2Int> GetPossibleMoves()
    {
        List<Vector2Int> moves =
            new List<Vector2Int>();

        if (emptyRow > 0)
        {
            moves.Add(
                new Vector2Int(
                    emptyRow - 1,
                    emptyColumn
                )
            );
        }

        if (emptyRow < GridSize - 1)
        {
            moves.Add(
                new Vector2Int(
                    emptyRow + 1,
                    emptyColumn
                )
            );
        }

        if (emptyColumn > 0)
        {
            moves.Add(
                new Vector2Int(
                    emptyRow,
                    emptyColumn - 1
                )
            );
        }

        if (emptyColumn < GridSize - 1)
        {
            moves.Add(
                new Vector2Int(
                    emptyRow,
                    emptyColumn + 1
                )
            );
        }

        return moves;
    }


    // ---------------------------------------------------------
    // INSTANT SCRAMBLE MOVE
    // ---------------------------------------------------------

    private void SwapTileInstantly(
        SlidingTile tile)
    {
        int tileRow =
            tile.CurrentRow;

        int tileColumn =
            tile.CurrentColumn;

        tiles[
            tileRow,
            tileColumn
        ] = null;

        tiles[
            emptyRow,
            emptyColumn
        ] = tile;

        tile.SetGridPosition(
            emptyRow,
            emptyColumn
        );

        tile.transform.position =
            GetWorldPosition(
                emptyRow,
                emptyColumn
            );

        emptyRow =
            tileRow;

        emptyColumn =
            tileColumn;
    }


    // ---------------------------------------------------------
    // PLAYER INTERACTION
    // ---------------------------------------------------------

    public void Interact(
        PlayerController player)
    {
        if (player == null)
            return;

        if (puzzleSolved)
            return;

        if (isMoving)
            return;

        SlidingTile tile =
            GetTargetTile(player);

        if (tile == null)
            return;

        TryMoveTile(
            tile,
            player
        );
    }


    // ---------------------------------------------------------
    // FIND CLOSEST MOVABLE TILE
    // ---------------------------------------------------------

    private SlidingTile GetTargetTile(
        PlayerController player)
    {
        if (player == null || tiles == null)
            return null;

        Vector2 playerPosition =
            player.transform.position;

        SlidingTile closestTile = null;

        float closestDistance =
            float.MaxValue;

        List<Vector2Int> possibleMoves =
            GetPossibleMoves();

        foreach (Vector2Int move in possibleMoves)
        {
            SlidingTile tile =
                tiles[
                    move.x,
                    move.y
                ];

            if (tile == null)
                continue;

            if (!tile.gameObject.activeSelf)
                continue;

            float distance =
                Vector2.Distance(
                    playerPosition,
                    tile.transform.position
                );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTile = tile;
            }
        }

        if (closestTile == null)
        {          
            return null;
        }

        return closestTile;
    }


    // ---------------------------------------------------------
    // CHECK TILE MOVE
    // ---------------------------------------------------------

    private void TryMoveTile(
        SlidingTile tile,
        PlayerController player)
    {
        if (tile == null)
            return;

        int tileRow =
            tile.CurrentRow;

        int tileColumn =
            tile.CurrentColumn;

        int rowDifference =
            Mathf.Abs(
                tileRow -
                emptyRow
            );

        int columnDifference =
            Mathf.Abs(
                tileColumn -
                emptyColumn
            );

        if (
            rowDifference +
            columnDifference != 1)
        {
            return;
        }

        StartCoroutine(
            MoveTile(
                tile,
                player
            )
        );
    }


    // ---------------------------------------------------------
    // SLIDE TILE
    // ---------------------------------------------------------

    private IEnumerator MoveTile(
        SlidingTile tile,
        PlayerController player)
    {
        isMoving = true;

        if (player != null)
        {
            player.SetMovementLocked(true);
        }

        int oldRow =
            tile.CurrentRow;

        int oldColumn =
            tile.CurrentColumn;

        int targetRow =
            emptyRow;

        int targetColumn =
            emptyColumn;

        tiles[
            oldRow,
            oldColumn
        ] = null;

        tiles[
            targetRow,
            targetColumn
        ] = tile;

        tile.SetGridPosition(
            targetRow,
            targetColumn
        );

        emptyRow =
            oldRow;

        emptyColumn =
            oldColumn;

        Vector3 targetPosition =
            GetWorldPosition(
                targetRow,
                targetColumn
            );

        yield return StartCoroutine(
            tile.SlideToPosition(
                targetPosition,
                slideSpeed
            )
        );

        isMoving = false;

        if (player != null)
        {
            player.SetMovementLocked(false);
        }

        CheckSolved();
    }


    // ---------------------------------------------------------
    // CHECK SOLUTION
    // ---------------------------------------------------------

    private void CheckSolved()
    {
        for (int row = 0; row < GridSize; row++)
        {
            for (
                int column = 0;
                column < GridSize;
                column++)
            {
                if (
                    row == GridSize - 1 &&
                    column == GridSize - 1)
                {
                    continue;
                }

                SlidingTile tile =
                    tiles[row, column];

                if (tile == null)
                    return;

                int expectedSpriteIndex =
                    row * GridSize +
                    column;

                SpriteRenderer renderer =
                    tile.GetComponent<SpriteRenderer>();

                if (renderer == null)
                    return;

                if (
                    renderer.sprite !=
                    puzzleSprites[
                        expectedSpriteIndex
                    ])
                {
                    return;
                }
            }
        }

        SolvePuzzle();
    }


    // ---------------------------------------------------------
    // SOLVE PUZZLE
    // ---------------------------------------------------------

    private void SolvePuzzle()
    {
        if (puzzleSolved)
            return;

        puzzleSolved = true;

        Debug.Log(
            "[SLIDING PUZZLE] SOLVED!"
        );


        // -----------------------------------------------------
        // OPEN DOOR
        // -----------------------------------------------------

        if (lockedDoor != null)
            lockedDoor.SetActive(false);

        if (openDoor != null)
            openDoor.SetActive(true);


        // -----------------------------------------------------
        // FILL EMPTY TILE
        // -----------------------------------------------------

        GameObject finalTileObject =
            new GameObject(
                "CompletedTile"
            );

        finalTileObject.transform.SetParent(
            transform
        );

        finalTileObject.transform.position =
            GetWorldPosition(
                GridSize - 1,
                GridSize - 1
            );

        SpriteRenderer finalRenderer =
            finalTileObject.AddComponent<SpriteRenderer>();

        finalRenderer.sprite =
            puzzleSprites[15];

        Debug.Log(
            "[SLIDING PUZZLE] " +
            "Final tile filled."
        );
    }


    // ---------------------------------------------------------
    // GRID POSITION
    // ---------------------------------------------------------

    private Vector3 GetWorldPosition(
        int row,
        int column)
    {
        float x =
            column * tileSpacing;

        float y =
            -row * tileSpacing;

        return transform.position +
               new Vector3(
                   x,
                   y,
                   0f
               );
    }


    // ---------------------------------------------------------
    // PLAYER TRIGGER
    // ---------------------------------------------------------

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        PlayerController player =
            other.GetComponent<PlayerController>();

        if (player != null)
        {
            player.SetCurrentSlidingPuzzle(
                this
            );
        }
    }

    private void OnTriggerExit2D(
        Collider2D other)
    {
        PlayerController player =
            other.GetComponent<PlayerController>();

        if (player != null)
        {
            player.ClearCurrentSlidingPuzzle(
                this
            );
        }
    }
}