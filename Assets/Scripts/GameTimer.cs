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

    void Awake()
    {
        // If playing vs bot, disable timer completely
        if (GameManager.Instance != null && GameManager.Instance.isSinglePlayerMode)
        {
            gameObject.SetActive(false);
            return;
        }

        // Respect TimerMenu setting for multiplayer
        useTimer = (PlayerPrefs.GetInt("UseTimer", 0) == 1);
        if (!useTimer)
        {
            gameObject.SetActive(false);
            return;
        }
    }

    void Start()
    {
        if (!gameObject.activeInHierarchy) return;

        if (winningScreen != null) winningScreen.SetActive(false);

        initialTotal = PlayerPrefs.GetInt("GameTimerSeconds", 300);
        initialBonus = PlayerPrefs.GetInt("BonusSeconds", 0);
        bonusTime = initialBonus;

        whiteTime = blackTime = initialTotal;
        running = true;

        // Initialize UI once
        if (whiteTimerText) whiteTimerText.text = Format(whiteTime);
        if (blackTimerText) blackTimerText.text = Format(blackTime);
    }

    void Update()
    {
        if (!running) return;

        float dt = Time.deltaTime;
        if (isWhiteTurn) whiteTime = Mathf.Max(0f, whiteTime - dt);
        else blackTime = Mathf.Max(0f, blackTime - dt);

        if (whiteTimerText) whiteTimerText.text = Format(whiteTime);
        if (blackTimerText) blackTimerText.text = Format(blackTime);

        // Time up?
        if (whiteTime <= 0f || blackTime <= 0f)
        {
            running = false;
            bool whiteWins = (blackTime <= 0f);

            if (winningScreen != null)
            {
                for (int i = 0; i < winningScreen.transform.childCount; i++)
                    winningScreen.transform.GetChild(i).gameObject.SetActive(false);

                winningScreen.SetActive(true);
                int winnerIndex = whiteWins ? 0 : 1; // 0=WHITE WINS, 1=BLACK WINS
                if (winnerIndex < winningScreen.transform.childCount)
                    winningScreen.transform.GetChild(winnerIndex).gameObject.SetActive(true);
            }

            Debug.Log(whiteWins ? "⏰ Black flagged — White wins!"
                                : "⏰ White flagged — Black wins!");
            
            // Update player rankings for time-based wins
            if (PlayerRanking.Instance != null)
            {
                UpdatePlayerRankingsForTimeWin(whiteWins);
            }
        }
    }

    public void SwitchTimer()
    {
        if (!running) return;
        if (isWhiteTurn) whiteTime += bonusTime;
        else blackTime += bonusTime;
        isWhiteTurn = !isWhiteTurn;
    }

    public void ResetTimers()
    {
        if (!useTimer) return;
        whiteTime = initialTotal;
        blackTime = initialTotal;
        bonusTime = initialBonus;
        isWhiteTurn = true;
        running = true;

        if (whiteTimerText) whiteTimerText.text = Format(whiteTime);
        if (blackTimerText) blackTimerText.text = Format(blackTime);
    }

    string Format(float t)
    {
        int sec = Mathf.CeilToInt(t);
        return $"{sec / 60:00}:{sec % 60:00}";
    }

    /// <summary>
    /// Update player rankings for time-based wins
    /// </summary>
    private void UpdatePlayerRankingsForTimeWin(bool whiteWins)
    {
        string currentPlayerName = PlayerPrefs.GetString("CurrentPlayerName", "");
        int currentPlayerAge = PlayerPrefs.GetInt("CurrentPlayerAge", -1);
        
        if (string.IsNullOrEmpty(currentPlayerName) || currentPlayerAge == -1)
        {
            Debug.LogWarning("No current player data found for time-based ranking update");
            return;
        }

        // Get current player data
        PlayerData currentPlayer = PlayerRanking.Instance.GetPlayerData(currentPlayerName, currentPlayerAge);
        if (currentPlayer == null)
        {
            Debug.LogWarning($"Player data not found for {currentPlayerName} ({currentPlayerAge})");
            return;
        }

        // Determine if current player won (assuming current player is white)
        bool currentPlayerWon = whiteWins;
        
        // Update player stats and ranking
        int rankingChange = currentPlayerWon ? 32 : -32;
        int newRanking = Mathf.Max(0, currentPlayer.rankingPoints + rankingChange);
        
        PlayerRanking.Instance.UpdatePlayerStats(currentPlayerName, currentPlayerAge, currentPlayerWon);
        PlayerRanking.Instance.UpdatePlayerRanking(currentPlayerName, currentPlayerAge, newRanking);
        
        Debug.Log($"Player {currentPlayerName} ranking updated (time win): {currentPlayer.rankingPoints} -> {newRanking} ({rankingChange:+0;-0})");
    }
}
