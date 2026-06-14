using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightFlicker2D : MonoBehaviour
{
    [Header("Light Settings")]
    public float minIntensity = 0.5f;
    public float maxIntensity = 1.0f;
    public float flickerSpeed = 0.1f;

    float timer;
    Light2D targetLight;

     void Start()
     {
        targetLight = GetComponent<Light2D>();
     }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= flickerSpeed)
        {
            //change levels of intensity
            targetLight.intensity = Random.Range(minIntensity, maxIntensity);

            //reset timer
            timer = 0;
        }
    }


}
