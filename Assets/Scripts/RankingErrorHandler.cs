using UnityEngine;
using System.Linq;

/// <summary>
/// Handles errors and warnings for the ranking system
/// </summary>
public static class RankingErrorHandler
{
    /// <summary>
    /// Check if PlayerRanking instance exists
    /// </summary>
    public static bool IsPlayerRankingAvailable()
    {
        try
        {
            var handlers = Object.FindObjectsOfType<MonoBehaviour>();
            foreach (var handler in handlers)
            {
                if (handler != null && handler.GetType().Name == "PlayerRanking")
                {
                    var instanceProperty = handler.GetType().GetProperty("Instance");
                    if (instanceProperty != null)
                    {
                        var instance = instanceProperty.GetValue(null);
                        if (instance != null)
                        {
                            return true;
                        }
                    }
                }
            }
            Debug.LogWarning("[Ranking] PlayerRanking instance not found! Make sure PlayerRanking GameObject exists in Game scene.");
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Ranking] PlayerRanking not found or not compiled yet: " + e.Message);
            return false;
        }
    }

    /// <summary>
    /// Check if GameResultHandler instance exists
    /// </summary>
    public static bool IsGameResultHandlerAvailable()
    {
        try
        {
            var handlers = Object.FindObjectsOfType<MonoBehaviour>();
            int count = 0;
            foreach (var handler in handlers)
            {
                if (handler != null && handler.GetType().Name == "GameResultHandler")
                {
                    count++;
                }
            }
            if (count == 0)
            {
                Debug.LogWarning("[Ranking] GameResultHandler not found! Make sure GameResultHandler GameObject exists in Game scene.");
                return false;
            }
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Ranking] GameResultHandler not found or not compiled yet: " + e.Message);
            return false;
        }
    }

    /// <summary>
    /// Validate ranking display components
    /// </summary>
    public static bool ValidateRankingDisplay(MonoBehaviour display)
    {
        if (display == null)
        {
            Debug.LogError("[Ranking] RankingDisplay component is null!");
            return false;
        }

        if (display.GetType().Name != "RankingDisplay")
        {
            Debug.LogError("[Ranking] Component is not a RankingDisplay!");
            return false;
        }

        try
        {
            var playerNameTextsField = display.GetType().GetField("playerNameTexts");
            var playerScoreTextsField = display.GetType().GetField("playerScoreTexts");
            var playerStatsTextsField = display.GetType().GetField("playerStatsTexts");

            if (playerNameTextsField == null)
            {
                Debug.LogWarning("[Ranking] Player name texts field not found in RankingDisplay.");
                return false;
            }

            if (playerScoreTextsField == null)
            {
                Debug.LogWarning("[Ranking] Player score texts field not found in RankingDisplay.");
                return false;
            }

            if (playerStatsTextsField == null)
            {
                Debug.LogWarning("[Ranking] Player stats texts field not found in RankingDisplay.");
                return false;
            }

            var playerNameTexts = playerNameTextsField.GetValue(display) as TMPro.TextMeshProUGUI[];
            var playerScoreTexts = playerScoreTextsField.GetValue(display) as TMPro.TextMeshProUGUI[];
            var playerStatsTexts = playerStatsTextsField.GetValue(display) as TMPro.TextMeshProUGUI[];

            if (playerNameTexts == null || playerNameTexts.Length < 5)
            {
                Debug.LogWarning("[Ranking] Player name texts array is null or too short. Expected 5 elements.");
                return false;
            }

            if (playerScoreTexts == null || playerScoreTexts.Length < 5)
            {
                Debug.LogWarning("[Ranking] Player score texts array is null or too short. Expected 5 elements.");
                return false;
            }

            if (playerStatsTexts == null || playerStatsTexts.Length < 5)
            {
                Debug.LogWarning("[Ranking] Player stats texts array is null or too short. Expected 5 elements.");
                return false;
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Ranking] Error validating RankingDisplay: " + e.Message);
            return false;
        }
    }

    /// <summary>
    /// Log ranking system status
    /// </summary>
    public static void LogRankingStatus()
    {
        Debug.Log($"[Ranking] Status Check:");
        Debug.Log($"[Ranking] PlayerRanking available: {IsPlayerRankingAvailable()}");
        Debug.Log($"[Ranking] GameResultHandler available: {IsGameResultHandlerAvailable()}");
        
        try
        {
            var handlers = Object.FindObjectsOfType<MonoBehaviour>();
            foreach (var handler in handlers)
            {
                if (handler != null && handler.GetType().Name == "PlayerRanking")
                {
                    var getTotalPlayerCountMethod = handler.GetType().GetMethod("GetTotalPlayerCount");
                    if (getTotalPlayerCountMethod != null)
                    {
                        var count = getTotalPlayerCountMethod.Invoke(handler, null);
                        Debug.Log($"[Ranking] Total players: {count}");
                    }
                    break;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Ranking] Error getting player count: " + e.Message);
        }
    }
} 