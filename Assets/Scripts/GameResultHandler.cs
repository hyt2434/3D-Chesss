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
                whitePlayerName = "Player";
                blackPlayerName = "AI";
            }
            else
            {
                whitePlayerName = "Player 1";
                blackPlayerName = "Player 2";
            }
        }
    }

    /// <summary>
    /// Called when a game ends with a winner
    /// </summary>
    public void OnGameEnd(int winnerTeam, bool isCheckmate = false, bool isTimeUp = false)
    {
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
        if (RankingErrorHandler.IsPlayerRankingAvailable())
        {
            PlayerRanking.Instance.AddPlayerScore(winnerName, winnerScore, true);
            PlayerRanking.Instance.AddPlayerScore(loserName, loserScore, false);

            Debug.Log($"Game ended! {winnerName} wins with {winnerScore} points, {loserName} loses with {loserScore} points");
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
            PlayerRanking.Instance.AddPlayerScore(whitePlayerName, drawScore, false);
            PlayerRanking.Instance.AddPlayerScore(blackPlayerName, drawScore, false);

            Debug.Log($"Game ended in draw! Both players get {drawScore} points");
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