using UnityEngine;

/// <summary>
/// Simple manager to ensure PlayerRanking is available in the scene
/// Attach this to any GameObject in your scene
/// </summary>
public class PlayerRankingManager : MonoBehaviour
{
    void Awake()
    {
        // Ensure PlayerRanking component exists
        if (PlayerRanking.Instance == null)
        {
            gameObject.AddComponent<PlayerRanking>();
        }
    }
} 