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

    public override void OnInspectorGUI()
    {
        if (generator == null) return;

        serializedObject.Update();
        DrawDefaultInspector();
        GUILayout.Space(10);

        if (GUILayout.Button("Generate Dungeon", GUILayout.Height(40)))
        {
            serializedObject.ApplyModifiedProperties();

            generator.generatedInEditor = true;
            generator.ClearDungeon();
            generator.GenerateDungeon();
            Debug.Log("Dungeon tiles generated in editor.");
        }

        if (GUILayout.Button("Clear Dungeon", GUILayout.Height(30)))
        {
            serializedObject.ApplyModifiedProperties();
            generator.ClearDungeon();
            Debug.Log("Dungeon cleared.");
        }

        serializedObject.ApplyModifiedProperties();
    }
}
