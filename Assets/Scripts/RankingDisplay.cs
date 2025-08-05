using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RankingDisplay : MonoBehaviour
{
    [Header("Ranking UI")]
    [Tooltip("Parent GameObject that contains all ranking elements")]
    public GameObject rankingPanel;
    
    [Tooltip("Text component for the ranking title")]
    public TextMeshProUGUI rankingTitleText;
    
    [Header("Player Rank Slots")]
    [Tooltip("Array of ranking slot GameObjects (should be 5)")]
    public GameObject[] rankingSlots;
    
    [Tooltip("Text components for player names (should be 5)")]
    public TextMeshProUGUI[] playerNameTexts;
    
    [Tooltip("Text components for player scores (should be 5)")]
    public TextMeshProUGUI[] playerScoreTexts;
    
    [Tooltip("Text components for player stats (should be 5)")]
    public TextMeshProUGUI[] playerStatsTexts;

    private void Start()
    {
        UpdateRankingDisplay();
    }

    private void OnEnable()
    {
        UpdateRankingDisplay();
    }

    /// <summary>
    /// Update the ranking display with current top players
    /// </summary>
    public void UpdateRankingDisplay()
    {
        if (!RankingErrorHandler.IsPlayerRankingAvailable())
        {
            return;
        }

        if (!RankingErrorHandler.ValidateRankingDisplay(this))
        {
            return;
        }

        List<PlayerScore> topPlayers = PlayerRanking.Instance.GetTopPlayers(5);
        int totalPlayers = PlayerRanking.Instance.GetTotalPlayerCount();

        // Update title
        if (rankingTitleText != null)
        {
            rankingTitleText.text = "TOP PLAYERS";
        }

        // Update each ranking slot
        for (int i = 0; i < 5; i++)
        {
            // Activate ranking slot if available
            if (rankingSlots != null && i < rankingSlots.Length && rankingSlots[i] != null)
            {
                rankingSlots[i].SetActive(true);
            }

            if (i < topPlayers.Count)
            {
                // Display actual player data
                PlayerScore player = topPlayers[i];
                
                if (playerNameTexts != null && i < playerNameTexts.Length && playerNameTexts[i] != null)
                {
                    playerNameTexts[i].text = $"{i + 1}. {player.playerName}";
                }
                
                if (playerScoreTexts != null && i < playerScoreTexts.Length && playerScoreTexts[i] != null)
                {
                    playerScoreTexts[i].text = $"Score: {player.score}";
                }
                
                if (playerStatsTexts != null && i < playerStatsTexts.Length && playerStatsTexts[i] != null)
                {
                    float winRate = player.gamesPlayed > 0 ? (float)player.gamesWon / player.gamesPlayed * 100 : 0;
                    playerStatsTexts[i].text = $"Wins: {player.gamesWon}/{player.gamesPlayed} ({winRate:F1}%)";
                }
            }
            else
            {
                // Display placeholder for empty slots
                if (playerNameTexts != null && i < playerNameTexts.Length && playerNameTexts[i] != null)
                {
                    playerNameTexts[i].text = $"{i + 1}. ...";
                }
                
                if (playerScoreTexts != null && i < playerScoreTexts.Length && playerScoreTexts[i] != null)
                {
                    playerScoreTexts[i].text = "Score: 0";
                }
                
                if (playerStatsTexts != null && i < playerStatsTexts.Length && playerStatsTexts[i] != null)
                {
                    playerStatsTexts[i].text = "Wins: 0/0 (0.0%)";
                }
            }
        }
    }

    /// <summary>
    /// Force refresh the ranking display
    /// </summary>
    public void RefreshRanking()
    {
        UpdateRankingDisplay();
    }
} 