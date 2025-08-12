using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class TimerSetup : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject modePanel;    // Panel with “TIMER” and “WITHOUT TIMER”
    public GameObject customPanel;  // Panel with TotalTimeInput, BonusTimeInput, Back, & Start

    [Header("Inputs")]
    [Tooltip("Enter total time in minutes")]
    public TMP_InputField totalTimeInput;
    [Tooltip("Enter bonus time in seconds")]
    public TMP_InputField bonusTimeInput;
    
    [Header("Multiplayer Info")]
    [Tooltip("Text to display opponent information")]
    public TextMeshProUGUI opponentInfoText;

    void Start()
    {
        // Display opponent information if in multiplayer mode
        if (PlayerPrefs.GetString("MultiplayerSetup", "") == "true" && opponentInfoText != null)
        {
            string opponentName = PlayerPrefs.GetString("OpponentName", "");
            int opponentAge = PlayerPrefs.GetInt("OpponentAge", 0);
            string currentPlayerName = PlayerPrefs.GetString("CurrentPlayerName", "");
            
            if (!string.IsNullOrEmpty(opponentName) && opponentAge > 0)
            {
                opponentInfoText.text = $"Player: {currentPlayerName} vs Opponent: {opponentName} ({opponentAge})";
            }
            else
            {
                opponentInfoText.text = "Multiplayer Game";
            }
        }
    }

    // Called by your "TIMER" button
    public void OnTimerChosen()
    {
        modePanel.SetActive(false);
        customPanel.SetActive(true);
    }

    // Called by your “WITHOUT TIMER” button
    public void OnNoTimerChosen()
    {
        PlayerPrefs.SetInt("UseTimer", 0);
        PlayerPrefs.Save();
        
        // Clear multiplayer setup flag
        PlayerPrefs.DeleteKey("MultiplayerSetup");
        
        SceneManager.LoadScene("Game");
    }

    // Called by “Back” in customPanel
    public void OnBack()
    {
        customPanel.SetActive(false);
        modePanel.SetActive(true);
    }
    
    // Called by "Back" button in modePanel (if it exists)
    public void OnBackToMain()
    {
        // If in multiplayer mode, go back to PlayerMenu for opponent selection
        if (PlayerPrefs.GetString("MultiplayerSetup", "") == "true")
        {
            PlayerPrefs.SetString("MultiplayerMode", "opponent_selection");
            SceneManager.LoadScene("PlayerMenu");
        }
        else
        {
            // Normal mode, go back to MainMenu
            SceneManager.LoadScene("MainMenu");
        }
    }

    // Called by “Start” in customPanel
    public void OnStart()
    {
        // parse total minutes → seconds
        if (!int.TryParse(totalTimeInput.text, out int minutes) || minutes < 0)
            minutes = 0;
        int totalSeconds = minutes * 60;

        // parse bonus seconds
        if (!int.TryParse(bonusTimeInput.text, out int bonusSeconds) || bonusSeconds < 0)
            bonusSeconds = 0;

        // save
        PlayerPrefs.SetInt("UseTimer", 1);
        PlayerPrefs.SetInt("GameTimerSeconds", totalSeconds);
        PlayerPrefs.SetInt("BonusSeconds", bonusSeconds);
        PlayerPrefs.Save();
        
        // Clear multiplayer setup flag
        PlayerPrefs.DeleteKey("MultiplayerSetup");

        // go to Game scene
        SceneManager.LoadScene("Game");
    }
}
