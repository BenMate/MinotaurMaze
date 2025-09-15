using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DungeonGenerator))]
public class DungeonGeneratorEditor : Editor
{
    private DungeonGenerator generator;
    private DungeonGenerator.TileType[,] previewDungeon;

    void OnEnable()
    {
        generator = (DungeonGenerator)target;
        CachePreviewDungeon();
    }

    void OnSceneGUI()
    {
        Event e = Event.current;
        if (!generator) return;

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.R)
        {
            serializedObject.ApplyModifiedProperties();
            generator.GenerateDungeon();
            CachePreviewDungeon();
            Debug.Log("Dungeon regenerated with hotkey [R]");
            e.Use();
        }

        if (generator.showPreview && !generator.useGizmos && previewDungeon != null)
        {
            DrawPreviewDungeon();
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (!generator) return;

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Dungeon", GUILayout.Height(40)))
        {
            serializedObject.ApplyModifiedProperties();
            generator.GenerateDungeon();
            CachePreviewDungeon();
        }

        if (GUILayout.Button("Random Seed", GUILayout.Height(30)))
        {
            serializedObject.ApplyModifiedProperties();
            generator.seed = Random.Range(int.MinValue, int.MaxValue);
            Debug.Log("New Random Seed: " + generator.seed);
            generator.GenerateDungeon();
            CachePreviewDungeon();
        }

        if (GUILayout.Button("Spawn Player", GUILayout.Height(30)))
        {
            serializedObject.ApplyModifiedProperties();
            generator.SpawnPlayer();
        }

        generator.showPreview = EditorGUILayout.Toggle("Show Preview", generator.showPreview);
        generator.useGizmos = EditorGUILayout.Toggle("Use Gizmos", generator.useGizmos);

        EditorGUILayout.HelpBox("Press 'R' in Scene view to regenerate.", MessageType.Info);
    }

    private void CachePreviewDungeon()
    {
        if (generator == null || generator.dungeon == null) return;
        previewDungeon = new DungeonGenerator.TileType[generator.gridWidth, generator.gridHeight];
        System.Array.Copy(generator.dungeon, previewDungeon, generator.dungeon.Length);
    }

    private void DrawPreviewDungeon()
    {
        if (previewDungeon == null) return;
        Color floorColor = new Color(0.4f, 0.8f, 1f, 0.3f);
        Color wallColor = new Color(1f, 0.4f, 0.4f, 0.3f);
        Color outlineColor = Color.black;

        for (int x = 0; x < generator.gridWidth; x++)
        {
            for (int y = 0; y < generator.gridHeight; y++)
            {
                Vector3 pos = new Vector3(x - 1, y - 1, 0);
                Rect rect = new Rect(pos.x, pos.y, 1f, 1f);

                switch (previewDungeon[x, y])
                {
                    case DungeonGenerator.TileType.Floor:
                        Handles.DrawSolidRectangleWithOutline(rect, floorColor, outlineColor);
                        break;
                    case DungeonGenerator.TileType.Wall:
                        Handles.DrawSolidRectangleWithOutline(rect, wallColor, outlineColor);
                        break;
                }
            }
        }
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    static void DrawGizmos(DungeonGenerator generator, GizmoType gizmoType)
    {
        if (generator == null) return;
        if (!generator.showPreview || !generator.useGizmos) return;
        if (generator.dungeon == null) return;

        for (int x = 0; x < generator.gridWidth; x++)
        {
            for (int y = 0; y < generator.gridHeight; y++)
            {
                Vector3 pos = new Vector3(x - 1, y - 1, 0);
                switch (generator.dungeon[x, y])
                {
                    case DungeonGenerator.TileType.Floor:
                        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.3f);
                        break;
                    case DungeonGenerator.TileType.Wall:
                        Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.3f);
                        break;
                    default: continue;
                }
                Gizmos.DrawCube(pos + Vector3.one * 0.5f, Vector3.one);
            }
        }
    }
}







