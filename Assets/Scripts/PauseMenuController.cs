using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    /// <summary>
    /// Called by your on-screen PauseButton in Game scene.
    /// </summary>
    public void ShowPauseMenu()
    {
        // freeze game
        Time.timeScale = 0f;
        // load the PauseMenu scene additively
        SceneManager.LoadScene("PauseMenu", LoadSceneMode.Additive);
    }

    /// <summary>
    /// Called by Resume button in PauseMenu.
    /// </summary>
    public void OnResume()
    {
        // unfreeze
        Time.timeScale = 1f;
        // unload the PauseMenu scene
        SceneManager.UnloadSceneAsync("PauseMenu");
    }

    /// <summary>
    /// Called by Home button in PauseMenu.
    /// </summary>
    public void OnHome()
    {
        Time.timeScale = 0f;
        GameManager.Instance.canResume = true;

        // Load MainMenu as overlay so we can Resume the current game
        if (!SceneManager.GetSceneByName("MainMenu").isLoaded)
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Additive);
    }

    /// <summary>
    /// Called by Quit button in PauseMenu.
    /// </summary>
    public void OnQuit()
    {
        Application.Quit();
    }
}
