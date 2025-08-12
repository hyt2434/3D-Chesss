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
        // Hide selection panel, show input panel directly for opponent selection
        selectionPanel.SetActive(false);
        inputPanel.SetActive(true);
        errorText.text = "";

        // Show only play button (for selecting opponent)
        saveButton.SetActive(false);
        playButton.SetActive(true);
        
        // Update UI text to indicate opponent selection
        if (nameInput != null)
        {
            nameInput.placeholder.GetComponent<TextMeshProUGUI>().text = "OPPONENT NAME";
        }
        if (ageInput != null)
        {
            ageInput.placeholder.GetComponent<TextMeshProUGUI>().text = "OPPONENT AGE";
        }
    }

    public void OnNewPlayer()
    {
        isNewMode = true;
        selectionPanel.SetActive(false);
        inputPanel.SetActive(true);
        errorText.text = "";

        saveButton.SetActive(true);
        playButton.SetActive(false);
    }

    public void OnOldPlayer()
    {
        isNewMode = false;
        selectionPanel.SetActive(false);
        inputPanel.SetActive(true);
        errorText.text = "";

        saveButton.SetActive(false);
        playButton.SetActive(true);
    }

    public void OnSave()
    {
        string name = nameInput.text.Trim();
        if (!int.TryParse(ageInput.text, out int age) || string.IsNullOrEmpty(name))
        {
            errorText.text = "Enter a valid name and age.";
            return;
        }

        // Check if player already exists using PlayerRanking system
        if (PlayerRanking.Instance.PlayerExists(name, age))
        {
            errorText.text = "Player already exists.";
            return;
        }

        // Add new player with default 1000 ranking points
        PlayerRanking.Instance.AddNewPlayer(name, age);

        SceneManager.LoadScene("MainMenu");
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
}
