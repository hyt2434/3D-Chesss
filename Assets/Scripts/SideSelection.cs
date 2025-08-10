using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SideSelection : MonoBehaviour
{
    [Tooltip("Button to choose White")]
    public Button whiteButton;

    [Tooltip("Button to choose Black")]
    public Button blackButton;

    [Tooltip("Button to go back")]
    public Button backButton;

    [Header("Text Elements")]
    [Tooltip("Title text for side selection")]
    public TMPro.TextMeshProUGUI titleText;


    void Start()
    {
        SetupUI();
        SetupButtons();
        Debug.Log("SideSelectionMenu loaded - Player can choose their side");
    }

    void SetupUI()
    {
        if (titleText != null)
            titleText.text = "CHOOSE YOUR SIDE";

    }

    void SetupButtons()
    {
        // White button
        if (whiteButton != null)
        {
            whiteButton.onClick.RemoveAllListeners();
            whiteButton.onClick.AddListener(() =>
            {
                if (GameManager.Instance == null) { Debug.LogError("GameManager missing"); return; }

                GameManager.Instance.isPlayerWhite = true;
                GameManager.Instance.isSinglePlayerMode = true;

                // match MainMenuController expectations for a fresh game
                GameManager.Instance.canResume = false;
                PlayerPrefs.SetInt("UseTimer", 0); // no timer vs bot
                PlayerPrefs.Save();

                StartBotGame();
            });
        }

        // Black button
        if (blackButton != null)
        {
            blackButton.onClick.RemoveAllListeners();
            blackButton.onClick.AddListener(() =>
            {
                if (GameManager.Instance == null) { Debug.LogError("GameManager missing"); return; }

                GameManager.Instance.isPlayerWhite = false;
                GameManager.Instance.isSinglePlayerMode = true;

                GameManager.Instance.canResume = false;
                PlayerPrefs.SetInt("UseTimer", 0); // no timer vs bot
                PlayerPrefs.Save();

                StartBotGame();
            });
        }

        // Back button
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() =>
            {
                // return to MainMenu; if you came from Pause, timescale stays 0 so Resume is still visible
                SceneManager.LoadScene("MainMenu");
            });
        }
    }

    void StartBotGame()
    {
        Debug.Log($"Starting bot game - Player is {(GameManager.Instance.isPlayerWhite ? "White" : "Black")}");

        // If we came here from Pause→Home, timeScale may still be 0. Unpause before loading Game.
        if (Time.timeScale == 0f) Time.timeScale = 1f;

        // GameManager handles loading "Game" and (again) forcing single-player mode
        GameManager.Instance.StartBotGame();
    }
}
