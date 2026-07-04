using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{

    public int loadScene;

    public GameObject[] scenesToHideOnStart;

    public GameObject deathScene;
    
    void Start()
    {
        foreach (GameObject go in scenesToHideOnStart) { go.SetActive(false); }
    }

    public void LoadGameScene()
    {
        SceneManager.LoadScene(loadScene);
    }

    public void PlayerDiedScene()
    {
        deathScene.SetActive(true);
    }

    public void Quit()
    {
        Debug.Log("Game has Quit");
        Application.Quit();
    }
}
