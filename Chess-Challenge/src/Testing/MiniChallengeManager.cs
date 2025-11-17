using System;
using ChessChallenge.API;
using ChessChallenge.Chess;
using Spectre.Console;
using Board = ChessChallenge.Chess.Board;
using Move = ChessChallenge.Chess.Move;

namespace ChessChallenge.Testing;

public class MiniChallengeManager
{
    enum MiniBotType
    {
        MyBot,
        Stockfish
    }

    public const int GameDurationMilliseconds = 60000;
    public int MyTimeSpentThinking = 0;
    public int StockfishTimeSpentThinking = 0;
    public IChessBot Bot;
    public UCIBot.UCIBot Stockfish;
    public bool BotPlaysWhite = true;

    public Board Board;
    public readonly MoveGenerator MoveGenerator = new();
    public GameResult GameResult;

    private readonly Random rng = new();

    public MiniChallengeManager(IChessBot bot)
    {
        Bot = bot;
    }

    public void StartMatch(string fen, bool? botPlaysWhite = null)
    {
        Board = new();
        Board.LoadPosition(fen);
        Stockfish = new UCIBot.UCIBot(UCIBot.UCIBot.STOCKFISH_PATH, false);
        GameResult = GameResult.NotStarted;
        if (!botPlaysWhite.HasValue)
        {
            BotPlaysWhite = Board.IsWhiteToMove;
        }
        else
        {
            BotPlaysWhite = botPlaysWhite.Value;
        }
    }

    public GameResult PlayMatch(string fen, bool? botPlaysWhite = null)
    {
        StartMatch(fen, botPlaysWhite);
        while (!Arbiter.IsWinResult(GameResult) && !Arbiter.IsDrawResult(GameResult))
        {
            PlayMove(GetNextMove());
        }

        return GameResult;
    }

    public void PlayMove(Move move)
    {
        if (move.IsNull)
        {
            GameResult = Board.IsWhiteToMove ? GameResult.WhiteIllegalMove : GameResult.BlackIllegalMove;
            return;
        }

        Board.MakeMove(move, false);
        GameResult = Arbiter.GetGameState(Board);
    }

    public Move GetNextMove()
    {
        bool botMove = (BotPlaysWhite && Board.IsWhiteToMove) || !(BotPlaysWhite || Board.IsWhiteToMove);
        Move move = Move.NullMove;
        if (botMove)
        {
            move = GetBotMove();
        }
        else
        {
            move = GetBotMove(MiniBotType.Stockfish);
        }

        return move;
    }

    Move GetBotMove(MiniBotType botType = MiniBotType.MyBot)
    {
        API.Board botBoard = new(Board);
        try
        {
            int timeSpentThinking = botType == MiniBotType.MyBot ? MyTimeSpentThinking : StockfishTimeSpentThinking;
            IChessBot bot = botType == MiniBotType.MyBot ? Bot : Stockfish;
            Timer timer = new(GameDurationMilliseconds - timeSpentThinking, GameDurationMilliseconds,
                GameDurationMilliseconds);
            API.Move move = bot.Think(botBoard, timer);
            if (botType == MiniBotType.MyBot)
            {
                MyTimeSpentThinking += timer.MillisecondsElapsedThisTurn;
            }
            else
            {
                StockfishTimeSpentThinking += timer.MillisecondsElapsedThisTurn;
            }

            return new Move(move.RawValue);
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine("[red]An error occurred while bot was thinking.[/]");
            AnsiConsole.WriteException(e);
        }

        return Move.NullMove;
    }
}