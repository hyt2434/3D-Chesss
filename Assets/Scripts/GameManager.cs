using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public bool isSinglePlayerMode = false;

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

    // Call this from "PLAY WITH BOT" button
    public void StartBotGame()
    {
        isSinglePlayerMode = true;
        SceneManager.LoadScene("Game");
    }

    // Call this from "PLAY GAME" button for multiplayer
    public void StartMultiplayerGame()
    {
        isSinglePlayerMode = false;
        SceneManager.LoadScene("Game");
    }
}