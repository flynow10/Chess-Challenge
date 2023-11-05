using System;
using System.Linq; // #DEBUG
using ChessChallenge.API;
using Board = ChessChallenge.API.Board;
using Move = ChessChallenge.API.Move;

namespace ChessChallenge.Version9;

public class MyBot : IChessBot
{
    // Constants
    const int TTSize = 1 << 20;
    bool _debug = false; // #DEBUG
    
    // Evaluation Tables
    
    int[] PieceCentipawnValues = { 0, 100, 310, 330, 500, 1000, 0 };
    
    readonly int[] PiecePhaseValues = { 0, 0, 1, 1, 2, 4, 0 };

    readonly int[] PieceKingTropismValues = { 0, 0, 0, 0, 3, 3, 2, 1, 2, 1, 2, 4, 0, 0 };

    // Ulong packed piece square tables
    // Tables for each piece at each stage of the game
    readonly ulong[] PieceSquareTables =
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
    
    int GetPieceSquareValue(int pieceSquare)
    {
        return (int)(((PieceSquareTables[pieceSquare / 10] >> (6 * (pieceSquare % 10))) & 63) - 20) * 8;
    }
    
    readonly int[] PassedPawnTable = { 0, 140, 92, 56, 32, 20, 20, 0 };
    
    // Search Info
    
    int _skillLevel;
    Board _board;
    Timer _timer;
    int _globalDepth;
    int _nodes; // #DEBUG
    int _ttHits; // #DEBUG
    Move _bestMove = Move.NullMove;
    readonly TTEntry[] _tt = new TTEntry[TTSize];
    Move[] _killers;
    
    public record struct TTEntry(ulong Key, Move Move, int Depth, int Eval, int NodeType);
    
    public MyBot(int skillLevel = 20) => _skillLevel = skillLevel;

    bool TimingFunction() => _timer.MillisecondsElapsedThisTurn < _timer.MillisecondsRemaining / 30;

    public Move Think(Board board, Timer timer)
    {
        _board = board;
        _timer = timer;
        _nodes = 0; // #DEBUG
        _ttHits = 0; // #DEBUG
        _bestMove = Move.NullMove;
        Move bestMove = Move.NullMove;
        int ttFilledCount = _tt.Count(e => e.Move != Move.NullMove); // #DEBUG
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
        if(_debug) // #DEBUG
            Console.WriteLine( // #DEBUG
                $"(MyBot) info ttCount {ttFilledCount} ttPercentFilled {ttFilledCount / (float)_tt.Length} zobrist {board.ZobristKey}"); // #DEBUG
        int lastIterTotalNodes = 0; // #DEBUG
        for (_globalDepth = 1; _globalDepth < 50; _globalDepth++)
        {
            _killers = new Move[_globalDepth + 1];
            int eval = NegamaxSearch(_globalDepth);
            if (!TimingFunction() && _globalDepth > 1)
                break;
            bestMove = _bestMove;
            if (_debug) // #DEBUG
                Console.WriteLine( // #DEBUG
                    $"(MyBot) info depth {_globalDepth} score cp {eval} currmove {_bestMove.StartSquare.Name + _bestMove.TargetSquare.Name} nodes {_nodes} currnodes {_nodes - lastIterTotalNodes} ttHits {_ttHits}");
            lastIterTotalNodes = _nodes; // #DEBUG
            if (Math.Abs(eval) >= 49950)
                break;
        }
        
        return bestMove;
    }
    
