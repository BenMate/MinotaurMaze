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
    public GenerationType generationType;
    public bool regenerateOnPlay = true;
    public int seed = 0;

    public enum GenerationType { RandomWalk, BSP, CellularAutomata }

    private enum TileType { Floor, Wall }
    private TileType[,] dungeon;

    // BSP room tracking
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

        GenerateDungeon();
    }

    public void GenerateDungeon()
    {
        width = Mathf.Max(width, minSize);
        height = Mathf.Max(height, minSize);

        // Seed random
        if (seed == 0)
            Random.InitState(System.Environment.TickCount);
        else
            Random.InitState(seed);

        dungeon = new TileType[width, height];

        switch (generationType)
        {
            case GenerationType.RandomWalk: GenerateRandomWalk(); break;
            case GenerationType.BSP: GenerateBSP(); break;
            case GenerationType.CellularAutomata: GenerateCellularAutomata(); break;
        }

        RenderDungeon();
    }

    // ---------------------------
    // RANDOM WALK
    // ---------------------------
    void GenerateRandomWalk()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                dungeon[x, y] = TileType.Wall;

        Vector2Int pos = new Vector2Int(width / 2, height / 2);
        int targetTiles = (int)(width * height * 0.6f);

        for (int i = 0; i < targetTiles; i++)
        {
            dungeon[pos.x, pos.y] = TileType.Floor;

            int dir = Random.Range(0, 4);
            if (dir == 0 && pos.x > 1) pos.x--;
            else if (dir == 1 && pos.x < width - 2) pos.x++;
            else if (dir == 2 && pos.y > 1) pos.y--;
            else if (dir == 3 && pos.y < height - 2) pos.y++;
        }
    }

    // ---------------------------
    // BSP WITH CORRIDORS
    // ---------------------------
    void GenerateBSP()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                dungeon[x, y] = TileType.Wall;

        rooms = new List<Room>();
        SubdivideWithRooms(0, 0, width, height, 5);
        ConnectRooms();
    }

    void SubdivideWithRooms(int x, int y, int w, int h, int depth)
    {
        if (depth <= 0 || w <= 4 || h <= 4)
        {
            int roomWidth = Random.Range(2, w);
            int roomHeight = Random.Range(2, h);
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
        for (int x = min; x <= max; x++) dungeon[x, y] = TileType.Floor;
    }

    void CarveVertical(int yStart, int yEnd, int x)
    {
        int min = Mathf.Min(yStart, yEnd);
        int max = Mathf.Max(yStart, yEnd);
        for (int y = min; y <= max; y++) dungeon[x, y] = TileType.Floor;
    }

    // ---------------------------
    // CELLULAR AUTOMATA
    // ---------------------------
    void GenerateCellularAutomata()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                dungeon[x, y] = (Random.value < 0.45f) ? TileType.Wall : TileType.Floor;

        for (int i = 0; i < 5; i++)
        {
            TileType[,] newMap = new TileType[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    int walls = CountWallsAround(x, y);
                    newMap[x, y] = (walls > 4) ? TileType.Wall : TileType.Floor;
                }
            dungeon = newMap;
        }
    }

    int CountWallsAround(int x, int y)
    {
        int count = 0;
        for (int i = x - 1; i <= x + 1; i++)
            for (int j = y - 1; j <= y + 1; j++)
            {
                if (i < 0 || j < 0 || i >= width || j >= height) count++;
                else if (dungeon[i, j] == TileType.Wall) count++;
            }
        return count;
    }

    // ---------------------------
    // RENDERING
    // ---------------------------
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

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3(x, y, 0);
                if (dungeon[x, y] == TileType.Floor)
                    SpawnWeightedTile(floorTiles, pos);
                else
                    SpawnWeightedTile(wallTiles, pos);
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
                GameObject obj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(t.prefab, transform);
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
}


