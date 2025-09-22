using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DungeonGenerator))]
public class DungeonGeneratorEditor : Editor
{
    private DungeonGenerator generator;

    void OnEnable()
    {
        generator = target as DungeonGenerator;
    }

    void OnSceneGUI()
    {
        if (generator == null) return;

        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.R)
        {
            serializedObject.ApplyModifiedProperties();
            generator.GenerateDungeon();
            Debug.Log("Dungeon regenerated with hotkey [R]");
            e.Use();
        }
    }

    public override void OnInspectorGUI()
    {
        if (generator == null) return;

        serializedObject.Update();

        DrawDefaultInspector();
        GUILayout.Space(10);

        if (GUILayout.Button("Generate Dungeon", GUILayout.Height(40)))
        {
            serializedObject.ApplyModifiedProperties();
            generator.GenerateDungeon();
        }

        if (GUILayout.Button("Clear Dungeon", GUILayout.Height(30)))
        {
            serializedObject.ApplyModifiedProperties();
            generator.ClearDungeon();
        }

        generator.showPreview = EditorGUILayout.Toggle("Show Preview", generator.showPreview);
        generator.useGizmos = EditorGUILayout.Toggle("Use Gizmos", generator.useGizmos);

        EditorGUILayout.HelpBox("Press 'R' in Scene view to regenerate the dungeon.", MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }
}
