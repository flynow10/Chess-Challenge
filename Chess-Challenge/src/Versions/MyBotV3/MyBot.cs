using System;
using ChessChallenge.API;

namespace ChessChallenge.Version3;

public class MyBot : IChessBot
{
    // Centi pawn values for: null, Pawn, Knight, Bishop, Rook, Queen, King
    int[] _centiPawnValues = { 0, 100, 300, 320, 500, 900, 0 };
    int[] _phasePieceValues = {0,0,1,1,2,4,0};
    Move _bestMove = Move.NullMove;
    ulong[] _pieceSquareTables = { 9913330531774723959, 8609676836631704936, 11078252110869744008, 8608480570021773311, 250098419548360960, 1715269411402468225, 1710465645101833345, 4803766359914288, 6374695211575366995, 6312245082029922709, 6307740382873098373, 3843071673468680053, 7455559058829379447, 7455559058560874358, 7455559058560874358, 8608480568035350936, 6302638648329659731, 7460362828890278021, 6307441315961931894, 3843370740631435125, 13508397255544502747, 3535326889997710133, 1152921509170249729, 1152921509170249729, 1258605535789388048, 1576256919301905233, 1566649386700635473, 5401900951762225 };

    // 0 = pawn; 1 = knight; 2 = bishop; 3 = rook; 4 = queen; 5 = king mid; 6 = king end
    int GetPieceSquareValue(int pieceTable, int square)
    {
        return ((int)((_pieceSquareTables[pieceTable * 4 + square/16] >> square % 16 * 4) & 15) -7) * 5;
    }

    public Move Think(Board board, Timer timer)
    {
        int depth;
        int eval = 0;
        Move best = Move.NullMove;
        for(depth = 1; depth < 50; depth ++)
        {
            int currentEval = SearchPosition(board, depth, 0, -50000, 50000, timer);
            if(timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30)
                break;
            best = _bestMove;
            eval = currentEval;
            if(Math.Abs(eval) >= 50000 - 50)
                break;
        }
//        Console.WriteLine("Move #" + board.PlyCount + ", Best move: " + best.StartSquare.Name +
//                      best.TargetSquare.Name + ", Eval: " + eval + ", Depth: " + depth);
        return best;
    }

    int SearchPosition(Board board, int depth, int plyFromRoot, int alpha, int beta, Timer timer)
    {
        if(plyFromRoot > 0)
        {
            alpha = Math.Max(alpha, -50000 + plyFromRoot);
            beta = Math.Min(beta, 50000 - plyFromRoot);
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
                return -(50000 - plyFromRoot);
            }

            return 0;
        }

        foreach (Move move in legalMoves)
        {
            if(timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30) return 50000;
            board.MakeMove(move);
            int eval = -SearchPosition(board, depth - 1, plyFromRoot + 1, -beta, -alpha, timer);
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
                    _bestMove = move;
                }
            }
        }

        return alpha;
    }

    int EvaluatePosition(Board board)
    {
        int phase = 0, midEval = 0, endEval = 0;
        foreach (bool white in new[]{true, false})
        {
            for (PieceType piece = PieceType.Pawn; piece <= PieceType.King; piece++)
            {
                int intPiece = (int)piece, square;
                ulong bitBoard = board.GetPieceBitboard(piece, white);
                while(bitBoard != 0)
                {
                    square = BitboardHelper.ClearAndGetIndexOfLSB(ref bitBoard);
                    phase += _phasePieceValues[intPiece];
                    midEval += GetPieceSquareValue(intPiece - 1, square) + _centiPawnValues[intPiece];
                    endEval += GetPieceSquareValue(intPiece - 1 + (intPiece == 6 ? 1 : 0), square) + _centiPawnValues[intPiece];
                }
            }
            midEval = -midEval;
            endEval = -endEval;
        }

        return (midEval * phase + endEval*(24-phase))/24;
    }
}