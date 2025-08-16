using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChessAI : MonoBehaviour
{
    // === Settings ===
    private const int MAX_DEPTH          = 6;
    private const int INFINITY           = 1_000_000;
    private const int MAX_SEARCH_TIME_MS = 5000;
    private const int ASPIRATION         = 60;
    private const int LMR_THRESHOLD      = 6;

    // Pruning margins
    private const int RAZOR_MARGIN    = 225; // shallow razoring
    private const int FUTILITY_MARGIN = 90;  // shallow futility

    // Small “anti-draw” bias to discourage aimless shuffling
    private const int CONTEMPT = 8; // centipawns

    // === Search state ===
    private int   nodes = 0;
    private float startMs;
    private bool  timeUp = false;

    // Variety
    [SerializeField] private bool smallVariety = true;

    // === Zobrist ===
    private static ulong[,,] zobrist = new ulong[8,8,12];
    private static ulong zobristSideToMove;
    private static bool zobristInit = false;
    private static System.Random rng64 = new System.Random(20250816);

    // === Transposition table ===
    private enum TTFlag { Exact, LowerBound, UpperBound }
    private struct TTEntry
    {
        public int depth;
        public int value;
        public TTFlag flag;
        public bool sideToMove;
        public PackedMove best;
    }
    private Dictionary<ulong, TTEntry> tt = new Dictionary<ulong, TTEntry>(1<<20);

    // === Move ordering helpers ===
    private PackedMove[,] killer = new PackedMove[64, 2];
    private int[,] history = new int[64,64];

    // === Values ===
    private static readonly Dictionary<ChessPieceType, int> val = new Dictionary<ChessPieceType, int>
    {
        { ChessPieceType.Pawn,   100 },
        { ChessPieceType.Knight, 320 },
        { ChessPieceType.Bishop, 330 },
        { ChessPieceType.Rock,   500 }, // rook (enum name kept)
        { ChessPieceType.Queen,  900 },
        { ChessPieceType.King,   20000 }
    };

    // === Piece-square tables (MG) ===
    private static readonly int[,] PST_P = {
        { 0,0,0,0,0,0,0,0},
        {50,50,50,50,50,50,50,50},
        {10,10,20,30,30,20,10,10},
        { 5, 5,10,25,25,10, 5, 5},
        { 0, 0, 0,20,20, 0, 0, 0},
        { 5,-5,-10, 0, 0,-10,-5, 5},
        { 5,10,10,-20,-20,10,10, 5},
        { 0, 0, 0, 0, 0, 0, 0, 0}
    };
    private static readonly int[,] PST_N = {
        {-50,-40,-30,-30,-30,-30,-40,-50},
        {-40,-20,  0,  0,  0,  0,-20,-40},
        {-30,  0, 10, 15, 15, 10,  0,-30},
        {-30,  5, 15, 20, 20, 15,  5,-30},
        {-30,  0, 15, 20, 20, 15,  0,-30},
        {-30,  5, 10, 15, 15, 10,  5,-30},
        {-40,-20,  0,  5,  5,  0,-20,-40},
        {-50,-40,-30,-30,-30,-30,-40,-50}
    };
    private static readonly int[,] PST_B = {
        {-20,-10,-10,-10,-10,-10,-10,-20},
        {-10,  0,  0,  0,  0,  0,  0,-10},
        {-10,  0,  5, 10, 10,  5,  0,-10},
        {-10,  5,  5, 10, 10,  5,  5,-10},
        {-10,  0, 10, 10, 10, 10,  0,-10},
        {-10, 10, 10, 10, 10, 10, 10,-10},
        {-10,  5,  0,  0,  0,  0,  5,-10},
        {-20,-10,-10,-10,-10,-10,-10,-20}
    };
    private static readonly int[,] PST_R = {
        { 0, 0, 0, 0, 0, 0, 0, 0},
        { 5,10,10,10,10,10,10, 5},
        {-5, 0, 0, 0, 0, 0, 0,-5},
        {-5, 0, 0, 0, 0, 0, 0,-5},
        {-5, 0, 0, 0, 0, 0, 0,-5},
        {-5, 0, 0, 0, 0, 0, 0,-5},
        {-5, 0, 0, 0, 0, 0, 0,-5},
        { 0, 0, 0, 5, 5, 0, 0, 0}
    };
    private static readonly int[,] PST_Q = {
        {-20,-10,-10,-5,-5,-10,-10,-20},
        {-10,  0,  0, 0, 0,  0,  0,-10},
        {-10,  0,  5, 5, 5,  5,  0,-10},
        { -5,  0,  5, 5, 5,  5,  0, -5},
        {  0,  0,  5, 5, 5,  5,  0, -5},
        {-10,  5,  5, 5, 5,  5,  0,-10},
        {-10,  0,  5, 0, 0,  0,  0,-10},
        {-20,-10,-10,-5,-5,-10,-10,-20}
    };
    private static readonly int[,] PST_K_MG = {
        {-30,-40,-40,-50,-50,-40,-40,-30},
        {-30,-40,-40,-50,-50,-40,-40,-30},
        {-30,-40,-40,-50,-50,-40,-40,-30},
        {-30,-40,-40,-50,-50,-40,-40,-30},
        {-20,-30,-30,-40,-40,-30,-30,-20},
        {-10,-20,-20,-20,-20,-20,-20,-10},
        { 20, 20,  0,  0,  0,  0, 20, 20},
        { 20, 30, 10,  0,  0, 10, 30, 20}
    };
    private static readonly int[,] PST_K_EG = {
        {-50,-40,-30,-20,-20,-30,-40,-50},
        {-30,-20,-10,  0,  0,-10,-20,-30},
        {-30,-10, 20, 30, 30, 20,-10,-30},
        {-30,-10, 30, 40, 40, 30,-10,-30},
        {-30,-10, 30, 40, 40, 30,-10,-30},
        {-30,-10, 20, 30, 30, 20,-10,-30},
        {-30,-30,  0,  0,  0,  0,-30,-30},
        {-50,-30,-30,-30,-30,-30,-30,-50}
    };

    // cache for tapered eval
    private int currentPhase = 24;

    // === Public AI move (matches your existing use) ===
    public struct AIMove
    {
        public ChessPiece piece;
        public Vector2Int targetPosition;
        public int score;
    }

    // Internal packed move (no Unity refs)
    private struct PackedMove
    {
        public int fx, fy, tx, ty;
        public PackedMove(int fx, int fy, int tx, int ty) { this.fx = fx; this.fy = fy; this.tx = tx; this.ty = ty; }
        public bool Equals(PackedMove o) => fx==o.fx && fy==o.fy && tx==o.tx && ty==o.ty;
        public bool IsNull => fx == -1;
        public static PackedMove Null => new PackedMove(-1,-1,-1,-1);
    }

    // === Public entrypoint ===
    public AIMove FindBestMove(ChessPiece[,] board, bool isWhiteTurn, int maxDepth = MAX_DEPTH)
    {
        InitZobrist();
        nodes   = 0;
        timeUp  = false;
        startMs = Time.realtimeSinceStartup * 1000f;
        tt.Clear();
        Array.Clear(killer, 0, killer.Length);
        Array.Clear(history, 0, history.Length);

        var legalRoot = GenerateLegalMoves(board, isWhiteTurn);
        if (legalRoot.Count == 0)
            return new AIMove { piece = null, targetPosition = Vector2Int.zero, score = -INFINITY };

        if (smallVariety) UnityEngine.Random.InitState(Environment.TickCount);

        int prevScore = 0;
        PackedMove bestPacked = legalRoot[0];
        ulong rootKey = Hash(board, isWhiteTurn);

        // repetition stack (contains position keys along the PV we’re exploring)
        var rep = new List<ulong>(128);
        rep.Add(rootKey);

        for (int depth = 1; depth <= Math.Max(2, maxDepth); depth++)
        {
            int alpha = prevScore - ASPIRATION;
            int beta  = prevScore + ASPIRATION;
            int score;

            while (true)
            {
                score = PVS(board, depth, alpha, beta, isWhiteTurn ? 1 : -1, 0, rep);
                if (timeExceeded()) break;

                if (score <= alpha && (alpha > -INFINITY/2)) { alpha = -INFINITY; continue; }
                if (score >= beta  && (beta  <  INFINITY/2)) { beta  =  INFINITY; continue; }
                break;
            }
            if (timeExceeded()) break;

            prevScore = score;

            if (tt.TryGetValue(rootKey, out var rootHit) && !rootHit.best.IsNull)
                bestPacked = rootHit.best;

            if ((Time.realtimeSinceStartup * 1000f - startMs) > MAX_SEARCH_TIME_MS * 0.75f)
                break;
        }

        var pieceRef = board[bestPacked.fx, bestPacked.fy];
        return new AIMove
        {
            piece = pieceRef,
            targetPosition = new Vector2Int(bestPacked.tx, bestPacked.ty),
            score = prevScore
        };
    }

    // === Core search with repetition handling ===
    private int PVS(ChessPiece[,] board, int depth, int alpha, int beta, int color, int ply, List<ulong> rep)
    {
        nodes++;
        if (timeExceeded()) return color * Evaluate(board);

        // Repetition draw: if current key repeats in the path, score = small draw with contempt
        ulong keyHere = Hash(board, color == 1);
        int repCount = 0;
        for (int i = rep.Count - 1; i >= 0 && i >= rep.Count - 128; i--)
            if (rep[i] == keyHere) { repCount++; if (repCount >= 2) break; } // seen this position before
        if (repCount >= 2)
            return (color == 1 ? -CONTEMPT : CONTEMPT); // slight bias to avoid shuffling

        int sideTeam = (color == 1) ? 0 : 1;
        bool inCheck = IsKingInCheck(board, sideTeam);
        int staticEval = color * Evaluate(board);

        // Razoring
        if (!inCheck && depth == 1 && staticEval + RAZOR_MARGIN <= alpha)
            return Quiescence(board, alpha, beta, color, rep);

        // TT probe
        PackedMove ttBest = PackedMove.Null;
        if (tt.TryGetValue(keyHere, out var entry) && entry.sideToMove == (color==1) && entry.depth >= depth)
        {
            if (entry.flag == TTFlag.Exact) return entry.value;
            if (entry.flag == TTFlag.LowerBound) alpha = Math.Max(alpha, entry.value);
            else if (entry.flag == TTFlag.UpperBound) beta = Math.Min(beta, entry.value);
            if (alpha >= beta) return entry.value;
            ttBest = entry.best;
        }

        // Null-move pruning
        if (depth >= 3 && !inCheck && !IsEndgameish(board))
        {
            int R = NullMoveReduction(depth);
            // null-move flips side to move; push that key
            ulong nullKey = Hash(board, color != 1);
            rep.Add(nullKey);
            int nullScore = -PVS(board, depth - 1 - R, -beta, -beta + 1, -color, ply + 1, rep);
            rep.RemoveAt(rep.Count - 1);

            if (nullScore >= beta)
            {
                StoreTT(keyHere, depth, nullScore, TTFlag.LowerBound, (color==1), PackedMove.Null);
                return nullScore;
            }
        }

        if (depth <= 0)
            return Quiescence(board, alpha, beta, color, rep);

        var moves = GenerateLegalMoves(board, color == 1);
        if (moves.Count == 0)
            return color * Evaluate(board);

        OrderMoves(board, moves, ttBest, ply);

        int bestVal = -INFINITY;
        PackedMove bestMove = PackedMove.Null;
        bool first = true;
        int moveIndex = 0;

        foreach (var m in moves)
        {
            bool isCapture = (board[m.tx, m.ty] != null && board[m.tx, m.ty].team != board[m.fx, m.fy].team);

            // Futility (quiet) at shallow depth
            if (!isCapture && !inCheck && depth <= 2 && staticEval + FUTILITY_MARGIN * depth <= alpha)
            {
                moveIndex++;
                continue;
            }

            var next = Apply(board, m);
            int newDepth = depth - 1;

            // Check extension
            bool givesCheck = GivesCheck(next, (color == 1) ? 0 : 1);
            if (givesCheck && newDepth < depth) newDepth++;

            // LMR
            if (!isCapture && !givesCheck && depth >= 3 && moveIndex >= LMR_THRESHOLD)
                newDepth = Math.Max(0, newDepth - 1);

            // Push child key for repetition tracking
            ulong childKey = Hash(next, color != 1);
            rep.Add(childKey);

            int score;
            if (first)
            {
                score = -PVS(next, newDepth, -beta, -alpha, -color, ply + 1, rep);
                first = false;
            }
            else
            {
                score = -PVS(next, newDepth, -alpha - 1, -alpha, -color, ply + 1, rep);
                if (score > alpha && score < beta)
                    score = -PVS(next, newDepth, -beta, -alpha, -color, ply + 1, rep);
            }

            rep.RemoveAt(rep.Count - 1);

            if (score > bestVal) { bestVal = score; bestMove = m; }
            if (score > alpha)
            {
                alpha = score;
                if (!isCapture)
                {
                    if (!killer[ply,0].Equals(m)) { killer[ply,1] = killer[ply,0]; killer[ply,0] = m; }
                    int from = m.fx*8 + m.fy, to = m.tx*8 + m.ty;
                    history[from, to] += depth * depth;
                }
            }
            if (alpha >= beta)
            {
                if (!isCapture)
                {
                    if (!killer[ply,0].Equals(m)) { killer[ply,1] = killer[ply,0]; killer[ply,0] = m; }
                    int from = m.fx*8 + m.fy, to = m.tx*8 + m.ty;
                    history[from, to] += depth * depth;
                }
                StoreTT(keyHere, depth, bestVal, TTFlag.LowerBound, (color==1), bestMove);
                return bestVal;
            }
            moveIndex++;
        }

        var flag = (bestVal <= alpha) ? TTFlag.UpperBound : TTFlag.Exact;
        StoreTT(keyHere, depth, bestVal, flag, (color==1), bestMove);
        return bestVal;
    }

    private int Quiescence(ChessPiece[,] board, int alpha, int beta, int color, List<ulong> rep)
    {
        if (timeExceeded()) return color * Evaluate(board);

        // repetition check in quiescence too
        ulong keyHere = Hash(board, color == 1);
        if (rep.Contains(keyHere))
            return (color == 1 ? -CONTEMPT : CONTEMPT);

        int standPat = color * Evaluate(board);
        if (standPat >= beta) return beta;
        if (alpha < standPat) alpha = standPat;

        var caps = GenerateCaptures(board, color == 1);
        OrderCapturesMVVLVA(board, caps);

        foreach (var m in caps)
        {
            if (IsClearlyLosingCapture(board, m)) continue;

            var next = Apply(board, m);
            ulong ck = Hash(next, color != 1);
            rep.Add(ck);
            int score = -Quiescence(next, -beta, -alpha, -color, rep);
            rep.RemoveAt(rep.Count - 1);

            if (score >= beta) return beta;
            if (score > alpha) alpha = score;
        }
        return alpha;
    }

    // === Robust attack detection (fixes king "suicide") ===
    private bool IsKingInCheck(ChessPiece[,] board, int teamOfKing)
    {
        // locate king
        for (int x=0;x<8;x++)
        for (int y=0;y<8;y++)
        {
            var p = board[x,y];
            if (p != null && p.type == ChessPieceType.King && p.team == teamOfKing)
                return SquareAttackedByTeam(board, x, y, teamOfKing ^ 1);
        }
        return false; // should not happen in a legal game
    }

    private bool SquareAttackedByTeam(ChessPiece[,] board, int x, int y, int byTeam)
    {
        // Pawns: white(team 0) attacks +1, black(team 1) attacks -1
        int dir = (byTeam == 0) ? 1 : -1;
        if (Inside(x-1, y+dir) && board[x-1, y+dir] != null && board[x-1, y+dir].team == byTeam && board[x-1, y+dir].type == ChessPieceType.Pawn) return true;
        if (Inside(x+1, y+dir) && board[x+1, y+dir] != null && board[x+1, y+dir].team == byTeam && board[x+1, y+dir].type == ChessPieceType.Pawn) return true;

        // Knights
        int[,] K = {{1,2},{2,1},{-1,2},{-2,1},{1,-2},{2,-1},{-1,-2},{-2,-1}};
        for (int i=0;i<8;i++)
        {
            int nx = x + K[i,0], ny = y + K[i,1];
            if (Inside(nx,ny))
            {
                var p = board[nx,ny];
                if (p != null && p.team == byTeam && p.type == ChessPieceType.Knight) return true;
            }
        }

        // King adjacent
        for (int dx=-1; dx<=1; dx++)
        for (int dy=-1; dy<=1; dy++)
        {
            if (dx==0 && dy==0) continue;
            int nx = x+dx, ny = y+dy;
            if (Inside(nx,ny))
            {
                var p = board[nx,ny];
                if (p != null && p.team == byTeam && p.type == ChessPieceType.King) return true;
            }
        }

        // Bishop/Queen diagonals
        int[,] diag = {{1,1},{1,-1},{-1,1},{-1,-1}};
        for (int d=0; d<4; d++)
        {
            int dx = diag[d,0], dy = diag[d,1];
            int nx = x+dx, ny = y+dy;
            while (Inside(nx,ny))
            {
                var p = board[nx,ny];
                if (p != null)
                {
                    if (p.team == byTeam && (p.type == ChessPieceType.Bishop || p.type == ChessPieceType.Queen)) return true;
                    break; // blocked
                }
                nx += dx; ny += dy;
            }
        }

        // Rook/Queen orthogonals
        int[,] ortho = {{1,0},{-1,0},{0,1},{0,-1}};
        for (int d=0; d<4; d++)
        {
            int dx = ortho[d,0], dy = ortho[d,1];
            int nx = x+dx, ny = y+dy;
            while (Inside(nx,ny))
            {
                var p = board[nx,ny];
                if (p != null)
                {
                    if (p.team == byTeam && (p.type == ChessPieceType.Rock || p.type == ChessPieceType.Queen)) return true;
                    break; // blocked
                }
                nx += dx; ny += dy;
            }
        }

        return false;
    }

    private bool Inside(int x, int y) => x>=0 && x<8 && y>=0 && y<8;

    private bool GivesCheck(ChessPiece[,] board, int defenderTeam)
    {
        // If defender king square is attacked by the mover after the move
        for (int x=0;x<8;x++)
        for (int y=0;y<8;y++)
        {
            var p = board[x,y];
            if (p != null && p.type == ChessPieceType.King && p.team == defenderTeam)
                return SquareAttackedByTeam(board, p.currentX, p.currentY, defenderTeam ^ 1);
        }
        return false;
    }

    // === Move gen & helpers ===
    private List<PackedMove> GenerateLegalMoves(ChessPiece[,] board, bool isWhiteTurn)
    {
        int side = isWhiteTurn ? 0 : 1;
        var list = new List<PackedMove>(64);

        for (int x=0;x<8;x++)
        for (int y=0;y<8;y++)
        {
            var p = board[x,y];
            if (p == null || p.team != side) continue;

            var avail = p.GetAvailableMoves(ref board, 8, 8);
            foreach (var mv in avail)
            {
                if (WouldLeaveKingInCheck(board, p, mv)) continue;
                list.Add(new PackedMove(x, y, mv.x, mv.y));
            }
        }
        return list;
    }

    private List<PackedMove> GenerateCaptures(ChessPiece[,] board, bool isWhiteTurn)
    {
        int side = isWhiteTurn ? 0 : 1;
        var list = new List<PackedMove>(64);

        for (int x=0;x<8;x++)
        for (int y=0;y<8;y++)
        {
            var p = board[x,y];
            if (p == null || p.team != side) continue;

            var avail = p.GetAvailableMoves(ref board, 8, 8);
            foreach (var mv in avail)
            {
                var t = board[mv.x, mv.y];
                if (t != null && t.team != side)
                {
                    if (WouldLeaveKingInCheck(board, p, mv)) continue;
                    list.Add(new PackedMove(x, y, mv.x, mv.y));
                }
            }
        }
        return list;
    }

    private void OrderMoves(ChessPiece[,] board, List<PackedMove> moves, PackedMove ttMove, int ply)
    {
        moves.Sort((a,b) =>
        {
            int sa = ScoreMove(board, a, ttMove, ply);
            int sb = ScoreMove(board, b, ttMove, ply);
            return sb.CompareTo(sa);
        });
    }

    private int ScoreMove(ChessPiece[,] board, PackedMove m, PackedMove ttMove, int ply)
    {
        int score = 0;

        if (!ttMove.IsNull && ttMove.Equals(m))
            score += 1_000_000;

        var to   = board[m.tx, m.ty];
        var from = board[m.fx, m.fy];

        if (to != null && to.team != from.team)
        {
            int victim = val[to.type];
            int att    = val[from.type];
            score += 500_000 + (victim * 10 - att);
        }

        if (killer[ply,0].Equals(m)) score += 300_000;
        else if (killer[ply,1].Equals(m)) score += 290_000;

        int fromSq = m.fx*8 + m.fy, toSq = m.tx*8 + m.ty;
        score += history[fromSq, toSq];

        // small PST bump for ordering only (uses last currentPhase; it's fine)
        score += PositionalBonus(from, m.tx, m.ty) - PositionalBonus(from, m.fx, m.fy);

        return score;
    }

    private void OrderCapturesMVVLVA(ChessPiece[,] board, List<PackedMove> caps)
    {
        caps.Sort((a,b) =>
        {
            var av = board[a.tx, a.ty] != null ? val[board[a.tx,a.ty].type] : 0;
            var bv = board[b.tx, b.ty] != null ? val[board[b.tx,b.ty].type] : 0;
            var aa = val[board[a.fx,a.fy].type];
            var ba = val[board[b.fx,b.fy].type];
            int sa = av*10 - aa;
            int sb = bv*10 - ba;
            return sb.CompareTo(sa);
        });
    }

    private ChessPiece[,] Apply(ChessPiece[,] board, PackedMove m)
    {
        var clone = CloneBoard(board);
        var pc = clone[m.fx, m.fy];
        clone[m.tx, m.ty] = pc;
        clone[m.fx, m.fy] = null;
        pc.currentX = m.tx; pc.currentY = m.ty;
        return clone;
    }

    private bool WouldLeaveKingInCheck(ChessPiece[,] board, ChessPiece piece, Vector2Int target)
    {
        var clone = CloneBoard(board);
        var p = clone[piece.currentX, piece.currentY];
        clone[target.x, target.y] = p;
        clone[piece.currentX, piece.currentY] = null;
        p.currentX = target.x; p.currentY = target.y;
        return IsKingInCheck(clone, piece.team);
    }

    private ChessPiece[,] CloneBoard(ChessPiece[,] src)
    {
        var dst = new ChessPiece[8,8];
        for (int x=0;x<8;x++)
        for (int y=0;y<8;y++)
        {
            var p = src[x,y];
            dst[x,y] = (p == null) ? null : CopyPiece(p);
        }
        return dst;
    }

    private ChessPiece CopyPiece(ChessPiece o)
    {
        ChessPiece c = null;
        switch (o.type)
        {
            case ChessPieceType.Pawn:   c = new Pawn();   break;
            case ChessPieceType.Knight: c = new Knight(); break;
            case ChessPieceType.Bishop: c = new Bishop(); break;
            case ChessPieceType.Rock:   c = new Rock();   break;
            case ChessPieceType.Queen:  c = new Queen();  break;
            case ChessPieceType.King:   c = new King();   break;
        }
        c.type = o.type;
        c.team = o.team;
        c.currentX = o.currentX;
        c.currentY = o.currentY;
        // TODO: copy extra rule flags if your engine uses them (hasMoved, enPassant, castling rights, etc.)
        return c;
    }

    // === Eval ===
    private int Evaluate(ChessPiece[,] board)
    {
        currentPhase = ComputePhase(board);

        int score = 0;
        for (int x=0;x<8;x++)
        for (int y=0;y<8;y++)
        {
            var p = board[x,y];
            if (p == null) continue;
            int baseV = val[p.type];
            int pst   = PositionalBonus(p, x, y);
            int s = baseV + pst;
            score += (p.team == 0) ? s : -s;
        }

        score += Mobility(board);
        score += CenterControl(board);
        score += PawnStructure(board);
        score += PassedPawns(board);
        score += KingSafety(board);
        score += BishopPair(board);
        score += RookOpenFiles(board);

        score += 10; // small tempo
        return score;
    }

    private int ComputePhase(ChessPiece[,] board)
    {
        int phase = 0; // max 24
        for (int x=0;x<8;x++)
        for (int y=0;y<8;y++)
        {
            var p = board[x,y];
            if (p == null) continue;
            switch (p.type)
            {
                case ChessPieceType.Knight: phase += 1; break;
                case ChessPieceType.Bishop: phase += 1; break;
                case ChessPieceType.Rock:   phase += 2; break;
                case ChessPieceType.Queen:  phase += 4; break;
            }
        }
        return Mathf.Clamp(phase, 0, 24);
    }

    private int PositionalBonus(ChessPiece p, int x, int y)
    {
        int ty = (p.team == 0) ? y : 7 - y;
        if (p.type == ChessPieceType.King)
        {
            int mg = PST_K_MG[ty, x];
            int eg = PST_K_EG[ty, x];
            return (mg * currentPhase + eg * (24 - currentPhase)) / 24;
        }
        switch (p.type)
        {
            case ChessPieceType.Pawn:   return PST_P[ty, x];
            case ChessPieceType.Knight: return PST_N[ty, x];
            case ChessPieceType.Bishop: return PST_B[ty, x];
            case ChessPieceType.Rock:   return PST_R[ty, x];
            case ChessPieceType.Queen:  return PST_Q[ty, x];
            default: return 0;
        }
    }

    private int Mobility(ChessPiece[,] board)
    {
        int s = 0;
        for (int x=0;x<8;x++)
        for (int y=0;y<8;y++)
        {
            var p = board[x,y];
            if (p == null) continue;
            var moves = p.GetAvailableMoves(ref board, 8, 8);
            int delta = moves.Count * 2;
            s += (p.team == 0) ? delta : -delta;
        }
        return s;
    }

    private int CenterControl(ChessPiece[,] board)
    {
        int s = 0;
        for (int x=3;x<=4;x++)
        for (int y=3;y<=4;y++)
        {
            var p = board[x,y];
            if (p == null) continue;
            s += (p.team == 0) ? 15 : -15;
        }
        return s;
    }

    private int PawnStructure(ChessPiece[,] board)
    {
        int s = 0;
        // doubled
        for (int file=0; file<8; file++)
        {
            int w=0, b=0;
            for (int r=0;r<8;r++)
            {
                var p = board[file,r];
                if (p == null || p.type != ChessPieceType.Pawn) continue;
                if (p.team==0) w++; else b++;
            }
            if (w > 1) s -= (w-1) * 18;
            if (b > 1) s += (b-1) * 18;
        }
        // isolated
        for (int x=0;x<8;x++)
        for (int y=0;y<8;y++)
        {
            var p = board[x,y];
            if (p == null || p.type != ChessPieceType.Pawn) continue;
            bool hasAdj = false;
            for (int dx=-1; dx<=1; dx+=2)
            {
                int fx = x + dx;
                if (fx < 0 || fx > 7) continue;
                for (int r=0;r<8;r++)
                {
                    var q = board[fx,r];
                    if (q != null && q.type==ChessPieceType.Pawn && q.team==p.team)
                    { hasAdj = true; break; }
                }
            }
            if (!hasAdj) s += (p.team==0) ? -25 : 25;
        }
        return s;
    }

    private int PassedPawns(ChessPiece[,] board)
    {
        int s = 0;
        for (int x=0;x<8;x++)
        for (int y=0;y<8;y++)
        {
            var p = board[x,y];
            if (p == null || p.type != ChessPieceType.Pawn) continue;
            int enemy = p.team ^ 1;
            bool passed = true;
            for (int fx = Math.Max(0, x-1); fx <= Math.Min(7, x+1); fx++)
            {
                for (int ry=0; ry<8; ry++)
                {
                    var q = board[fx, ry];
                    if (q == null || q.type != ChessPieceType.Pawn || q.team != enemy) continue;
                    bool ahead = (p.team==0) ? (ry > y) : (ry < y);
                    if (ahead) { passed = false; break; }
                }
                if (!passed) break;
            }
            if (passed)
            {
                int adv = (p.team==0) ? y : (7 - y);
                int bonus = adv * 18;
                s += (p.team==0) ? bonus : -bonus;
            }
        }
        return s;
    }

    private int BishopPair(ChessPiece[,] board)
    {
        int wb=0, bb=0;
        for (int x=0;x<8;x++)
        for (int y=0;y<8;y++)
        {
            var p = board[x,y];
            if (p == null || p.type != ChessPieceType.Bishop) continue;
            if (p.team==0) wb++; else bb++;
        }
        int s = 0;
        if (wb >= 2) s += 35;
        if (bb >= 2) s -= 35;
        return s;
    }

    private int RookOpenFiles(ChessPiece[,] board)
    {
        int s = 0;
        for (int x=0;x<8;x++)
        {
            bool hasW=false, hasB=false;
            for (int y=0;y<8;y++)
            {
                var p = board[x,y];
                if (p == null || p.type != ChessPieceType.Pawn) continue;
                if (p.team==0) hasW=true; else hasB=true;
            }
            bool openForW = !hasW && !hasB;
            bool semiForW = !hasW &&  hasB;
            bool openForB = openForW;
            bool semiForB = !hasB &&  hasW;

            for (int y=0;y<8;y++)
            {
                var r = board[x,y];
                if (r == null || r.type != ChessPieceType.Rock) continue;
                if (r.team==0) { if (openForW) s += 25; else if (semiForW) s += 12; }
                else          { if (openForB) s -= 25; else if (semiForB) s -= 12; }
            }
        }
        return s;
    }

    private int KingSafety(ChessPiece[,] board)
    {
        int s = 0;
        for (int team=0; team<=1; team++)
        {
            ChessPiece k = null;
            for (int x=0;x<8;x++)
            {
                for (int y=0;y<8;y++)
                {
                    var p = board[x,y];
                    if (p!=null && p.team==team && p.type==ChessPieceType.King) { k = p; break; }
                }
                if (k!=null) break;
            }
            if (k==null) continue;

            int def=0, atk=0;
            for (int dx=-1; dx<=1; dx++)
            for (int dy=-1; dy<=1; dy++)
            {
                if (dx==0 && dy==0) continue;
                int cx = k.currentX + dx, cy = k.currentY + dy;
                if (cx<0||cx>7||cy<0||cy>7) continue;
                // simple coverage count via attack map
                if (SquareAttackedByTeam(board, cx, cy, team)) def++;
                if (SquareAttackedByTeam(board, cx, cy, team^1)) atk++;
            }
            s += (team==0) ? (def*2 - atk*3) : -(def*2 - atk*3);
        }
        return s;
    }

    // === Extra helpers ===
    private bool IsEndgameish(ChessPiece[,] board)
    {
        int nonPawn = 0;
        for (int x=0;x<8;x++)
        for (int y=0;y<8;y++)
        {
            var p = board[x,y];
            if (p == null) continue;
            if (p.type == ChessPieceType.Pawn || p.type == ChessPieceType.King) continue;
            nonPawn += val[p.type];
        }
        return nonPawn <= 2 * val[ChessPieceType.Rock];
    }

    private int NullMoveReduction(int depth) => (depth >= 6 ? 3 : 2);

    private int CountAttackers(ChessPiece[,] board, int team, int tx, int ty)
    {
        int cnt = 0;
        // Use attack map instead of GetAvailableMoves (robust)
        if (SquareAttackedByTeam(board, tx, ty, team)) cnt++; // at least 1; we only need relative count
        return cnt;
    }

    private bool IsClearlyLosingCapture(ChessPiece[,] board, PackedMove m)
    {
        var from = board[m.fx, m.fy];
        var to   = board[m.tx, m.ty];
        if (to == null || to.team == from.team) return false;

        int swing = val[to.type] - val[from.type];
        if (swing >= -50) return false;

        var after = Apply(board, m);
        int enemy = from.team ^ 1;
        int our   = from.team;
        int atk = CountAttackers(after, enemy, m.tx, m.ty);
        int def = CountAttackers(after, our,   m.tx, m.ty);
        return atk > def + 1;
    }

    // === TT and hashing ===
    private void StoreTT(ulong key, int depth, int value, TTFlag flag, bool sideToMove, PackedMove best)
    {
        tt[key] = new TTEntry { depth = depth, value = value, flag = flag, sideToMove = sideToMove, best = best };
    }

    private static ulong Rand64()
    {
        var hi = (ulong)(uint)rng64.Next(int.MinValue, int.MaxValue);
        var lo = (ulong)(uint)rng64.Next(int.MinValue, int.MaxValue);
        return (hi << 32) ^ lo;
    }

    private static void InitZobrist()
    {
        if (zobristInit) return;
        for (int x=0;x<8;x++)
            for (int y=0;y<8;y++)
                for (int k=0;k<12;k++)
                    zobrist[x,y,k] = Rand64();
        zobristSideToMove = Rand64();
        zobristInit = true;
    }

    private ulong Hash(ChessPiece[,] board, bool isWhiteTurn)
    {
        ulong h = 0;
        for (int x=0;x<8;x++)
        for (int y=0;y<8;y++)
        {
            var p = board[x,y];
            if (p == null) continue;
            int typeIndex = p.type switch
            {
                ChessPieceType.Pawn   => 0,
                ChessPieceType.Knight => 1,
                ChessPieceType.Bishop => 2,
                ChessPieceType.Rock   => 3,
                ChessPieceType.Queen  => 4,
                ChessPieceType.King   => 5,
                _ => 0
            };
            int id = p.team * 6 + typeIndex;
            h ^= zobrist[x,y,id];
        }
        if (!isWhiteTurn) h ^= zobristSideToMove;
        return h;
    }

    // === Time ===
    private bool timeExceeded()
    {
        if (timeUp) return true;
        if ((Time.realtimeSinceStartup * 1000f - startMs) > MAX_SEARCH_TIME_MS)
            timeUp = true;
        return timeUp;
    }
}
