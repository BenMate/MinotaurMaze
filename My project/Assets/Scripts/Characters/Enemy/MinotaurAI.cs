using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MinotaurAI : MonoBehaviour
{
    [Header("AI Settings")]
    public float moveSpeed = 3f;
    public float sightRange = 10f;

    [HideInInspector] public DungeonGenerator dungeonGenerator;
    [HideInInspector] public Transform player;

    private Vector2Int gridPos;
    private string state = "Wander";

    private List<Vector2Int> currentPath = new List<Vector2Int>();

    void Start()
    {
        gridPos = WorldToGrid(transform.position);
        StartCoroutine(AIMovementLoop());
    }

    IEnumerator AIMovementLoop()
    {
        Vector2Int lastKnownPlayerPos = gridPos;

        while (true)
        {
            Vector2Int nextTile = gridPos;

            // ------------------- CHASE -------------------
            if (state == "Chase" && player != null)
            {
                Vector2Int playerPos = WorldToGrid(player.position);
                float dist = Vector2Int.Distance(gridPos, playerPos);

                if (dist <= sightRange)
                {
                    // Player in sight: update last known position
                    lastKnownPlayerPos = playerPos;

                    // Update path dynamically
                    if (currentPath == null || currentPath.Count <= 1 || currentPath[currentPath.Count - 1] != playerPos)
                        currentPath = FindPath(gridPos, playerPos);

                    if (currentPath != null && currentPath.Count > 1)
                    {
                        nextTile = currentPath[1];
                        currentPath.RemoveAt(0);
                    }
                }
                else
                {
                    // Player out of sight: finish moving to last known position
                    if (currentPath == null || currentPath.Count <= 1 || currentPath[currentPath.Count - 1] != lastKnownPlayerPos)
                        currentPath = FindPath(gridPos, lastKnownPlayerPos);

                    if (currentPath != null && currentPath.Count > 1)
                    {
                        nextTile = currentPath[1];
                        currentPath.RemoveAt(0);
                    }
                    else
                    {
                        // Reached last known location, switch to wander
                        state = "Wander";
                        currentPath = null;
                    }
                }
            }

            // ------------------- WANDER -------------------
            else if (state == "Wander")
            {
                if (currentPath == null || currentPath.Count <= 1)
                {
                    DungeonGenerator.Room room = GetRandomRoomAwayFromCurrent();
                    if (room != null)
                    {
                        // Collect all tiles where 3x3 Minotaur can fit
                        List<Vector2Int> safeTiles = new List<Vector2Int>();
                        Dictionary<Vector2Int, float> distanceToCenter = new Dictionary<Vector2Int, float>();

                        for (int x = room.x; x < room.x + room.width; x++)
                            for (int y = room.y; y < room.y + room.height; y++)
                            {
                                Vector2Int tile = new Vector2Int(x, y);
                                if (CanFit(tile))
                                {
                                    safeTiles.Add(tile);
                                    distanceToCenter[tile] = Vector2Int.Distance(tile, room.Center);
                                }
                            }

                        if (safeTiles.Count > 0)
                        {
                            safeTiles.Sort((a, b) => distanceToCenter[a].CompareTo(distanceToCenter[b]));
                            int topCount = Mathf.Max(1, safeTiles.Count / 3);
                            Vector2Int targetTile = safeTiles[Random.Range(0, topCount)];

                            currentPath = FindPath(gridPos, targetTile);
                        }
                    }
                }

                if (currentPath != null && currentPath.Count > 1)
                {
                    nextTile = currentPath[1];
                    currentPath.RemoveAt(0);
                }

                // Check if player comes into sight while wandering
                if (player != null && Vector2Int.Distance(gridPos, WorldToGrid(player.position)) <= sightRange)
                {
                    state = "Chase";
                    currentPath = null;
                }
            }

            // ------------------- MOVE -------------------
            if (nextTile != gridPos)
                yield return MoveTileSmooth(nextTile);
            else
                yield return null;
        }
    }

    IEnumerator MoveTileSmooth(Vector2Int targetTile)
    {
        Vector3 start = transform.position;
        Vector3 end = GridToWorld(targetTile);
        float t = 0f;
        float distance = Vector3.Distance(start, end);
        float speed = moveSpeed / distance;

        while (t < 1f)
        {
            t += Time.deltaTime * speed;
            transform.position = Vector3.Lerp(start, end, t);
            gridPos = Vector2Int.RoundToInt(transform.position);

            // While moving, if in chase, update path dynamically
            if (state == "Chase" && player != null)
            {
                Vector2Int playerPos = WorldToGrid(player.position);
                if (Vector2Int.Distance(gridPos, playerPos) <= sightRange)
                {
                    currentPath = FindPath(gridPos, playerPos);
                }
            }

            yield return null;
        }

        transform.position = end;
        gridPos = targetTile;
    }

    Vector2Int WorldToGrid(Vector3 pos) => Vector2Int.RoundToInt(pos);
    Vector3 GridToWorld(Vector2Int grid) => new Vector3(grid.x, grid.y, -0.1f);

    private DungeonGenerator.Room GetRandomRoomAwayFromCurrent()
    {
        if (dungeonGenerator == null || dungeonGenerator.Rooms.Count == 0) return null;

        DungeonGenerator.Room currentRoom = null;
        foreach (var room in dungeonGenerator.Rooms)
        {
            if (gridPos.x >= room.x && gridPos.x < room.x + room.width &&
                gridPos.y >= room.y && gridPos.y < room.y + room.height)
            {
                currentRoom = room;
                break;
            }
        }

        List<DungeonGenerator.Room> candidates = new List<DungeonGenerator.Room>();
        foreach (var room in dungeonGenerator.Rooms)
        {
            if (room != currentRoom)
                candidates.Add(room);
        }

        if (candidates.Count == 0) return currentRoom;

        return candidates[Random.Range(0, candidates.Count)];
    }

    bool CanFit(Vector2Int pos)
    {
        // Ensure a 3x3 space for the Minotaur fits within dungeon bounds and floor
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                Vector2Int check = new Vector2Int(pos.x + dx, pos.y + dy);
                if (!IsWalkable(check)) return false;
            }
        return true;
    }

    List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        var open = new List<Vector2Int> { start };
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, int> { [start] = 0 };
        var fScore = new Dictionary<Vector2Int, int> { [start] = Heuristic(start, goal) };

        while (open.Count > 0)
        {
            open.Sort((a, b) => fScore[a].CompareTo(fScore[b]));
            var current = open[0];
            if (current == goal) return ReconstructPath(cameFrom, current);

            open.RemoveAt(0);

            foreach (var neighbor in GetNeighbors(current))
            {
                if (!IsWalkable(neighbor)) continue;
                int tentativeG = gScore[current] + 1;
                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);
                    if (!open.Contains(neighbor)) open.Add(neighbor);
                }
            }
        }

        return null;
    }

    int Heuristic(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        return new List<Vector2Int>
        {
            pos + Vector2Int.up,
            pos + Vector2Int.down,
            pos + Vector2Int.left,
            pos + Vector2Int.right
        };
    }

    List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        var total = new List<Vector2Int> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            total.Insert(0, current);
        }
        return total;
    }

    bool IsWalkable(Vector2Int pos)
    {
        if (dungeonGenerator == null) return false;
        if (pos.x < 0 || pos.y < 0 || pos.x >= dungeonGenerator.width || pos.y >= dungeonGenerator.height)
            return false;
        return dungeonGenerator.dungeon[pos.x, pos.y] == DungeonGenerator.TileType.Floor;
    }

    private void OnDrawGizmos()
    {
        if (gridPos == null) return;

        // Draw AI position
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(GridToWorld(gridPos), 0.3f);

        // Draw path tiles as circles
        if (currentPath != null)
        {
            Gizmos.color = state == "Chase" ? Color.blue : Color.yellow;
            foreach (var tile in currentPath)
                Gizmos.DrawSphere(GridToWorld(tile), 0.2f);
        }

        // Draw line to player if chasing
        if (state == "Chase" && player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(GridToWorld(gridPos), player.position);
        }
    }
}
