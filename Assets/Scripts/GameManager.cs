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
        else Destroy(gameObject);
    }

    public void StartBotGame()
    {
        isSinglePlayerMode = true;
        canResume = false;
        SceneManager.LoadScene("Game");
    }

    public void StartMultiplayerGame()
    {
        isSinglePlayerMode = false;
        canResume = false;
        SceneManager.LoadScene("Game");
    }
}
