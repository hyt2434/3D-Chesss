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
        ResetGameResultHandlers();
        SceneManager.LoadScene("Game");
    }

    public void StartMultiplayerGame()
    {
        isSinglePlayerMode = false;
        canResume = false;
        ResetGameResultHandlers();
        SceneManager.LoadScene("Game");
    }

    /// <summary>
    /// Reset all game result handlers to prepare for a new game
    /// </summary>
    private void ResetGameResultHandlers()
    {
        // Reset winning screen manager
        try
        {
            var winningScreenManagerType = System.Type.GetType("WinningScreenManager");
            if (winningScreenManagerType != null)
            {
                var resetMethod = winningScreenManagerType.GetMethod("ResetGameEnded");
                if (resetMethod != null)
                {
                    resetMethod.Invoke(null, null);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("WinningScreenManager not found or not compiled yet: " + e.Message);
        }

        // Find all GameResultHandler instances and reset them using reflection
        try
        {
            var handlers = FindObjectsOfType<MonoBehaviour>();
            foreach (var handler in handlers)
            {
                if (handler != null && handler.GetType().Name == "GameResultHandler")
                {
                    var resetMethod = handler.GetType().GetMethod("ResetGameEnded");
                    if (resetMethod != null)
                    {
                        resetMethod.Invoke(handler, null);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("GameResultHandler not found or not compiled yet: " + e.Message);
        }
    }
}
