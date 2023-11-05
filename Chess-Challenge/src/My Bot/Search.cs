using System;
using System.Diagnostics;
using System.Linq;
using ChessChallenge.API;

namespace ChessChallenge.MyBot;

public class Search
{
    private static readonly int DefaultDepth = 3;
    private static readonly int UpperDepthLimit = 50;
    private const int NullScore = 9999999;
    private const int TTSize = 1 << 20;

    private readonly SearchFlags _flags;
    private readonly Evaluate _evaluate;
    private readonly Func<Timer, bool> _timingFunction;
    private readonly int _skillLevel;
    private readonly bool _debug;

    private Board _board;
    private Timer _timer;
    private int _globalDepth = 0;
    private int _nodes;
    private int _ttHits;
    private Move _bestMove = Move.NullMove;
    private readonly TTEntry[] _tt = new TTEntry[TTSize];
    private Move[] _killers;

    public Search(SearchFlags flags, Evaluate evaluate, Func<Timer, bool> timingFunction, int skillLevel, bool debug = false)
    {
        _flags = flags;
        _evaluate = evaluate;
        _timingFunction = timingFunction;
        _skillLevel = skillLevel;
        _debug = debug;
        
    }

    public Move DepthFirstSearch(Board board, Timer timer)
    {
        _board = board;
        _timer = timer;
        _nodes = 0;
        _ttHits = 0;
        _bestMove = Move.NullMove;
        Move bestMove = Move.NullMove;
        int ttFilledCount = _tt.Count(e => e.Move != Move.NullMove);
        Random rng = new Random();
        if (rng.Next(0, 20) >= _skillLevel)
        {
            int[] weights = { 20 - _skillLevel, 10, _skillLevel };
            int rndDepth = rng.Next(weights.Sum());
            for (int i = 0; i < weights.Length; i++)
            {
                if (rndDepth < weights[i])
                {
                    _globalDepth = i + 1;
                    break;
                }

                rndDepth -= weights[i];
            }
            _killers = new Move[_globalDepth + 1];
            int eval = NegamaxSearch(_globalDepth);
            
            // Pick a completely random move if below skill 5
            if (_skillLevel < 5 && _globalDepth == 1 && Math.Abs(eval) < 49950)
            {
                Move[] moves = _board.GetLegalMoves();
                if(_debug) // #DEBUG
                    Console.WriteLine($"(MyBot) info random depth 1 score cp {eval}"); // #DEBUG
                return moves[rng.Next(moves.Length)];
            }
            
            if(_debug) // #DEBUG
                Console.WriteLine($"(MyBot) info low depth {_globalDepth} score cp {eval}"); // #DEBUG
            return _bestMove;
        }
        Console.WriteLine( // #DEBUG
            $"(MyBot) info ttCount {ttFilledCount} ttPercentFilled {ttFilledCount / (float)_tt.Length} zobrist {board.ZobristKey}"); // #DEBUG
        if ((_flags & SearchFlags.UseIterativeDeepening) != 0)
        {
            int lastIterTotalNodes = 0;
            for (_globalDepth = 1; _globalDepth < UpperDepthLimit; _globalDepth++)
            {
                _killers = new Move[_globalDepth + 1];
                int eval = NegamaxSearch(_globalDepth);
                if (!_timingFunction.Invoke(timer) && _globalDepth > 1)
                    break;
                bestMove = _bestMove;
                WriteDebugInfo(_globalDepth, eval, _bestMove, _nodes - lastIterTotalNodes);
                lastIterTotalNodes = _nodes;
                if (Math.Abs(eval) >= Evaluate.CHECKMATE_SCORE - UpperDepthLimit)
                    break;
            }
        }
        else
        {
            _globalDepth = DefaultDepth;
            _killers = new Move[_globalDepth + 1];
            int eval = NegamaxSearch(_globalDepth);
            bestMove = _bestMove;
            WriteDebugInfo(_globalDepth, eval, _bestMove, _nodes);
        }
        
        return bestMove;
    }

