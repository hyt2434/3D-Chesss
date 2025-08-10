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
        // (1) prevent stacked listeners when reopening MainMenu
        resumeButton.onClick.RemoveAllListeners();
        multiplayerButton.onClick.RemoveAllListeners();
        botButton.onClick.RemoveAllListeners();
        quitButton.onClick.RemoveAllListeners();

        // 1) Show Resume only if we came via Pause→Home, and hide Quit in that case
        bool canResume = GameManager.Instance.canResume;
        resumeButton.gameObject.SetActive(canResume);
        quitButton.gameObject.SetActive(!canResume);

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
            if (GameManager.Instance.canResume) // (2) use live flag
            {
                mainMenuPanel.SetActive(false);
                confirmDialog.Show(
                    "ARE YOU SURE YOU WANT TO START A NEW GAME?",
                    onYes: () =>
                    {
                        GameManager.Instance.canResume = false;
                        GameManager.Instance.isSinglePlayerMode = false;
                        PlayerPrefs.SetString("MultiplayerSetup", "true");
                        PlayerPrefs.Save();
                        SceneManager.LoadScene("TimerMenu");
                    },
                    onNo: () =>
                    {
                        mainMenuPanel.SetActive(true);
                        quitButton.gameObject.SetActive(false);
                        resumeButton.gameObject.SetActive(true);
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

        // 4) Play With Bot -> ALWAYS go to SideSelectionMenu first
        botButton.onClick.AddListener(() =>
        {
            if (GameManager.Instance.canResume) // (2) use live flag
            {
                mainMenuPanel.SetActive(false);
                confirmDialog.Show(
                    "ARE YOU SURE YOU WANT TO START A NEW GAME?",
                    onYes: () =>
                    {
                        GameManager.Instance.canResume = false;
                        GameManager.Instance.isSinglePlayerMode = true;
                        PlayerPrefs.SetInt("UseTimer", 0);
                        PlayerPrefs.Save();
                        SceneManager.LoadScene("SideSelection"); // (3) correct scene name
                    },
                    onNo: () =>
                    {
                        mainMenuPanel.SetActive(true);
                        quitButton.gameObject.SetActive(false);
                        resumeButton.gameObject.SetActive(true);
                    }
                );
            }
            else
            {
                GameManager.Instance.isSinglePlayerMode = true;
                PlayerPrefs.SetInt("UseTimer", 0);
                PlayerPrefs.Save();
                SceneManager.LoadScene("SideSelection"); // (3) correct scene name
            }
        });

        // 5) Quit
        quitButton.onClick.AddListener(() => Application.Quit());
    }


}
