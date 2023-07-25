using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;
namespace ChessChallenge.Version2;

public class MyBot : IChessBot
{
    int checkMateValue = 1000000;

    int infinity = 999999999;

    // Centi pawn values for: null, Pawn, Knight, Bishop, Rook, Queen, King
    int[] centiPawnValues = { 0, 100, 300, 320, 500, 900, 0 };
    Move bestMove = Move.NullMove;

    public Move Think(Board board, Timer timer)
    {
        int depth;
        Move best = Move.NullMove;
        for(depth = 1; depth < 50; depth ++)
        {
            /*double eval = */SearchPosition(board, depth, 0, -infinity, infinity, timer);
            if(timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30)
            {
                break;
            }
            best = bestMove;
//            Console.WriteLine("Move #" + board.PlyCount + ", Best move: " + best.StartSquare.Name +
//                          best.TargetSquare.Name + ", Eval: " + eval + ", Depth: " + depth);
        }
        return best;
    }

    public double SearchPosition(Board board, int depth, int plyFromRoot, double alpha, double beta, Timer timer)
    {
        if(plyFromRoot > 0)
        {
            alpha = Math.Max(alpha, -checkMateValue + plyFromRoot);
            beta = Math.Min(beta, checkMateValue - plyFromRoot);
            if (alpha >= beta)
            {
                return alpha;
            }
        }

        if (depth == 0)
        {
            return (board.IsWhiteToMove ? 1 : -1) * EvaluatePosition(board);
        }

        Move[] legalMoves = board.GetLegalMoves();
        if (legalMoves.Length == 0 || board.IsDraw())
        {
            if (board.IsInCheckmate())
            {
                return -(checkMateValue - plyFromRoot);
            }

            return 0;
        }

        foreach (Move move in legalMoves)
        {
            if(timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30) return infinity;
            board.MakeMove(move);
            double eval = -SearchPosition(board, depth - 1, plyFromRoot + 1, -beta, -alpha, timer);
            board.UndoMove(move);
            if (eval >= beta)
            {
                return beta;
            }

            if (eval > alpha)
            {
                alpha = eval;
                if(plyFromRoot == 0)
                {
                    bestMove = move;
                }
            }
        }

        return alpha;
    }

    public double EvaluatePosition(Board board)
    {
        var whitePieces = board.GetAllPieceLists().Where(l => l.IsWhitePieceList);
        var blackPieces = board.GetAllPieceLists().Where(l => !l.IsWhitePieceList);
        double whiteEval = CentiPawnCount(whitePieces);
        double blackEval = CentiPawnCount(blackPieces);
        return whiteEval - blackEval;
    }

    public int CentiPawnCount(IEnumerable<PieceList> pieceLists)
    {
        return pieceLists.Aggregate(0, (acc, list) =>
        {
            var count = centiPawnValues[(int)list.TypeOfPieceInList] * list.Count;
            return acc + count;
        });
    }
}