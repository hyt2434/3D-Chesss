using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public bool isSinglePlayerMode = false;
    public bool canResume = false;
    public bool isPlayerWhite = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Single-player: force timer OFF
    public void StartBotGame()
    {
        isSinglePlayerMode = true;
        canResume = false;

        // If we came from Pause→Home, unpause before loading a fresh scene
        if (Time.timeScale == 0f) Time.timeScale = 1f;

        PlayerPrefs.SetInt("UseTimer", 0); // make sure timer is off vs bot
        PlayerPrefs.Save();

        SceneManager.LoadScene("Game");
    }

    // Multiplayer: Timer controlled by TimerMenu (it sets UseTimer)
    public void StartMultiplayerGame()
    {
        isSinglePlayerMode = false;
        canResume = false;

        // Same: ensure not paused when starting a new match
        if (Time.timeScale == 0f) Time.timeScale = 1f;

        SceneManager.LoadScene("Game");
    }
}
