using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChessChallenge.API;
using ChessChallenge.Application;
using ChessChallenge.Chess;
using Spectre.Console;
using Board = ChessChallenge.Chess.Board;
using Move = ChessChallenge.Chess.Move;
using Timer = ChessChallenge.API.Timer;

namespace ChessChallenge.Testing;

public class MiniChallengeManager
{
    private readonly BotReference _testBot;
    private readonly BotReference _opponentBot;

    private ConcurrentBag<MatchResult> _matchResults = new();

    public int MaxMoves = -1;
    public int GameDurationMilliseconds = 60000;
    public int IncrementMilliseconds = 0;

    public MiniChallengeManager(BotReference testBot) : this(testBot,
        new BotReference(PlayerType.StockFish))

    {
    }

    public MiniChallengeManager(BotReference bot, BotReference opponentBot)
    {
        _testBot = bot;
        _opponentBot = opponentBot;
    }

    public MatchResult[] PlayMatches(string[] fens, bool parallel = false, Action<MatchResult>? onMatchFinished = null)
    {
        List<string> doubledFens = DoubleFens(fens);
        _matchResults.Clear();
        if (parallel)
        {
            CancellationTokenSource cts = new();
            Task.Factory.StartNew(() =>
            {
                if (Console.ReadKey(true).Key == ConsoleKey.Q)
                    cts.Cancel();
            }, cts.Token);
            ParallelOptions options = new() { CancellationToken = cts.Token, MaxDegreeOfParallelism = 2};
            try
            {
                Parallel.ForEach(doubledFens, options, (fen, _, index) =>
                {
                    bool playsWhite = index % 2 == 0;
                    MatchResult matchResult = PlayMatch(fen, playsWhite);
                    _matchResults.Add(matchResult);
                });
                foreach (MatchResult matchResult in _matchResults)
                {
                    onMatchFinished?.Invoke(matchResult);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Canceled bot match!");
            }
            finally
            {
                cts.Dispose();
            }
        }
        else
        {
            for (var i = 0; i < doubledFens.Count; i++)
            {
                string fen = doubledFens[i];
                bool playsWhite = i % 2 == 0;
                MatchResult matchResult = PlayMatch(fen, playsWhite);
                _matchResults.Add(matchResult);
                onMatchFinished?.Invoke(matchResult);
            }
        }
        return _matchResults.ToArray();
    }

    public SPRTResult RunSPRT(string[] fens, SPRTOptions options)
    {
        List<string> doubledFens = DoubleFens(fens);
        List<MatchResult> matchResults = new();
        int hypothesis = 0;
        int fenIndex = 0;
        int wins = 0;
        int loss = 0;
        int draw = 0;
        while (hypothesis == 0)
        {
            bool playsWhite = fenIndex % 2 == 0;
            string fen = doubledFens[fenIndex];
            MatchResult matchResult = PlayMatch(fen, playsWhite);
            if (Arbiter.IsDrawResult(matchResult.Result))
                draw++;
            else if (matchResult.TesterPlayedWhite == Arbiter.IsWhiteWinsResult(matchResult.Result))
                wins++;
            else
                loss++;
            matchResults.Add(matchResult);
            hypothesis = Stats.SPRT(wins, loss, draw, options.elo0, options.elo1, options.alpha, options.beta);
            
            var (LA, LB) = Stats.AlphaBetaWindow(options.alpha, options.beta);
            Console.WriteLine($"W: {wins} L: {loss} D: {draw} T: {wins + loss + draw}");
            Console.WriteLine($"LLR: {Stats.LLR(wins, loss, draw, options.elo0, options.elo1)} ({LA}, {LB})");
            
            fenIndex = (fenIndex + 1) % doubledFens.Count;
        }

        return new SPRTResult(matchResults.ToArray(), Stats.LLR(wins, loss, draw, options.elo0, options.elo1));
    }

    public MatchResult PlayMatch(string fen, bool? testerPlaysWhite = null, CancellationToken cancellationToken = default)
    {
        IChessBot testBot = _testBot.CreateBot();
        IChessBot opponentBot = _opponentBot.CreateBot();
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
            if (cancellationToken.IsCancellationRequested)
            {
                result = GameResult.DrawByArbiter;
                break;
            }
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

        string whiteName = _playsWhite ? _testBot.ToString() : _opponentBot.ToString();
        string blackName = !_playsWhite ? _testBot.ToString() : _opponentBot.ToString();
        string pgn = PGNCreator.CreatePGN(board, result, whiteName, blackName);

        MatchResult matchResult = new MatchResult(result, _testBot, _opponentBot, _playsWhite, board.plyCount, plyCount, position, pgn);

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
                GameDurationMilliseconds, IncrementMilliseconds);
            API.Move move = bot.Think(botBoard, timer);
            timeUsed += timer.MillisecondsElapsedThisTurn - IncrementMilliseconds;
            return new Move(move.RawValue);
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine("[red]An error occurred while bot was thinking.[/]");
            AnsiConsole.WriteException(e);
        }

        return Move.NullMove;
    }

    private List<string> DoubleFens(string[] fens)
    {
        List<string> doubledFens = new List<string>();
        foreach (string fen in fens)
        {
            doubledFens.Add(fen);
            doubledFens.Add(fen);
        }

        return doubledFens;
    }

    public record struct MatchResult(GameResult Result, BotReference newBot, BotReference baseBot, bool TesterPlayedWhite, int FullPlyCount, int PlyCount, FenUtility.PositionInfo Position, string PGN);

    public record struct SPRTResult(MatchResult[] Results, double LLR);

    public record struct SPRTOptions(double elo0 = 0, double elo1 = 10, double alpha = 0.05, double beta = 0.05);
}