    private int NegamaxSearch(int depth, int plyFromRoot = 0, int alpha = -NullScore, int beta = NullScore)
    {
        if (_board.IsRepeatedPosition())
            return 0;
        bool isQuiescent = depth <= 0;

        if (isQuiescent && (_flags & SearchFlags.UseQuiescenceSearch) == 0)
        {
            return _evaluate.EvaluatePosition(_board);
        }

        _nodes++;
        ulong key = _board.ZobristKey;

        TTEntry entry = _tt[key % TTSize];

        if (plyFromRoot > 0 &&
            entry.Key == key &&
            entry.Depth >= depth &&
            (entry.NodeType == NodeType.PV || (entry.NodeType == NodeType.Cut && entry.Eval >= beta) ||
             (entry.NodeType == NodeType.All && entry.Eval <= alpha)) &&
            (_flags & SearchFlags.UseTranspositionTable) != 0)
        {
            _ttHits++;
            return entry.Eval;
        }

        int bestEval = -NullScore;
        int standPat = _evaluate.EvaluatePosition(_board);
        if (isQuiescent)
        {
            bestEval = standPat;
            if (standPat >= beta) return standPat;
            if (standPat > alpha) alpha = standPat;
        }
        else if (plyFromRoot > 0 && !_board.IsInCheck() && depth <= 6 && standPat - 100 * depth >= beta)
            return standPat;


        // Null Move Observation
        // See if the score is affected much if we do nothing
        if (depth >= 3 && plyFromRoot > 0 && (_flags & SearchFlags.UseNullMoveObservation) != 0 && _board.TrySkipTurn())
        {
            int nullMoveObservation = -NegamaxSearch(depth - 3, plyFromRoot + 1, -beta, 1 - beta);
            _board.UndoSkipTurn();
            if (nullMoveObservation >= beta)
                return beta;
        }

        Move[] legalMoves = _board.GetLegalMoves(isQuiescent);

        int[] scores = GetMoveScores(legalMoves, plyFromRoot, entry);

        Move bestMove = Move.NullMove;
        int originalAlpha = alpha;
        for (int i = 0; i < legalMoves.Length; i++)
        {
            // Selection sort
            if ((_flags & SearchFlags.UseMoveOrdering) != 0)
            {
                int jMin = i;
                for (int j = i + 1; j < legalMoves.Length; j++)
                    if (scores[j] > scores[jMin])
                        jMin = j;

                if (jMin != i)
                {
                    (scores[i], scores[jMin]) = (scores[jMin], scores[i]);
                    (legalMoves[i], legalMoves[jMin]) = (legalMoves[jMin], legalMoves[i]);
                }
            }

            Move move = legalMoves[i];
            if (!_timingFunction.Invoke(_timer) && _globalDepth > 1)
                return NullScore;

            // Delta Pruning
            // Only done during quiescent search
            if (isQuiescent && standPat + Evaluate.PieceCentipawnValues[(int)move.CapturePieceType] + 200 < alpha &&
                !move.IsPromotion)
                continue;
            
            _board.MakeMove(move);
            int evaluation = -NegamaxSearch(depth - 1, plyFromRoot + 1, -beta, -alpha);
            _board.UndoMove(move);

            if (evaluation > bestEval)
            {
                bestEval = evaluation;
                bestMove = move;
                if (plyFromRoot == 0)
                    _bestMove = bestMove;
                if (evaluation > alpha && (_flags & SearchFlags.UseAlphaBetaPruning) != 0)
                {
                    alpha = evaluation;
                    if (alpha >= beta)
                    {
                        if (!(move.IsCapture || move.IsPromotion) && !isQuiescent)
                            _killers[plyFromRoot] = move;
                        break;
                    }
                }
            }
        }

        if (!isQuiescent && legalMoves.Length == 0)
        {
            if (_board.IsInCheck())
                return -Evaluate.CHECKMATE_SCORE + plyFromRoot;
            return 0;
        }

        if ((_flags & SearchFlags.UseTranspositionTable) != 0)
            _tt[key % TTSize] = new TTEntry(key, bestMove, depth, bestEval,
                bestEval >= beta ? NodeType.Cut : bestEval > originalAlpha ? NodeType.PV :  NodeType.All);

        return bestEval;
    }

    private int[] GetMoveScores(Span<Move> moves, int plyFromRoot, TTEntry entry)
    {
        int[] scores = new int[moves.Length];

        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            if ((_flags & SearchFlags.UseTranspositionTable) != 0 && entry.Move == move) scores[i] = 1000000;
            else if (plyFromRoot < _killers.Length && _killers[plyFromRoot].StartSquare == move.StartSquare &&
                     _killers[plyFromRoot].TargetSquare == move.TargetSquare &&
                     (_flags & SearchFlags.UseKillerMoveOrdering) != 0) scores[i] = 90000;
            else if (move.IsCapture)
                scores[i] = 100 * (int)move.CapturePieceType - (int)move.MovePieceType;
        }

        return scores;
    }

    private void WriteDebugInfo(int depthReached, int bestEval, Move bestMove, int nodesThisIter)
    {
        if (_debug)
            Console.WriteLine(
                $"(MyBot) info depth {depthReached} score cp {bestEval} currmove {bestMove.StartSquare.Name + bestMove.TargetSquare.Name} nodes {_nodes} currnodes {nodesThisIter} ttHits {_ttHits}");
    }
}