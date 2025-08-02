using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [Header("Clock UI")]
    public TextMeshProUGUI whiteTimerText;
    public TextMeshProUGUI blackTimerText;

    [Header("Winning Screen")]
    [Tooltip("Drag in the GameObject that has your two victory children")]
    public GameObject winningScreen;

    bool useTimer;
    bool running;
    bool isWhiteTurn = true;
    float whiteTime, blackTime, bonusTime;
    int initialTotal;
    int initialBonus;

    void Start()
    {
        //if we're in "play with bot", shut the timer right off
        if (GameManager.Instance != null && GameManager.Instance.isSinglePlayerMode)
        {
            gameObject.SetActive(false);
            return;
        }
        // 1) Hide winning screen up front
        if (winningScreen != null)
            winningScreen.SetActive(false);

        // 2) Read timer mode
        useTimer = (PlayerPrefs.GetInt("UseTimer", 0) == 1);
        if (!useTimer)
        {
            gameObject.SetActive(false);
            return;
        }

        // 3) Initialize clocks
        initialTotal = PlayerPrefs.GetInt("GameTimerSeconds", 300);
        initialBonus = PlayerPrefs.GetInt("BonusSeconds", 0);
        bonusTime = initialBonus;
        // initialize
        whiteTime = blackTime = initialTotal;
        running = true;
    }

    void Update()
    {
        if (!running) return;

        float dt = Time.deltaTime;
        if (isWhiteTurn) whiteTime = Mathf.Max(0f, whiteTime - dt);
        else blackTime = Mathf.Max(0f, blackTime - dt);

        whiteTimerText.text = Format(whiteTime);
        blackTimerText.text = Format(blackTime);

        // 4) Time-up?
        if (whiteTime <= 0f || blackTime <= 0f)
        {
            running = false;
            bool whiteWins = (blackTime <= 0f);

            // 5) Show the winning screen
            if (winningScreen != null)
            {
                winningScreen.SetActive(true);
                int winnerIndex = whiteWins ? 0 : 1;
                // Assumes child[0] = White-win UI, child[1] = Black-win UI
                winningScreen.transform.GetChild(winnerIndex).gameObject.SetActive(true);
            }

            Debug.Log(whiteWins
                ? "⏰ Black flagged — White wins!"
                : "⏰ White flagged — Black wins!");
        }
    }

    public void SwitchTimer()
    {
        if (!running) return;
        if (isWhiteTurn) whiteTime += bonusTime;
        else blackTime += bonusTime;
        isWhiteTurn = !isWhiteTurn;
    }

    string Format(float t)
    {
        int sec = Mathf.CeilToInt(t);
        return $"{sec / 60:00}:{sec % 60:00}";
    }
    public void ResetTimers()
    {
        if (!useTimer) return;
        whiteTime = initialTotal;
        blackTime = initialTotal;
        bonusTime = initialBonus;
        isWhiteTurn = true;
        running = true;
        // update your UI immediately
        whiteTimerText.text = Format(whiteTime);
        blackTimerText.text = Format(blackTime);
    }

}
