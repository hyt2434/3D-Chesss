using System.Collections.Generic;
using UnityEngine;

public class Pawn : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(
        ref ChessPiece[,] board,
        int tileCountX,
        int tileCountY)
    {
        var moves = new List<Vector2Int>();
        int dir = (team == 0 ? +1 : -1);
        int nextY = currentY + dir;

        // --- 1. one square forward ---
        if (nextY >= 0 && nextY < tileCountY
            && board[currentX, nextY] == null)
        {
            moves.Add(new Vector2Int(currentX, nextY));

            // --- 2. two squares forward from start rank ---
            int startRank = (team == 0 ? 1 : tileCountY - 2);
            int twoY = currentY + dir * 2;
            if (currentY == startRank
                && twoY >= 0 && twoY < tileCountY
                && board[currentX, twoY] == null)
            {
                moves.Add(new Vector2Int(currentX, twoY));
            }
        }

        // --- 3. captures to the left/right ---
        // left
        if (currentX - 1 >= 0
            && nextY >= 0 && nextY < tileCountY)
        {
            var target = board[currentX - 1, nextY];
            if (target != null && target.team != team)
                moves.Add(new Vector2Int(currentX - 1, nextY));
        }
        // right
        if (currentX + 1 < tileCountX
            && nextY >= 0 && nextY < tileCountY)
        {
            var target = board[currentX + 1, nextY];
            if (target != null && target.team != team)
                moves.Add(new Vector2Int(currentX + 1, nextY));
        }

        return moves;
    }

    public override SpecialMove GetSpecialMoves(
        ref ChessPiece[,] board,
        ref List<Vector2Int[]> moveList,
        ref List<Vector2Int> availableMoves)
    {
        int dir = (team == 0 ? +1 : -1);
        int finalY = (team == 0 ? board.GetLength(1) - 1 : 0);

        // --- 1) Promotion if any forward move lands on the back rank ---
        foreach (var m in availableMoves)
        {
            if (m.y == finalY)
                return SpecialMove.Promotion;
        }

        // --- 2) En passant ---
        if (moveList.Count > 0)
        {
            var last = moveList[moveList.Count - 1];
            var start = last[0];
            var end = last[1];

            // Was it an enemy pawn moving two squares?
            if (board[end.x, end.y]?.type == ChessPieceType.Pawn
                && board[end.x, end.y].team != team
                && Mathf.Abs(start.y - end.y) == 2
                && end.y == currentY
                && Mathf.Abs(end.x - currentX) == 1)
            {
                var epTarget = new Vector2Int(end.x, currentY + dir);
                // make sure that landing square is empty
                if (epTarget.y >= 0
                    && epTarget.y < board.GetLength(1)
                    && board[epTarget.x, epTarget.y] == null)
                {
                    availableMoves.Add(epTarget);
                    return SpecialMove.enPassant;
                }
            }
        }

        return SpecialMove.Nothing;
    }
}
