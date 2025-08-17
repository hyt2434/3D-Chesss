using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject selectionPanel;
    public GameObject inputPanel;

    [Header("Inputs & Message")]
    public TMP_InputField nameInput;
    public TMP_InputField ageInput;
    public TextMeshProUGUI errorText;

    [Header("Buttons")]
    public GameObject saveButton; // for New Player
    public GameObject playButton; // for Existing Player

    // track mode: true=new player, false=existing
    private bool isNewMode;
    
    // track if we're in multiplayer opponent selection mode
    private bool isMultiplayerOpponentSelection;

    void Start()
    {
        // Check if we're in multiplayer opponent selection mode
        isMultiplayerOpponentSelection = PlayerPrefs.GetString("MultiplayerMode", "") == "opponent_selection";
        
        if (isMultiplayerOpponentSelection)
        {
            // In multiplayer mode, we're selecting opponent
            // Show different UI text and behavior
            SetupMultiplayerOpponentSelection();
        }
        else
        {
            // Normal player selection mode
            SetupNormalPlayerSelection();
        }
    }
    
    private void SetupNormalPlayerSelection()
    {
        selectionPanel.SetActive(true);
        inputPanel.SetActive(false);
        errorText.text = "";

        saveButton.SetActive(false);
        playButton.SetActive(false);
    }
    
    private void SetupMultiplayerOpponentSelection()
    {
        // Use the exact same UI structure as normal player selection
        // Just update the text to indicate we're selecting an opponent
        selectionPanel.SetActive(true);
        inputPanel.SetActive(false);
        errorText.text = "";

        saveButton.SetActive(false);
        playButton.SetActive(false);
        
        // Update the selection panel text to show opponent context
        UpdateSelectionPanelForMultiplayer();
    }
    
    private void UpdateSelectionPanelForMultiplayer()
    {
        // Find and update the text for the selection buttons
        // Keep the same button structure, just change the text and size
        var selectionButtons = selectionPanel.GetComponentsInChildren<TextMeshProUGUI>();
        
        foreach (var text in selectionButtons)
        {
            if (text.text.Contains("NEW PLAYER") || text.text.Contains("New Player"))
            {
                text.text = "NEW OPPONENT";
                // Make text smaller for multiplayer mode
                text.fontSize = text.fontSize * 0.8f;
            }
            else if (text.text.Contains("EXISTING PLAYER") || text.text.Contains("Existing Player"))
            {
                text.text = "EXISTING OPPONENT";
                // Make text smaller for multiplayer mode
                text.fontSize = text.fontSize * 0.8f;
            }
        }
    }

    public void OnNewPlayer()
    {
        isNewMode = true;
        selectionPanel.SetActive(false);
        inputPanel.SetActive(true);
        errorText.text = "";

        if (isMultiplayerOpponentSelection)
        {
            // In multiplayer mode, creating new opponent
            saveButton.SetActive(true);
            playButton.SetActive(false);
            
            // Update input panel text for opponent creation
            UpdateInputPanelForOpponent();
        }
        else
        {
            // Normal mode, creating new player
            saveButton.SetActive(true);
            playButton.SetActive(false);
        }
    }

    public void OnOldPlayer()
    {
        isNewMode = false;
        selectionPanel.SetActive(false);
        inputPanel.SetActive(true);
        errorText.text = "";

        if (isMultiplayerOpponentSelection)
        {
            // In multiplayer mode, selecting existing opponent
            saveButton.SetActive(false);
            playButton.SetActive(true);
            
            // Update input panel text for opponent selection
            UpdateInputPanelForOpponent();
        }
        else
        {
            // Normal mode, selecting existing player
            saveButton.SetActive(false);
            playButton.SetActive(true);
        }
    }
    
    private void UpdateInputPanelForOpponent()
    {
        // Update UI text to indicate opponent selection
        if (nameInput != null)
        {
            nameInput.placeholder.GetComponent<TextMeshProUGUI>().text = "OPPONENT NAME";
        }
        if (ageInput != null)
        {
            ageInput.placeholder.GetComponent<TextMeshProUGUI>().text = "OPPONENT AGE";
        }
        
        // Update button text for multiplayer mode - keep it simple
        if (saveButton != null && saveButton.activeInHierarchy)
        {
            TextMeshProUGUI saveButtonText = saveButton.GetComponentInChildren<TextMeshProUGUI>();
            if (saveButtonText != null)
            {
                saveButtonText.text = "SAVE";
            }
        }
        
        if (playButton != null && playButton.activeInHierarchy)
        {
            TextMeshProUGUI playButtonText = playButton.GetComponentInChildren<TextMeshProUGUI>();
            if (playButtonText != null)
            {
                playButtonText.text = "PLAY";
            }
        }
    }

    public void OnSave()
    {
        string name = nameInput.text.Trim();
        if (!int.TryParse(ageInput.text, out int age) || string.IsNullOrEmpty(name))
        {
            errorText.text = "Enter a valid name and age.";
            return;
        }

        if (isMultiplayerOpponentSelection)
        {
            // In multiplayer mode, we're creating a new opponent
            HandleOpponentCreation(name, age);
        }
        else
        {
            // Normal mode, creating a new player
            HandlePlayerCreation(name, age);
        }
    }
    
    private void HandlePlayerCreation(string name, int age)
    {
        // Check if player already exists using PlayerRanking system
        if (PlayerRanking.Instance.PlayerExists(name, age))
        {
            errorText.text = "Player already exists.";
            return;
        }

        // Add new player with default 1000 ranking points
        PlayerRanking.Instance.AddNewPlayer(name, age);
        PlayerPrefs.SetString("CurrentPlayerName", name);
        PlayerPrefs.SetInt("CurrentPlayerAge", age);
        PlayerPrefs.Save();

        SceneManager.LoadScene("MainMenu");
    }
    
    private void HandleOpponentCreation(string name, int age)
    {
        // Check if opponent already exists using PlayerRanking system
        if (PlayerRanking.Instance.PlayerExists(name, age))
        {
            errorText.text = "Opponent already exists.";
            return;
        }

        // Add new opponent with default 1000 ranking points
        PlayerRanking.Instance.AddNewPlayer(name, age);
        
        // Store opponent info for use in game
        PlayerPrefs.SetString("OpponentName", name);
        PlayerPrefs.SetInt("OpponentAge", age);
        PlayerPrefs.Save();
        
        // Clear multiplayer mode flag and go to timer menu
        PlayerPrefs.DeleteKey("MultiplayerMode");
        SceneManager.LoadScene("TimerMenu");
    }

    public void OnPlay()
    {
        string name = nameInput.text.Trim();
        if (!int.TryParse(ageInput.text, out int age) || string.IsNullOrEmpty(name))
        {
            errorText.text = "Enter a valid name and age.";
            return;
        }

        if (isMultiplayerOpponentSelection)
        {
            // In multiplayer mode, we're selecting opponent
            HandleOpponentSelection(name, age);
        }
        else
        {
            // Normal player selection mode
            HandlePlayerSelection(name, age);
        }
    }
    
    private void HandlePlayerSelection(string name, int age)
    {
        // Check if player exists using PlayerRanking system
        if (PlayerRanking.Instance.PlayerExists(name, age))
        {
            // Store current player info for use in game
            PlayerPrefs.SetString("CurrentPlayerName", name);
            PlayerPrefs.SetInt("CurrentPlayerAge", age);
            PlayerPrefs.Save();
            
            SceneManager.LoadScene("MainMenu");
            return;
        }

        errorText.text = "No matching player found.";
    }
    
    private void HandleOpponentSelection(string name, int age)
    {
        // Check if opponent exists using PlayerRanking system
        if (PlayerRanking.Instance.PlayerExists(name, age))
        {
            // Store opponent info for use in game
            PlayerPrefs.SetString("OpponentName", name);
            PlayerPrefs.SetInt("OpponentAge", age);
            PlayerPrefs.Save();
            
            // Clear multiplayer mode flag and go to timer menu
            PlayerPrefs.DeleteKey("MultiplayerMode");
            SceneManager.LoadScene("TimerMenu");
            return;
        }

        errorText.text = "No matching opponent found.";
    }

    public void OnBackToSelection()
    {
        if (isMultiplayerOpponentSelection)
        {
            // In multiplayer mode, go back to main menu
            PlayerPrefs.DeleteKey("MultiplayerMode");
            SceneManager.LoadScene("MainMenu");
        }
        else
        {
            // Normal mode, go back to selection panel
            inputPanel.SetActive(false);
            selectionPanel.SetActive(true);
            errorText.text = "";
            nameInput.text = "";
            ageInput.text = "";

            saveButton.SetActive(false);
            playButton.SetActive(false);
        }
    }
    public void OnQuitGame()
    {
        Debug.LogWarning("Quit game");
        // Quit the application
        Application.Quit();

    }
}
