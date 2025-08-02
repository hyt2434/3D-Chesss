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
        // 1) Should we show “Resume”? (only if we came via Pause→Home)
        bool canResume = GameManager.Instance.canResume;
        resumeButton.gameObject.SetActive(canResume);

        // 2) Resume simply unpauses & unloads this menu
        resumeButton.onClick.AddListener(() =>
        {
            GameManager.Instance.canResume = false;
            Time.timeScale = 1f;
            SceneManager.UnloadSceneAsync("MainMenu");
        });

        // 3) Multiplayer path always goes through TimerMenu
        multiplayerButton.onClick.AddListener(() =>
        {
            if (canResume)
            {
                // hide main menu while we ask
                mainMenuPanel.SetActive(false);

                confirmDialog.Show(
                    "ARE YOU SURE YOU WANT TO START A NEW GAME?",
                    onYes: () =>
                    {
                        // clear the resume flag
                        GameManager.Instance.canResume = false;
                        // make sure we're no longer in single-player/bot mode
                        GameManager.Instance.isSinglePlayerMode = false;
                        // unload this menu
                        SceneManager.UnloadSceneAsync("MainMenu");
                        // now pick timer settings
                        SceneManager.LoadScene("TimerMenu");
                    },
                    onNo: () =>
                    {
                        // user canceled → show main menu again
                        mainMenuPanel.SetActive(true);
                    }
                );
            }
            else
            {
                // direct start → ensure multiplayer mode, then timer
                GameManager.Instance.isSinglePlayerMode = false;
                SceneManager.LoadScene("TimerMenu");
            }
        });

        // 4) Play With Bot stays the same
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
                        SceneManager.UnloadSceneAsync("MainMenu");
                        GameManager.Instance.StartBotGame();
                    },
                    onNo: () =>
                    {
                        mainMenuPanel.SetActive(true);
                    }
                );
            }
            else
            {
                GameManager.Instance.isSinglePlayerMode = true;
                GameManager.Instance.StartBotGame();
            }
        });

        // 5) Quit the application
        quitButton.onClick.AddListener(() =>
        {
            Application.Quit();
        });
    }
}
