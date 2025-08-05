using UnityEngine;

public class RankingTestData : MonoBehaviour
{
    [Header("Test Data")]
    [Tooltip("Add sample players for testing")]
    public bool addTestData = false;

    void Start()
    {
        if (addTestData && RankingErrorHandler.IsPlayerRankingAvailable())
        {
            // Add some sample player data
            PlayerRanking.Instance.AddPlayerScore("Alice", 150, true);
            PlayerRanking.Instance.AddPlayerScore("Bob", 120, false);
            PlayerRanking.Instance.AddPlayerScore("Charlie", 200, true);
            PlayerRanking.Instance.AddPlayerScore("Diana", 180, true);
            PlayerRanking.Instance.AddPlayerScore("Eve", 90, false);
            
            // Add more games to existing players
            PlayerRanking.Instance.AddPlayerScore("Alice", 50, true);
            PlayerRanking.Instance.AddPlayerScore("Bob", 80, true);
            PlayerRanking.Instance.AddPlayerScore("Charlie", 30, false);
            
            Debug.Log("Test data added to ranking system");
        }
    }

    /// <summary>
    /// Clear all ranking data (for testing)
    /// </summary>
    [ContextMenu("Clear All Rankings")]
    public void ClearAllRankings()
    {
        if (RankingErrorHandler.IsPlayerRankingAvailable())
        {
            PlayerRanking.Instance.ClearAllScores();
            Debug.Log("All ranking data cleared");
        }
    }

    /// <summary>
    /// Add a single test player
    /// </summary>
    [ContextMenu("Add Test Player")]
    public void AddTestPlayer()
    {
        if (RankingErrorHandler.IsPlayerRankingAvailable())
        {
            string playerName = "TestPlayer" + Random.Range(1, 1000);
            int score = Random.Range(50, 300);
            bool won = Random.value > 0.5f;
            
            PlayerRanking.Instance.AddPlayerScore(playerName, score, won);
            Debug.Log($"Added test player: {playerName} with score {score}, won: {won}");
        }
    }
} 