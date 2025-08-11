using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

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

    [Header("Top Players Display")]
    [Tooltip("Text components to display player names")]
    public TextMeshProUGUI[] playerNameTexts = new TextMeshProUGUI[5];
    [Tooltip("Text components to display player scores/points")]
    public TextMeshProUGUI[] playerScoreTexts = new TextMeshProUGUI[5];
    [Tooltip("Text components to display player stats")]
    public TextMeshProUGUI[] playerStatsTexts = new TextMeshProUGUI[5];
    [Tooltip("Panel containing the top players display")]
    public GameObject topPlayersPanel;



    void Start()
    {
        // Debug: Check if buttons are assigned
        Debug.Log($"MainMenuController Start - Buttons assigned: Resume={resumeButton != null}, Multiplayer={multiplayerButton != null}, Bot={botButton != null}, Quit={quitButton != null}");
        
        // Check EventSystem
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        Debug.Log($"EventSystem found: {eventSystem != null}");
        
        // Check Canvas Graphic Raycaster
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            Debug.Log($"Canvas GraphicRaycaster found: {raycaster != null}");
        }
        
        // Initialize the main menu
        InitializeMainMenu();
    }

    void InitializeMainMenu()
    {
        // Debug: Check for null buttons before adding listeners
        if (resumeButton == null) Debug.LogError("Resume button is null!");
        if (multiplayerButton == null) Debug.LogError("Multiplayer button is null!");
        if (botButton == null) Debug.LogError("Bot button is null!");
        if (quitButton == null) Debug.LogError("Quit button is null!");
        
        // (1) prevent stacked listeners when reopening MainMenu
        if (resumeButton != null) resumeButton.onClick.RemoveAllListeners();
        if (multiplayerButton != null) multiplayerButton.onClick.RemoveAllListeners();
        if (botButton != null) botButton.onClick.RemoveAllListeners();
        if (quitButton != null) quitButton.onClick.RemoveAllListeners();

        // Update top players display
        UpdateTopPlayersDisplay();

        // 1) Show Resume only if we came via Pause→Home, and hide Quit in that case
        bool canResume = GameManager.Instance.canResume;
        if (resumeButton != null) resumeButton.gameObject.SetActive(canResume);
        if (quitButton != null) quitButton.gameObject.SetActive(!canResume);

        // 2) Resume → unpause & unload MainMenu
        if (resumeButton != null) resumeButton.onClick.AddListener(() =>
        {
            GameManager.Instance.canResume = false;
            Time.timeScale = 1f;
            SceneManager.UnloadSceneAsync("MainMenu");
        });

        // 3) Multiplayer (no opponent info; go straight to TimerMenu)
        if (multiplayerButton != null) multiplayerButton.onClick.AddListener(() =>
        {
            Debug.Log("Multiplayer button clicked!");
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
        if (botButton != null) botButton.onClick.AddListener(() =>
        {
            Debug.Log("Bot button clicked!");
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
        if (quitButton != null) quitButton.onClick.AddListener(() => 
        {
            Debug.Log("Quit button clicked!");
            Application.Quit();
        });
        
        // Debug: Check button interactability
        Debug.Log($"Button interactability - Resume: {resumeButton?.interactable}, Multiplayer: {multiplayerButton?.interactable}, Bot: {botButton?.interactable}, Quit: {quitButton?.interactable}");
        
        // Test button raycast targets
        if (multiplayerButton != null)
        {
            Image buttonImage = multiplayerButton.GetComponent<Image>();
            Debug.Log($"Multiplayer button Image Raycast Target: {buttonImage?.raycastTarget}");
        }
    }
    
    // Test method - call this from console or inspector
    [ContextMenu("Test Multiplayer Button")]
    public void TestMultiplayerButton()
    {
        Debug.Log("Testing multiplayer button programmatically...");
        if (multiplayerButton != null)
        {
            multiplayerButton.onClick.Invoke();
        }
    }
    
    [ContextMenu("Force Refresh Buttons")]
    public void ForceRefreshButtons()
    {
        Debug.Log("Forcing button refresh...");
        
        // Force refresh button states
        if (multiplayerButton != null)
        {
            multiplayerButton.interactable = false;
            multiplayerButton.interactable = true;
        }
        if (botButton != null)
        {
            botButton.interactable = false;
            botButton.interactable = true;
        }
        if (quitButton != null)
        {
            quitButton.interactable = false;
            quitButton.interactable = true;
        }
        
        Debug.Log("Button refresh complete");
    }
    
    [ContextMenu("Create Test Button")]
    public void CreateTestButton()
    {
        Debug.Log("Creating test button...");
        
        // Create a simple test button
        GameObject testButton = new GameObject("TestButton");
        testButton.transform.SetParent(transform);
        
        // Add required components
        RectTransform rectTransform = testButton.AddComponent<RectTransform>();
        Image image = testButton.AddComponent<Image>();
        Button button = testButton.AddComponent<Button>();
        
        // Set position
        rectTransform.anchoredPosition = new Vector2(0, 200);
        rectTransform.sizeDelta = new Vector2(200, 50);
        
        // Add click listener
        button.onClick.AddListener(() => Debug.Log("Test button clicked!"));
        
        Debug.Log("Test button created!");
    }
    
    // Add Update method to check for mouse clicks
    void Update()
    {
        // Check for mouse clicks on buttons
        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            Vector2 mousePosition = Input.mousePosition;
            Debug.Log($"Mouse clicked at position: {mousePosition}");
            
            // Check if click is within button bounds
            if (multiplayerButton != null)
            {
                RectTransform buttonRect = multiplayerButton.GetComponent<RectTransform>();
                if (RectTransformUtility.RectangleContainsScreenPoint(buttonRect, mousePosition))
                {
                    Debug.Log("Mouse click detected within multiplayer button bounds!");
                }
            }
        }
    }

    /// <summary>
    /// Update the top 5 players display
    /// </summary>
    private void UpdateTopPlayersDisplay()
    {
        if (PlayerRanking.Instance == null)
        {
            Debug.LogWarning("PlayerRanking instance not found!");
            return;
        }

        List<PlayerData> topPlayers = PlayerRanking.Instance.GetTopPlayers(5);

        // Clear all text fields first
        for (int i = 0; i < playerNameTexts.Length; i++)
        {
            if (playerNameTexts[i] != null)
            {
                playerNameTexts[i].text = "";
            }
            if (playerScoreTexts[i] != null)
            {
                playerScoreTexts[i].text = "";
            }
            if (playerStatsTexts[i] != null)
            {
                playerStatsTexts[i].text = "";
            }
        }

        // Display top players
        for (int i = 0; i < topPlayers.Count && i < playerNameTexts.Length; i++)
        {
            PlayerData player = topPlayers[i];
            
            // Set player name (with rank and age)
            if (playerNameTexts[i] != null)
            {
                playerNameTexts[i].text = $"{i + 1}. {player.name} ({player.age})";
            }
            
            // Set player score/points
            if (playerScoreTexts[i] != null)
            {
                playerScoreTexts[i].text = $"{player.rankingPoints} pts";
            }
            
            // Set player stats (you can customize this based on what stats you want to show)
            if (playerStatsTexts[i] != null)
            {
                playerStatsTexts[i].text = $"Games: {player.gamesPlayed} | Wins: {player.gamesWon}";
            }
        }

        // Show "No players yet" message if no players exist
        if (topPlayers.Count == 0)
        {
            if (playerNameTexts[0] != null)
            {
                playerNameTexts[0].text = "No players yet. Create a new player to get started!";
            }
            if (playerScoreTexts[0] != null)
            {
                playerScoreTexts[0].text = "";
            }
            if (playerStatsTexts[0] != null)
            {
                playerStatsTexts[0].text = "";
            }
        }
    }
}
