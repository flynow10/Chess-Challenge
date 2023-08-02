using ChessChallenge.Chess;
using System;
using System.Reflection;
using ChessChallenge.API;
using ChessChallenge.Application;
using Board = ChessChallenge.Chess.Board;
using Move = ChessChallenge.Chess.Move;

namespace ChessChallenge.Version4;

public static class Tester {
    const bool throwOnAssertFail = false;
    private const bool runMateTests = true;

    private static MiniChallengeManager controller = new();
    
    static bool anyFailed;
    private static bool humanPlaysOpponent;
    public static void Run(bool humanPlaysOpponent = false)
    {
        anyFailed = false;
        Tester.humanPlaysOpponent = humanPlaysOpponent;

        if (runMateTests)
        {
            MateInThreeTests();
            MateInFourTests();
        }
        CenterManhattenDistanceTest();

        if (anyFailed)
        {
            WriteWithCol("TEST FAILED");
        }
        else
        {
            WriteWithCol("ALL TESTS PASSED", ConsoleColor.Green);
        }
    }
    
    struct FenPair
    {
        public string better;
        public string worse;

        public FenPair(string better, string worse)
        {
            this.better = better;
            this.worse = worse;
        }
    }

    static void CenterManhattenDistanceTest()
    {
        var bot = new MyBot();
        FenPair[] fens =
        {
            new ("8/6k1/8/8/4K3/8/8/8 w - - 0 1", "8/8/5k2/8/4K3/8/8/8 w - - 0 1"),
            new ("8/6k1/8/8/4K3/8/8/8 w - - 0 1", "8/6k1/8/8/8/8/4K3/8 w - - 0 1")
        };
        var evaluateMethod =
            typeof(MyBot).GetMethod("EvaluatePosition", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (FenPair pair in fens)
        {
            var betterBoard = new Board();
            betterBoard.LoadPosition(pair.better);
            var betterBoardAPI = new API.Board(betterBoard);
            var worseBoard = new Board();
            worseBoard.LoadPosition(pair.worse);
            var worseBoardAPI = new API.Board(worseBoard);
            int betterEval = (int)evaluateMethod.Invoke(bot, new []{betterBoardAPI})!;
            int worseEval = (int)evaluateMethod.Invoke(bot, new []{worseBoardAPI})!;
            Assert( betterEval> worseEval, $"Position evaluation comparison is incorrect for {pair.better} > {pair.worse}, evaluation (better): {betterEval}, (worse): {worseEval}");
        }
    }

    static void MateInThreeTests()
    {
        Console.WriteLine("Running Mate In Three Tests");
        string[] mateInThreeFens =
        {
            "r1b1kb1r/pppp1ppp/5q2/4n3/3KP3/2N3PN/PPP4P/R1BQ1B1R b kq - 0 1",
            "r3k2r/ppp2Npp/1b5n/4p2b/2B1P2q/BQP2P2/P5PP/RN5K w kq - 1 1",
            "r1b3kr/ppp1Bp1p/1b6/n2P4/2p3q1/2Q2N2/P4PPP/RN2R1K1 w - - 1 1",
            "r2n1rk1/1ppb2pp/1p1p4/3Ppq1n/2B3P1/2P4P/PP1N1P1K/R2Q1RN1 b - - 0 1",
            "3q1r1k/2p4p/1p1pBrp1/p2Pp3/2PnP3/5PP1/PP1Q2K1/5R1R w - - 1 1",
            "6k1/ppp2ppp/8/2n2K1P/2P2P1P/2Bpr3/PP4r1/4RR2 b - - 0 1",
            "rn3rk1/p5pp/2p5/3Ppb2/2q5/1Q6/PPPB2PP/R3K1NR b - - 0 1",
            "N1bk4/pp1p1Qpp/8/2b5/3n3q/8/PPP2RPP/RNB1rBK1 b - - 0 1",
            "8/2p3N1/6p1/5PB1/pp2Rn2/7k/P1p2K1P/3r4 w - - 1 1",
            "r1b1k1nr/p2p1ppp/n2B4/1p1NPN1P/6P1/3P1Q2/P1P1K3/q5b1 w - - 1 1"
        };
        MateTest(mateInThreeFens, 3);
    }

    static void MateInFourTests()
    {
        Console.WriteLine("Running Mate In Four Tests");
        string[] mateInFourFens =
        {
            "r5rk/2p1Nppp/3p3P/pp2p1P1/4P3/2qnPQK1/8/R6R w - - 1 1",
            "1r2k1r1/pbppnp1p/1b3P2/8/Q7/B1PB1q2/P4PPP/3R2K1 w - - 1 1",
            "Q7/p1p1q1pk/3p2rp/4n3/3bP3/7b/PP3PPK/R1B2R2 b - - 0 1",
            "r1bqr3/ppp1B1kp/1b4p1/n2B4/3PQ1P1/2P5/P4P2/RN4K1 w - - 1 1",
            "r1b3kr/3pR1p1/ppq4p/5P2/4Q3/B7/P5PP/5RK1 w - - 1 1",
            "2k4r/1r1q2pp/QBp2p2/1p6/8/8/P4PPP/2R3K1 w - - 1 1"
        };
        MateTest(mateInFourFens, 4);
    }

    static void MateTest(string[] fens, int mateDistance, bool forceQuickest = true)
    {
        foreach (string fenString in fens)
        {
            Console.WriteLine($"Mate in {mateDistance} test:");
            Console.WriteLine($"Fen: {fenString}");
            if (forceQuickest)
            {
                controller.StartMatch(fenString);
                for (int moveIndex = 0; moveIndex < Math.Max(mateDistance, 50); moveIndex++)
                {
                    controller.PlayMove(controller.GetNextMove());
                    if (controller.GameResult != GameResult.InProgress)
                        break;
                    if (!Assert(moveIndex / 2 < mateDistance-1, "Mate move limit reached"))
                        break;
                }
            }
            else
            {
                controller.PlayMatch(fenString);
            }

            Assert(!Arbiter.IsWinResult(controller.GameResult) || (Arbiter.IsWinResult(controller.GameResult) && controller.Board.IsWhiteToMove != controller.BotPlaysWhite), "Wrong player won!");
            Assert(!Arbiter.IsDrawResult(controller.GameResult), "Game ended in a draw");
        }
    }

    static bool Assert(bool condition, string msg)
    {
        if (!condition)
        {
            WriteWithCol(msg);
            anyFailed = true;
            if (throwOnAssertFail)
            {
                throw new Exception();
            }
        }

        return condition;
    }


    static void WriteWithCol(string msg, ConsoleColor col = ConsoleColor.Red)
    {
        Console.ForegroundColor = col;
        Console.WriteLine(msg);
        Console.ResetColor();
    }

    class MiniChallengeManager
    {
        public const int GameDurationMilliseconds = 60000;
        public int TimeSpentThinking = 0;
        public MyBot Bot;
        public bool BotPlaysWhite = true;
        
        public Board Board;
        public readonly MoveGenerator MoveGenerator = new();
        public GameResult GameResult;
        
        private readonly Random rng = new();

        public void StartMatch(string fen, bool? botPlaysWhite = null)
        {
            Board = new();
            Board.LoadPosition(fen);
            Bot = new MyBot();
            GameResult = GameResult.NotStarted;
            if (botPlaysWhite == null)
            {
                BotPlaysWhite = Board.IsWhiteToMove;
            }
            else
            {
                BotPlaysWhite = (bool)botPlaysWhite;
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
                if (humanPlaysOpponent)
                {
                    move = GetHumanMove();
                }
                else
                {
                    move = GetRandomMove();
                }
            }

            return move;
        }
        
        Move GetBotMove()
        {
            API.Board botBoard = new(Board);
            try
            {
                API.Timer timer = new(GameDurationMilliseconds - TimeSpentThinking, GameDurationMilliseconds, GameDurationMilliseconds);
                API.Move move = Bot.Think(botBoard, timer);
                return new Move(move.RawValue);
            }
            catch (Exception e)
            {
                WriteWithCol("An error occurred while bot was thinking.\n" + e);
            }

            return Move.NullMove;
        }

        Move GetHumanMove()
        {
            Move opponentMove = Move.NullMove;
            while (!IsLegal(opponentMove))
            {
                Console.Write("Input Opponent Move: ");
                string? moveString = null;
                try
                {
                    moveString = Console.ReadLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    continue;
                }
                if (moveString != null)
                {
                    opponentMove = MoveUtility.GetMoveFromUCIName(moveString, Board);
                }
            }

            return opponentMove;
        }

        Move GetRandomMove()
        {
            var legalMoves = MoveGenerator.GenerateMoves(Board);
            return legalMoves[rng.Next(legalMoves.Length)];
        }
        
        bool IsLegal(Move givenMove)
        {
            var moves = MoveGenerator.GenerateMoves(Board);
            foreach (var legalMove in moves)
            {
                if (givenMove.Value == legalMove.Value)
                {
                    return true;
                }
            }

            return false;
        }
    }
}