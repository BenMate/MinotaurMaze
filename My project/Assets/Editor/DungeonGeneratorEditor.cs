using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DungeonGenerator))]
public class DungeonGeneratorEditor : Editor
{
    void OnSceneGUI()
    {
        DungeonGenerator generator = (DungeonGenerator)target;
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
        DrawDefaultInspector();

        DungeonGenerator generator = (DungeonGenerator)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Dungeon", GUILayout.Height(40)))
        {
            serializedObject.ApplyModifiedProperties();
            generator.GenerateDungeon();
        }

        if (GUILayout.Button("Random Seed", GUILayout.Height(30)))
        {
            serializedObject.ApplyModifiedProperties();

            // Generate a new random seed
            generator.seed = Random.Range(int.MinValue, int.MaxValue);
            Debug.Log("New Random Seed: " + generator.seed);

            // Regenerate dungeon with the new seed
            generator.GenerateDungeon();
        }

        EditorGUILayout.HelpBox("Press 'R' in Scene view to regenerate.", MessageType.Info);
    }
}

