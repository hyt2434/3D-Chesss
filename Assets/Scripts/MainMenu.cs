using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Button: Play with Bot (NO timer)
    public void PlayWithBot()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartBotGame();
        }
        else
        {
            // Fallback if GameManager isn't in this scene (shouldn't happen)
            PlayerPrefs.SetInt("UseTimer", 0);
            PlayerPrefs.Save();
            SceneManager.LoadScene("Game");
        }
    }

    // Button: Play with Player (go pick timer settings)
    public void PlayMultiplayer()
    {
        SceneManager.LoadScene("TimerMenu");
    }

    public void Back()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}