    int NegamaxSearch(int depth, int plyFromRoot = 0, int alpha = -9999999, int beta = 9999999)
    {
        if (_board.IsRepeatedPosition())
            return 0;
        bool isQuiescent = depth <= 0;
        _nodes++; // #DEBUG
        ulong key = _board.ZobristKey;

        TTEntry entry = _tt[key % TTSize];

        if (plyFromRoot > 0 &&
            entry.Key == key &&
            entry.Depth >= depth &&
            (entry.NodeType == 1 || (entry.NodeType == 2 && entry.Eval >= beta) ||
             (entry.NodeType == 3 && entry.Eval <= alpha)))
        {
            _ttHits++; // #DEBUG
            return entry.Eval;
        }

        int bestEval = -9999999;
        int standPat = EvaluatePosition();
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
        // if (depth >= 3 && plyFromRoot > 0 && (_flags & SearchFlags.UseNullMoveObservation) != 0 && _board.TrySkipTurn())
        // {
        //     int nullMoveObservation = -NegamaxSearch(depth - 3, plyFromRoot + 1, -beta, 1 - beta);
        //     _board.UndoSkipTurn();
        //     if (nullMoveObservation >= beta)
        //         return beta;
        // }

        Move[] legalMoves = _board.GetLegalMoves(isQuiescent);

        int[] scores = new int[legalMoves.Length];

        for (int i = 0; i < legalMoves.Length; i++)
        {
            Move move = legalMoves[i];
            if (entry.Move == move) scores[i] = 1000000;
            else if (plyFromRoot < _killers.Length && _killers[plyFromRoot] == move) scores[i] = 90000;
            else if (move.IsCapture)
                scores[i] = 100 * (int)move.CapturePieceType - (int)move.MovePieceType;
        }
        
        Move bestMove = Move.NullMove;
        int originalAlpha = alpha;
        for (int i = 0; i < legalMoves.Length; i++)
        {
            // Selection sort
            int jMin = i;
            for (int j = i + 1; j < legalMoves.Length; j++)
                if (scores[j] > scores[jMin])
                    jMin = j;

            if (jMin != i)
            {
                (scores[i], scores[jMin]) = (scores[jMin], scores[i]);
                (legalMoves[i], legalMoves[jMin]) = (legalMoves[jMin], legalMoves[i]);
            }

            Move move = legalMoves[i];
            if (!TimingFunction() && _globalDepth > 1)
                return 9999999;

            // Delta Pruning
            // Only done during quiescent search
            if (isQuiescent && standPat + PieceCentipawnValues[(int)move.CapturePieceType] + 200 < alpha &&
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
                if (evaluation > alpha)
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
                return -50000 + plyFromRoot;
            return 0;
        }

        _tt[key % TTSize] = new TTEntry(key, bestMove, depth, bestEval,
            bestEval >= beta ? 2 : bestEval > originalAlpha ? 1 :  3);

        return bestEval;
    }
    
    int EvaluatePosition()
    {
        // Evaluation from white's perspective
        int eval = 0,
            whiteMaterial = 0,
            blackMaterial = 0,
            materialBalance = 0,
            pawnBonuses = 0,
            midPieceSquareBonus = 0,
            endPieceSquareBonus = 0,
            midKingTropism = 0,
            endKingTropism = 0,
            mobility = 0,
            phase = 0;

        foreach (bool isWhite in new[] { true, false })
        {
            for (int piece = 1; piece <= 6; piece++)
            {
                // Find every piece of a certain type and color
                ulong bitBoard = _board.GetPieceBitboard((PieceType)piece, isWhite);
                while (bitBoard != 0)
                {
                    int squareIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref bitBoard);
                    Square square = new Square(squareIndex);
                    
                    // Get centipawn values for each piece
                    int cpValue = PieceCentipawnValues[piece];
                    if (isWhite)
                        whiteMaterial += cpValue;
                    else
                        blackMaterial += cpValue;
                    materialBalance += cpValue;
                    
                    // Calculate passed, doubled, weakened, and protected pawn bonuses
                    // Check flag here so as to not run expensive Evaluate Pawn method when unneeded
                    if(piece == (int)PieceType.Pawn)
                        pawnBonuses = EvaluatePawn(_board, square, isWhite);
                    
                    // Calculate King Tropism (how close each piece is to the enemy king
                    Square kingSquare = _board.GetKingSquare(!isWhite);
                    int kingTropism = KingTropism(square, kingSquare);
                    int pieceTropismIndex = piece * 2;
                    midKingTropism += PieceKingTropismValues[pieceTropismIndex] * kingTropism;
                    endKingTropism += PieceKingTropismValues[pieceTropismIndex + 1] * kingTropism;
                    
                    // Calculate mobility from attack bitboards
                    ulong pieceAttacks = BitboardHelper.GetPieceAttacks((PieceType) piece, square, _board, isWhite);
                    mobility += BitboardHelper.GetNumberOfSetBits(pieceAttacks);
                    
                    // Calculate the phase of the game (mid, end)
                    phase += PiecePhaseValues[piece];
                    
                    // Get Piece Square Table Bonuses
                    int pstIndex = 128 * (piece - 1) + squareIndex ^
                                (isWhite ? 56 : 0);
                    midPieceSquareBonus += GetPieceSquareValue(pstIndex);
                    endPieceSquareBonus += GetPieceSquareValue(pstIndex + 64);
                }
            }
            
            // Invert side based values for evaluating opponent
            materialBalance = -materialBalance;
            pawnBonuses = -pawnBonuses;
            midPieceSquareBonus = -midPieceSquareBonus;
            endPieceSquareBonus = -endPieceSquareBonus;
            midKingTropism = -midKingTropism;
            endKingTropism = -endKingTropism;
            mobility = -mobility;
        }

        eval += materialBalance;
        
        int midScore = midPieceSquareBonus + midKingTropism + (KingShield(true) - KingShield(false));
        int endScore = endPieceSquareBonus + endKingTropism;
        
        // Merge mid game and end game score bonuses based on phase
        eval += (midScore * phase + endScore * (24 - phase)) / 24;
        
        // Phase Independent Bonuses
        eval += pawnBonuses;
        eval += mobility;
        
        bool whiteWinning = eval > 0;
        int strongerMaterial = whiteWinning ? whiteMaterial : blackMaterial;
        int strongerPawns = _board.GetPieceList(PieceType.Pawn, whiteWinning).Count;
        int weakerPawns = _board.GetPieceList(PieceType.Pawn, !whiteWinning).Count;
        if (strongerPawns == 0)
        {
            if (strongerMaterial < 400 || (weakerPawns == 0 && strongerMaterial == 2 * PieceCentipawnValues[(int)PieceType.Knight])) return 0;
        }

        // Flip eval when evaluating for black
        if (!_board.IsWhiteToMove)
            eval = -eval;
        return eval;
    }
    
