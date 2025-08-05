using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("The root panel that contains your main menu buttons")]
    public GameObject mainMenuPanel;

    [Header("Buttons")]
    [Tooltip("Resume button (only shown if returning from a paused game)")]
    public Button resumeButton;
    [Tooltip("Go to timer setup before multiplayer game")]
    public Button multiplayerButton;
    [Tooltip("Start a new game vs. bot")]
    public Button botButton;
    [Tooltip("Quit the application")]
    public Button quitButton;

    [Header("Confirm Dialog")]
    [Tooltip("Your confirmation‐dialog controller")]
    public ConfirmDialogController confirmDialog;

    [Header("Ranking Display")]
    [Tooltip("The ranking display component")]
    public MonoBehaviour rankingDisplayRef;



    void Start()
    {
        // Check for new players that need points (in case PlayerRanking didn't catch it)
        CheckForNewPlayerNeedingPoints();
        
        // Initialize ranking display
        // --- Ranking Display (unchanged) ---
        if (rankingDisplayRef != null && rankingDisplayRef.GetType().Name == "RankingDisplay")
        {
            var updateMethod = rankingDisplayRef.GetType().GetMethod("UpdateRankingDisplay");
            updateMethod?.Invoke(rankingDisplayRef, null);
        }

        // Initialize the main menu
        InitializeMainMenu();
    }

    private void CheckForNewPlayerNeedingPoints()
    {
        string newPlayerName = PlayerPrefs.GetString("NewPlayerNeedsPoints", "");
        if (!string.IsNullOrEmpty(newPlayerName))
        {
            if (PlayerRanking.Instance != null)
            {
                // Give this new player exactly 1000 points
                PlayerRanking.Instance.CreateNewPlayer(newPlayerName, 1000);
                // Clear the flag
                PlayerPrefs.DeleteKey("NewPlayerNeedsPoints");
                PlayerPrefs.Save();
            }
        }
    }

    void InitializeMainMenu()
    {
        // 1) Should we show "Resume"? (only if we came via Pause→Home)
        bool canResume = GameManager.Instance.canResume;
        resumeButton.gameObject.SetActive(canResume);

        // 2) Resume simply unpauses & unloads this menu
        resumeButton.onClick.AddListener(() =>
        {
            GameManager.Instance.canResume = false;
            Time.timeScale = 1f;
            SceneManager.UnloadSceneAsync("MainMenu");
        });

        // 3) Multiplayer → TimerMenu (unchanged)
        multiplayerButton.onClick.AddListener(() =>
        {
            if (canResume)
            {
                mainMenuPanel.SetActive(false);
                confirmDialog.Show(
                    "ARE YOU SURE YOU WANT TO START A NEW GAME?",
                    onYes: () =>
                    {
                        GameManager.Instance.canResume = false;
                        GameManager.Instance.isSinglePlayerMode = false;
                        // Store that we're setting up multiplayer
                        PlayerPrefs.SetString("MultiplayerSetup", "true");
                        PlayerPrefs.Save();
                        // unload this menu
                        SceneManager.UnloadSceneAsync("MainMenu");
                        // go to player menu for second player
                        SceneManager.LoadScene("PlayerMenu");
                        SceneManager.UnloadSceneAsync("MainMenu");
                        SceneManager.LoadScene("TimerMenu");
                    },
                    onNo: () => mainMenuPanel.SetActive(true)
                );
            }
            else
            {
                // direct start → ensure multiplayer mode, then go to player menu for second player
                GameManager.Instance.isSinglePlayerMode = false;
                // Store that we're setting up multiplayer
                PlayerPrefs.SetString("MultiplayerSetup", "true");
                PlayerPrefs.Save();
                SceneManager.LoadScene("PlayerMenu");
            }
        });

        // 4) Play With Bot - shows side selection panel
        botButton.onClick.AddListener(() =>
        {
            if (canResume)
            {
                mainMenuPanel.SetActive(false);
                confirmDialog.Show(
                    "ARE YOU SURE YOU WANT TO START A NEW GAME?",
                    onYes: () =>
                    {
                        GameManager.Instance.canResume = false;
                        GameManager.Instance.isSinglePlayerMode = true;
                        SceneManager.LoadScene("SideSelectionMenu");
                    },
                    onNo: () => mainMenuPanel.SetActive(true)
                );
            }
            else
            {
                GameManager.Instance.isSinglePlayerMode = true;
                SceneManager.LoadScene("SideSelectionMenu");
            }
        });

        // 5) Quit
        quitButton.onClick.AddListener(() => Application.Quit());
    }


}
