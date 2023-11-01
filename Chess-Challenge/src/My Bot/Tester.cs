using ChessChallenge.Chess;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ChessChallenge.API;
using ChessChallenge.Application;
using Board = ChessChallenge.Chess.Board;
using Move = ChessChallenge.Chess.Move;

namespace ChessChallenge.MyBot;

public static class Tester {
    const bool throwOnAssertFail = false;
    const bool runMateTests = true;

    private static MiniChallengeManager controller = new();
    
    static bool anyFailed;
    private static bool humanPlaysOpponent;
    public static void Run(bool humanPlaysOpponent = false)
    {
        anyFailed = false;
        Tester.humanPlaysOpponent = humanPlaysOpponent;

        if (runMateTests)
        {
            MateInTwoTests();
            MateInThreeTests();
            MateInFourTests();
        }
        // PieceSquareTablesTest();
        EvaluationTest();

        if (anyFailed)
        {
            WriteWithCol("TESTS FAILED");
        }
        else
        {
            WriteWithCol("ALL TESTS PASSED", ConsoleColor.Green);
        }
    }

    public static Board RandomBoard(int moveCount = 20)
    {
        Board board = new Board();
        board.LoadStartPosition();
        MoveGenerator moveGenerator = new MoveGenerator();
        Random random = new Random();
        for (int i = 0; i < moveCount; i++)
        {
            Span<Move> legalMoves = moveGenerator.GenerateMoves(board);
            if (legalMoves.Length != 0)
            {
                Move nextMove = legalMoves[random.Next(legalMoves.Length)];
                board.MakeMove(nextMove, false);
                if (Arbiter.GetGameState(board) != GameResult.InProgress)
                {
                    board.UndoMove(nextMove, false);
                    break;
                }
            }
            else break;
        }
        return board;
    }
    
    static void EvaluationTest()
    {
        WriteWithCol("Running Stockfish Evaluation Comparison Tests", ConsoleColor.Cyan);
        MyBot bot = new();
        MethodInfo? evaluatePositionMove =
            typeof(MyBot).GetMethod("EvaluatePosition", BindingFlags.Instance | BindingFlags.NonPublic);
        UCIBot.UCIBot stockfish = new UCIBot.UCIBot(UCIBot.UCIBot.STOCKFISH_PATH, false);
        Dictionary<string, int> gamePhaseMoveCounts =
            new() { { "Early Game", 15 }, { "Mid Game", 25 }, { "End Game", 50 } };
        
        List<string> cumulativeStatsArrays = new();
        string greatestDistanceStats = "";
        int greatestDistance = 0;
        foreach (KeyValuePair<string,int> gamePhase in gamePhaseMoveCounts)
        {
            WriteWithCol($"Testing {gamePhase.Key} positions", ConsoleColor.Cyan);
            string fileName;
            switch (gamePhase.Key)
            {
                case "Early Game":
                    fileName = "earlyFens";
                    break;
                case "Mid Game":
                    fileName = "midFens";
                    break;
                case "End Game":
                    fileName = "endFens";
                    break;
                default:
                    fileName = "fens";
                    break;
            }

            string[] fens = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "TestData", fileName + ".txt")).Split("\n");
            int totalTestCount = fens.Length;
            int skippedTestCount = 0;
            int failedTestCount = 0;
            int differenceSum = 0;
            for (int i = 0; i < totalTestCount; i++)
            {
                Board board = new Board();
                board.LoadPosition(fens[i]);
                API.Board apiBoard = new(board);
                int stockfishEval = stockfish.EvaluatePosition(apiBoard);
                int myBotEval = (int)evaluatePositionMove.Invoke(bot, new object?[] { apiBoard })!;
                int difference = Math.Abs(myBotEval - stockfishEval);
                float percentError = Math.Abs((float)stockfishEval - myBotEval) / (Math.Abs((float)stockfishEval)) * 100;
                WriteWithCol($"Position: {apiBoard.GetFenString()}", ConsoleColor.DarkGray);
                string stats = $"Stockfish: {stockfishEval} cp; MyBot: {myBotEval} cp; Difference: {difference}; Percent Error: {percentError}%";
                if (Math.Abs(stockfishEval) > 49000)
                {
                    skippedTestCount++;
                    continue;
                }
                
                if (difference > greatestDistance)
                {
                    greatestDistance = difference;
                    greatestDistanceStats = stats;
                }

                differenceSum += difference;
                if ((difference > 200 && percentError > 75) || Math.Sign(myBotEval) != Math.Sign(stockfishEval))
                {
                    WriteWithCol(stats);
                    failedTestCount++;
                }
                else
                {
                    WriteWithCol(stats, ConsoleColor.Green);
                }
            }

