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



    void Start()
    {
        selectionPanel.SetActive(true);
        inputPanel.SetActive(false);
        errorText.text = "";

        saveButton.SetActive(false);
        playButton.SetActive(false);
        
        // Show current player info if in multiplayer setup mode
        string multiplayerSetup = PlayerPrefs.GetString("MultiplayerSetup", "");
        if (multiplayerSetup == "true")
        {
            string firstPlayerName = PlayerPrefs.GetString("CurrentPlayerName", "");
            if (!string.IsNullOrEmpty(firstPlayerName))
            {
                errorText.text = $"Player 1: {firstPlayerName} is already logged in. Please login as Player 2.";
            }
        }
    }

    public void OnNewPlayer()
    {
        selectionPanel.SetActive(false);
        inputPanel.SetActive(true);
        errorText.text = "";

        saveButton.SetActive(true);
        playButton.SetActive(false);
    }

    public void OnOldPlayer()
    {
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

        int count = PlayerPrefs.GetInt("PlayerCount", 0);
        // check duplicate
        for (int i = 0; i < count; i++)
        {
            if (PlayerPrefs.GetString($"Player_{i}_Name", "") == name &&
                PlayerPrefs.GetInt($"Player_{i}_Age", -1) == age)
            {
                errorText.text = "Player already exists.";
                return;
            }
        }

        // save new player
        PlayerPrefs.SetString($"Player_{count}_Name", name);
        PlayerPrefs.SetInt($"Player_{count}_Age", age);
        PlayerPrefs.SetInt("PlayerCount", count + 1);
        
        // Check if we're setting up multiplayer first
        string multiplayerSetup = PlayerPrefs.GetString("MultiplayerSetup", "");
        if (multiplayerSetup == "true")
        {
            // Check if second player is the same as first player
            string firstPlayerName = PlayerPrefs.GetString("CurrentPlayerName", "");
            if (name == firstPlayerName)
            {
                errorText.text = "You cannot play against yourself! Please use a different player account.";
                return;
            }
            
            // Store second player name and age, but DON'T override CurrentPlayerName
            PlayerPrefs.SetString("SecondPlayerName", name);
            PlayerPrefs.SetInt("SecondPlayerAge", age);
            PlayerPrefs.SetString("NewPlayerNeedsPoints", name);
            PlayerPrefs.DeleteKey("MultiplayerSetup");
            PlayerPrefs.Save();
            Debug.Log($"Second player {name} (age {age}) added for multiplayer");
            SceneManager.LoadScene("TimerMenu");
        }
        else
        {
            // Normal single player mode - store as current player
            PlayerPrefs.SetString("CurrentPlayerName", name);
            PlayerPrefs.SetString("NewPlayerNeedsPoints", name);
            PlayerPrefs.Save();
            Debug.Log($"New player {name} marked to receive 1000 starting points when ranking system is available");
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void OnPlay()

    {
        Debug.Log("OnPlay() called!");
        string name = nameInput.text.Trim();
        if (!int.TryParse(ageInput.text, out int age) || string.IsNullOrEmpty(name))
        {
            errorText.text = "Enter a valid name and age.";
            return;
        }

        int count = PlayerPrefs.GetInt("PlayerCount", 0);
        for (int i = 0; i < count; i++)
        {
            if (PlayerPrefs.GetString($"Player_{i}_Name", "") == name &&
                PlayerPrefs.GetInt($"Player_{i}_Age", -1) == age)
            {
                // Check if we're setting up multiplayer
                string multiplayerSetup = PlayerPrefs.GetString("MultiplayerSetup", "");
                if (multiplayerSetup == "true")
                {
                    // Check if second player is the same as first player
                    string firstPlayerName = PlayerPrefs.GetString("CurrentPlayerName", "");
                    if (name == firstPlayerName)
                    {
                        errorText.text = "You cannot play against yourself! Please use a different player account.";
                        return;
                    }
                    
                    // Store second player name and age, but DON'T override CurrentPlayerName
                    PlayerPrefs.SetString("SecondPlayerName", name);
                    PlayerPrefs.SetInt("SecondPlayerAge", age);
                    PlayerPrefs.DeleteKey("MultiplayerSetup");
                    PlayerPrefs.Save();
                    Debug.Log($"Second player {name} (age {age}) logged in for multiplayer");
                    SceneManager.LoadScene("TimerMenu");
                }
                else
                {
                    // found existing - store as current player (only for normal login)
                    PlayerPrefs.SetString("CurrentPlayerName", name);
                    PlayerPrefs.Save();
                    SceneManager.LoadScene("MainMenu");
                }
                return;
            }
        }

        errorText.text = "No matching player found.";
    }

    public void OnBackToSelection()
    {
        inputPanel.SetActive(false);
        selectionPanel.SetActive(true);
        errorText.text = "";
        nameInput.text = "";
        ageInput.text = "";

        saveButton.SetActive(false);
        playButton.SetActive(false);
    }
}
