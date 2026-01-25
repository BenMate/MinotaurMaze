using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreen : MonoBehaviour
{
    public int loadScene;

    public GameObject mainScreen;
    public GameObject settingsScreen;

    void Start()
    {
        //resets the titlescreens
        mainScreen.gameObject.SetActive(true);
        settingsScreen.gameObject.SetActive(false);
    }

    //used by the play button
    public void LoadGameScene()
    {
        SceneManager.LoadScene(loadScene);
    }

    public void Quit()
    {
        Debug.Log("Game has Quit");
        Application.Quit();      
    }
   
}