    int KingShield(bool isWhite)
    {
        int result = 0;
        int backRank = isWhite ? 0 : 7;
        int forward = isWhite ? 1 : -1;
        Square kingSquare = _board.GetKingSquare(isWhite);
        if (kingSquare.Rank != backRank || kingSquare.File == 3 || kingSquare.File == 4) return 0;
        int startFile = kingSquare.File < 3 ? 0 : 5;
        int endFile = kingSquare.File < 3 ? 2 : 7;
        for (int file = startFile; file <= endFile; file++)
        {
            for (int forwardCount = 1; forwardCount < 2; forwardCount++)
            {
                int rank = backRank + forwardCount * forward;
                Piece piece = _board.GetPiece(new Square(file, rank));
                if (piece.IsPawn && piece.IsWhite == isWhite)
                    result += (3-forwardCount) * 5;
            }
        }

        return result;
    }
    
    int EvaluatePawn(Board board, Square square, bool isWhite)
    {
        int result = 0;
        ulong opponentPawns = board.GetPieceBitboard(PieceType.Pawn, !isWhite);
        ulong friendlyPawns = board.GetPieceBitboard(PieceType.Pawn, isWhite);
        ulong forwardMask = ForwardMask(square, isWhite) & FileMask(square.File);
        bool isPassedPawn = (PassedPawnMask(square, isWhite) & opponentPawns) == 0;
        // bool isOpposed = (opponentPawns & forwardMask) != 0;
        int doubledPawns = BitboardHelper.GetNumberOfSetBits(friendlyPawns & forwardMask);
        result -= doubledPawns * 20;
        
        int pawnRank = isWhite ? square.Rank : 7 - square.Rank;
        if (isPassedPawn)
        {
            if (IsPawnSupported(board, square, isWhite))
                result += PassedPawnTable[pawnRank] * 5 / 4;
            else
                result += PassedPawnTable[pawnRank];
        }
        
        return result;
    }

    bool IsPawnSupported(Board board, Square square, bool isWhite)
    {
        int backward = isWhite ? -8 : 8;
        foreach (int fileChange in new [] {1,-1})
        {
            for (int rankChange = 0; rankChange <= 1; rankChange++)
            {
                Square checkSquare = new Square(square.File + fileChange, square.Rank + rankChange * backward);
                if (checkSquare.File is >= 0 and < 8 && checkSquare.Rank is >= 0 and < 8)
                {
                    if (BitboardHelper.SquareIsSet(board.GetPieceBitboard(PieceType.Pawn, isWhite), checkSquare))
                        return true;
                }
            }
        }

        return false;
    }
    
    ulong ForwardMask(Square square, bool isWhite)
    {
        ulong wForwardMask = ulong.MaxValue << 8 * (square.Rank + 1);
        ulong bForwardMask = ulong.MaxValue >> 8 * (8 - square.Rank);
        return isWhite ? wForwardMask : bForwardMask;
    }

    ulong FileMask(int file) => (ulong)0x0101010101010101 << file;

    ulong PassedPawnMask(Square square, bool isWhite)
    {
        ulong fileMask = FileMask(square.File);
        ulong fileLeft = FileMask(Math.Max(0, square.File - 1));
        ulong fileRight = FileMask(Math.Min(7, square.File + 1));
        ulong tripleFileMask = fileMask | fileLeft | fileRight;
        
        return tripleFileMask & ForwardMask(square, isWhite);
    }

    int KingTropism(Square square1, Square square2) => 7 - ManhattanDistance(square1, square2);

    int ManhattanDistance(Square square1, Square square2)
    {
        return Math.Abs(square2.File - square1.File) + Math.Abs(square2.Rank - square1.Rank);
    }
}