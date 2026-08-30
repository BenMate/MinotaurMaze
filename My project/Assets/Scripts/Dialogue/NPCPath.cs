using System.Collections.Generic;
using UnityEngine;

public class NPCPath : MonoBehaviour
{
    [SerializeField]
    private List<Transform> points =
        new List<Transform>();


    public Transform GetPoint(int index)
    {
        if (index < 0 ||
            index >= points.Count)
        {
            return null;
        }

        return points[index];
    }


    public int PointCount
    {
        get
        {
            return points.Count;
        }
    }


    private void OnDrawGizmos()
    {
        if (points == null ||
            points.Count == 0)
        {
            return;
        }


        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] == null)
                continue;

            Gizmos.DrawSphere(
                points[i].position,
                0.1f
            );


            if (i < points.Count - 1 &&
                points[i + 1] != null)
            {
                Gizmos.DrawLine(
                    points[i].position,
                    points[i + 1].position
                );
            }
        }
    }
}