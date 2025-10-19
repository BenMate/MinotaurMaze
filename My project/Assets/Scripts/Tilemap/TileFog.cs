using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
public class TileFog : MonoBehaviour
{
    [Header("Renderer")]
    public SpriteRenderer sr;

    [Header("Fog Settings")]
    public float fadeMultiplier = 1f;      // multiplies alpha (e.g., 1.2 = +20%)
    public float distanceOffset = 0f;      // reduces effective distance for earlier fade-in

    private List<LightSource> lightSources = new List<LightSource>();

    void Awake()
    {
        if (sr == null)
            sr = GetComponent<SpriteRenderer>();

        if (sr == null)
            Debug.LogWarning($"TileFog on '{name}' has no SpriteRenderer assigned.");
    }

    void Update()
    {
        UpdateFog();
    }

    public void UpdateFog()
    {
        if (sr == null || lightSources == null || lightSources.Count == 0)
            return;

        float alpha = 0f;
        Vector2 myPos = new Vector2(transform.position.x, transform.position.y);

        foreach (var light in lightSources)
        {
            if (light == null) continue;

            Vector2 lightPos = new Vector2(light.transform.position.x, light.transform.position.y);
            float dist = Vector2.Distance(myPos, lightPos) - distanceOffset;

            if (dist < 0f) dist = 0f;

            float lightAlpha = 1f - Mathf.Clamp01(dist / light.lightRadius);

            alpha = Mathf.Max(alpha, lightAlpha);
        }

        alpha *= fadeMultiplier;
        alpha = Mathf.Clamp01(alpha);

        SetAlpha(alpha);
    }

    public void SetAlpha(float alpha)
    {
        if (sr == null) return;

        Color c = sr.color;
        c.a = Mathf.Clamp01(alpha);
        sr.color = c;
    }

    public void SetLightSources(List<LightSource> lights)
    {
        lightSources = (lights == null) ? new List<LightSource>() : new List<LightSource>(lights);
        UpdateFog();
    }
}
