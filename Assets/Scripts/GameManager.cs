using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public bool isSinglePlayerMode = false;
    public bool canResume = false;

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

        PlayerPrefs.SetInt("UseTimer", 0); // make sure timer is off
        PlayerPrefs.Save();

        SceneManager.LoadScene("Game");
    }

    // Multiplayer: Timer controlled by TimerMenu (it sets UseTimer)
    public void StartMultiplayerGame()
    {
        isSinglePlayerMode = false;
        canResume = false;

        SceneManager.LoadScene("Game");
    }
}
