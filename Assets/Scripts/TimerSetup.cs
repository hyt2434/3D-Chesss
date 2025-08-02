using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class TimerSetup : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject modePanel;    // Panel with “TIMER” and “WITHOUT TIMER” buttons
    public GameObject customPanel;  // Panel with TotalTimeInput, BonusTimeInput, Back, & Start

    [Header("Inputs")]
    [Tooltip("Enter total time in minutes")]
    public TMP_InputField totalTimeInput;
    [Tooltip("Enter bonus time in seconds")]
    public TMP_InputField bonusTimeInput;

    void Start()
    {
        // If we're in single-player (bot) mode, disable the timer and go straight to Game
        if (GameManager.Instance.isSinglePlayerMode)
        {
            PlayerPrefs.SetInt("UseTimer", 0);
            PlayerPrefs.Save();
            SceneManager.LoadScene("Game");
            return;
        }

        // Otherwise (multiplayer) show the mode panel and hide the custom panel
        modePanel.SetActive(true);
        customPanel.SetActive(false);
    }

    /// <summary>
    /// Called by your “TIMER” button.
    /// </summary>
    public void OnTimerChosen()
    {
        modePanel.SetActive(false);
        customPanel.SetActive(true);
    }

    /// <summary>
    /// Called by your “WITHOUT TIMER” button.
    /// </summary>
    public void OnNoTimerChosen()
    {
        PlayerPrefs.SetInt("UseTimer", 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Game");
    }

    /// <summary>
    /// Called by “Back” in customPanel.
    /// </summary>
    public void OnBack()
    {
        customPanel.SetActive(false);
        modePanel.SetActive(true);
    }

    /// <summary>
    /// Called by “Start” in customPanel.
    /// </summary>
    public void OnStart()
    {
        // parse total minutes → seconds
        if (!int.TryParse(totalTimeInput.text, out int minutes) || minutes < 0)
            minutes = 0;
        int totalSeconds = minutes * 60;

        // parse bonus seconds
        if (!int.TryParse(bonusTimeInput.text, out int bonusSeconds) || bonusSeconds < 0)
            bonusSeconds = 0;

        // save preferences
        PlayerPrefs.SetInt("UseTimer", 1);
        PlayerPrefs.SetInt("GameTimerSeconds", totalSeconds);
        PlayerPrefs.SetInt("BonusSeconds", bonusSeconds);
        PlayerPrefs.Save();

        Time.timeScale = 1f;
        SceneManager.LoadScene("Game");
    }
}
