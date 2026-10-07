using System.Collections;
using UnityEngine;

public class SlidingTile : MonoBehaviour
{
    private SlidingTilePuzzle puzzle;

    private int currentRow;
    private int currentColumn;

    public int CurrentRow => currentRow;
    public int CurrentColumn => currentColumn;

    public void Setup(
        SlidingTilePuzzle slidingPuzzle,
        int row,
        int column)
    {
        puzzle = slidingPuzzle;

        currentRow = row;
        currentColumn = column;
    }

    public void SetGridPosition(
        int row,
        int column)
    {
        currentRow = row;
        currentColumn = column;
    }

    public IEnumerator SlideToPosition(
        Vector3 targetPosition,
        float slideSpeed)
    {
        while (
            Vector3.Distance(
                transform.position,
                targetPosition
            ) > 0.01f)
        {
            transform.position =
                Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    slideSpeed * Time.deltaTime
                );

            yield return null;
        }

        transform.position =
            targetPosition;
    }
}