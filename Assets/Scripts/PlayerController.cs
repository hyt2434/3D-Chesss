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

    void Start()
    {
        selectionPanel.SetActive(true);
        inputPanel.SetActive(false);
        errorText.text = "";

        saveButton.SetActive(false);
        playButton.SetActive(false);
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
