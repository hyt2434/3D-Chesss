// PauseMenuController.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    /// <summary>
    /// Called by the Pause button in the Game scene.
    /// It pauses the game and loads the PauseMenu scene additively.
    /// </summary>
    public void OnPauseButton()
    {
        // Mark that we can resume later from MainMenu
        GameManager.Instance.canResume = true;

        // Freeze the game
        Time.timeScale = 0f;

        // Load the PauseMenu scene on top of the Game scene
        SceneManager.LoadScene("PauseMenu", LoadSceneMode.Additive);
    }

    /// <summary>
    /// Called by the Resume button in the PauseMenu scene.
    /// It unloads the PauseMenu scene and unpauses the game.
    /// </summary>
    public void OnResume()
    {
        // Unload the PauseMenu scene
        SceneManager.UnloadSceneAsync("PauseMenu");

        // Unfreeze the game
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Called by the Home button in the PauseMenu scene.
    /// It keeps the Game scene paused in the background,
    /// loads the MainMenu scene additively, and unloads PauseMenu.
    /// </summary>
    public void OnHome()
    {
        // Keep the resume flag so MainMenu shows the Resume button
        GameManager.Instance.canResume = true;

        // Load the MainMenu scene additively
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Additive);

        // Unload the PauseMenu scene
        SceneManager.UnloadSceneAsync("PauseMenu");
    }
   // Called by the Quit button in the PauseMenu scene.
   // It quits the application.
    public void OnQuit()
    {
        Application.Quit();
    }
}
