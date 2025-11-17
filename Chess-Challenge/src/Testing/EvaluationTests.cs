using ChessChallenge.API;

namespace ChessChallenge.Testing;

public class EvaluationTests: TestSuite
{
    
    // static void StockFishEvalComparisonTest()
    // {
    //     WriteWithCol("Running Stockfish Evaluation Comparison Tests", ConsoleColor.Cyan);
    //     MyBot bot = new();
    //     MethodInfo? evaluatePositionMove =
    //         typeof(MyBot).GetMethod("EvaluatePosition", BindingFlags.Instance | BindingFlags.NonPublic);
    //     UCIBot.UCIBot stockfish = new UCIBot.UCIBot(UCIBot.UCIBot.STOCKFISH_PATH, false);
    //     Dictionary<string, int> gamePhaseMoveCounts =
    //         new() { { "Early Game", 15 }, { "Mid Game", 25 }, { "End Game", 50 } };
    //
    //     List<string> cumulativeStatsArrays = new();
    //     string greatestDistanceStats = "";
    //     int greatestDistance = 0;
    //     foreach (KeyValuePair<string, int> gamePhase in gamePhaseMoveCounts)
    //     {
    //         WriteWithCol($"Testing {gamePhase.Key} positions", ConsoleColor.Cyan);
    //         string fileName;
    //         switch (gamePhase.Key)
    //         {
    //             case "Early Game":
    //                 fileName = "earlyFens";
    //                 break;
    //             case "Mid Game":
    //                 fileName = "midFens";
    //                 break;
    //             case "End Game":
    //                 fileName = "endFens";
    //                 break;
    //             default:
    //                 fileName = "fens";
    //                 break;
    //         }
    //
    //         string[] fens = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "TestData", fileName + ".txt"))
    //             .Split("\n");
    //         int totalTestCount = fens.Length;
    //         int skippedTestCount = 0;
    //         int failedTestCount = 0;
    //         int differenceSum = 0;
    //         for (int i = 0; i < totalTestCount; i++)
    //         {
    //             Board board = new Board();
    //             board.LoadPosition(fens[i]);
    //             API.Board apiBoard = new(board);
    //             int stockfishEval = stockfish.EvaluatePosition(apiBoard);
    //             int myBotEval = (int)evaluatePositionMove.Invoke(bot, new object?[] { apiBoard })!;
    //             int difference = Math.Abs(myBotEval - stockfishEval);
    //             float percentError =
    //                 Math.Abs((float)stockfishEval - myBotEval) / (Math.Abs((float)stockfishEval)) * 100;
    //             WriteWithCol($"Position: {apiBoard.GetFenString()}", ConsoleColor.DarkGray);
    //             string stats =
    //                 $"Stockfish: {stockfishEval} cp; MyBot: {myBotEval} cp; Difference: {difference}; Percent Error: {percentError}%";
    //             if (Math.Abs(stockfishEval) > 49000)
    //             {
    //                 skippedTestCount++;
    //                 continue;
    //             }
    //
    //             if (difference > greatestDistance)
    //             {
    //                 greatestDistance = difference;
    //                 greatestDistanceStats = stats;
    //             }
    //
    //             differenceSum += difference;
    //             if ((difference > 200 && percentError > 75) || Math.Sign(myBotEval) != Math.Sign(stockfishEval))
    //             {
    //                 WriteWithCol(stats);
    //                 failedTestCount++;
    //             }
    //             else
    //             {
    //                 WriteWithCol(stats, ConsoleColor.Green);
    //             }
    //         }
    //
    //         int adjustedTotalTestCount = totalTestCount - skippedTestCount;
    //         if (failedTestCount > adjustedTotalTestCount / 2)
    //         {
    //             anyFailed = true;
    //         }
    //
    //         int passedTestCount = adjustedTotalTestCount - failedTestCount;
    //         cumulativeStatsArrays.Add(
    //             $"{gamePhase.Key}:\nSkipped Tests: {skippedTestCount}; Successful Tests: {passedTestCount}; Failed Tests: {failedTestCount}; Ratio (Success/Failed): {(float)passedTestCount / failedTestCount}\nAverage Difference: {(float)differenceSum / adjustedTotalTestCount}");
    //     }
    //
    //     Console.WriteLine($"Greatest Difference: {greatestDistance}\nStats: {greatestDistanceStats}");
    //
    //     string fileData = "";
    //     foreach (string stats in cumulativeStatsArrays)
    //     {
    //         fileData += stats + "\n";
    //         WriteWithCol(stats, ConsoleColor.Cyan);
    //     }
    //
    //     string directoryPath = Path.Combine(Environment.CurrentDirectory, "TestData");
    //     Directory.CreateDirectory(directoryPath);
    //     string fullPath = Path.Combine(directoryPath, "testData.txt");
    //     using (StreamWriter sw = new StreamWriter(fullPath, true))
    //     {
    //         sw.Write("\n" + fileData);
    //     }
    // }
    
    // static void EvaluationBenchmark(EvaluationFlags flags)
    // {
    //     Stopwatch sw = new();
    //     Random rng = new();
    //     Evaluate evaluate = new Evaluate(flags);
    //     int testCount = 10000;
    //     for (int i = 0; i < testCount; i++)
    //     {
    //         int moveCount = rng.Next(5, 30);
    //         Board board = RandomBoard(moveCount);
    //         API.Board apiBoard = new(board);
    //         sw.Start();
    //         evaluate.EvaluatePosition(apiBoard);
    //         sw.Stop();
    //     }
    //     WriteWithCol("Evaluation Benchmarks:", ConsoleColor.Cyan);
    //     Console.WriteLine($"Flags: {flags.ToString()}");
    //     Console.WriteLine($"Test count: {testCount} Total time: {sw.ElapsedMilliseconds}ms Average time per test: {sw.ElapsedMilliseconds / (double)testCount}ms");
    //     
    // }

    public EvaluationTests(BotTester botTester) : base(botTester)
    {
    }

    public override string Name { get; } = "Evaluation Tests";
    public override SuiteResult RunSuiteGroup(IChessBot bot, string groupId)
    {
        throw new System.NotImplementedException();
    }

    public override SuiteGroup[] GetAllGroups()
    {
        throw new System.NotImplementedException();
    }

    public override bool SetOpponent(IChessBot opponent)
    {
        throw new System.NotImplementedException();
    }
}