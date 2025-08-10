using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;

public class ChessAI : MonoBehaviour
{
    private const int MAX_DEPTH = 5; // Increased from 4 to 5 for much better play
    private const int INFINITY = 100000;
    private const int MAX_SEARCH_TIME = 5000; // 5 seconds max search time
    private int currentMoveCount = 0; // Track current move count for opening detection
    private int openingStrategy = 0; // Different opening strategies
    
    // Transposition table for caching evaluated positions
    private Dictionary<ulong, int> transpositionTable = new Dictionary<ulong, int>();
    private Dictionary<ulong, AIMove> moveTable = new Dictionary<ulong, AIMove>();
    
    // Search statistics
    private int nodesEvaluated = 0;
    private float searchStartTime = 0f;
    private bool searchTimeExceeded = false;
    
    // Enhanced piece values with positional bonuses
    private static readonly Dictionary<ChessPieceType, int> pieceValues = new Dictionary<ChessPieceType, int>
    {
        { ChessPieceType.Pawn, 100 },
        { ChessPieceType.Knight, 320 },
        { ChessPieceType.Bishop, 330 },
        { ChessPieceType.Rock, 500 },
        { ChessPieceType.Queen, 900 },
        { ChessPieceType.King, 20000 }
    };

    // Positional piece-square tables for better positional understanding
    private static readonly int[,] pawnPositionTable = {
        { 0,  0,  0,  0,  0,  0,  0,  0},
        {50, 50, 50, 50, 50, 50, 50, 50},
        {10, 10, 20, 30, 30, 20, 10, 10},
        { 5,  5, 10, 25, 25, 10,  5,  5},
        { 0,  0,  0, 20, 20,  0,  0,  0},
        { 5, -5,-10,  0,  0,-10, -5,  5},
        { 5, 10, 10,-20,-20, 10, 10,  5},
        { 0,  0,  0,  0,  0,  0,  0,  0}
    };

    private static readonly int[,] knightPositionTable = {
        {-50,-40,-30,-30,-30,-30,-40,-50},
        {-40,-20,  0,  0,  0,  0,-20,-40},
        {-30,  0, 10, 15, 15, 10,  0,-30},
        {-30,  5, 15, 20, 20, 15,  5,-30},
        {-30,  0, 15, 20, 20, 15,  0,-30},
        {-30,  5, 10, 15, 15, 10,  5,-30},
        {-40,-20,  0,  5,  5,  0,-20,-40},
        {-50,-40,-30,-30,-30,-30,-40,-50}
    };

    private static readonly int[,] bishopPositionTable = {
        {-20,-10,-10,-10,-10,-10,-10,-20},
        {-10,  0,  0,  0,  0,  0,  0,-10},
        {-10,  0,  5, 10, 10,  5,  0,-10},
        {-10,  5,  5, 10, 10,  5,  5,-10},
        {-10,  0, 10, 10, 10, 10,  0,-10},
        {-10, 10, 10, 10, 10, 10, 10,-10},
        {-10,  5,  0,  0,  0,  0,  5,-10},
        {-20,-10,-10,-10,-10,-10,-10,-20}
    };

    private static readonly int[,] rookPositionTable = {
        { 0,  0,  0,  0,  0,  0,  0,  0},
        { 5, 10, 10, 10, 10, 10, 10,  5},
        {-5,  0,  0,  0,  0,  0,  0, -5},
        {-5,  0,  0,  0,  0,  0,  0, -5},
        {-5,  0,  0,  0,  0,  0,  0, -5},
        {-5,  0,  0,  0,  0,  0,  0, -5},
        {-5,  0,  0,  0,  0,  0,  0, -5},
        { 0,  0,  0,  5,  5,  0,  0,  0}
    };

    private static readonly int[,] queenPositionTable = {
        {-20,-10,-10, -5, -5,-10,-10,-20},
        {-10,  0,  0,  0,  0,  0,  0,-10},
        {-10,  0,  5,  5,  5,  5,  0,-10},
        { -5,  0,  5,  5,  5,  5,  0, -5},
        {  0,  0,  5,  5,  5,  5,  0, -5},
        {-10,  5,  5,  5,  5,  5,  0,-10},
        {-10,  0,  5,  0,  0,  0,  0,-10},
        {-20,-10,-10, -5, -5,-10,-10,-20}
    };

    private static readonly int[,] kingPositionTable = {
        {-30,-40,-40,-50,-50,-40,-40,-30},
        {-30,-40,-40,-50,-50,-40,-40,-30},
        {-30,-40,-40,-50,-50,-40,-40,-30},
        {-30,-40,-40,-50,-50,-40,-40,-30},
        {-20,-30,-30,-40,-40,-30,-30,-20},
        {-10,-20,-20,-20,-20,-20,-20,-10},
        { 20, 20,  0,  0,  0,  0, 20, 20},
        { 20, 30, 10,  0,  0, 10, 30, 20}
    };

