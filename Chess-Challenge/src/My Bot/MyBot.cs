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
    
    Move _pv = Move.NullMove, _currSearch;
    Timer _timer;
    int[] _pieceValues = { 0, 100, 300, 300, 500, 900, 0 }, _phaseValues = {0, 0, 1, 1, 2, 4, 0};
    const int mateScore = 100000, nullScore = 9999999, ttSize = 1 << 20;
    int _nodes, _ttHits, _betaCutoffs; // #DEBUG
    
    TTEntry[] _tt = new TTEntry[ttSize];

    private ulong[] psts =
    {
        657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569,
        366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421,
        366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430,
        402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514,
        329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759,
        291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181,
        402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804,
        347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047,
        347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538,
        384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492,
        347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100,
        366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863,
        419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932,
        329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691,
        383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375,
        329978099633296596, 67159620133902
    };

    int GetPstVal(int psq) {
        return (int)(((psts[psq / 10] >> (6 * (psq % 10))) & 63) - 20) * 8;
    }
    
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
                break;
            // Console.WriteLine($"info depth {depth} score {score} ttHits {_ttHits} nodes {_nodes} time {timer.MillisecondsElapsedThisTurn}"); // #DEBUG
            _pv = _currSearch;
            if (Abs(score) >= mateScore - 50)
                break;
        }
        // Console.WriteLine($"PV: {_pv} Depth: {depth}"); // #DEBUG
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
                    phase += _phaseValues[pieceType];
                    int ind = 128 * (pieceType - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref bb) ^ (white ? 56 : 0);
                    mid += _pieceValues[pieceType] + GetPstVal(ind);
                    end += _pieceValues[pieceType] + GetPstVal(ind + 64);
                }
            }

            mid = -mid;
            end = -end;
        }

        return (board.IsWhiteToMove ? 1 : -1) * (mid * phase + end * (24 - phase)) / 24;
    }
}