            int adjustedTotalTestCount = totalTestCount - skippedTestCount;
            if (failedTestCount > adjustedTotalTestCount / 2)
            {
                anyFailed = true;
            }

            int passedTestCount = adjustedTotalTestCount - failedTestCount;
            cumulativeStatsArrays.Add($"{gamePhase.Key}:\nSkipped Tests: {skippedTestCount}; Successful Tests: {passedTestCount}; Failed Tests: {failedTestCount}; Ratio (Success/Failed): {(float)passedTestCount/failedTestCount}\nAverage Difference: {(float)differenceSum/adjustedTotalTestCount}");
        }
        
        Console.WriteLine($"Greatest Difference: {greatestDistance}\nStats: {greatestDistanceStats}");

        string fileData = "";
        foreach (string stats in cumulativeStatsArrays)
        {
            fileData += stats + "\n";
            WriteWithCol(stats, ConsoleColor.Cyan);
        }

        string directoryPath = Path.Combine(Environment.CurrentDirectory, "TestData");
        Directory.CreateDirectory(directoryPath);
        string fullPath = Path.Combine(directoryPath, "testData.txt");
        using (StreamWriter sw = new StreamWriter(fullPath, true))
        {
            sw.Write("\n" + fileData);
        }
    }

    static void PieceSquareTablesTest()
    {
        var bot = new MyBot();
        var getPieceSquareValueMethod =
            typeof(MyBot).GetMethod("GetPieceSquareValue", BindingFlags.Instance | BindingFlags.NonPublic);

        for (PieceType pieceType = 0; pieceType < PieceType.King + 1; pieceType++)
        {
            if(pieceType < PieceType.King)
                Console.WriteLine($"{(pieceType + 1).ToString()} Piece Square Table");
            else
                Console.WriteLine("King End Piece Square Table");
            var table = new int[64].Select((_, index) =>
            {
                var square = new Square(index);
                square = new Square((7 - square.Rank) * 8 + square.File);
                return (int)getPieceSquareValueMethod.Invoke(bot, new object?[]{pieceType, square.Index})!;
            }).ToArray();
            
            for (int i = 0; i < table.Length; i++)
            {
                if(i % 8 == 0)
                    Console.WriteLine();
                Console.Write($"{table[i]}, ");
            }
            Console.WriteLine();
        }
    }

    static void MateInTwoTests()
    {
        WriteWithCol("Running Mate in Two Tests", ConsoleColor.Cyan);
        string[] mateInTwoFens =
        {
            "r2qkb1r/pp2nppp/3p4/2pNN1B1/2BnP3/3P4/PPP2PPP/R2bK2R w KQkq - 1 25",
            "1rb4r/pkPp3p/1b1P3n/1Q6/N3Pp2/8/P1P3PP/7K w - - 1 25",
            "4kb1r/p2n1ppp/4q3/4p1B1/4P3/1Q6/PPP2PPP/2KR4 w k - 1 25",
            "r1b2k1r/ppp1bppp/8/1B1Q4/5q2/2P5/PPP2PPP/R3R1K1 w - - 1 25",
            "5rkr/pp2Rp2/1b1p1Pb1/3P2Q1/2n3P1/2p5/P4P2/4R1K1 w - - 1 25",
            "6k1/pp4p1/2p5/2bp4/8/P5Pb/1P3rrP/2BRRN1K b - - 0 25",
            "rnbqkbn1/ppppp3/7r/6pp/3P1p2/3BP1B1/PPP2PPP/RN1QK1NR w - - 1 25"
        };
        MateTest(mateInTwoFens, 2);
    }

    static void MateInThreeTests()
    {
        WriteWithCol("Running Mate In Three Tests", ConsoleColor.Cyan);
        string[] mateInThreeFens =
        {
            "r1b1kb1r/pppp1ppp/5q2/4n3/3KP3/2N3PN/PPP4P/R1BQ1B1R b kq - 0 25",
            "r3k2r/ppp2Npp/1b5n/4p2b/2B1P2q/BQP2P2/P5PP/RN5K w kq - 1 25",
            "r1b3kr/ppp1Bp1p/1b6/n2P4/2p3q1/2Q2N2/P4PPP/RN2R1K1 w - - 1 25",
            "r2n1rk1/1ppb2pp/1p1p4/3Ppq1n/2B3P1/2P4P/PP1N1P1K/R2Q1RN1 b - - 0 25",
            "3q1r1k/2p4p/1p1pBrp1/p2Pp3/2PnP3/5PP1/PP1Q2K1/5R1R w - - 1 25",
            "6k1/ppp2ppp/8/2n2K1P/2P2P1P/2Bpr3/PP4r1/4RR2 b - - 0 25",
            "rn3rk1/p5pp/2p5/3Ppb2/2q5/1Q6/PPPB2PP/R3K1NR b - - 0 25",
            "N1bk4/pp1p1Qpp/8/2b5/3n3q/8/PPP2RPP/RNB1rBK1 b - - 0 25",
            "8/2p3N1/6p1/5PB1/pp2Rn2/7k/P1p2K1P/3r4 w - - 1 25",
            "r1b1k1nr/p2p1ppp/n2B4/1p1NPN1P/6P1/3P1Q2/P1P1K3/q5b1 w - - 1 25"
        };
        MateTest(mateInThreeFens, 3);
    }

    static void MateInFourTests()
    {
        WriteWithCol("Running Mate In Four Tests", ConsoleColor.Cyan);
        string[] mateInFourFens =
        {
            "r5rk/2p1Nppp/3p3P/pp2p1P1/4P3/2qnPQK1/8/R6R w - - 1 25",
            "1r2k1r1/pbppnp1p/1b3P2/8/Q7/B1PB1q2/P4PPP/3R2K1 w - - 1 25",
            "Q7/p1p1q1pk/3p2rp/4n3/3bP3/7b/PP3PPK/R1B2R2 b - - 0 25",
            "r1bqr3/ppp1B1kp/1b4p1/n2B4/3PQ1P1/2P5/P4P2/RN4K1 w - - 1 25",
            "r1b3kr/3pR1p1/ppq4p/5P2/4Q3/B7/P5PP/5RK1 w - - 1 25",
            "2k4r/1r1q2pp/QBp2p2/1p6/8/8/P4PPP/2R3K1 w - - 1 25"
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

    enum MiniBotType
    {
        MyBot,
        Stockfish
    }

    class MiniChallengeManager
    {
        public const int GameDurationMilliseconds = 60000;
        public int MyTimeSpentThinking = 0;
        public int StockfishTimeSpentThinking = 0;
        public MyBot Bot;
        public UCIBot.UCIBot Stockfish;
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
            Stockfish = new UCIBot.UCIBot(UCIBot.UCIBot.STOCKFISH_PATH, false);
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
                    move = GetBotMove(MiniBotType.Stockfish);
                }
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
                Timer timer = new(GameDurationMilliseconds - timeSpentThinking, GameDurationMilliseconds, GameDurationMilliseconds);
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