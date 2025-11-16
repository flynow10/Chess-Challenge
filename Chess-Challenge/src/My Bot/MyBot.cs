using System;
using ChessChallenge.API;
using ChessChallenge.Chess;
using Board = ChessChallenge.API.Board;
using Move = ChessChallenge.API.Move;

namespace ChessChallenge.MyBot;

public class MyBot : IChessBot
{

    record struct TTEntry(ulong key, Move move, int score, int depth);
    
    Move _pv = Move.NullMove, _currSearch;
    Timer _timer;
    int[] _pieceValues = { 0, 100, 300, 300, 500, 900, 0 };
    const int mateScore = 100000, nullScore = 9999999, ttSize = 1 << 20;
    
    TTEntry[] _tt = new TTEntry[ttSize];
    
    public Move Think(Board board, Timer timer)
    {
        _timer = timer;
        int depth = 1;
        for (; depth < 50; depth++)
        {
            int score = Search(board, depth, 0, -nullScore, nullScore);
            if (OutOfTime)
                break;
            // Console.WriteLine($"info depth {depth} score {score} ttHits {_ttHits} nodes {_nodes} betaCutoffs {_betaCutoffs}"); // #DEBUG
            _pv = _currSearch;
            if (Math.Abs(score) >= mateScore - 50)
                break;
        }
        Console.WriteLine($"PV: {MoveUtility.GetMoveNameUCI(new Chess.Move(_pv.RawValue))} Depth: {depth}"); // #DEBUG
        return _pv;
    }

    bool OutOfTime => _timer.MillisecondsElapsedThisTurn >= _timer.MillisecondsRemaining / 30;
    
    int Search(Board board, int depth, int plyFromRoot, int alpha, int beta)
    {
        // _nodes++;
        if (board.IsRepeatedPosition())
            return 0;
        if (OutOfTime) return nullScore;

        bool qSearch = depth <= 0;
        
        ulong key = board.ZobristKey;
        TTEntry entry = _tt[key % ttSize];

        if (plyFromRoot > 0 && entry.key == key && entry.depth >= depth)
        {
            // _ttHits++;
            return entry.score;
        }
        
        int bestScore = qSearch ? Evaluate(board) : -nullScore;
        Move bestMove = Move.NullMove;
        if (qSearch)
        {
            if(bestScore >= beta)
                return bestScore;
            if (bestScore > alpha)
                alpha = bestScore;
        }
        
        Move[] moves = board.GetLegalMoves(qSearch);
        int[] sortScores = new int[moves.Length];
        
        if (!qSearch && moves.Length == 0)
        {
            if (board.IsInCheck())
                return -mateScore + plyFromRoot;
            return 0;
        }
        
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            if (entry.key == key && entry.move == move)
                sortScores[i] = 100000;
            else if (move.IsCapture)
                sortScores[i] = 100 * ((int)move.CapturePieceType - (int)move.MovePieceType);
        }

        for (int i = 0; i < moves.Length; i++)
        {
            int jMin = i;
            for (int j = i + 1; j < moves.Length; j++)
                if (sortScores[j] > sortScores[jMin])
                    jMin = j;
            if (jMin != i)
            {
                (sortScores[jMin], sortScores[i]) = (sortScores[i], sortScores[jMin]);
                (moves[jMin], moves[i]) =  (moves[i], moves[jMin]);
            }
            
            Move move = moves[i];
            
            if (OutOfTime)
                return nullScore;
            board.MakeMove(move);
            int score = -Search(board, depth - 1, plyFromRoot + 1, -beta, -alpha);
            board.UndoMove(move);
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
                if (plyFromRoot == 0)
                    _currSearch = bestMove;
                if (bestScore > alpha)
                    alpha = bestScore;
            }

            if (alpha >= beta)
            {
                // _betaCutoffs++;
                break;
            }
        }

        if (!OutOfTime)
            _tt[key % ttSize] = new TTEntry(key, bestMove, bestScore, depth);
        
        return bestScore;
    }

    public //#DEBUG
    int Evaluate(Board board)
    {
        int score = 0;

        foreach (bool white in new[] { true, false })
        {
            for (PieceType pieceType = PieceType.Pawn; pieceType <= PieceType.Queen; pieceType++)
            {
                ulong bb = board.GetPieceBitboard(pieceType, white);
                while (bb != 0)
                {
                    BitboardHelper.ClearAndGetIndexOfLSB(ref bb);
                    score += _pieceValues[(int)pieceType];
                }
            }

            score = -score;
        }

        return score * (board.IsWhiteToMove ? 1 : -1);
    }
}