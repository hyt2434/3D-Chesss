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
    
    [Tooltip("Description text")]
    public TMPro.TextMeshProUGUI descriptionText;

    void Start()
    {
        SetupUI();
        SetupButtons();
        
        // Debug info
        Debug.Log("SideSelectionMenu loaded - Player can choose their side");
    }

    void SetupUI()
    {
        if (titleText != null)
            titleText.text = "Choose Your Side";
        
        if (descriptionText != null)
            descriptionText.text = "Select which color you want to play as:";
    }

    void SetupButtons()
    {
        // White button
        if (whiteButton != null)
        {
            whiteButton.onClick.AddListener(() =>
            {
                GameManager.Instance.isPlayerWhite = true;
                GameManager.Instance.isSinglePlayerMode = true;
                StartBotGame();
            });
        }

        // Black button
        if (blackButton != null)
        {
            blackButton.onClick.AddListener(() =>
            {
                GameManager.Instance.isPlayerWhite = false;
                GameManager.Instance.isSinglePlayerMode = true;
                StartBotGame();
            });
        }

        // Back button
        if (backButton != null)
        {
            backButton.onClick.AddListener(() =>
            {
                SceneManager.LoadScene("MainMenu");
            });
        }
    }

    void StartBotGame()
    {
        Debug.Log($"Starting bot game - Player is {(GameManager.Instance.isPlayerWhite ? "White" : "Black")}");
        GameManager.Instance.StartBotGame();
    }


} 