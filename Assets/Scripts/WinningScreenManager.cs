using UnityEngine;

/// <summary>
/// Centralized manager for the winning screen to prevent conflicts
/// </summary>
public static class WinningScreenManager
{
    private static bool isGameEnded = false;

    /// <summary>
    /// Show the winning screen with the specified winner
    /// </summary>
    /// <param name="winningScreen">The winning screen GameObject</param>
    /// <param name="winnerIndex">0 = White wins, 1 = Black wins, 2 = Draw</param>
    public static void ShowWinningScreen(GameObject winningScreen, int winnerIndex)
    {
        if (winningScreen == null || isGameEnded) return;

        isGameEnded = true;

        // First, deactivate all children to ensure only one is active
        for (int i = 0; i < winningScreen.transform.childCount; i++)
        {
            winningScreen.transform.GetChild(i).gameObject.SetActive(false);
        }
        
        // Then activate the winning screen and the correct child
        winningScreen.SetActive(true);
        if (winnerIndex < winningScreen.transform.childCount)
        {
            winningScreen.transform.GetChild(winnerIndex).gameObject.SetActive(true);
        }

        Debug.Log($"[WinningScreen] Showing winner: {winnerIndex} (0=White, 1=Black, 2=Draw)");
    }

    /// <summary>
    /// Hide the winning screen and reset the game ended flag
    /// </summary>
    /// <param name="winningScreen">The winning screen GameObject</param>
    public static void HideWinningScreen(GameObject winningScreen)
    {
        if (winningScreen != null)
        {
            // Deactivate all children first
            for (int i = 0; i < winningScreen.transform.childCount; i++)
            {
                winningScreen.transform.GetChild(i).gameObject.SetActive(false);
            }
            // Then deactivate the winning screen itself
            winningScreen.SetActive(false);
        }

        isGameEnded = false;
        Debug.Log("[WinningScreen] Hidden and reset");
    }

    /// <summary>
    /// Check if the game has ended
    /// </summary>
    public static bool IsGameEnded()
    {
        return isGameEnded;
    }

    /// <summary>
    /// Reset the game ended flag (for new games)
    /// </summary>
    public static void ResetGameEnded()
    {
        isGameEnded = false;
    }
} 