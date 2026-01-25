using UnityEngine;
using System.Collections.Generic;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Dungeon Size")]
    [Range(5, 150)] public int width = 50;
    [Range(5, 150)] public int height = 50;
    
    const int MIN_SIZE = 5;

    [Header("Tiles")]
    public List<WeightedTile> floorTiles;
    public List<WeightedTile> wallTiles;

    [Header("Generation Settings")]
    public int seed = 0;
    public bool useRandomSeed = true;

    [Header("Player & Enemy Prefabs")]
    public GameObject playerPrefab;
    public GameObject enemyPrefab;
    public int enemyCount = 1;

    [HideInInspector] public TileType[,] dungeon;
    public enum TileType { Floor, Wall }

    private List<Room> rooms;
    private Transform tilesParent;
    private GameObject spawnedPlayer;
    public bool generatedInEditor = false;

    public List<Room> Rooms => rooms;

    private void Start()
    {
        SpawnPlayerAndEnemies();
    }

    // ---------------------------
    // PLAYER & ENEMY SPAWNING
    // ---------------------------
    public void SpawnPlayerAndEnemies()
    {
        if (rooms == null || rooms.Count == 0)
            GenerateDungeon();

        // Spawn Player
        if (playerPrefab != null)
        {
            Room startRoom = rooms[0];
            Vector3 spawnPos = new Vector3(startRoom.Center.x, startRoom.Center.y, -0.1f);
            spawnedPlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity, transform);
            spawnedPlayer.name = "Player";
        }

        // Spawn Enemies
        if (enemyPrefab != null)
        {
            for (int i = 0; i < enemyCount; i++)
            {
                Room randomRoom;
                do
                {
                    randomRoom = rooms[Random.Range(1, rooms.Count)];
                } while (Vector2Int.Distance(randomRoom.Center, rooms[0].Center) < Mathf.Max(width, height) / 3);

                Vector3 spawnPos = new Vector3(randomRoom.Center.x, randomRoom.Center.y, -0.1f);
                GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity, transform);
                enemy.name = $"Minotaur_{i + 1}";

                MinotaurAI ai = enemy.GetComponent<MinotaurAI>();
                if (ai != null)
                {
                    ai.dungeonGenerator = this;
                    ai.player = spawnedPlayer?.transform;
                }

                TileFog fog = enemy.GetComponent<TileFog>();
                if (fog == null) fog = enemy.AddComponent<TileFog>();

                fog.fadeMultiplier = 1.2f;
                fog.distanceOffset = 1f;

                if (spawnedPlayer != null)
                {
                    LightSource playerLight = spawnedPlayer.GetComponent<LightSource>();
                    if (playerLight != null)
                        fog.SetLightSources(new List<LightSource> { playerLight });
                }
            }
        }

        // Assign lights to tiles
        if (tilesParent != null)
        {
            TileFog[] tileFogs = tilesParent.GetComponentsInChildren<TileFog>();
            if (tileFogs.Length > 0 && spawnedPlayer != null)
            {
                LightSource playerLight = spawnedPlayer.GetComponent<LightSource>();
                if (playerLight != null)
                {
                    foreach (var tile in tileFogs)
                        tile.SetLightSources(new List<LightSource> { playerLight });
                }
            }
        }
    }

    // ---------------------------
    // DUNGEON GENERATION
    // ---------------------------
    public void GenerateDungeon()
    {
        ClearDungeon();

        width = Mathf.Max(width, MIN_SIZE);
        height = Mathf.Max(height, MIN_SIZE);

        if (useRandomSeed) seed = Random.Range(int.MinValue, int.MaxValue);
        Random.InitState(seed);

        dungeon = new TileType[width, height];

        // Fill walls
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                dungeon[x, y] = TileType.Wall;

        GenerateBSP();
        SurroundFloorsWithWalls();
        ForceEdgeWalls();
        RenderDungeon();
    }

    private void GenerateBSP()
    {
        rooms = new List<Room>();
        SubdivideWithRooms(0, 0, width, height, 5);
        ConnectRooms();
    }

    private void SubdivideWithRooms(int x, int y, int w, int h, int depth)
    {
        if (depth <= 0 || w <= 6 || h <= 6)
        {
            int roomWidth = Random.Range(3, Mathf.Max(4, w - 2));
            int roomHeight = Random.Range(3, Mathf.Max(4, h - 2));
            int roomX = x + Random.Range(0, Mathf.Max(1, w - roomWidth));
            int roomY = y + Random.Range(0, Mathf.Max(1, h - roomHeight));

            CarveRoom(roomX, roomY, roomWidth, roomHeight);
            rooms.Add(new Room(roomX, roomY, roomWidth, roomHeight));
            return;
        }

        if (w > h)
        {
            int split = Random.Range(3, w - 3);
            SubdivideWithRooms(x, y, split, h, depth - 1);
            SubdivideWithRooms(x + split, y, w - split, h, depth - 1);
        }
        else
        {
            int split = Random.Range(3, h - 3);
            SubdivideWithRooms(x, y, w, split, depth - 1);
            SubdivideWithRooms(x, y + split, w, h - split, depth - 1);
        }
    }

    private void CarveRoom(int x, int y, int w, int h)
    {
        for (int i = x; i < x + w; i++)
            for (int j = y; j < y + h; j++)
                SetFloorSafe(i, j);
    }

    private void ConnectRooms()
    {
        for (int i = 1; i < rooms.Count; i++)
        {
            Vector2Int prev = rooms[i - 1].Center;
            Vector2Int curr = rooms[i].Center;
            int thickness = 4;

            if (Random.value < 0.5f)
                CarveCorridor(prev, curr, thickness);
            else
                CarveCorridor(curr, prev, thickness);
        }
    }

    private void CarveCorridor(Vector2Int start, Vector2Int end, int thickness)
    {
        int x0 = start.x, y0 = start.y;
        int x1 = end.x, y1 = end.y;
        int dx = x1 >= x0 ? 1 : -1;
        int dy = y1 >= y0 ? 1 : -1;

        // Horizontal
        for (int x = x0; x != x1 + dx; x += dx)
            for (int t = 0; t < thickness; t++)
                SetFloorSafe(x, y0 + t);

        // Vertical
        for (int y = y0; y != y1 + dy; y += dy)
            for (int t = 0; t < thickness; t++)
                SetFloorSafe(x1 + t, y);
    }

    private void SetFloorSafe(int x, int y)
    {
        if (x >= 0 && y >= 0 && x < width && y < height)
            dungeon[x, y] = TileType.Floor;
    }

    // ---------------------------
    // WALLS
    // ---------------------------
    private void SurroundFloorsWithWalls()
    {
        HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (dungeon[x, y] == TileType.Floor)
                    floorPositions.Add(new Vector2Int(x, y));

        TileType[,] newDungeon = (TileType[,])dungeon.Clone();

        foreach (var pos in floorPositions)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = pos.x + dx;
                    int ny = pos.y + dy;

                    if (nx >= 0 && ny >= 0 && nx < width && ny < height)
                        if (!floorPositions.Contains(new Vector2Int(nx, ny)))
                            newDungeon[nx, ny] = TileType.Wall;
                }
        }

        dungeon = newDungeon;
    }

    private void ForceEdgeWalls()
    {
        for (int x = 0; x < width; x++) { dungeon[x, 0] = TileType.Wall; dungeon[x, height - 1] = TileType.Wall; }
        for (int y = 0; y < height; y++) { dungeon[0, y] = TileType.Wall; dungeon[width - 1, y] = TileType.Wall; }
    }

    // ---------------------------
    // TILE SPAWNING
    // ---------------------------
    private void RenderDungeon()
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

        if (tilesParent.childCount > 0 && (!generatedInEditor || !Application.isPlaying))
        {
            Transform[] children = new Transform[tilesParent.childCount];
            for (int i = 0; i < tilesParent.childCount; i++)
                children[i] = tilesParent.GetChild(i);

            foreach (Transform t in children)
#if UNITY_EDITOR
                DestroyImmediate(t.gameObject);
#else
                Destroy(t.gameObject);
#endif
        }

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3(x, y, 0);
                if (dungeon[x, y] == TileType.Floor) SpawnWeightedTile(floorTiles, pos);
                else SpawnWeightedTile(wallTiles, pos);
            }
    }

    private void SpawnWeightedTile(List<WeightedTile> tiles, Vector3 pos)
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
                GameObject obj = Instantiate(t.prefab, tilesParent);
                obj.transform.localPosition = pos;
                obj.transform.localScale = Vector3.one;

                TileFog fog = obj.GetComponent<TileFog>();
                if (fog == null) fog = obj.AddComponent<TileFog>();

                fog.fadeMultiplier = 1f;
                fog.distanceOffset = 0f;

                if (spawnedPlayer != null)
                {
                    LightSource playerLight = spawnedPlayer.GetComponent<LightSource>();
                    if (playerLight != null)
                        fog.SetLightSources(new List<LightSource> { playerLight });
                }

                return;
            }
        }
    }

    // ---------------------------
    // CLEAR DUNGEON
    // ---------------------------
    public void ClearDungeon()
    {
        if (tilesParent != null)
        {
            Transform[] children = new Transform[tilesParent.childCount];
            for (int i = 0; i < tilesParent.childCount; i++)
                children[i] = tilesParent.GetChild(i);

            foreach (Transform t in children)
#if UNITY_EDITOR
                DestroyImmediate(t.gameObject);
#else
                Destroy(t.gameObject);
#endif
        }

        dungeon = new TileType[width, height];
        rooms = new List<Room>();
        spawnedPlayer = null;
        generatedInEditor = false;
    }

    // ---------------------------
    // ROOM CLASS
    // ---------------------------
    [System.Serializable]
    public class Room
    {
        public int x, y, width, height;
        public Vector2Int Center => new Vector2Int(x + width / 2, y + height / 2);
        public Room(int x, int y, int w, int h) { this.x = x; this.y = y; width = w; height = h; }
    }
}
