using System;
using ChessChallenge.API;

namespace ChessChallenge.MyBot;
public class MyBot : IChessBot
{
    private Move _bestMove = Move.NullMove;
    
    const int entries = 1 << 20;
    Transposition[] _tt = new Transposition[entries];

    struct Transposition
    {
        public ulong key;
        public Move move;
        public int depth, eval, bound;

        public Transposition(ulong _key, Move _move, int _depth, int _eval, int _bound)
        {
            key = _key;
            move = _move;
            depth = _depth;
            eval = _eval;
            bound = _bound;
        }
    }

    bool hasPassedTimeThreshold(Timer timer)
    {
        return timer.MillisecondsElapsedThisTurn >= Math.Min(timer.MillisecondsRemaining / 30, 8000);
    }

    public Move Think(Board board, Timer timer)
    {
        int depthReached = 0; // #DEBUG
        int eval = 0; // #DEBUG
        Move best = Move.NullMove;
        for (int depth = 1; depth < 50; depth++)
        {
            int currentEval = SearchPosition(board, depth, 0, -50000, 50000, timer);
            if (hasPassedTimeThreshold(timer))
                break;
            best = _bestMove;
            depthReached = depth; // #DEBUG
            eval = currentEval; // #DEBUG
            if(Math.Abs(currentEval) >= 50000 - 50)
                break;
        }

        Console.WriteLine("Move #" + board.PlyCount + ", Best move: " + best.StartSquare.Name + best.TargetSquare.Name + //#DEBUG
            ", Eval: " + eval + ", Depth: " + depthReached); // #DEBUG
        return best;
    }

    int SearchPosition(Board board, int depth, int plyFromRoot, int alpha, int beta, Timer timer)
    {
        ulong key = board.ZobristKey;
        bool notRoot = plyFromRoot > 0;
        bool qsearch = depth <= 0;

        if (notRoot && board.IsRepeatedPosition())
            return 0;

        Transposition entry = _tt[key % entries];
        if(notRoot && entry.key == key && entry.depth >= depth && (entry.bound == 3 || (entry.bound == 2 && entry.eval >= beta) || (entry.bound == 1 && entry.eval <= alpha)))
            return entry.eval;
        Move[] legalMoves = board.GetLegalMoves(qsearch);
        if (!qsearch && legalMoves.Length == 0)
            return board.IsInCheck() ? -50000 + plyFromRoot : 0;

        if (qsearch)
        {
            int eval = EvaluatePosition(board);
            if (eval >= beta)
                return eval;
            alpha = Math.Max(alpha, eval);
        }

        int[] scores = new int[legalMoves.Length];
        Move bestMove = Move.NullMove;

        for(int i = 0; i < legalMoves.Length; i++) {
            if(legalMoves[i] == entry.move) scores[i] = 1000000;
            else if(legalMoves[i].IsCapture) scores[i] = 100 * (int)board.GetPiece(legalMoves[i].TargetSquare).PieceType - (int)board.GetPiece(legalMoves[i].StartSquare).PieceType;
        }


        int origAlpha = alpha;
        for (int i = 0; i < legalMoves.Length; i ++)
        {
            if (hasPassedTimeThreshold(timer)) return 50000;
            
            for(int j = i + 1; j < legalMoves.Length; j++) {
                if(scores[j] > scores[i])
                    (scores[i], scores[j], legalMoves[i], legalMoves[j]) = (scores[j], scores[i], legalMoves[j], legalMoves[i]);
            }

            Move move = legalMoves[i];
            
            board.MakeMove(move);
            int score = -SearchPosition(board, depth - 1, plyFromRoot + 1, -beta, -alpha, timer);
            board.UndoMove(move);
            if (score >= beta)
                return beta;
            if (score > alpha)
            {
                alpha = score;
                bestMove = move;
                if (!notRoot)
                    _bestMove = move;
            }
        }

        _tt[key % entries] = new Transposition(key, bestMove, depth, alpha, alpha >= beta ? 2 : alpha > origAlpha ? 3 : 1);

        return alpha;
    }

    int EvaluatePosition(Board board)
    {
        return 0;
    }
}