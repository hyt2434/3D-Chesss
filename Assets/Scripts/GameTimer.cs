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

    [Header("Game Result Handler")]
    [Tooltip("Reference to the game result handler")]
    public MonoBehaviour gameResultHandlerRef;

    bool useTimer;
    bool running;
    bool isWhiteTurn = true;
    float whiteTime, blackTime, bonusTime;
    int initialTotal;
    int initialBonus;

    void Start()
    {
        // if we're in "play with bot", shut the timer right off
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
        whiteTime = blackTime = initialTotal;
        running = true;
    }

    void Update()
    {
        // --- NEW: if the winningScreen has been activated by anyone, stop the timer immediately ---
        if (winningScreen != null && winningScreen.activeInHierarchy)
        {
            running = false;
            return;
        }

        if (!running)
            return;

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
                // deactivate all children
                for (int i = 0; i < winningScreen.transform.childCount; i++)
                    winningScreen.transform.GetChild(i).gameObject.SetActive(false);

                // activate the panel + correct child
                winningScreen.SetActive(true);
                int winnerIndex = whiteWins ? 0 : 1;
                if (winnerIndex < winningScreen.transform.childCount)
                    winningScreen.transform.GetChild(winnerIndex).gameObject.SetActive(true);
            }

            // 6) Update player rankings
            if (gameResultHandlerRef != null && gameResultHandlerRef.GetType().Name == "GameResultHandler")
            {
                var onGameEnd = gameResultHandlerRef.GetType().GetMethod("OnGameEnd");
                if (onGameEnd != null)
                    onGameEnd.Invoke(gameResultHandlerRef, new object[] { whiteWins ? 0 : 1, false, true });
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
        whiteTime = blackTime = initialTotal;
        bonusTime = initialBonus;
        isWhiteTurn = true;
        running = true;
        whiteTimerText.text = Format(whiteTime);
        blackTimerText.text = Format(blackTime);
    }
}
