
using UnityEngine;

public class MirrorReflection : MonoBehaviour
{
    [Header("Pushable Objects")]
    [SerializeField] private PushableObject[] pushableObjects;

    [Header("Reflection Display")]
    [SerializeField] private SpriteRenderer reflectionSprite;
    [SerializeField] private bool flipHorizontally = true;

    [Header("Distance Settings")]
    [SerializeField, Min(0f)] private float fullAlphaDistance = 1f;
    [SerializeField, Min(0.1f)] private float invisibleDistance = 4f;

    [Header("Appearance")]
    [SerializeField, Range(0f, 1f)] private float maximumAlpha = 0.6f;

    private PushableObject nearestObject;
    private SpriteRenderer sourceSprite;

    private void Awake()
    {
        if (reflectionSprite != null)
            SetReflectionAlpha(0f);
    }

    private void LateUpdate()
    {
        if (reflectionSprite == null)
            return;

        FindNearestObject();

        if (nearestObject == null || sourceSprite == null)
        {
            SetReflectionAlpha(0f);
            return;
        }

        reflectionSprite.sprite = sourceSprite.sprite;
        reflectionSprite.flipX = flipHorizontally
            ? !sourceSprite.flipX
            : sourceSprite.flipX;
        reflectionSprite.flipY = sourceSprite.flipY;

        float distance = Vector2.Distance(
            transform.position,
            nearestObject.transform.position
        );

        float alpha;

        if (distance <= fullAlphaDistance)
        {
            alpha = maximumAlpha;
        }
        else if (distance >= invisibleDistance)
        {
            alpha = 0f;
        }
        else
        {
            float t = Mathf.InverseLerp(
                fullAlphaDistance,
                invisibleDistance,
                distance
            );

            alpha = Mathf.Lerp(maximumAlpha, 0f, t);
        }

        SetReflectionAlpha(alpha);
    }

    private void FindNearestObject()
    {
        nearestObject = null;
        sourceSprite = null;

        float nearestDistance = float.MaxValue;

        if (pushableObjects == null)
            return;

        foreach (PushableObject pushable in pushableObjects)
        {
            if (pushable == null)
                continue;

            float distance = Vector2.Distance(
                transform.position,
                pushable.transform.position
            );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestObject = pushable;
            }
        }

        if (nearestObject != null)
        {
            sourceSprite =
                nearestObject.GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void SetReflectionAlpha(float alpha)
    {
        Color color = reflectionSprite.color;
        color.a = alpha;
        reflectionSprite.color = color;
    }
}
