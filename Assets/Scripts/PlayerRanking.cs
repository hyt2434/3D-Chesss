using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class PlayerScore
{
    public string playerName;
    public int score;
    public int gamesWon;
    public int gamesPlayed;

    public PlayerScore(string name, int score, int won = 0, int played = 0)
    {
        this.playerName = name;
        this.score = score;
        this.gamesWon = won;
        this.gamesPlayed = played;
    }
}

public class PlayerRanking : MonoBehaviour
{
    public static PlayerRanking Instance;
    
    private List<PlayerScore> playerScores = new List<PlayerScore>();
    private const string PLAYER_SCORES_KEY = "PlayerScores";
    private const string PLAYER_COUNT_KEY = "PlayerCount";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadPlayerScores();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Add or update a player's score
    /// </summary>
    public void AddPlayerScore(string playerName, int score, bool won = false)
    {
        var existingPlayer = playerScores.FirstOrDefault(p => p.playerName == playerName);
        
        if (existingPlayer != null)
        {
            existingPlayer.score += score;
            existingPlayer.gamesPlayed++;
            if (won) existingPlayer.gamesWon++;
        }
        else
        {
            playerScores.Add(new PlayerScore(playerName, score, won ? 1 : 0, 1));
        }

        // Sort by score (highest first)
        playerScores = playerScores.OrderByDescending(p => p.score).ToList();
        
        SavePlayerScores();
    }

    /// <summary>
    /// Get the top N players
    /// </summary>
    public List<PlayerScore> GetTopPlayers(int count = 5)
    {
        return playerScores.Take(count).ToList();
    }

    /// <summary>
    /// Get the total number of players
    /// </summary>
    public int GetTotalPlayerCount()
    {
        return playerScores.Count;
    }

    /// <summary>
    /// Clear all player scores (for testing)
    /// </summary>
    public void ClearAllScores()
    {
        playerScores.Clear();
        SavePlayerScores();
    }

    private void SavePlayerScores()
    {
        PlayerPrefs.SetInt(PLAYER_COUNT_KEY, playerScores.Count);
        
        for (int i = 0; i < playerScores.Count; i++)
        {
            string prefix = $"Player_{i}_";
            PlayerPrefs.SetString(prefix + "Name", playerScores[i].playerName);
            PlayerPrefs.SetInt(prefix + "Score", playerScores[i].score);
            PlayerPrefs.SetInt(prefix + "Won", playerScores[i].gamesWon);
            PlayerPrefs.SetInt(prefix + "Played", playerScores[i].gamesPlayed);
        }
        
        PlayerPrefs.Save();
    }

    private void LoadPlayerScores()
    {
        playerScores.Clear();
        int playerCount = PlayerPrefs.GetInt(PLAYER_COUNT_KEY, 0);
        
        for (int i = 0; i < playerCount; i++)
        {
            string prefix = $"Player_{i}_";
            string name = PlayerPrefs.GetString(prefix + "Name", "");
            int score = PlayerPrefs.GetInt(prefix + "Score", 0);
            int won = PlayerPrefs.GetInt(prefix + "Won", 0);
            int played = PlayerPrefs.GetInt(prefix + "Played", 0);
            
            if (!string.IsNullOrEmpty(name))
            {
                playerScores.Add(new PlayerScore(name, score, won, played));
            }
        }
        
        // Sort by score (highest first)
        playerScores = playerScores.OrderByDescending(p => p.score).ToList();
    }
} 