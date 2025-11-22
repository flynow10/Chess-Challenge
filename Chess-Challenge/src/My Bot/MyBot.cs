using System;
using ChessChallenge.API;
using ChessChallenge.Chess;
using static System.Math;
using Board = ChessChallenge.API.Board;
using Move = ChessChallenge.API.Move;

namespace ChessChallenge.MyBot;

public class MyBot : IChessBot
{
    // Transposition table entries
    // Entry types are as follows
    // 0 => Exact node or PV-Node
    // 1 => Lower bound or Cut-Node
    // 2 => Upper bound or All-Node
    record struct TTEntry(ulong key, Move move, int score, int depth, int type);
    
    Move _pv = Move.NullMove, _currSearch;
    Timer _timer;
    int[] _pieceValues = { 0, 100, 300, 300, 500, 900, 0 }, _phaseValues = {0, 0, 1, 1, 2, 4, 0};
    const int mateScore = 100000, nullScore = 9999999, ttSize = 1 << 20;
    int _nodes, _ttHits, _betaCutoffs; // #DEBUG
    
    TTEntry[] _tt = new TTEntry[ttSize];
    
    ulong[] _pieceSquareTables =
    {
        9913330531774723959, 8609676836631704936, 11078252110869744008, 8608480570021773311,
        250098419548360960, 1715269411402468225, 1710465645101833345, 4803766359914288,
        6374695211575366995, 6312245082029922709, 6307740382873098373, 3843071673468680053,
        7455559058829379447, 7455559058560874358, 7455559058560874358, 8608480568035350936,
        6302638648329659731, 7460362828890278021, 6307441315961931894, 3843370740631435125,
        13508397255544502747, 3535326889997710133, 1152921509170249729, 1152921509170249729,
        1258605535789388048, 1576256919301905233, 1566649386700635473, 5401900951762225
    };
    
    int GetPieceSquareValue(int pieceTable, int square) =>
        (int)((_pieceSquareTables[pieceTable * 4 + square / 16] >> square % 16 * 4) & 15) * 5 - 35;
    
    public Move Think(Board board, Timer timer)
    {
        _timer = timer;
        _nodes = _betaCutoffs = _ttHits = 0; // #DEBUG
        _pv = default;
        int depth = 1;
        for (; depth < 50; depth++)
        {
            int score = Search(board, depth, 0, -nullScore, nullScore);
            if (OutOfTime)
            {
                depth--;
                break;
            }
            // Console.WriteLine($"info depth {depth} score {score} ttHits {_ttHits} nodes {_nodes} time {timer.MillisecondsElapsedThisTurn}"); // #DEBUG
            _pv = _currSearch;
            if (Abs(score) >= mateScore - 50)
                break;
        }
        Console.WriteLine($"PV: {MoveUtility.GetMoveNameUCI(new Chess.Move(_pv.RawValue))} Depth: {depth}"); // #DEBUG
        return _pv;
    }

    bool OutOfTime => _timer.MillisecondsElapsedThisTurn >= _timer.MillisecondsRemaining / 30;
    
    int Search(Board board, int depth, int plyFromRoot, int alpha, int beta)
    {
        _nodes++; // #DEBUG
        if (OutOfTime) return nullScore;

        // Transposition table
        ulong key = board.ZobristKey;
        TTEntry entry = _tt[key % ttSize];
        bool qSearch = depth <= 0,
            ttHit = entry.key == key;
        
        if (ttHit && entry.depth >= depth && entry.type switch
            {
                1 => entry.score >= beta, // lower bound
                2 => entry.score <= alpha, // upper bound
                _ => plyFromRoot > 0 || qSearch, // pv node
            })
        { // #DEBUG
            _ttHits++; // #DEBUG
            return entry.score;
        } // #DEBUG
        
        int i, 
            bestScore = qSearch ? Evaluate(board) : -nullScore,
            originalAlpha = alpha,
            score;
        Move bestMove = default;
        
        // Quiescence Search
        if (qSearch)
        {
            if(bestScore >= beta)
                return bestScore;
            alpha = Max(alpha, bestScore);
        }
        
        Move[] moves = board.GetLegalMoves(qSearch);
        int[] sortScores = new int[moves.Length];
        
        if (!qSearch && moves.Length == 0)
        {
            if (board.IsInCheck())
                return -mateScore + plyFromRoot;
            return 0;
        }
        
        // Move ordering based on principal variation
        for (i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            if (ttHit && entry.move == move)
                sortScores[i] = 100000;
            else if (move.IsCapture)
                sortScores[i] = 100 * ((int)move.CapturePieceType - (int)move.MovePieceType);
        }

        Array.Sort(sortScores, moves);
        foreach (Move move in moves)
        {
            // Negamax a/b search 
            board.MakeMove(move);
            
            if (board.IsRepeatedPosition())
                score = 0;
            else 
                score = -Search(board, depth - 1, plyFromRoot + 1, -beta, -alpha);
            
            board.UndoMove(move);
            
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
                alpha = Max(alpha, bestScore);
                if (plyFromRoot == 0)
                    _currSearch = bestMove;
            }

            // Beta cutoff
            if (alpha >= beta)
            { // #DEBUG
                _betaCutoffs++; // #DEBUG
                break;
            } // #DEBUG
        }

        // Save to tt table only if valid move
        if (!OutOfTime)
            _tt[key % ttSize] = new TTEntry(
                key, 
                bestMove, 
                bestScore, 
                depth, 
                // Beta cutoff = Lower bound
                // No change alpha = Upper bound
                // Score within a/b window = PV node
                bestScore >= beta ? 1 : 
                alpha == originalAlpha ? 2 : 0);
        
        return bestScore;
    }

    public //#DEBUG
    int Evaluate(Board board)
    {
        int phase = 0, mid = 0, end = 0;

        foreach (bool white in new[] { true, false })
        {
            for (int pieceType = 1; pieceType <= 6; pieceType++)
            {
                ulong bb = board.GetPieceBitboard((PieceType) pieceType, white);
                while (bb != 0)
                {
                    int square = BitboardHelper.ClearAndGetIndexOfLSB(ref bb);
                    int pieceSquareValue = GetPieceSquareValue(pieceType - 1, square);
                    phase += _phaseValues[pieceType];
                    mid += pieceSquareValue + _pieceValues[pieceType];
                    end += (pieceType == 6 ? GetPieceSquareValue(6, square) : pieceSquareValue) + _pieceValues[pieceType];
                }
            }

            mid = -mid;
            end = -end;
        }

        return (board.IsWhiteToMove ? 1 : -1) * (mid * phase + end * (24 - phase)) / 24;
    }
}