using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoomModule))]
public class RoomModuleGizmos : Editor
{
    void OnEnable()
    {
        EditorApplication.update += RepaintScene;
    }

    void OnDisable()
    {
        EditorApplication.update -= RepaintScene;
    }

    void RepaintScene()
    {
        SceneView.RepaintAll();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RoomModule room = (RoomModule)target;
        if (room.attachmentPoints == null) return;

        // Clamp sizes so cubes are visible
        foreach (var point in room.attachmentPoints)
        {
            point.size.x = Mathf.Max(0.01f, point.size.x);
            point.size.y = Mathf.Max(0.01f, point.size.y);
        }
    }

    void OnSceneGUI()
    {      
        RoomModule room = (RoomModule)target;
        if (room.attachmentPoints == null) return;

        Handles.color = Color.cyan;

        foreach (var point in room.attachmentPoints)
        {
            Vector3 worldPos = room.transform.TransformPoint(point.localPosition);
            Vector3 forward = room.transform.TransformDirection(point.ForwardVector);

            // Clamp width/height
            float width = Mathf.Max(0.01f, point.size.x);
            float height = Mathf.Max(0.01f, point.size.y);

            // Draw 2D wire cube (blue cube)
            Handles.DrawWireCube(worldPos, new Vector3(width, height, 0.05f));

            // Draw flat rectangle along forward for visualizing connection
            Vector3 right = Vector3.Cross(Vector3.forward, forward).normalized;
            Vector3 halfRight = right * (width / 2f);
            Vector3 halfForward = forward * 0.01f; // tiny for 2D

            Vector3 r1 = worldPos + halfRight + halfForward;
            Vector3 r2 = worldPos - halfRight + halfForward;
            Vector3 r3 = worldPos - halfRight - halfForward;
            Vector3 r4 = worldPos + halfRight - halfForward;

            Handles.DrawLine(r1, r2);
            Handles.DrawLine(r2, r3);
            Handles.DrawLine(r3, r4);
            Handles.DrawLine(r4, r1);

            // Draw arrow for forward
            Handles.ArrowHandleCap(0, worldPos, Quaternion.LookRotation(forward), 0.5f, EventType.Repaint);

            // Allow moving attachment point
            EditorGUI.BeginChangeCheck();
            Vector3 newPos = Handles.PositionHandle(worldPos, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(room, "Move Attachment Point");
                point.localPosition = room.transform.InverseTransformPoint(newPos);
            }
        }
    }
}
