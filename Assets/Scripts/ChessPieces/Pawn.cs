using System.Collections.Generic;
using UnityEngine;

public class Pawn : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        int direction = team == 0 ? 1 : -1; // Assuming team 0 moves up and team 1 moves down

        // One in front
        if (board[currentX, currentY + direction] == null)
        {
            r.Add(new Vector2Int(currentX, currentY + direction));
        }

        // Two in front 
        if (board[currentX, currentY + direction] == null)
        {
            if ((team == 0 && currentY == 1) && board[currentX, currentY + (direction * 2)] == null)
            {
                r.Add(new Vector2Int(currentX, currentY + (direction * 2)));
            }
            if ((team == 1 && currentY == 6) && board[currentX, currentY + (direction * 2)] == null)
            {
                r.Add(new Vector2Int(currentX, currentY + (direction * 2)));
            }
        }
        // Kill move
        if (currentX  != tileCountX - 1)
        {
            if (board[currentX + 1, currentY + direction] != null && board[currentX + 1, currentY + direction].team != team)
            {
                r.Add(new Vector2Int(currentX + 1, currentY + direction)); 
            }
        }
        if (currentX != 0)
        {
            if (board[currentX - 1, currentY + direction] != null && board[currentX - 1, currentY + direction].team != team)
            {
                r.Add(new Vector2Int(currentX - 1, currentY + direction));
            }
        }
        return r;
    }

    public override SpecialMove GetSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        int direction = 0;
        if (team == 0)
        {
            direction = 1;
        }
        else if (team == 1)
        {
            direction = -1;
        }
        if ((team == 0 && currentY == 6) || (team == 1 && currentY == 1))
            return SpecialMove.Promotion;
        //EnPassant
        if (moveList.Count > 0)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            if (board[lastMove[1].x, lastMove[1].y].type == ChessPieceType.Pawn) //Check if the last moved piece was a pawn or not
            {
                if (Mathf.Abs(lastMove[0].y - lastMove[1].y) == 2) //If the Pawn moved +2 in either direction
                {
                    if (board[lastMove[1].x, lastMove[1].y].team != team) //If the move was from the other team 
                    {
                        if (lastMove[1].y == currentY) //If both pawns are on the same line to execute an EnPassant
                        {
                            if (lastMove[1].x == currentX - 1) //Moved left
                            {
                                availableMoves.Add(new Vector2Int(currentX - 1, currentY + direction));
                                return SpecialMove.enPassant;
                            }
                            if (lastMove[1].x == currentX + 1) //Moved right
                            {
                                availableMoves.Add(new Vector2Int(currentX + 1, currentY + direction));
                                return SpecialMove.enPassant;
                            }
                        }
                    }
                }
            }
        }

        return SpecialMove.Nothing;
    }
}
