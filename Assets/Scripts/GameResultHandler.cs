using UnityEngine;

public class GameResultHandler : MonoBehaviour
{
    [Header("Player Names")]
    [Tooltip("Name for the white player")]
    public string whitePlayerName = "White";
    [Tooltip("Name for the black player")]
    public string blackPlayerName = "Black";

    [Header("Score Settings")]
    [Tooltip("Base score for winning a game")]
    public int winScore = 100;
    [Tooltip("Base score for losing a game")]
    public int loseScore = 10;
    [Tooltip("Bonus score for checkmate")]
    public int checkmateBonus = 50;
    [Tooltip("Bonus score for capturing pieces")]
    public int captureBonus = 5;

    private bool gameEnded = false;

    void Start()
    {
        // Set default player names based on game mode
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.isSinglePlayerMode)
            {
                // Get the actual player name from PlayerPrefs
                string actualPlayerName = GetCurrentPlayerName();
                whitePlayerName = !string.IsNullOrEmpty(actualPlayerName) ? actualPlayerName : "Player";
                blackPlayerName = "AI";
            }
            else
            {
                // In multiplayer mode, use first player (white) and second player (black)
                string firstPlayerName = GetCurrentPlayerName();
                string secondPlayerName = PlayerPrefs.GetString("SecondPlayerName", "");
                
                whitePlayerName = !string.IsNullOrEmpty(firstPlayerName) ? firstPlayerName : "Player 1";
                blackPlayerName = !string.IsNullOrEmpty(secondPlayerName) ? secondPlayerName : "Player 2";
            }
        }
    }

    /// <summary>
    /// Get the current player's name from PlayerPrefs
    /// This assumes the last player to log in is the current player
    /// </summary>
    private string GetCurrentPlayerName()
    {
        // For now, we'll use a simple approach - store the current player name when they log in
        return PlayerPrefs.GetString("CurrentPlayerName", "");
    }

    /// <summary>
    /// Called when a game ends with a winner
    /// </summary>
    public void OnGameEnd(int winnerTeam, bool isCheckmate = false, bool isTimeUp = false)
    {
        Debug.LogError($"[GAME DEBUG] OnGameEnd called! winnerTeam: {winnerTeam}, checkmate: {isCheckmate}, timeUp: {isTimeUp}");
        if (gameEnded) return; // Prevent multiple calls
        gameEnded = true;

        string winnerName = winnerTeam == 0 ? whitePlayerName : blackPlayerName;
        string loserName = winnerTeam == 0 ? blackPlayerName : whitePlayerName;

        int winnerScore = winScore;
        int loserScore = loseScore;

        // Add bonuses
        if (isCheckmate)
        {
            winnerScore += checkmateBonus;
        }

        // Update player rankings
        Debug.LogError($"[GAME DEBUG] Checking if PlayerRanking is available...");
        if (RankingErrorHandler.IsPlayerRankingAvailable())
        {
            Debug.LogError($"[GAME DEBUG] PlayerRanking is available! Adding scores...");
            
            if (GameManager.Instance.isSinglePlayerMode)
            {
                // Single player mode: Add scores for human players, not AI
                if (winnerName != "AI")
                {
                    Debug.LogError($"[GAME DEBUG] Adding score for winner: {winnerName} = {winnerScore}");
                    PlayerRanking.Instance.AddPlayerScore(winnerName, winnerScore, true);
                }
                if (loserName != "AI")
                {
                    Debug.LogError($"[GAME DEBUG] Adding score for loser: {loserName} = {loserScore}");
                    PlayerRanking.Instance.AddPlayerScore(loserName, loserScore, false);
                }
            }
            else
            {
                // Multiplayer mode: Give points to both players (different people)
                Debug.LogError($"[GAME DEBUG] Multiplayer mode - adding points for winner: {winnerName} = {winnerScore}");
                PlayerRanking.Instance.AddPlayerScore(winnerName, winnerScore, true);
                Debug.LogError($"[GAME DEBUG] Multiplayer mode - adding points for loser: {loserName} = {loserScore}");
                PlayerRanking.Instance.AddPlayerScore(loserName, loserScore, false);
            }
        }
        else
        {
            Debug.LogError($"[GAME DEBUG] PlayerRanking is NOT available!");
        }

        Debug.Log($"Game ended! Winner: {winnerName} (team {winnerTeam}) wins with {winnerScore} points, Loser: {loserName} loses with {loserScore} points");
        Debug.Log($"Player is white: {whitePlayerName}, Player is black: {blackPlayerName}");
        
        // Clear second player data after multiplayer game ends
        if (!GameManager.Instance.isSinglePlayerMode)
        {
            PlayerPrefs.DeleteKey("SecondPlayerName");
            PlayerPrefs.DeleteKey("SecondPlayerAge");
            PlayerPrefs.Save();
            Debug.Log("Cleared second player data after multiplayer game");
        }
        
        // force the on-screen top-5 to refresh
        var display = Object.FindObjectOfType<RankingDisplay>();
        if (display != null)
            display.RefreshRanking();
    }

    /// <summary>
    /// Called when a game ends in a draw
    /// </summary>
    public void OnGameDraw()
    {
        if (gameEnded) return; // Prevent multiple calls
        gameEnded = true;

        int drawScore = (winScore + loseScore) / 2; // Half points for draw

        // Update player rankings
        if (RankingErrorHandler.IsPlayerRankingAvailable())
        {
            if (GameManager.Instance.isSinglePlayerMode)
            {
                // Single player mode: Add scores for human players, not AI
                if (whitePlayerName != "AI")
                {
                    PlayerRanking.Instance.AddPlayerScore(whitePlayerName, drawScore, false);
                }
                if (blackPlayerName != "AI")
                {
                    PlayerRanking.Instance.AddPlayerScore(blackPlayerName, drawScore, false);
                }
                Debug.Log($"Game ended in draw! Both players get {drawScore} points");
            }
            else
            {
                // Multiplayer mode: Give draw points to both players (different people)
                PlayerRanking.Instance.AddPlayerScore(whitePlayerName, drawScore, false);
                PlayerRanking.Instance.AddPlayerScore(blackPlayerName, drawScore, false);
                Debug.Log($"Game ended in draw! Both players get {drawScore} points");
            }
        }
        
        // Clear second player data after multiplayer game ends
        if (!GameManager.Instance.isSinglePlayerMode)
        {
            PlayerPrefs.DeleteKey("SecondPlayerName");
            PlayerPrefs.DeleteKey("SecondPlayerAge");
            PlayerPrefs.Save();
            Debug.Log("Cleared second player data after multiplayer draw");
        }
        
        // **NEW** force the on-screen top-5 to refresh
        var display = Object.FindObjectOfType<RankingDisplay>();
        if (display != null)
            display.RefreshRanking();
    }

    /// <summary>
    /// Reset the game ended flag (called when starting a new game)
    /// </summary>
    public void ResetGameEnded()
    {
        gameEnded = false;
    }

    /// <summary>
    /// Set custom player names
    /// </summary>
    public void SetPlayerNames(string whiteName, string blackName)
    {
        whitePlayerName = whiteName;
        blackPlayerName = blackName;
    }
} 