    // Endgame piece-square tables for better endgame play
    private static readonly int[,] endgameKingPositionTable = {
        {-50,-40,-30,-20,-20,-30,-40,-50},
        {-30,-20,-10,  0,  0,-10,-20,-30},
        {-30,-10, 20, 30, 30, 20,-10,-30},
        {-30,-10, 30, 40, 40, 30,-10,-30},
        {-30,-10, 30, 40, 40, 30,-10,-30},
        {-30,-10, 20, 30, 30, 20,-10,-30},
        {-30,-30,  0,  0,  0,  0,-30,-30},
        {-50,-30,-30,-30,-30,-30,-30,-50}
    };

    private void Start()
    {
        // Randomly choose an opening strategy for this game
        openingStrategy = Random.Range(0, 4);
        Debug.Log($"ChessAI using opening strategy: {openingStrategy}");
    }

    public void SetMoveCount(int moveCount)
    {
        currentMoveCount = moveCount;
    }

    public int GetMoveCount()
    {
        return currentMoveCount;
    }

    public void ResetOpeningStrategy()
    {
        openingStrategy = Random.Range(0, 4);
        transpositionTable.Clear();
        moveTable.Clear();
    }

    // Generate a hash for the current board position
    private ulong GenerateBoardHash(ChessPiece[,] board)
    {
        ulong hash = 0;
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (board[x, y] != null)
                {
                    ChessPiece piece = board[x, y];
                    int pieceIndex = (int)piece.type + (piece.team * 6);
                    hash ^= (ulong)(pieceIndex << ((x * 8 + y) * 4));
                }
            }
        }
        return hash;
    }

    public struct AIMove
    {
        public ChessPiece piece;
        public Vector2Int targetPosition;
        public int score;
        public bool isCapture;
        public bool isCheck;
        public bool isCheckEscape;
        public int movePriority; // Higher priority moves are evaluated first

        public AIMove(ChessPiece piece, Vector2Int targetPosition, int score)
        {
            this.piece = piece;
            this.targetPosition = targetPosition;
            this.score = score;
            this.isCapture = false;
            this.isCheck = false;
            this.isCheckEscape = false;
            this.movePriority = 0;
        }
    }

    public AIMove FindBestMove(ChessPiece[,] board, bool isWhiteTurn, int depth = MAX_DEPTH)
    {
        // Reset search statistics
        nodesEvaluated = 0;
        searchStartTime = Time.realtimeSinceStartup * 1000f;
        searchTimeExceeded = false;
        
        // Clear transposition table for new search
        transpositionTable.Clear();
        moveTable.Clear();
        
        List<AIMove> allMoves = GetAllPossibleMoves(board, isWhiteTurn);
        
        if (allMoves.Count == 0)
        {
            Debug.LogWarning("Bot could not find a valid move!");
            return new AIMove(null, Vector2Int.zero, -INFINITY);
        }

        // Enhanced move analysis
        for (int i = 0; i < allMoves.Count; i++)
        {
            var move = allMoves[i];
            
            // Check if this move escapes check
            if (IsKingInCheck(board, isWhiteTurn))
            {
                ChessPiece[,] tempBoard = CloneBoard(board);
                ChessPiece clonedPiece = tempBoard[move.piece.currentX, move.piece.currentY];
                tempBoard[move.targetPosition.x, move.targetPosition.y] = clonedPiece;
                tempBoard[move.piece.currentX, move.piece.currentY] = null;
                clonedPiece.currentX = move.targetPosition.x;
                clonedPiece.currentY = move.targetPosition.y;
                
                if (!IsKingInCheck(tempBoard, isWhiteTurn))
                {
                    move.isCheckEscape = true;
                }
            }
            
            // Check if this move gives check
            ChessPiece[,] checkBoard = CloneBoard(board);
            ChessPiece checkPiece = checkBoard[move.piece.currentX, move.piece.currentY];
            checkBoard[move.targetPosition.x, move.targetPosition.y] = checkPiece;
            checkBoard[move.piece.currentX, move.piece.currentY] = null;
            checkPiece.currentX = move.targetPosition.x;
            checkPiece.currentY = move.targetPosition.y;
            
            if (IsKingInCheck(checkBoard, !isWhiteTurn))
            {
                move.isCheck = true;
            }
            
            // Check if this is a capture
            if (board[move.targetPosition.x, move.targetPosition.y] != null)
            {
                move.isCapture = true;
            }
            
            allMoves[i] = move;
        }

        // Sort moves for better alpha-beta pruning
        SortMoves(allMoves);
        
        // Apply opening move bonuses
        if (currentMoveCount < 10)
        {
            for (int i = 0; i < allMoves.Count; i++)
            {
                var move = allMoves[i];
                move.score += GetOpeningMoveBonus(move.piece, move.targetPosition, isWhiteTurn);
                allMoves[i] = move;
            }
        }

        // Shuffle moves for variety
        ShuffleMoves(allMoves);

        AIMove bestMove = allMoves[0];
        int bestScore = isWhiteTurn ? -INFINITY : INFINITY;
        
        // Iterative deepening - start with depth 2 and increase
        int currentDepth = 2;
        while (currentDepth <= depth && !searchTimeExceeded)
        {
            Debug.Log($"Searching at depth {currentDepth}");
            
            AIMove currentBestMove = allMoves[0];
            int currentBestScore = isWhiteTurn ? -INFINITY : INFINITY;
            
            foreach (var move in allMoves)
            {
                if (searchTimeExceeded) break;
                
                ChessPiece[,] tempBoard = CloneBoard(board);
                ChessPiece clonedPiece = tempBoard[move.piece.currentX, move.piece.currentY];
                tempBoard[move.targetPosition.x, move.targetPosition.y] = clonedPiece;
                tempBoard[move.piece.currentX, move.piece.currentY] = null;
                clonedPiece.currentX = move.targetPosition.x;
                clonedPiece.currentY = move.targetPosition.y;

                int score = Minimax(tempBoard, currentDepth - 1, -INFINITY, INFINITY, !isWhiteTurn);
                
                if (searchTimeExceeded) break;
                
                if (isWhiteTurn)
                {
                    if (score > currentBestScore)
                    {
                        currentBestScore = score;
                        currentBestMove = move;
                    }
                }
                else
                {
                    if (score < currentBestScore)
                    {
                        currentBestScore = score;
                        currentBestMove = move;
                    }
                }
            }
            
            if (!searchTimeExceeded)
            {
                bestMove = currentBestMove;
                bestScore = currentBestScore;
                Debug.Log($"Depth {currentDepth} completed. Best move: {bestMove.piece.type} to ({bestMove.targetPosition.x}, {bestMove.targetPosition.y}) with score {bestScore}");
            }
            
            currentDepth++;
        }
        
        Debug.Log($"Final search completed. Nodes evaluated: {nodesEvaluated}. Best move: {bestMove.piece.type} to ({bestMove.targetPosition.x}, {bestMove.targetPosition.y})");
        
        return bestMove;
    }

    private void SortMoves(List<AIMove> moves)
    {
        // Calculate move priorities
        for (int i = 0; i < moves.Count; i++)
        {
            var move = moves[i];
            move.movePriority = 0;
            
            // Check escapes have highest priority
            if (move.isCheckEscape) move.movePriority += 10000;
            
            // Checks have high priority
            if (move.isCheck) move.movePriority += 5000;
            
            // Captures have medium priority
            if (move.isCapture) move.movePriority += 1000;
            
            // Add positional bonuses
            move.movePriority += GetPositionalBonus(move.piece, move.targetPosition.x, move.targetPosition.y);
            
            moves[i] = move;
        }
        
        // Sort by priority (highest first)
        moves.Sort((a, b) => b.movePriority.CompareTo(a.movePriority));
    }

    private int Minimax(ChessPiece[,] board, int depth, int alpha, int beta, bool isWhiteTurn)
    {
        nodesEvaluated++;
        
        // Check time limit
        if (Time.realtimeSinceStartup * 1000f - searchStartTime > MAX_SEARCH_TIME)
        {
            searchTimeExceeded = true;
            return EvaluateBoard(board, isWhiteTurn);
        }
        
        // Check transposition table
        ulong boardHash = GenerateBoardHash(board);
        if (transpositionTable.ContainsKey(boardHash) && depth <= 3)
        {
            return transpositionTable[boardHash];
        }
        
        if (depth == 0)
        {
            // Use quiescence search for tactical positions
            return QuiescenceSearch(board, alpha, beta, isWhiteTurn);
        }

        List<AIMove> moves = GetAllPossibleMoves(board, isWhiteTurn);
        
        if (moves.Count == 0)
            return EvaluateBoard(board, isWhiteTurn);

        // Sort moves for better alpha-beta pruning
        SortMoves(moves);

        if (isWhiteTurn)
        {
            int maxScore = -INFINITY;
            for (int i = 0; i < moves.Count; i++)
            {
                if (searchTimeExceeded) break;
                
                var move = moves[i];
                ChessPiece[,] tempBoard = CloneBoard(board);
                ChessPiece clonedPiece = tempBoard[move.piece.currentX, move.piece.currentY];
                tempBoard[move.targetPosition.x, move.targetPosition.y] = clonedPiece;
                tempBoard[move.piece.currentX, move.piece.currentY] = null;
                clonedPiece.currentX = move.targetPosition.x;
                clonedPiece.currentY = move.targetPosition.y;

                int score = Minimax(tempBoard, depth - 1, alpha, beta, false);

                maxScore = Mathf.Max(maxScore, score);
                alpha = Mathf.Max(alpha, score);
                if (alpha >= beta)
                    break;
            }
            
            // Store in transposition table
            if (!searchTimeExceeded)
                transpositionTable[boardHash] = maxScore;
                
            return maxScore;
        }
        else
        {
            int minScore = INFINITY;
            for (int i = 0; i < moves.Count; i++)
            {
                if (searchTimeExceeded) break;
                
                var move = moves[i];
                ChessPiece[,] tempBoard = CloneBoard(board);
                ChessPiece clonedPiece = tempBoard[move.piece.currentX, move.piece.currentY];
                tempBoard[move.targetPosition.x, move.targetPosition.y] = clonedPiece;
                tempBoard[move.piece.currentX, move.piece.currentY] = null;
                clonedPiece.currentX = move.targetPosition.x;
                clonedPiece.currentY = move.targetPosition.y;

                int score = Minimax(tempBoard, depth - 1, alpha, beta, true);

                minScore = Mathf.Min(minScore, score);
                beta = Mathf.Min(beta, score);
                if (alpha >= beta)
                    break;
            }
            
            // Store in transposition table
            if (!searchTimeExceeded)
                transpositionTable[boardHash] = minScore;
                
            return minScore;
        }
    }

    // Quiescence search to handle tactical sequences
    private int QuiescenceSearch(ChessPiece[,] board, int alpha, int beta, bool isWhiteTurn)
    {
        int standPat = EvaluateBoard(board, isWhiteTurn);
        
        if (isWhiteTurn)
        {
            if (standPat >= beta) return beta;
            alpha = Mathf.Max(alpha, standPat);
        }
        else
        {
            if (standPat <= alpha) return alpha;
            beta = Mathf.Min(beta, standPat);
        }
        
        // Only look at captures in quiescence search
        List<AIMove> captures = GetAllPossibleMoves(board, isWhiteTurn).Where(m => m.isCapture).ToList();
        
        if (captures.Count == 0) return standPat;
        
        // Sort captures by MVV-LVA (Most Valuable Victim - Least Valuable Attacker)
        captures.Sort((a, b) => {
            int aValue = pieceValues.ContainsKey(a.piece.type) ? pieceValues[a.piece.type] : 0;
            int bValue = pieceValues.ContainsKey(b.piece.type) ? pieceValues[b.piece.type] : 0;
            return bValue.CompareTo(aValue);
        });
        
        foreach (var capture in captures)
        {
            ChessPiece[,] tempBoard = CloneBoard(board);
            ChessPiece clonedPiece = tempBoard[capture.piece.currentX, capture.piece.currentY];
            tempBoard[capture.targetPosition.x, capture.targetPosition.y] = clonedPiece;
            tempBoard[capture.piece.currentX, capture.piece.currentY] = null;
            clonedPiece.currentX = capture.targetPosition.x;
            clonedPiece.currentY = capture.targetPosition.y;

            int score = QuiescenceSearch(tempBoard, alpha, beta, !isWhiteTurn);
            
            if (isWhiteTurn)
            {
                alpha = Mathf.Max(alpha, score);
                if (alpha >= beta) break;
            }
            else
            {
                beta = Mathf.Min(beta, score);
                if (alpha >= beta) break;
            }
        }
        
        return isWhiteTurn ? alpha : beta;
    }

    private List<AIMove> GetAllPossibleMoves(ChessPiece[,] board, bool isWhiteTurn)
    {
        List<AIMove> moves = new List<AIMove>();
        int targetTeam = isWhiteTurn ? 0 : 1;
        
        Debug.Log($"GetAllPossibleMoves: Looking for moves for team {targetTeam} ({(isWhiteTurn ? "White" : "Black")})");
        
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (board[x, y] != null)
                {
                    ChessPiece piece = board[x, y];
                    if (piece.team == targetTeam)
                    {
                        List<Vector2Int> availableMoves = piece.GetAvailableMoves(ref board, 8, 8);
                        Debug.Log($"Piece {piece.type} at ({x},{y}) has {availableMoves.Count} available moves");
                        
                        foreach (Vector2Int move in availableMoves)
                        {
                            // Check if this move doesn't put own king in check
                            if (!WouldMovePutKingInCheck(board, piece, move))
                            {
                                moves.Add(new AIMove(piece, move, 0));
                            }
                            else
                            {
                                Debug.Log($"Move ({move.x},{move.y}) for {piece.type} at ({x},{y}) would put king in check");
                            }
                        }
                    }
                }
            }
        }
        
        Debug.Log($"GetAllPossibleMoves: Found {moves.Count} legal moves for team {targetTeam}");
        return moves;
    }

    private bool WouldMovePutKingInCheck(ChessPiece[,] board, ChessPiece piece, Vector2Int targetPosition)
    {
        // Create temporary board
        ChessPiece[,] tempBoard = CloneBoard(board);
        
        // Find the cloned piece in the temp board
        ChessPiece clonedPiece = tempBoard[piece.currentX, piece.currentY];
        
        // Make the move
        tempBoard[targetPosition.x, targetPosition.y] = clonedPiece;
        tempBoard[piece.currentX, piece.currentY] = null;
        
        // Update cloned piece position
        clonedPiece.currentX = targetPosition.x;
        clonedPiece.currentY = targetPosition.y;
        
        // Find own king
        ChessPiece ownKing = null;
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (tempBoard[x, y] != null && tempBoard[x, y].type == ChessPieceType.King && tempBoard[x, y].team == piece.team)
                {
                    ownKing = tempBoard[x, y];
                    break;
                }
            }
            if (ownKing != null) break;
        }
        
        if (ownKing == null) return false;
        
        // Check if any enemy piece can attack the king
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (tempBoard[x, y] != null && tempBoard[x, y].team != piece.team)
                {
                    List<Vector2Int> enemyMoves = tempBoard[x, y].GetAvailableMoves(ref tempBoard, 8, 8);
                    if (enemyMoves.Contains(new Vector2Int(ownKing.currentX, ownKing.currentY)))
                    {
                        return true; // King would be in check
                    }
                }
            }
        }
        
        return false;
    }

    private ChessPiece[,] CloneBoard(ChessPiece[,] original)
    {
        ChessPiece[,] clone = new ChessPiece[8, 8];
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (original[x, y] != null)
                {
                    // Create a new instance of the piece with the same properties
                    ChessPiece originalPiece = original[x, y];
                    ChessPiece clonedPiece = CreatePieceCopy(originalPiece);
                    clone[x, y] = clonedPiece;
                }
                else
                {
                    clone[x, y] = null;
                }
            }
        }
        return clone;
    }

    private ChessPiece CreatePieceCopy(ChessPiece original)
    {
        // Create a new piece of the same type
        ChessPiece copy = null;
        
        switch (original.type)
        {
            case ChessPieceType.Pawn:
                copy = new Pawn();
                break;
            case ChessPieceType.Knight:
                copy = new Knight();
                break;
            case ChessPieceType.Bishop:
                copy = new Bishop();
                break;
            case ChessPieceType.Rock:
                copy = new Rock();
                break;
            case ChessPieceType.Queen:
                copy = new Queen();
                break;
            case ChessPieceType.King:
                copy = new King();
                break;
        }
        
        if (copy != null)
        {
            // Copy the essential properties
            copy.type = original.type;
            copy.team = original.team;
            copy.currentX = original.currentX;
            copy.currentY = original.currentY;
        }
        
        return copy;
    }

    private int EvaluateBoard(ChessPiece[,] board, bool isWhiteTurn)
    {
        int score = 0;
        
        // First, check if the current player's king is in check
        bool isKingInCheck = IsKingInCheck(board, isWhiteTurn);
        
        // If king is in check, this is a critical situation
        if (isKingInCheck)
        {
            // Check if there are any legal moves to get out of check
            List<AIMove> legalMoves = GetAllPossibleMoves(board, isWhiteTurn);
            
            if (legalMoves.Count == 0)
            {
                // Checkmate - this is the worst possible position
                return isWhiteTurn ? -INFINITY : INFINITY;
            }
            else
            {
                // King is in check but there are legal moves - this is very bad but not hopeless
                score += isWhiteTurn ? -5000 : 5000;
            }
        }
        
        // Enhanced evaluation with positional understanding
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (board[x, y] != null)
                {
                    ChessPiece piece = board[x, y];
                    int pieceValue = pieceValues.ContainsKey(piece.type) ? pieceValues[piece.type] : 0;
                    int positionalBonus = GetPositionalBonus(piece, x, y);
                    int totalValue = pieceValue + positionalBonus;
                    
                    if (piece.team == 0) // White
                        score += totalValue;
                    else // Black
                        score -= totalValue;
                }
            }
        }
        
        // Add king safety evaluation
        score += EvaluateKingSafety(board, isWhiteTurn);
        
        // Add pawn structure evaluation
        score += EvaluatePawnStructure(board, isWhiteTurn);
        
        // Add piece mobility evaluation
        score += EvaluateMobility(board, isWhiteTurn);
        
        // Add center control evaluation
        score += EvaluateCenterControl(board, isWhiteTurn);
        
        // Add passed pawn evaluation
        score += EvaluatePassedPawns(board, isWhiteTurn);
        
        // Add threat detection
        score += EvaluateThreats(board, isWhiteTurn);
        
        // Add material imbalance adjustments
        score += EvaluateMaterialImbalance(board, isWhiteTurn);
        
        // Add endgame-specific evaluation
        score += EvaluateEndgame(board, isWhiteTurn);
        
        return score;
    }

    private int GetPositionalBonus(ChessPiece piece, int x, int y)
    {
        int bonus = 0;
        int tableY = piece.team == 0 ? y : 7 - y; // Flip table for black pieces
        
        switch (piece.type)
        {
            case ChessPieceType.Pawn:
                bonus = pawnPositionTable[tableY, x];
                break;
            case ChessPieceType.Knight:
                bonus = knightPositionTable[tableY, x];
                break;
            case ChessPieceType.Bishop:
                bonus = bishopPositionTable[tableY, x];
                break;
            case ChessPieceType.Rock:
                bonus = rookPositionTable[tableY, x];
                break;
            case ChessPieceType.Queen:
                bonus = queenPositionTable[tableY, x];
                break;
            case ChessPieceType.King:
                bonus = kingPositionTable[tableY, x];
                break;
        }
        
        return bonus;
    }

    private int EvaluatePawnStructure(ChessPiece[,] board, bool isWhiteTurn)
    {
        int score = 0;
        int targetTeam = isWhiteTurn ? 0 : 1;
        
        // Count doubled pawns (penalty)
        for (int x = 0; x < 8; x++)
        {
            int pawnCount = 0;
            for (int y = 0; y < 8; y++)
            {
                if (board[x, y] != null && board[x, y].type == ChessPieceType.Pawn && board[x, y].team == targetTeam)
                {
                    pawnCount++;
                }
            }
            if (pawnCount > 1)
            {
                score -= (pawnCount - 1) * 20; // Penalty for doubled pawns
            }
        }
        
        // Bonus for isolated pawns (penalty)
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (board[x, y] != null && board[x, y].type == ChessPieceType.Pawn && board[x, y].team == targetTeam)
                {
                    bool hasAdjacentPawn = false;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int checkX = x + dx;
                        if (checkX >= 0 && checkX < 8 && checkX != x)
                        {
                            for (int checkY = 0; checkY < 8; checkY++)
                            {
                                if (board[checkX, checkY] != null && board[checkX, checkY].type == ChessPieceType.Pawn && board[checkX, checkY].team == targetTeam)
                                {
                                    hasAdjacentPawn = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (!hasAdjacentPawn)
                    {
                        score -= 30; // Penalty for isolated pawn
                    }
                }
            }
        }
        
        return score;
    }

    private int EvaluateMobility(ChessPiece[,] board, bool isWhiteTurn)
    {
        int score = 0;
        int targetTeam = isWhiteTurn ? 0 : 1;
        
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (board[x, y] != null && board[x, y].team == targetTeam)
                {
                    List<Vector2Int> moves = board[x, y].GetAvailableMoves(ref board, 8, 8);
                    score += moves.Count * 5; // Bonus for each legal move
                }
            }
        }
        
        return score;
    }

    private int EvaluateCenterControl(ChessPiece[,] board, bool isWhiteTurn)
    {
        int score = 0;
        int targetTeam = isWhiteTurn ? 0 : 1;
        
        // Bonus for controlling center squares
        for (int x = 3; x <= 4; x++)
        {
            for (int y = 3; y <= 4; y++)
            {
                if (board[x, y] != null && board[x, y].team == targetTeam)
                {
                    score += 20; // Bonus for center control
                }
            }
        }
        
        return score;
    }

    private bool IsKingInCheck(ChessPiece[,] board, bool isWhiteTurn)
    {
        // Find the king - when isWhiteTurn is true, we're looking for the white king (team 0)
        // when isWhiteTurn is false, we're looking for the black king (team 1)
        ChessPiece king = null;
        int targetTeam = isWhiteTurn ? 0 : 1;
        
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (board[x, y] != null && board[x, y].type == ChessPieceType.King && board[x, y].team == targetTeam)
                {
                    king = board[x, y];
                    break;
                }
            }
            if (king != null) break;
        }
        
        if (king == null) 
        {
            Debug.LogWarning($"IsKingInCheck: Could not find king for team {targetTeam} (isWhiteTurn: {isWhiteTurn})");
            return false;
        }
        
        // Check if any enemy piece can attack the king
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (board[x, y] != null && board[x, y].team != king.team)
                {
                    List<Vector2Int> enemyMoves = board[x, y].GetAvailableMoves(ref board, 8, 8);
                    if (enemyMoves.Contains(new Vector2Int(king.currentX, king.currentY)))
                    {
                        Debug.Log($"King in check! Enemy piece at ({x},{y}) can attack king at ({king.currentX},{king.currentY})");
                        return true; // King is in check
                    }
                }
            }
        }
        
        return false;
    }

    private int EvaluateKingSafety(ChessPiece[,] board, bool isWhiteTurn)
    {
        int score = 0;
        
        // Find the king - when isWhiteTurn is true, we're looking for the white king (team 0)
        // when isWhiteTurn is false, we're looking for the black king (team 1)
        ChessPiece king = null;
        int targetTeam = isWhiteTurn ? 0 : 1;
        
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (board[x, y] != null && board[x, y].type == ChessPieceType.King && board[x, y].team == targetTeam)
                {
                    king = board[x, y];
                    break;
                }
            }
            if (king != null) break;
        }
        
        if (king == null) return 0;
        
        // Count how many enemy pieces are attacking squares around the king
        int attackingPieces = 0;
        int defendingPieces = 0;
        
        // Check the 8 squares around the king
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue; // Skip the king's own square
                
                int checkX = king.currentX + dx;
                int checkY = king.currentY + dy;
                
                if (checkX >= 0 && checkX < 8 && checkY >= 0 && checkY < 8)
                {
                    // Count enemy pieces that can attack this square
                    for (int x = 0; x < 8; x++)
                    {
                        for (int y = 0; y < 8; y++)
                        {
                            if (board[x, y] != null && board[x, y].team != king.team)
                            {
                                List<Vector2Int> enemyMoves = board[x, y].GetAvailableMoves(ref board, 8, 8);
                                if (enemyMoves.Contains(new Vector2Int(checkX, checkY)))
                                {
                                    attackingPieces++;
                                }
                            }
                            else if (board[x, y] != null && board[x, y].team == king.team)
                            {
                                List<Vector2Int> friendlyMoves = board[x, y].GetAvailableMoves(ref board, 8, 8);
                                if (friendlyMoves.Contains(new Vector2Int(checkX, checkY)))
                                {
                                    defendingPieces++;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        // Penalize positions where the king is under attack
        score -= attackingPieces * 50;
        score += defendingPieces * 30;
        
        // Bonus for king in the corner (safer in endgame)
        if (king.currentX <= 1 || king.currentX >= 6)
        {
            if (king.currentY <= 1 || king.currentY >= 6)
            {
                score += 20;
            }
        }
        
        return score;
    }

    private int GetOpeningMoveBonus(ChessPiece piece, Vector2Int targetPosition, bool isWhiteTurn)
    {
        switch (openingStrategy)
        {
            case 0: return GetStandardOpeningBonus(piece, targetPosition, isWhiteTurn);
            case 1: return GetAggressiveOpeningBonus(piece, targetPosition, isWhiteTurn);
            case 2: return GetDefensiveOpeningBonus(piece, targetPosition, isWhiteTurn);
            case 3: return GetUnconventionalOpeningBonus(piece, targetPosition, isWhiteTurn);
            default: return 0;
        }
    }

    private int GetStandardOpeningBonus(ChessPiece piece, Vector2Int targetPosition, bool isWhiteTurn)
    {
        int bonus = 0;
        
        // Standard opening principles
        if (piece.type == ChessPieceType.Pawn)
        {
            // Encourage pawn development
            if (isWhiteTurn && targetPosition.y > piece.currentY)
                bonus += 10;
            else if (!isWhiteTurn && targetPosition.y < piece.currentY)
                bonus += 10;
                
            // Bonus for center pawn moves
            if (targetPosition.x >= 3 && targetPosition.x <= 4)
                bonus += 15;
        }
        else if (piece.type == ChessPieceType.Knight || piece.type == ChessPieceType.Bishop)
        {
            // Encourage piece development
            if (isWhiteTurn && targetPosition.y > 1)
                bonus += 20;
            else if (!isWhiteTurn && targetPosition.y < 6)
                bonus += 20;
        }
        
        return bonus;
    }

    private int GetAggressiveOpeningBonus(ChessPiece piece, Vector2Int targetPosition, bool isWhiteTurn)
    {
        int bonus = 0;
        
        // Aggressive opening - prioritize attacking moves
        if (piece.type == ChessPieceType.Pawn)
        {
            // Encourage pawn advances
            if (isWhiteTurn && targetPosition.y > piece.currentY)
                bonus += 20;
            else if (!isWhiteTurn && targetPosition.y < piece.currentY)
                bonus += 20;
        }
        else if (piece.type == ChessPieceType.Knight || piece.type == ChessPieceType.Bishop)
        {
            // Encourage attacking piece placement
            if (isWhiteTurn && targetPosition.y > 2)
                bonus += 30;
            else if (!isWhiteTurn && targetPosition.y < 5)
                bonus += 30;
        }
        
        return bonus;
    }

    private int GetDefensiveOpeningBonus(ChessPiece piece, Vector2Int targetPosition, bool isWhiteTurn)
    {
        int bonus = 0;
        
        // Defensive opening - prioritize safety
        if (piece.type == ChessPieceType.King)
        {
            // Encourage castling
            if (targetPosition.x == 6 || targetPosition.x == 2)
                bonus += 50;
        }
        else if (piece.type == ChessPieceType.Pawn)
        {
            // Keep pawns connected
            bonus += 10;
        }
        
        return bonus;
    }

    private int GetUnconventionalOpeningBonus(ChessPiece piece, Vector2Int targetPosition, bool isWhiteTurn)
    {
        int bonus = 0;
        
        // Unconventional opening - encourage unusual moves
        if (piece.type == ChessPieceType.Pawn)
        {
            // Encourage flank pawn moves
            if (targetPosition.x <= 1 || targetPosition.x >= 6)
                bonus += 25;
        }
        else if (piece.type == ChessPieceType.Knight)
        {
            // Encourage knight to the edge
            if (targetPosition.x <= 1 || targetPosition.x >= 6)
                bonus += 20;
        }
        
        return bonus;
    }

    private void ShuffleMoves(List<AIMove> moves)
    {
        // Fisher-Yates shuffle for move variety
        for (int i = moves.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            AIMove temp = moves[i];
            moves[i] = moves[j];
            moves[j] = temp;
        }
    }

    // Evaluate passed pawns (pawns that can't be stopped by enemy pawns)
    private int EvaluatePassedPawns(ChessPiece[,] board, bool isWhiteTurn)
    {
        int score = 0;
        int targetTeam = isWhiteTurn ? 0 : 1;
        int enemyTeam = isWhiteTurn ? 1 : 0;
        
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (board[x, y] != null && board[x, y].type == ChessPieceType.Pawn && board[x, y].team == targetTeam)
                {
                    bool isPassed = true;
                    
                    // Check if there are enemy pawns that can block this pawn
                    for (int checkX = Mathf.Max(0, x - 1); checkX <= Mathf.Min(7, x + 1); checkX++)
                    {
                        if (checkX == x) continue;
                        
                        for (int checkY = 0; checkY < 8; checkY++)
                        {
                            if (board[checkX, checkY] != null && board[checkX, checkY].type == ChessPieceType.Pawn && board[checkX, checkY].team == enemyTeam)
                            {
                                // If enemy pawn is ahead, this is not a passed pawn
                                if ((isWhiteTurn && checkY > y) || (!isWhiteTurn && checkY < y))
                                {
                                    isPassed = false;
                                    break;
                                }
                            }
                        }
                        if (!isPassed) break;
                    }
                    
                    if (isPassed)
                    {
                        // Bonus for passed pawns, more for advanced ones
                        int advancement = isWhiteTurn ? y : (7 - y);
                        score += advancement * 20;
                    }
                }
            }
        }
        
        return score;
    }

    // Evaluate threats (forks, pins, skewers)
    private int EvaluateThreats(ChessPiece[,] board, bool isWhiteTurn)
    {
        int score = 0;
        int targetTeam = isWhiteTurn ? 0 : 1;
        int enemyTeam = isWhiteTurn ? 1 : 0;
        
        // Look for pieces that attack multiple valuable targets
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (board[x, y] != null && board[x, y].team == targetTeam)
                {
                    ChessPiece piece = board[x, y];
                    List<Vector2Int> attacks = piece.GetAvailableMoves(ref board, 8, 8);
                    
                    int valuableTargets = 0;
                    foreach (var attack in attacks)
                    {
                        if (board[attack.x, attack.y] != null && board[attack.x, attack.y].team == enemyTeam)
                        {
                            int targetValue = pieceValues.ContainsKey(board[attack.x, attack.y].type) ? pieceValues[board[attack.x, attack.y].type] : 0;
                            if (targetValue > pieceValues[piece.type])
                            {
                                valuableTargets++;
                            }
                        }
                    }
                    
                    // Bonus for attacking multiple valuable targets (potential fork)
                    if (valuableTargets > 1)
                    {
                        score += valuableTargets * 50;
                    }
                }
            }
        }
        
        return score;
    }

    // Evaluate material imbalance and adjust evaluation accordingly
    private int EvaluateMaterialImbalance(ChessPiece[,] board, bool isWhiteTurn)
    {
        int whiteMaterial = 0;
        int blackMaterial = 0;
        
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (board[x, y] != null)
                {
                    int pieceValue = pieceValues.ContainsKey(board[x, y].type) ? pieceValues[board[x, y].type] : 0;
                    if (board[x, y].team == 0) // White
                        whiteMaterial += pieceValue;
                    else // Black
                        blackMaterial += pieceValue;
                }
            }
        }
        
        int materialDifference = whiteMaterial - blackMaterial;
        
        // Adjust evaluation based on material imbalance
        if (isWhiteTurn)
        {
            if (materialDifference > 200) // White is ahead
                return 100; // Encourage simplification
            else if (materialDifference < -200) // Black is ahead
                return -100; // Encourage complications
        }
        else
        {
            if (materialDifference > 200) // White is ahead
                return -100; // Encourage complications
            else if (materialDifference < -200) // Black is ahead
                return 100; // Encourage simplification
        }
        
        return 0;
    }

    // Evaluate endgame-specific factors
    private int EvaluateEndgame(ChessPiece[,] board, bool isWhiteTurn)
    {
        int score = 0;
        int targetTeam = isWhiteTurn ? 0 : 1;
        
        // Count remaining pieces to determine if we're in endgame
        int totalPieces = 0;
        int pawns = 0;
        int queens = 0;
        
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (board[x, y] != null)
                {
                    totalPieces++;
                    if (board[x, y].type == ChessPieceType.Pawn) pawns++;
                    if (board[x, y].type == ChessPieceType.Queen) queens++;
                }
            }
        }
        
        // If we have few pieces, we're in endgame
        if (totalPieces <= 12)
        {
            // In endgame, king activity becomes more important
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    if (board[x, y] != null && board[x, y].type == ChessPieceType.King && board[x, y].team == targetTeam)
                    {
                        // Use endgame king table
                        int tableY = targetTeam == 0 ? y : 7 - y;
                        score += endgameKingPositionTable[tableY, x];
                        
                        // Bonus for king centralization in endgame
                        if (x >= 3 && x <= 4 && y >= 3 && y <= 4)
                            score += 30;
                    }
                }
            }
            
            // Bonus for pawn advancement in endgame
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    if (board[x, y] != null && board[x, y].type == ChessPieceType.Pawn && board[x, y].team == targetTeam)
                    {
                        int advancement = targetTeam == 0 ? y : (7 - y);
                        score += advancement * 10;
                    }
                }
            }
        }
        
        return score;
    }
} 