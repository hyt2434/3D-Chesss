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



    void Start()
    {


        // Initialize the main menu
        InitializeMainMenu();
    }

    void InitializeMainMenu()
    {
        // 1) Show Resume only if we came via Pause→Home
        bool canResume = GameManager.Instance.canResume;
        resumeButton.gameObject.SetActive(canResume);

        // 2) Resume → unpause & unload MainMenu
        resumeButton.onClick.AddListener(() =>
        {
            GameManager.Instance.canResume = false;
            Time.timeScale = 1f;
            SceneManager.UnloadSceneAsync("MainMenu");
        });

        // 3) Multiplayer (no opponent info; go straight to TimerMenu)
        multiplayerButton.onClick.AddListener(() =>
        {
            if (canResume)
            {
                // When confirming, hide Quit and show Resume as requested
                quitButton.gameObject.SetActive(false);
                resumeButton.gameObject.SetActive(true);

                mainMenuPanel.SetActive(false);
                confirmDialog.Show(
                    "ARE YOU SURE YOU WANT TO START A NEW GAME?",
                    onYes: () =>
                    {
                        GameManager.Instance.canResume = false;
                        GameManager.Instance.isSinglePlayerMode = false;
                        PlayerPrefs.SetString("MultiplayerSetup", "true");
                        PlayerPrefs.Save();

                        // Go straight to TimerMenu (no PlayerMenu)
                        SceneManager.LoadScene("TimerMenu");
                    },
                    onNo: () =>
                    {
                        // Restore UI
                        mainMenuPanel.SetActive(true);
                        quitButton.gameObject.SetActive(true);
                        resumeButton.gameObject.SetActive(true); // still coming from Pause
                    }
                );
            }
            else
            {
                GameManager.Instance.isSinglePlayerMode = false;
                PlayerPrefs.SetString("MultiplayerSetup", "true");
                PlayerPrefs.Save();
                SceneManager.LoadScene("TimerMenu");
            }
        });

        // 4) Play With Bot (no side selection; start Game directly, timer OFF)
        botButton.onClick.AddListener(() =>
        {
            if (canResume)
            {
                // When confirming, hide Quit and show Resume as requested
                quitButton.gameObject.SetActive(false);
                resumeButton.gameObject.SetActive(true);

                mainMenuPanel.SetActive(false);
                confirmDialog.Show(
                    "ARE YOU SURE YOU WANT TO START A NEW GAME?",
                    onYes: () =>
                    {
                        GameManager.Instance.canResume = false;
                        GameManager.Instance.isSinglePlayerMode = true;

                        // Force NO timer vs bot
                        PlayerPrefs.SetInt("UseTimer", 0);
                        PlayerPrefs.Save();

                        // Go straight into Game scene
                        SceneManager.LoadScene("Game");
                    },
                    onNo: () =>
                    {
                        // Restore UI
                        mainMenuPanel.SetActive(true);
                        quitButton.gameObject.SetActive(true);
                        resumeButton.gameObject.SetActive(true); // still coming from Pause
                    }
                );
            }
            else
            {
                GameManager.Instance.isSinglePlayerMode = true;
                PlayerPrefs.SetInt("UseTimer", 0);
                PlayerPrefs.Save();
                SceneManager.LoadScene("Game");
            }
        });

        // 5) Quit
        quitButton.onClick.AddListener(() => Application.Quit());
    }


}
