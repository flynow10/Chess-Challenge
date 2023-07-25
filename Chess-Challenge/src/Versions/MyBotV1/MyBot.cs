using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

namespace ChessChallenge.Version1;

public class MyBot : IChessBot
{
    int checkMateValue = 1000000;

    int infinity = 999999999;

    // Centi pawn values for: null, Pawn, Knight, Bishop, Rook, Queen, King
    int[] centiPawnValues = { 0, 100, 300, 320, 500, 900, 0 };

    public Move Think(Board board, Timer timer)
    {
        List<(double, Move)> scores = new();
        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            double score = -SearchPosition(board, 3, 1, -infinity, infinity);
            scores.Add((score, move));
            board.UndoMove(move);
        }

        var best = scores.MaxBy(score => score.Item1);
        var bestMove = best.Item2;
        return bestMove;
    }

    public double SearchPosition(Board board, int depth, int plyFromRoot, double alpha, double beta)
    {
        alpha = Math.Max(alpha, -checkMateValue + plyFromRoot);
        beta = Math.Min(beta, checkMateValue - plyFromRoot);
        if (alpha >= beta)
        {
            return alpha;
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
            board.MakeMove(move);
            double eval = -SearchPosition(board, depth - 1, plyFromRoot + 1, -beta, -alpha);
            board.UndoMove(move);
            if (eval >= beta)
            {
                return beta;
            }

            if (eval > alpha)
            {
                alpha = eval;
            }
        }

        return alpha;
    }

    public double EvaluatePosition(Board board)
    {
        var whitePieces = board.GetAllPieceLists().Where(l => l.IsWhitePieceList);
        var blackPieces = board.GetAllPieceLists().Where(l => !l.IsWhitePieceList);
        return CentiPawnCount(whitePieces) - CentiPawnCount(blackPieces);
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