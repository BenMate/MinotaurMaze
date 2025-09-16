using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
public class TileFog : MonoBehaviour
{
    public SpriteRenderer sr; //add this from the prefab cus im lazy
    public float fadeDistance = 5f; 

    private List<LightSource> lightSources = new List<LightSource>();

    void Awake()
    {
        if (sr == null)
            sr = GetComponent<SpriteRenderer>();

        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();

        if (sr == null)
            Debug.LogWarning("TileFog: No SpriteRenderer found on " + gameObject.name);

        // Find all light sources in the scene - bad and should be fixed but if it works for now big meh
        lightSources = new List<LightSource>(FindObjectsOfType<LightSource>());
    }

    void Update()
    {
        if (lightSources.Count == 0) return;

        float closestDistance = float.MaxValue;

        foreach (var light in lightSources)
        {
            float dist = Vector3.Distance(transform.position, light.transform.position);
            if (dist < closestDistance) closestDistance = dist;
        }

        float alpha = 1f - Mathf.Clamp01(closestDistance / fadeDistance);
        SetAlpha(alpha);
    }

    public void SetAlpha(float alpha)
    {
        if (sr == null) return;
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }
}
