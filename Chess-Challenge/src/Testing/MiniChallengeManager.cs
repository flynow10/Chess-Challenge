using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChessChallenge.API;
using ChessChallenge.Application;
using ChessChallenge.Chess;
using Spectre.Console;
using Board = ChessChallenge.Chess.Board;
using Move = ChessChallenge.Chess.Move;

namespace ChessChallenge.Testing;

public class MiniChallengeManager
{
    private readonly PlayerType _testBot;
    private readonly PlayerType _opponentBot;
    private readonly Func<PlayerType, IChessBot> botFactory;

    private ConcurrentBag<MatchResult> _matchResults = new();

    public int MaxMoves = -1;
    public int GameDurationMilliseconds = 60000;

    public MiniChallengeManager(PlayerType testBot, Func<PlayerType, IChessBot> botFactory) : this(testBot,
        PlayerType.StockFish, botFactory)

    {
    }

    public MiniChallengeManager(PlayerType bot, PlayerType opponentBot, Func<PlayerType, IChessBot> botFactory)
    {
        _testBot = bot;
        _opponentBot = opponentBot;
        this.botFactory = botFactory;
    }

    public MatchResult[] PlayMatches(string[] fens)
    {
        List<string> doubledFens = new List<string>();
        foreach (string fen in fens)
        {
            doubledFens.Add(fen);
            doubledFens.Add(fen);
        }
        _matchResults.Clear();
        Parallel.ForEach(doubledFens, (fen, _, index) =>
        {
            bool playsWhite = index % 2 == 0;
            MatchResult matchResult = PlayMatch(fen, playsWhite);
            _matchResults.Add(matchResult);
        });
        return _matchResults.ToArray();
    }

    public MatchResult PlayMatch(string fen, bool? testerPlaysWhite = null)
    {
        IChessBot testBot = botFactory.Invoke(_testBot);
        IChessBot opponentBot = botFactory.Invoke(_opponentBot);
        int testBotSecondsElapsed = 0;
        int opponentSecondsElapsed = 0;
        Board board = new Board();
        
        FenUtility.PositionInfo position = FenUtility.PositionFromFen(fen);
        board.LoadPosition(position);

        bool _playsWhite = testerPlaysWhite ?? position.whiteToMove;

        int plyCount = 0;
        GameResult result = GameResult.InProgress;
        while (!Arbiter.IsWinResult(result) && !Arbiter.IsDrawResult(result))
        {
            Move move = GetMove(board, testBot, opponentBot, _playsWhite, ref testBotSecondsElapsed,
                ref opponentSecondsElapsed);
            result = PlayMove(board, move);
            plyCount++;
            if (MaxMoves != -1 && plyCount / 2 >= MaxMoves)
            {
                result = _playsWhite ? GameResult.WhiteTimeout : GameResult.BlackTimeout;
                break;
            }
        }

        MatchResult matchResult = new MatchResult(result, _playsWhite, board.plyCount, plyCount, position);

        return matchResult;
    }

    private GameResult PlayMove(Board board, Move move)
    {
        if (move.IsNull)
        {
            return board.IsWhiteToMove ? GameResult.WhiteIllegalMove : GameResult.BlackIllegalMove;
        }

        try
        {
            board.MakeMove(move, false);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Console.WriteLine(FenUtility.CurrentFen(board));
            Console.WriteLine(MoveUtility.GetMoveNameUCI(move));
            return board.IsWhiteToMove ? GameResult.WhiteIllegalMove : GameResult.BlackIllegalMove;
        }
        return Arbiter.GetGameState(board);
    }

    private Move GetMove(Board board, IChessBot tester, IChessBot opponent, bool testerPlaysWhite, ref int testerTime,
        ref int opponentBotTime)
    {
        bool isTesterTurn = board.IsWhiteToMove == testerPlaysWhite;
        IChessBot bot = isTesterTurn ? tester : opponent;

        if (isTesterTurn)
            return GetBotMove(board, bot, ref testerTime, opponentBotTime);
        return GetBotMove(board, bot, ref opponentBotTime, testerTime);
    }

    private Move GetBotMove(Board board, IChessBot bot, ref int timeUsed, int opponentTimeUsed)
    {
        API.Board botBoard = new(board);
        try
        {
            Timer timer = new(GameDurationMilliseconds - timeUsed, GameDurationMilliseconds - opponentTimeUsed,
                GameDurationMilliseconds);
            API.Move move = bot.Think(botBoard, timer);
            timeUsed += timer.MillisecondsElapsedThisTurn;
            return new Move(move.RawValue);
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine("[red]An error occurred while bot was thinking.[/]");
            AnsiConsole.WriteException(e);
        }

        return Move.NullMove;
    }

    public record struct MatchResult(GameResult Result, bool TesterPlayedWhite, int FullPlyCount, int PlyCount, FenUtility.PositionInfo Position);
}