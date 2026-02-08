using UnityEngine;
using System.Collections.Generic;

public class RoomModule : MonoBehaviour
{
    // Enum is inside the class now
    public enum Direction
    {
        North, South, East, West
    }

    [System.Serializable]
    public class AttachmentPoint
    {
        public Vector3 localPosition;       // Position relative to room
        public Vector3 size = new Vector3(1, 1, 0); // Width = size.x, height for gizmo = size.y
        public Direction forwardDir = Direction.North;
        [HideInInspector] public bool isUsed = false;

        public Vector3 ForwardVector
        {
            get
            {
                switch (forwardDir)
                {
                    case Direction.North: return Vector3.up;
                    case Direction.South: return Vector3.down;
                    case Direction.East: return Vector3.right;
                    case Direction.West: return Vector3.left;
                }
                return Vector3.up;
            }
        }
    }

    public List<AttachmentPoint> attachmentPoints = new List<AttachmentPoint>();

    public AttachmentPoint GetUnusedAttachment()
    {
        foreach (var point in attachmentPoints)
        {
            if (!point.isUsed) return point;
        }
        return null;
    }

    public Bounds GetRoomBounds()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) return col.bounds;

        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null) return rend.bounds;

        return new Bounds(transform.position, Vector3.one);
    }
}
