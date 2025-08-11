using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public string name;
    public int age;
    public int rankingPoints;
    public int gamesPlayed;
    public int gamesWon;

    public PlayerData(string name, int age, int rankingPoints = 1000, int gamesPlayed = 0, int gamesWon = 0)
    {
        this.name = name;
        this.age = age;
        this.rankingPoints = rankingPoints;
        this.gamesPlayed = gamesPlayed;
        this.gamesWon = gamesWon;
    }
}

public class PlayerRanking : MonoBehaviour
{
    public static PlayerRanking Instance;

    private const string PLAYER_COUNT_KEY = "PlayerCount";
    private const string PLAYER_NAME_KEY = "Player_Name";
    private const string PLAYER_AGE_KEY = "Player_Age";
    private const string PLAYER_RANKING_KEY = "Player_Ranking";
    private const string PLAYER_GAMES_PLAYED_KEY = "Player_GamesPlayed";
    private const string PLAYER_GAMES_WON_KEY = "Player_GamesWon";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Add a new player with default 1000 ranking points
    /// </summary>
    public void AddNewPlayer(string name, int age)
    {
        int count = PlayerPrefs.GetInt(PLAYER_COUNT_KEY, 0);
        
        PlayerPrefs.SetString($"{PLAYER_NAME_KEY}_{count}", name);
        PlayerPrefs.SetInt($"{PLAYER_AGE_KEY}_{count}", age);
        PlayerPrefs.SetInt($"{PLAYER_RANKING_KEY}_{count}", 1000); // Default 1000 points
        PlayerPrefs.SetInt($"{PLAYER_GAMES_PLAYED_KEY}_{count}", 0);
        PlayerPrefs.SetInt($"{PLAYER_GAMES_WON_KEY}_{count}", 0);
        PlayerPrefs.SetInt(PLAYER_COUNT_KEY, count + 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Get player data by name and age
    /// </summary>
    public PlayerData GetPlayerData(string name, int age)
    {
        int count = PlayerPrefs.GetInt(PLAYER_COUNT_KEY, 0);
        
        for (int i = 0; i < count; i++)
        {
            string playerName = PlayerPrefs.GetString($"{PLAYER_NAME_KEY}_{i}", "");
            int playerAge = PlayerPrefs.GetInt($"{PLAYER_AGE_KEY}_{i}", -1);
            
            if (playerName == name && playerAge == age)
            {
                return new PlayerData(
                    playerName,
                    playerAge,
                    PlayerPrefs.GetInt($"{PLAYER_RANKING_KEY}_{i}", 1000),
                    PlayerPrefs.GetInt($"{PLAYER_GAMES_PLAYED_KEY}_{i}", 0),
                    PlayerPrefs.GetInt($"{PLAYER_GAMES_WON_KEY}_{i}", 0)
                );
            }
        }
        
        return null;
    }

    /// <summary>
    /// Update player's ranking points
    /// </summary>
    public void UpdatePlayerRanking(string name, int age, int newRankingPoints)
    {
        int count = PlayerPrefs.GetInt(PLAYER_COUNT_KEY, 0);
        
        for (int i = 0; i < count; i++)
        {
            string playerName = PlayerPrefs.GetString($"{PLAYER_NAME_KEY}_{i}", "");
            int playerAge = PlayerPrefs.GetInt($"{PLAYER_AGE_KEY}_{i}", -1);
            
            if (playerName == name && playerAge == age)
            {
                PlayerPrefs.SetInt($"{PLAYER_RANKING_KEY}_{i}", newRankingPoints);
                PlayerPrefs.Save();
                return;
            }
        }
    }

    /// <summary>
    /// Update player's game statistics
    /// </summary>
    public void UpdatePlayerStats(string name, int age, bool won = false)
    {
        int count = PlayerPrefs.GetInt(PLAYER_COUNT_KEY, 0);
        
        for (int i = 0; i < count; i++)
        {
            string playerName = PlayerPrefs.GetString($"{PLAYER_NAME_KEY}_{i}", "");
            int playerAge = PlayerPrefs.GetInt($"{PLAYER_AGE_KEY}_{i}", -1);
            
            if (playerName == name && playerAge == age)
            {
                int gamesPlayed = PlayerPrefs.GetInt($"{PLAYER_GAMES_PLAYED_KEY}_{i}", 0) + 1;
                int gamesWon = PlayerPrefs.GetInt($"{PLAYER_GAMES_WON_KEY}_{i}", 0);
                
                if (won) gamesWon++;
                
                PlayerPrefs.SetInt($"{PLAYER_GAMES_PLAYED_KEY}_{i}", gamesPlayed);
                PlayerPrefs.SetInt($"{PLAYER_GAMES_WON_KEY}_{i}", gamesWon);
                PlayerPrefs.Save();
                return;
            }
        }
    }

    /// <summary>
    /// Get top 5 players by ranking points
    /// </summary>
    public List<PlayerData> GetTopPlayers(int count = 5)
    {
        List<PlayerData> allPlayers = GetAllPlayers();
        
        // Sort by ranking points (descending) and return top players
        return allPlayers
            .OrderByDescending(p => p.rankingPoints)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Get all players
    /// </summary>
    public List<PlayerData> GetAllPlayers()
    {
        List<PlayerData> players = new List<PlayerData>();
        int count = PlayerPrefs.GetInt(PLAYER_COUNT_KEY, 0);
        
        for (int i = 0; i < count; i++)
        {
            string name = PlayerPrefs.GetString($"{PLAYER_NAME_KEY}_{i}", "");
            int age = PlayerPrefs.GetInt($"{PLAYER_AGE_KEY}_{i}", -1);
            int ranking = PlayerPrefs.GetInt($"{PLAYER_RANKING_KEY}_{i}", 1000);
            int gamesPlayed = PlayerPrefs.GetInt($"{PLAYER_GAMES_PLAYED_KEY}_{i}", 0);
            int gamesWon = PlayerPrefs.GetInt($"{PLAYER_GAMES_WON_KEY}_{i}", 0);
            
            if (!string.IsNullOrEmpty(name) && age != -1)
            {
                players.Add(new PlayerData(name, age, ranking, gamesPlayed, gamesWon));
            }
        }
        
        return players;
    }

    /// <summary>
    /// Check if player exists
    /// </summary>
    public bool PlayerExists(string name, int age)
    {
        return GetPlayerData(name, age) != null;
    }

    /// <summary>
    /// Calculate ranking points change based on game result
    /// </summary>
    public int CalculateRankingChange(bool won, int opponentRanking, int playerRanking)
    {
        int baseChange = won ? 32 : -32;
        int rankingDifference = opponentRanking - playerRanking;
        
        // Adjust based on ranking difference
        float multiplier = 1.0f;
        if (rankingDifference > 0)
        {
            // Playing against higher ranked player
            multiplier = won ? 1.2f : 0.8f;
        }
        else if (rankingDifference < 0)
        {
            // Playing against lower ranked player
            multiplier = won ? 0.8f : 1.2f;
        }
        
        return Mathf.RoundToInt(baseChange * multiplier);
    }
} 