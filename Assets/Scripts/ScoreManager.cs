using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ScoreManager : MonoBehaviour
{
    [Header("UI Texts")]
    public TextMeshProUGUI whiteScoreText;
    public TextMeshProUGUI blackScoreText;

    private int whiteScore;
    private int blackScore;
    
    // Track captured pieces for multiplayer scoring
    private List<ChessPieceType> capturedWhitePieces = new List<ChessPieceType>();
    private List<ChessPieceType> capturedBlackPieces = new List<ChessPieceType>();

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
    /// Track a captured piece for multiplayer scoring
    /// </summary>
    public void TrackCapturedPiece(int capturedTeam, ChessPieceType pieceType)
    {
        if (capturedTeam == 0) // White piece captured
        {
            capturedWhitePieces.Add(pieceType);
        }
        else // Black piece captured
        {
            capturedBlackPieces.Add(pieceType);
        }
    }
    
    /// <summary>
    /// Get the total value of captured pieces for a team
    /// </summary>
    public int GetCapturedPiecesValue(int team)
    {
        List<ChessPieceType> capturedPieces = (team == 0) ? capturedWhitePieces : capturedBlackPieces;
        int totalValue = 0;
        
        foreach (var pieceType in capturedPieces)
        {
            totalValue += GetPieceValue(pieceType);
        }
        
        return totalValue;
    }
    
    /// <summary>
    /// Get the number of captured pieces for a team
    /// </summary>
    public int GetCapturedPiecesCount(int team)
    {
        return (team == 0) ? capturedWhitePieces.Count : capturedBlackPieces.Count;
    }
    
    /// <summary>
    /// Get the standard chess piece value
    /// </summary>
    private int GetPieceValue(ChessPieceType pieceType)
    {
        return pieceType switch
        {
            ChessPieceType.Pawn => 1,
            ChessPieceType.Knight => 3,
            ChessPieceType.Bishop => 3,
            ChessPieceType.Rock => 5,
            ChessPieceType.Queen => 9,
            _ => 0
        };
    }

    /// <summary>
    /// Reset both scores to zero and update the labels.
    /// </summary>
    public void ResetScores()
    {
        whiteScore = 0;
        blackScore = 0;
        capturedWhitePieces.Clear();
        capturedBlackPieces.Clear();
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
