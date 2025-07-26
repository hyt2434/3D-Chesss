using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    [Header("UI Texts")]
    public TextMeshProUGUI whiteScoreText;
    public TextMeshProUGUI blackScoreText;

    private int whiteScore;
    private int blackScore;

    void Start()
    {
        ResetScores();
    }

    /// <summary>
    /// Call this whenever a side captures a piece.
    /// team: 0=White, 1=Black
    /// points: standard chess piece values.
    /// </summary>
    public void AddPoints(int team, int points)
    {
        if (team == 0) whiteScore += points;
        else blackScore += points;

        UpdateUI();
    }

    /// <summary>
    /// Reset both scores to zero and update the labels.
    /// </summary>
    public void ResetScores()
    {
        whiteScore = 0;
        blackScore = 0;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (whiteScoreText != null)
            whiteScoreText.text = $"White: {whiteScore}";
        if (blackScoreText != null)
            blackScoreText.text = $"Black: {blackScore}";
    }
}
