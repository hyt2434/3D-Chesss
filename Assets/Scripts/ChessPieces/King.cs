using System.Collections.Generic;
using UnityEngine;

public class King : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        // Right
        if (currentX + 1 < tileCountX)
        {
            // Right
            if (board[currentX + 1, currentY] == null || board[currentX + 1, currentY].team != team)
            {
                r.Add(new Vector2Int(currentX + 1, currentY));
            }
            // Top Right
            if (currentY + 1 < tileCountY )
            {
                if (board[currentX + 1, currentY + 1] == null || board[currentX + 1, currentY + 1].team != team)
                {
                    r.Add(new Vector2Int(currentX + 1, currentY + 1));
                }
            }
            // Bottom Right
            if (currentY - 1 >= 0)
            {
                if (board[currentX + 1, currentY - 1] == null || board[currentX + 1, currentY - 1].team != team)
                {
                    r.Add(new Vector2Int(currentX + 1, currentY - 1));
                }
            }
        }
        // Left
        if (currentX - 1 >= 0)
        {
            // Left
            if (board[currentX - 1, currentY] == null || board[currentX - 1, currentY].team != team)
            {
                r.Add(new Vector2Int(currentX - 1, currentY));
            }
            // Top Right
            if (currentY + 1 < tileCountY)
            {
                if (board[currentX - 1, currentY + 1] == null || board[currentX - 1, currentY + 1].team != team)
                {
                    r.Add(new Vector2Int(currentX - 1, currentY + 1));
                }
            }
            // Bottom Right
            if (currentY - 1 >= 0)
            {
                if (board[currentX - 1, currentY - 1] == null || board[currentX - 1, currentY - 1].team != team)
                {
                    r.Add(new Vector2Int(currentX - 1, currentY - 1));
                }
            }
        }
        // Up
        if (currentY + 1 < tileCountY)
        {
            // Up
            if (board[currentX, currentY + 1] == null || board[currentX, currentY + 1].team != team)
            {
                r.Add(new Vector2Int(currentX, currentY + 1));
            }
        }
        // Down
        if (currentY - 1 >= 0)
        {
            // Down
            if (board[currentX, currentY - 1] == null || board[currentX, currentY - 1].team != team)
            {
                r.Add(new Vector2Int(currentX, currentY - 1));
            }
        }
        return r;
    }
    public override SpecialMove GetSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        SpecialMove C = SpecialMove.Nothing;

        var kingMove = moveList.Find(m => m[0].x == 4 && m[0].y == ((team == 0) ? 0 : 7));
        var leftRook = moveList.Find(m => m[0].x == 0 && m[0].y == ((team == 0) ? 0 : 7));
        var rightRook = moveList.Find(m => m[0].x == 7 && m[0].y == ((team == 0) ? 0 : 7));

        if (kingMove == null && currentX == 4)
        {
            if (team == 0) //White side
            {
                if (leftRook == null)
                    if (board[0, 0].type == ChessPieceType.Rock) //check if it's a rook
                        if (board[0, 0].team == 0) //check if it's white team 
                            if (board[3, 0] == null)
                                if (board[2, 0] == null) // these lines check if there are spaces between the rook and the king, same also applied for below lines
                                    if (board[1, 0] == null)
                                    {
                                        availableMoves.Add(new Vector2Int(2, 0));
                                        C = SpecialMove.Castling;
                                    }

                if (rightRook == null)
                    if (board[7, 0].type == ChessPieceType.Rock)
                        if (board[7, 0].team == 0)
                            if (board[5, 0] == null)
                                if (board[6, 0] == null)
                                {
                                    availableMoves.Add(new Vector2Int(6, 0));
                                    C = SpecialMove.Castling;
                                }
            }
            else //Black side
            {
                if (leftRook == null)
                    if (board[0, 7].type == ChessPieceType.Rock)
                        if (board[0, 7].team == 1)
                            if (board[3, 7] == null)
                                if (board[2, 7] == null)
                                    if (board[1, 7] == null)
                                    {
                                        availableMoves.Add(new Vector2Int(2, 7));
                                        C = SpecialMove.Castling;
                                    }
                if (rightRook == null)
                    if (board[7, 7].type == ChessPieceType.Rock)
                        if (board[7, 7].team == 1)
                            if (board[5, 7] == null)
                                if (board[6, 7] == null)
                                {
                                    availableMoves.Add(new Vector2Int(6, 7));
                                    C = SpecialMove.Castling;
                                }
            }

        }


        return C;
    }
}
