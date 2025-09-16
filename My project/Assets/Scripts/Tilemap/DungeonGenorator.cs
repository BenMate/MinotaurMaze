using UnityEngine;
using System.Collections.Generic;

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
    public int seed = 0;
    public bool useRandomSeed = true;

    [Header("Player")]
    public GameObject playerPrefab;

    [HideInInspector] public TileType[,] dungeon;
    public enum TileType { Floor, Wall }

    [HideInInspector] public bool showPreview = true;
    [HideInInspector] public bool useGizmos = false;

    private class Room
    {
        public int x, y, width, height;
        public Vector2Int Center => new Vector2Int(x + width / 2, y + height / 2);
        public Room(int x, int y, int w, int h) { this.x = x; this.y = y; width = w; height = h; }
    }
    private List<Room> rooms;

    [HideInInspector] public GameObject spawnedPlayer;
    private Transform tilesParent;
    [HideInInspector] public Dictionary<Vector2Int, GameObject> spawnedTiles = new Dictionary<Vector2Int, GameObject>();

    // ---------------------------
    // GENERATION
    // ---------------------------
    public void GenerateDungeon()
    {
        width = Mathf.Max(width, minSize);
        height = Mathf.Max(height, minSize);

        if (useRandomSeed) seed = Random.Range(int.MinValue, int.MaxValue);
        Random.InitState(seed);

        dungeon = new TileType[width, height];

        // Fill all with walls
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                dungeon[x, y] = TileType.Wall;

        GenerateBSP();
        SurroundFloorsWithWalls();
        ForceEdgeWalls();
        RenderDungeon();
        SpawnPlayer();
    }

    // ---------------------------
    // BSP GENERATION
    // ---------------------------
    void GenerateBSP()
    {
        rooms = new List<Room>();
        SubdivideWithRooms(0, 0, width, height, 5);
        ConnectRooms();
    }

    void SubdivideWithRooms(int x, int y, int w, int h, int depth)
    {
        if (depth <= 0 || w <= 4 || h <= 4)
        {
            int roomWidth = Random.Range(3, w);
            int roomHeight = Random.Range(3, h);
            int roomX = x + Random.Range(0, w - roomWidth + 1);
            int roomY = y + Random.Range(0, h - roomHeight + 1);

            CarveRoom(roomX, roomY, roomWidth, roomHeight);
            rooms.Add(new Room(roomX, roomY, roomWidth, roomHeight));
            return;
        }

        if (w > h)
        {
            int split = Random.Range(2, w - 2);
            SubdivideWithRooms(x, y, split, h, depth - 1);
            SubdivideWithRooms(x + split, y, w - split, h, depth - 1);
        }
        else
        {
            int split = Random.Range(2, h - 2);
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

            int corridorThickness = Random.Range(2, 4);

            if (Random.value < 0.5f)
            {
                CarveHorizontal(prev.x, curr.x, prev.y, corridorThickness);
                CarveVertical(prev.y, curr.y, curr.x, corridorThickness);
            }
            else
            {
                CarveVertical(prev.y, curr.y, prev.x, corridorThickness);
                CarveHorizontal(prev.x, curr.x, curr.y, corridorThickness);
            }
        }
    }

    void CarveHorizontal(int xStart, int xEnd, int y, int thickness)
    {
        int min = Mathf.Min(xStart, xEnd);
        int max = Mathf.Max(xStart, xEnd);
        for (int x = min; x <= max; x++)
            for (int t = 0; t < thickness; t++)
                if (y + t < height) dungeon[x, y + t] = TileType.Floor;
    }

    void CarveVertical(int yStart, int yEnd, int x, int thickness)
    {
        int min = Mathf.Min(yStart, yEnd);
        int max = Mathf.Max(yStart, yEnd);
        for (int y = min; y <= max; y++)
            for (int t = 0; t < thickness; t++)
                if (x + t < width) dungeon[x + t, y] = TileType.Floor;
    }

    // ---------------------------
    // WALLS
    // ---------------------------
    void SurroundFloorsWithWalls()
    {
        TileType[,] newDungeon = (TileType[,])dungeon.Clone();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (dungeon[x, y] == TileType.Floor)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nx = x + dx;
                            int ny = y + dy;
                            if (nx >= 0 && ny >= 0 && nx < width && ny < height)
                            {
                                if (dungeon[nx, ny] != TileType.Floor)
                                    newDungeon[nx, ny] = TileType.Wall;
                            }
                        }
                    }
                }
            }
        }

        dungeon = newDungeon;
    }

    void ForceEdgeWalls()
    {
        for (int x = 0; x < width; x++)
        {
            dungeon[x, 0] = TileType.Wall;
            dungeon[x, height - 1] = TileType.Wall;
        }
        for (int y = 0; y < height; y++)
        {
            dungeon[0, y] = TileType.Wall;
            dungeon[width - 1, y] = TileType.Wall;
        }
    }

    // ---------------------------
    // RENDERING
    // ---------------------------
    void RenderDungeon()
    {
        if (tilesParent == null)
        {
            Transform found = transform.Find("Tiles");
            if (found != null) tilesParent = found;
            else
            {
                GameObject go = new GameObject("Tiles");
                go.transform.parent = transform;
                go.transform.localPosition = Vector3.zero;
                tilesParent = go.transform;
            }
        }

        // Clear previous tiles
        foreach (Transform t in tilesParent)
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(t.gameObject); else Destroy(t.gameObject);
#else
            Destroy(t.gameObject);
#endif

        spawnedTiles.Clear();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3(x, y, 0f);
                Vector2Int gridPos = new Vector2Int(x, y);

                if (dungeon[x, y] == TileType.Floor)
                    SpawnWeightedTile(floorTiles, pos, gridPos, 0);
                else
                    SpawnWeightedTile(wallTiles, pos, gridPos, 1);
            }
        }
    }

    void SpawnWeightedTile(List<WeightedTile> tiles, Vector3 pos, Vector2Int gridPos, int orderInLayer)
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
                GameObject obj;
                if (!Application.isPlaying)
                    obj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(t.prefab, tilesParent) as GameObject;
                else
                    obj = Instantiate(t.prefab, tilesParent);
