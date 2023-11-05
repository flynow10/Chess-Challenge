using System;
using ChessChallenge.API;
using ChessChallenge.Chess;
using Board = ChessChallenge.API.Board;

namespace ChessChallenge.MyBot;

public class Evaluate
{
    public static readonly int CHECKMATE_SCORE = 50000;

    private static readonly int KING_SHIELD_MULTIPLIER = 5;

    public static readonly int[] PieceCentipawnValues = { 0, 100, 310, 330, 500, 1000, 0 };

    static readonly int[] PiecePhaseValues = { 0, 0, 1, 1, 2, 4, 0 };

    private static readonly int[] PieceKingTropismValues = { 0, 0, 0, 0, 3, 3, 2, 1, 2, 1, 2, 4, 0, 0 };

    // Ulong packed piece square tables
    // Tables for each piece at each stage of the game
    static readonly ulong[] PieceSquareTables =
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
    
    static readonly int[] PassedPawnTable = {
        0,   0,   0,   0,   0,   0,   0,   0,
        140, 140, 140, 140, 140, 140, 140, 140,
        92,  92,  92,  92,  92,  92,  92,  92,
        56,  56,  56,  56,  56,  56,  56,  56,
        32,  32,  32,  32,  32,  32,  32,  32,
        20,  20,  20,  20,  20,  20,  20,  20,
        20,  20,  20,  20,  20,  20,  20,  20,
        0,   0,   0,   0,   0,   0,   0,   0
    };

    private const int entries = 1 << 22;
    readonly EvaluationFlags _flags;
    private TTEvalEntry[] _ttEval = new TTEvalEntry[entries];

    record struct TTEvalEntry(ulong key, int eval);

    public Evaluate(EvaluationFlags flags)
    {
        _flags = flags;
    }

    static int GetPieceSquareValue(int pieceSquare)
    {
        return (int)(((PieceSquareTables[pieceSquare / 10] >> (6 * (pieceSquare % 10))) & 63) - 20) * 8;
    }

    // Evaluates how "good" a position is for the CURRENT side to move
    // Returns a value in centi-pawns
    public int EvaluatePosition(Board board)
    {
        ulong key = board.ZobristKey;
        TTEvalEntry entry = _ttEval[key % entries];
        if ((_flags & EvaluationFlags.UseEvalTT) != 0&&entry.key == key) return entry.eval;
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
                ulong bitBoard = board.GetPieceBitboard((PieceType)piece, isWhite);
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
                    if((_flags & EvaluationFlags.UsePawnBonuses) != 0 && piece == (int)PieceType.Pawn)
                        pawnBonuses = EvaluatePawn(board, square, isWhite);
                    
                    // Calculate King Tropism (how close each piece is to the enemy king
                    Square kingSquare = board.GetKingSquare(!isWhite);
                    int kingTropism = KingTropism(square, kingSquare);
                    int pieceTropismIndex = piece * 2;
                    midKingTropism += PieceKingTropismValues[pieceTropismIndex] * kingTropism;
                    endKingTropism += PieceKingTropismValues[pieceTropismIndex + 1] * kingTropism;
                    
                    // Calculate mobility from attack bitboards
                    ulong pieceAttacks = BitboardHelper.GetPieceAttacks((PieceType) piece, square, board, isWhite);
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
        
        int midScore = midPieceSquareBonus;
        int endScore = endPieceSquareBonus;

        if ((_flags & EvaluationFlags.UseKingTropism) != 0)
        {
            midScore += midKingTropism;
            endScore += endKingTropism;
        }

        if ((_flags & EvaluationFlags.UseKingShield) != 0)
        {
            midScore += KingShield(board, true) - KingShield(board, false);
        }
        
        // Merge mid game and end game score bonuses based on phase
        eval += (midScore * phase + endScore * (24 - phase)) / 24;
        
        if ((_flags & EvaluationFlags.UseMobility) != 0)
        {
            eval += mobility;
        }

        if ((_flags & EvaluationFlags.UsePawnBonuses) != 0)
        {
            eval += pawnBonuses;
        }

        if ((_flags & EvaluationFlags.UseLowMaterialCutoffs) != 0)
        {
            bool whiteWinning = eval > 0;
            int strongerMaterial = whiteWinning ? whiteMaterial : blackMaterial;
            int strongerPawns = board.GetPieceList(PieceType.Pawn, whiteWinning).Count;
            int weakerPawns = board.GetPieceList(PieceType.Pawn, !whiteWinning).Count;
            if (strongerPawns == 0)
            {
                if (strongerMaterial < 400) return 0;

                if (weakerPawns == 0 && strongerMaterial == 2 * PieceCentipawnValues[(int)PieceType.Knight]) return 0;
                
                
            }
        }

        // Flip eval when evaluating for black
        if (!board.IsWhiteToMove)
            eval = -eval;
        if((_flags & EvaluationFlags.UseEvalTT) != 0)
            _ttEval[key % entries] = new(key, eval);
        return eval;
    }

    int KingShield(Board board, bool isWhite)
    {
        int result = 0;
        int backRank = isWhite ? 0 : 7;
        int forward = isWhite ? 1 : -1;
        Square kingSquare = board.GetKingSquare(isWhite);
        if (kingSquare.Rank != backRank || kingSquare.File == 3 || kingSquare.File == 4) return 0;
        int startFile = kingSquare.File < 3 ? 0 : 5;
        int endFile = kingSquare.File < 3 ? 2 : 7;
        for (int file = startFile; file <= endFile; file++)
        {
            for (int forwardCount = 1; forwardCount < 2; forwardCount++)
            {
                int rank = backRank + forwardCount * forward;
                Piece piece = board.GetPiece(new Square(file, rank));
                if (piece.IsPawn && piece.IsWhite == isWhite)
                    result += (3-forwardCount) * KING_SHIELD_MULTIPLIER;
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
        
        Square reverseSquare = isWhite ? square : new Square(square.File, 7 - square.Rank);
        if (isPassedPawn)
        {
            if (IsPawnSupported(board, square, isWhite))
                result += PassedPawnTable[reverseSquare.Index] * 5 / 4;
            else
                result += PassedPawnTable[reverseSquare.Index];
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
                if (BoardHelper.IsValidCoordinate(checkSquare.File, checkSquare.Rank))
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