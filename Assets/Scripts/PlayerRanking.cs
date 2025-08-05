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
            CheckForNewPlayerNeedingPoints();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CheckForNewPlayerNeedingPoints()
    {
        string newPlayerName = PlayerPrefs.GetString("NewPlayerNeedsPoints", "");
        if (!string.IsNullOrEmpty(newPlayerName))
        {
            // Give this new player exactly 1000 points
            CreateNewPlayer(newPlayerName, 1000);
            // Clear the flag
            PlayerPrefs.DeleteKey("NewPlayerNeedsPoints");
            PlayerPrefs.Save();
            Debug.Log($"Processed new player {newPlayerName} - gave 1000 starting points");
        }
    }

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
            // ALL new players get exactly 1000 points (not 1000 + game score)
            playerScores.Add(new PlayerScore(playerName, 1000, won ? 1 : 0, 1));
        }

        // Sort by score (highest first)
        playerScores = playerScores.OrderByDescending(p => p.score).ToList();
        
        SavePlayerScores();
    }

    public void CreateNewPlayer(string playerName, int initialScore = 1000)
    {
        var existingPlayer = playerScores.FirstOrDefault(p => p.playerName == playerName);
        
        if (existingPlayer == null)
        {
            playerScores.Add(new PlayerScore(playerName, initialScore, 0, 0));
            // Sort by score (highest first)
            playerScores = playerScores.OrderByDescending(p => p.score).ToList();
            SavePlayerScores();
            Debug.Log($"CreateNewPlayer: Added {playerName} with {initialScore} points. Total players: {playerScores.Count}");
        }
        else if (existingPlayer.score == 0 && existingPlayer.gamesPlayed == 0)
        {
            // Player exists but has 0 points and no games - give them starting points
            existingPlayer.score = initialScore;
            // Sort by score (highest first)
            playerScores = playerScores.OrderByDescending(p => p.score).ToList();
            SavePlayerScores();
            Debug.Log($"CreateNewPlayer: Updated {playerName} from 0 to {initialScore} points (was empty player)");
        }
        else
        {
            Debug.Log($"CreateNewPlayer: Player {playerName} already exists with {existingPlayer.score} points");
        }
    }

    /// <summary>
    /// Get the top N players
    /// </summary>
    public List<PlayerScore> GetTopPlayers(int count = 5)
    {
        return playerScores.Take(count).ToList();
    }

    public int GetTotalPlayerCount()
    {
        return playerScores.Count;
    }

    [ContextMenu("Clear All Player Data")]
    public void ClearAllScores()
    {
        Debug.LogError($"[CLEAR DEBUG] Before clear - player count: {playerScores.Count}");
        
        // Clear in-memory data
        playerScores.Clear();
        
        // Clear PlayerPrefs data completely
        PlayerPrefs.DeleteKey(PLAYER_COUNT_KEY);
        for (int i = 0; i < 100; i++)
        {
            // Clear the correct key format used by SavePlayerScores
            string prefix = $"Player_{i}_";
            PlayerPrefs.DeleteKey(prefix + "Name");
            PlayerPrefs.DeleteKey(prefix + "Score");
            PlayerPrefs.DeleteKey(prefix + "Won");
            PlayerPrefs.DeleteKey(prefix + "Played");
        }
        
        PlayerPrefs.Save();
        SavePlayerScores(); // This will save empty list
        
        Debug.LogError($"[CLEAR DEBUG] After clear - player count: {playerScores.Count}");
        Debug.Log("All player ranking data cleared completely");
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