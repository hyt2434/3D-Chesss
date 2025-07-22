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

    // Called by your “TIMER” button
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
        SceneManager.LoadScene("Game");
    }

    // Called by “Back” in customPanel
    public void OnBack()
    {
        customPanel.SetActive(false);
        modePanel.SetActive(true);
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

        // go to Game scene
        SceneManager.LoadScene("Game");
    }
}
