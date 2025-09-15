using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DungeonGenerator : MonoBehaviour
{
    [Header("Dungeon Size")]
    public int width = 50;
    public int height = 50;
    private const int minSize = 10;

    [Header("Tiles")]
    public List<WeightedTile> floorTiles;
    public List<WeightedTile> wallTiles;

    [Header("Generation Settings")]
    public bool regenerateOnPlay = true;
    public int seed = 0;

    [Header("Editor Preview")]
    public bool showPreview = true;
    public bool useGizmos = false;

    [Header("Player")]
    public GameObject playerPrefab;
    [HideInInspector] public Transform playerInstance;

    public enum TileType { Empty, Floor, Wall }
    public TileType[,] dungeon;

    public int gridWidth;
    public int gridHeight;

    private class Room
    {
        public int x, y, width, height;
        public Vector2Int Center => new Vector2Int(x + width / 2, y + height / 2);
        public Room(int x, int y, int w, int h) { this.x = x; this.y = y; width = w; height = h; }
    }
    private List<Room> rooms;

    void Start()
    {
        width = Mathf.Max(width, minSize);
        height = Mathf.Max(height, minSize);

        if (regenerateOnPlay) GenerateDungeon();
    }

    public void GenerateDungeon()
    {
        width = Mathf.Max(width, minSize);
        height = Mathf.Max(height, minSize);

        if (seed == 0) Random.InitState(System.Environment.TickCount);
        else Random.InitState(seed);

        gridWidth = width + 2;
        gridHeight = height + 2;
        dungeon = new TileType[gridWidth, gridHeight];

        // initialize empty
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                dungeon[x, y] = TileType.Empty;

        // BSP generation
        rooms = new List<Room>();
        SubdivideWithRooms(1, 1, width, height, 5);
        ConnectRooms();

        AddWallsAroundFloors();
        RenderDungeon();
        SpawnPlayer(); // Spawn player at root node
    }

    void SubdivideWithRooms(int x, int y, int w, int h, int depth)
    {
        if (depth <= 0 || w <= 4 || h <= 4)
        {
            int roomWidth = Random.Range(2, Mathf.Max(2, w));
            int roomHeight = Random.Range(2, Mathf.Max(2, h));
            int roomX = x + Random.Range(0, Mathf.Max(1, w - roomWidth + 1));
            int roomY = y + Random.Range(0, Mathf.Max(1, h - roomHeight + 1));

            CarveRoom(roomX, roomY, roomWidth, roomHeight);
            rooms.Add(new Room(roomX, roomY, roomWidth, roomHeight));
            return;
        }

        if (w > h)
        {
            int split = Random.Range(2, w - 1);
            SubdivideWithRooms(x, y, split, h, depth - 1);
            SubdivideWithRooms(x + split, y, w - split, h, depth - 1);
        }
        else
        {
            int split = Random.Range(2, h - 1);
            SubdivideWithRooms(x, y, w, split, depth - 1);
            SubdivideWithRooms(x, y + split, w, h - split, depth - 1);
        }
    }

    void CarveRoom(int x, int y, int w, int h)
    {
        for (int i = x; i < x + w; i++)
            for (int j = y; j < y + h; j++)
                dungeon[i, j] = TileType.Floor;
    }

    void ConnectRooms()
    {
        for (int i = 1; i < rooms.Count; i++)
        {
            Vector2Int prev = rooms[i - 1].Center;
            Vector2Int curr = rooms[i].Center;

            if (Random.value < 0.5f)
            {
                CarveHorizontal(prev.x, curr.x, prev.y);
                CarveVertical(prev.y, curr.y, curr.x);
            }
            else
            {
                CarveVertical(prev.y, curr.y, prev.x);
                CarveHorizontal(prev.x, curr.x, curr.y);
            }
        }
    }

    void CarveHorizontal(int xStart, int xEnd, int y)
    {
        int min = Mathf.Min(xStart, xEnd);
        int max = Mathf.Max(xStart, xEnd);
        int corridorWidth = Random.Range(2, 4);

        for (int x = min; x <= max; x++)
            for (int w = 0; w < corridorWidth; w++)
            {
                int yy = y + w;
                if (yy >= 0 && yy < gridHeight) dungeon[x, yy] = TileType.Floor;
            }
    }

    void CarveVertical(int yStart, int yEnd, int x)
    {
        int min = Mathf.Min(yStart, yEnd);
        int max = Mathf.Max(yStart, yEnd);
        int corridorWidth = Random.Range(2, 4);

        for (int y = min; y <= max; y++)
            for (int w = 0; w < corridorWidth; w++)
            {
                int xx = x + w;
                if (xx >= 0 && xx < gridWidth) dungeon[xx, y] = TileType.Floor;
            }
    }

    void AddWallsAroundFloors()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (dungeon[x, y] == TileType.Floor)
                {
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;
                            if (nx >= 0 && ny >= 0 && nx < gridWidth && ny < gridHeight)
                            {
                                if (dungeon[nx, ny] == TileType.Empty)
                                    dungeon[nx, ny] = TileType.Wall;
                            }
                        }
                }
            }
        }

        // Fill edges
        for (int x = 0; x < gridWidth; x++)
        {
            dungeon[x, 0] = TileType.Wall;
            dungeon[x, gridHeight - 1] = TileType.Wall;
        }
        for (int y = 0; y < gridHeight; y++)
        {
            dungeon[0, y] = TileType.Wall;
            dungeon[gridWidth - 1, y] = TileType.Wall;
        }
    }

    void RenderDungeon()
    {
        List<Transform> children = new List<Transform>();
        foreach (Transform t in transform) children.Add(t);
        foreach (Transform t in children)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(t.gameObject);
            else Destroy(t.gameObject);
#else
            Destroy(t.gameObject);
#endif
        }

        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 pos = new Vector3(x - 1, y - 1, 0);
                switch (dungeon[x, y])
                {
                    case TileType.Floor:
                        SpawnWeightedTile(floorTiles, pos);
                        break;
                    case TileType.Wall:
                        SpawnWeightedTile(wallTiles, pos);
                        break;
                }
            }
    }

    void SpawnWeightedTile(List<WeightedTile> tiles, Vector3 pos)
    {
        if (tiles.Count == 0) return;
        float total = 0;
        foreach (var t in tiles) total += t.spawnChance;

        float r = Random.value * total;
        float cumulative = 0;

        foreach (var t in tiles)
        {
            cumulative += t.spawnChance;
            if (r <= cumulative)
            {
#if UNITY_EDITOR
                GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(t.prefab, transform);
                obj.transform.position = pos;
#else
                GameObject obj = Instantiate(t.prefab, pos, Quaternion.identity, transform);
#endif
                float scale = Random.Range(t.minScale, t.maxScale);
                obj.transform.localScale = new Vector3(scale, scale, scale);
                if (t.randomRotation)
                    obj.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                return;
            }
        }
    }

    public void SpawnPlayer()
    {
        if (playerPrefab == null) return;

        // Ensure dungeon exists
        if (dungeon == null || rooms == null || rooms.Count == 0)
        {
            Debug.Log("Dungeon not generated yet. Generating now...");
            GenerateDungeon();
        }

        Room rootRoom = rooms[0]; // root BSP room
        Vector2Int spawnPos = rootRoom.Center;
        Vector3 worldPos = new Vector3(spawnPos.x - 1, spawnPos.y - 1, 0); // adjust for padding

        if (playerInstance != null) DestroyImmediate(playerInstance.gameObject);

        GameObject playerObj = Instantiate(playerPrefab, worldPos, Quaternion.identity);
        playerObj.name = "Player";
        playerInstance = playerObj.transform;
    }
}








