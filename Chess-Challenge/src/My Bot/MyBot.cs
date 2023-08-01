using System;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    // Centi pawn values for: null, Pawn, Knight, Bishop, Rook, Queen, King
    int[] _centiPawnValues = { 0, 100, 300, 320, 500, 900, 0 }, _phasePieceValues = { 0, 0, 1, 1, 2, 4, 0 };
    Move _bestMove = Move.NullMove;

    ulong[] _pieceSquareTables =
        {
            9913330531774723959, 8609676836631704936, 11078252110869744008, 8608480570021773311, 250098419548360960,
            1715269411402468225, 1710465645101833345, 4803766359914288, 6374695211575366995, 6312245082029922709,
            6307740382873098373, 3843071673468680053, 7455559058829379447, 7455559058560874358, 7455559058560874358,
            8608480568035350936, 6302638648329659731, 7460362828890278021, 6307441315961931894, 3843370740631435125,
            13508397255544502747, 3535326889997710133, 1152921509170249729, 1152921509170249729, 1258605535789388048,
            1576256919301905233, 1566649386700635473, 5401900951762225
        },
        _hashStack = new ulong[1000];

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
        return timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30;
    }

    // 0 = pawn; 1 = knight; 2 = bishop; 3 = rook; 4 = queen; 5 = king mid; 6 = king end
    int GetPieceSquareValue(int pieceTable, int square)
    {
        return ((int)((_pieceSquareTables[pieceTable * 4 + square / 16] >> square % 16 * 4) & 15) - 7) * 5;
    }

    public Move Think(Board board, Timer timer)
    {
        int depth;
        int eval = 0; // #DEBUG
        Move best = board.GetLegalMoves()[0];
        for (depth = 1; depth < 50; depth++)
        {
            int currentEval = SearchPosition(board, depth, 0, -50000, 50000, timer);
            if (hasPassedTimeThreshold(timer))
                break;
            best = _bestMove;
            eval = currentEval; // #DEBUG
            //            if(Math.Abs(currentEval) >= 50000 - 50)
            //                break;
        }

        Console.WriteLine("Move #" + board.PlyCount + ", Best move: " + best.StartSquare.Name + best.TargetSquare.Name + //#DEBUG
            ", Eval: " + eval + ", Depth: " + depth); // #DEBUG
        return best;
    }

    int SearchPosition(Board board, int depth, int plyFromRoot, int alpha, int beta, Timer timer)
    {
        ulong key = board.ZobristKey;
        _hashStack[board.PlyCount] = key;
        bool notRoot = plyFromRoot > 0;
        if (notRoot)
        {
            for (int i = board.PlyCount - 2; i >= 0; i -= 2)
            {
                if (_hashStack[i] == _hashStack[board.PlyCount])
                {
                    Console.WriteLine("Duplicate position! HashStack length: " +
                                      _hashStack.Count(x => x != 0)); //#DEBUG
                    return 0;
                }
            }
        }

        Transposition entry = _tt[key % entries];
//        if(notRoot && entry.key == key && entry.depth >= depth && (entry.bound == 3 || (entry.bound == 2 && entry.eval >= beta) || (entry.bound == 1 && entry.eval <= alpha)))
//        {// #DEBUG
//            if(Math.Abs(entry.eval) >= 50000 - 50) //#DEBUG
//                Console.WriteLine("Found Mate TT entry: " + entry.move + ", Eval:" + entry.eval); //#DEBUG
//            return entry.eval;
//        }// #DEBUG

        if (depth == 0)
            return EvaluatePosition(board);

        Move[] legalMoves = board.GetLegalMoves();
        int origAlpha = alpha;
        int best = -50000;
        Move bestMove = Move.NullMove;
        foreach (Move move in legalMoves)
        {
            if (hasPassedTimeThreshold(timer)) return 50000;
            board.MakeMove(move);
            int eval = -SearchPosition(board, depth - 1, plyFromRoot + 1, -beta, -alpha, timer);
            board.UndoMove(move);
            if (eval > best)
            {
                best = eval;
                bestMove = move;
                if (!notRoot)
                    _bestMove = move;
                if (eval > alpha)
                {
                    alpha = eval;
                    if (alpha >= beta) break;
                }
            }
        }

        if (legalMoves.Length == 0)
        {
            if (board.IsInCheckmate())
                return -50000 + plyFromRoot;
            return 0;
        }

        _tt[key % entries] = new Transposition(key, bestMove, depth, best, best >= beta ? 2 : best > origAlpha ? 3 : 1);

        return best;
    }

    int EvaluatePosition(Board board)
    {
        int phase = 0, midEval = 0, endEval = 0;
        foreach (bool white in new[] { true, false })
        {
            for (PieceType piece = PieceType.Pawn; piece <= PieceType.King; piece++)
            {
                int intPiece = (int)piece, square;
                ulong bitBoard = board.GetPieceBitboard(piece, white);
                while (bitBoard != 0)
                {
                    square = BitboardHelper.ClearAndGetIndexOfLSB(ref bitBoard);
                    phase += _phasePieceValues[intPiece];
                    midEval += GetPieceSquareValue(intPiece - 1, square) + _centiPawnValues[intPiece];
                    endEval += GetPieceSquareValue(intPiece - 1 + (intPiece == 6 ? 1 : 0), square) +
                               _centiPawnValues[intPiece];
                }
            }

            endEval += 5 * centerManhattanDistance(board.GetKingSquare(!white));
            midEval = -midEval;
            endEval = -endEval;
        }

        return (board.IsWhiteToMove ? 1 : -1) * (midEval * phase + endEval * (24 - phase)) / 24;
    }

    int centerManhattanDistance(Square square)
    {
        int file = square.File;
        int rank = square.Rank;
        file ^= (file - 4) >> 8;
        rank ^= (rank - 4) >> 8;
        return (file + rank) & 7;
    }
//    int manhattanDistance(Square square1, Square square2)
//    {
//        return Math.Abs(square1.Rank - square2.Rank) + Math.Abs(square1.File - square2.File);
//    }
}