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

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Always unpause on entering a scene (prevents frozen interpolation)
        if (Time.timeScale == 0f) Time.timeScale = 1f;

        if (scene.name == "Game")
        {
            // Bot: timer OFF. Multiplayer: follow PlayerPrefs("UseTimer")
            bool useTimer = !isSinglePlayerMode && PlayerPrefs.GetInt("UseTimer", 0) == 1;

            var timer = FindObjectOfType<GameTimer>(true);
            if (timer != null)
            {
                timer.gameObject.SetActive(useTimer);
                if (useTimer) timer.ResetTimers();
            }
            else
            {
                Debug.LogWarning("GameTimer not found in Game scene.");
            }

            canResume = false;
        }
    }

    // Single-player: force timer OFF
    public void StartBotGame()
    {
        isSinglePlayerMode = true;
        canResume = false;

        if (Time.timeScale == 0f) Time.timeScale = 1f;

        PlayerPrefs.SetInt("UseTimer", 0);
        PlayerPrefs.Save();

        Debug.Log("Starting Bot Game - Loading Game scene in Single mode");
        SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }

    // Multiplayer: Timer controlled by TimerMenu (it sets UseTimer)
    public void StartMultiplayerGame()
    {
        isSinglePlayerMode = false;
        canResume = false;

        if (Time.timeScale == 0f) Time.timeScale = 1f;

        ResetGameState();

        Debug.Log("Starting Multiplayer Game - Loading Game scene in Single mode");
        SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }

    private void ResetGameState()
    {
        Debug.Log("Resetting game state for new multiplayer session");
        System.GC.Collect();
    }
}
