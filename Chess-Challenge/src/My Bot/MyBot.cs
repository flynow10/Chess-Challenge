using System;
using ChessChallenge.API;
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
    
    Move _pv, _currSearch;
    Timer _timer;
    Board _board;
    const int mateScore = 100000, nullScore = 9999999, ttSize = 1 << 20;
    int _nodes, _ttHits; // #DEBUG
    int _searchDepth = 1;
    
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
        _board = board;
        _timer = timer;
        _nodes = _ttHits = 0; // #DEBUG
        for (_searchDepth = 1; _searchDepth < 50; _searchDepth++)
        {
            int score = Search(_searchDepth, 0, -nullScore, nullScore);
            if (OutOfTime)
                break;
            Logger.WriteLine(Logger.LogLevel.Full, $"info depth {_searchDepth} score {score} ttHits {_ttHits} nodes {_nodes} time {timer.MillisecondsElapsedThisTurn}"); // #DEBUG
            _pv = _currSearch;
        }
        Logger.WriteLine(Logger.LogLevel.PV, $"PV: {_pv} Depth: {_searchDepth}"); // #DEBUG
        return _pv;
    }

    bool OutOfTime => _timer.MillisecondsElapsedThisTurn >= _timer.MillisecondsRemaining / 30 && _searchDepth > 1;
    
    int Search(int depth, int plyFromRoot, int alpha, int beta)
    {
        _nodes++; // #DEBUG
        if (OutOfTime) return nullScore;

        // Transposition table
        ulong key = _board.ZobristKey;
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
        
        int i = 0, 
            bestScore = qSearch ? Evaluate() : -nullScore,
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
        
        var moves = _board.GetLegalMoves(qSearch);
        var sortScores = new int[moves.Length];
        
        if (!qSearch && moves.Length == 0)
        {
            if (_board.IsInCheck())
                return -mateScore + plyFromRoot;
            return 0;
        }
        
        // Move ordering based on principal variation
        foreach (Move move in moves)
        {
            sortScores[i++] = ttHit && entry.move == move ? 100000 :
                move.IsCapture ? 100 * ((int)move.CapturePieceType - (int)move.MovePieceType) : 0;
        }

        Array.Sort(sortScores, moves);
        foreach (Move move in moves)
        {
            // Negamax a/b search 
            _board.MakeMove(move);
            
            score = _board.IsRepeatedPosition() ? 0 : -Search(depth - 1, plyFromRoot + 1, -beta, -alpha);
            
            _board.UndoMove(move);
            
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
                break;
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
    int Evaluate()
    {
        int phase = 0, mid = 0, end = 0;

        foreach (bool white in new[] { true, false })
        {
            for (int pieceType = 1; pieceType <= 6; pieceType++)
            {
                Square square;
                ulong bb = _board.GetPieceBitboard((PieceType) pieceType, white);
                while (bb != 0)
                {
                    square = new Square(BitboardHelper.ClearAndGetIndexOfLSB(ref bb));
                    if(!white)
                        square = new Square(square.File, 7 - square.Rank);
                    int pieceSquareValue = GetPieceSquareValue(pieceType - 1, square.Index),
                    // Pack piece values into single number - saves 4 tokens over array
                    // int[] _pieceValues = { 0, 100, 300, 300, 500, 900, 0 };
                    value = (0x0953310 >> pieceType * 4 & 0xf) * 100;
                    mid += pieceSquareValue + value;
                    end += (pieceType == 6 ? GetPieceSquareValue(6, square.Index) : pieceSquareValue) + value;
                    // pack phase values into single number - saves 7 tokens over array
                    // int[] _phaseValues = { 0, 0, 1, 1, 2, 4, 0 };
                    phase += 0x0421100 >> pieceType * 4 & 0xf;
                }
            }

            mid = -mid;
            end = -end;
        }

        return (_board.IsWhiteToMove ? 1 : -1) * (mid * phase + end * (24 - phase)) / 24;
    }
}