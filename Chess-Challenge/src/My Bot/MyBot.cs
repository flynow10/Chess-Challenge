using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    int checkMateValue = 1000000;

    int infinity = 999999999;

    // Centi pawn values for: null, Pawn, Knight, Bishop, Rook, Queen, King
    int[] centiPawnValues = { 0, 100, 300, 320, 500, 900, 0 };

    int[] kingOpeningTable =
    {
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -20,-30,-30,-40,-40,-30,-30,-20,
        -10,-20,-20,-20,-20,-20,-20,-10,
        20, 20,  0,  0,  0,  0, 20, 20,
        20, 30, 10,  0,  0, 10, 30, 20
    };

    int[] kingEndingTable =
    {
        -50,-40,-30,-20,-20,-30,-40,-50,
        -30,-20,-10,  0,  0,-10,-20,-30,
        -30,-10, 20, 30, 30, 20,-10,-30,
        -30,-10, 30, 40, 40, 30,-10,-30,
        -30,-10, 30, 40, 40, 30,-10,-30,
        -30,-10, 20, 30, 30, 20,-10,-30,
        -30,-30,  0,  0,  0,  0,-30,-30,
        -50,-30,-30,-30,-30,-30,-30,-50
    };

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
        Console.WriteLine("Move #" + board.PlyCount + ", Best move: " + bestMove.StartSquare.Name +
                          bestMove.TargetSquare.Name + ", Eval: " + best.Item1 + ", Phase: " + GetGamePhase(board));
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
        double whiteEval = CentiPawnCount(whitePieces);
        double blackEval = CentiPawnCount(blackPieces);
        whiteEval += GetKingPieceTableEval(board, true);
        blackEval += GetKingPieceTableEval(board, false);
        return whiteEval - blackEval;
    }

    public double GetKingPieceTableEval(Board board, bool white)
    {
        var phase = GetGamePhase(board);
        var openingScore = ReadPieceTable(kingOpeningTable, board.GetKingSquare(white), white);
        var endingScore = ReadPieceTable(kingEndingTable, board.GetKingSquare(white), white);
        return (openingScore * (256 - phase) + endingScore * phase)/256;
    }

    public double ReadPieceTable(int[] table, Square square, bool white)
    {
        var index = square.Index;
        if(white)
        {
            index = (7 - square.Rank) * 8 + square.File;
        }
        return table[index];
    }

    public double GetGamePhase(Board board)
    {
        // null = 0;
        // pawnPhase = 0;
        // knightPhase = 1;
        // bishopPhase = 1;
        // rookPhase = 2;
        // queenPhase = 4;
        // king = 0;
        int[] piecePhases = { 0, 0, 1, 1, 2, 4, 0 };
        // totalPhase = pawnPhase * 16 + knightPhase * 4 + bishopPhase*4 + rookPhase*4 + queenPhase*2;
        int totalPhase = 24;
        int phase = totalPhase;

        foreach (PieceList list in board.GetAllPieceLists())
        {
            phase -= piecePhases[(int)list.TypeOfPieceInList] * list.Count;
        }

        return (phase * 256 + (double)totalPhase / 2) / totalPhase;
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