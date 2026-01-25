using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashScreen : MonoBehaviour
{
    public float delay = 4.0f;
    public int buildIndex;


    void Start()
    {
        Invoke("LoadNextScene", delay);
    }

    void LoadNextScene()
    {
        SceneManager.LoadScene(buildIndex);
    }


}
