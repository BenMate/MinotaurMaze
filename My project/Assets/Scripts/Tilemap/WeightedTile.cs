using UnityEngine;

[System.Serializable]
public class WeightedTile
{
    public GameObject prefab;
    [Range(0, 1)]
    public float spawnChance = 1f;
    [Range(0.5f, 1.5f)]
    public float minScale = 0.8f;
    [Range(0.5f, 1.5f)]
    public float maxScale = 1.0f;
    public bool randomRotation = true;
}
