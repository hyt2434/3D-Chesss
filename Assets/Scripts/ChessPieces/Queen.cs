using System.Collections.Generic;
using UnityEngine;

public class Queen : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();
        //Down
        for (int i = currentY - 1; i >= 0; i--)
        {
            if (board[currentX, i] == null)
            {
                r.Add(new Vector2Int(currentX, i));
            }
            else
            {
                if (board[currentX, i].team != team)
                {
                    r.Add(new Vector2Int(currentX, i));
                }
                break;
            }
        }

        // Up 
        for (int i = currentY + 1; i < tileCountY; i++)
        {
            if (board[currentX, i] == null)
            {
                r.Add(new Vector2Int(currentX, i));
            }
            else
            {
                if (board[currentX, i].team != team)
                {
                    r.Add(new Vector2Int(currentX, i));
                }
                break;
            }
        }

        // Left
        for (int i = currentX - 1; i >= 0; i--)
        {
            if (board[i, currentY] == null)
            {
                r.Add(new Vector2Int(i, currentY));
            }
            else
            {
                if (board[i, currentY].team != team)
                {
                    r.Add(new Vector2Int(i, currentY));
                }
                break;
            }
        }
        // Right
        for (int i = currentX + 1; i < tileCountX; i++)
        {
            if (board[i, currentY] == null)
            {
                r.Add(new Vector2Int(i, currentY));
            }
            else
            {
                if (board[i, currentY].team != team)
                {
                    r.Add(new Vector2Int(i, currentY));
                }
                break;
            }
        }
        // Top Right
        for (int i = currentX + 1, j = currentY + 1; i < tileCountX && j < tileCountY; i++, j++)
        {
            if (board[i, j] == null)
            {
                r.Add(new Vector2Int(i, j));
            }
            else
            {
                if (board[i, j].team != team)
                {
                    r.Add(new Vector2Int(i, j));
                }
                break;
            }
        }
        // Top Left
        for (int i = currentX - 1, j = currentY + 1; i >= 0 && j < tileCountY; i--, j++)
        {
            if (board[i, j] == null)
            {
                r.Add(new Vector2Int(i, j));
            }
            else
            {
                if (board[i, j].team != team)
                {
                    r.Add(new Vector2Int(i, j));
                }
                break;
            }
        }
        // Bottom Right
        for (int i = currentX + 1, j = currentY - 1; i < tileCountX && j >= 0; i++, j--)
        {
            if (board[i, j] == null)
            {
                r.Add(new Vector2Int(i, j));
            }
            else
            {
                if (board[i, j].team != team)
                {
                    r.Add(new Vector2Int(i, j));
                }
                break;
            }
        }
        // Bottom Left
        for (int i = currentX - 1, j = currentY - 1; i >= 0 && j >= 0; i--, j--)
        {
            if (board[i, j] == null)
            {
                r.Add(new Vector2Int(i, j));
            }
            else
            {
                if (board[i, j].team != team)
                {
                    r.Add(new Vector2Int(i, j));
                }
                break;
            }
        }

        return r;

    }
}
