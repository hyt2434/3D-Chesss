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
        PlayerPrefs.Save();

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

        int count = PlayerPrefs.GetInt("PlayerCount", 0);
        for (int i = 0; i < count; i++)
        {
            if (PlayerPrefs.GetString($"Player_{i}_Name", "") == name &&
                PlayerPrefs.GetInt($"Player_{i}_Age", -1) == age)
            {
                // found existing
                SceneManager.LoadScene("MainMenu");
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