#else
                GameObject obj = Instantiate(t.prefab, tilesParent);
#endif
                obj.transform.localPosition = pos;
                obj.transform.localScale = Vector3.one;

                // Add TileFog if not already present
                if (obj.GetComponent<TileFog>() == null)
                    obj.AddComponent<TileFog>();

                SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = orderInLayer;

                spawnedTiles[gridPos] = obj;
                return;
            }
        }
    }

    // ---------------------------
    // PLAYER
    // ---------------------------
    public void SpawnPlayer()
    {
        if (playerPrefab == null || rooms == null || rooms.Count == 0) return;

        Room rootRoom = rooms[0];
        Vector3 spawnPos = new Vector3(rootRoom.Center.x, rootRoom.Center.y, -0.1f);

        if (spawnedPlayer != null)
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(spawnedPlayer); else Destroy(spawnedPlayer);
#else
            Destroy(spawnedPlayer);
#endif

        spawnedPlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity, transform);
        spawnedPlayer.transform.localScale = Vector3.one;
        SpriteRenderer sr = spawnedPlayer.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 2;

        // Ensure player has LightSource for fog
        if (spawnedPlayer.GetComponent<LightSource>() == null)
            spawnedPlayer.AddComponent<LightSource>();
    }

    // ---------------------------
    // CLEAR DUNGEON
    // ---------------------------
    public void ClearDungeon()
    {
        if (tilesParent != null)
        {
            List<Transform> children = new List<Transform>();
            foreach (Transform t in tilesParent) children.Add(t);
            foreach (Transform t in children)
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(t.gameObject); else Destroy(t.gameObject);
#else
                Destroy(t.gameObject);
#endif
        }

        if (spawnedPlayer != null)
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(spawnedPlayer); else Destroy(spawnedPlayer);
#else
            Destroy(spawnedPlayer);
#endif

        spawnedTiles.Clear();
    }
}
