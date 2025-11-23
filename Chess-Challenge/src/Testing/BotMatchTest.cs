using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ChessChallenge.Application;
using ChessChallenge.Chess;
using Spectre.Console;

namespace ChessChallenge.Testing;

public class BotMatchTest(BotTester botTester) : TestSuite(botTester)
{
    private static readonly string[] fenList =
        FileHelper.ReadResourceFile("Fens.txt").Split('\n').Where(fen => fen.Length > 0).ToArray();

    private string _testDate = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");

    public override string Name => "Bot Match Test";

    private int AskSuiteSize()
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<int>()
                .Title("What size suite would you like to run?")
                .AddChoices(-1, 1, 10, 50, 250, 500)
                .UseConverter(size => size switch
                {
                    -1 => "SPRT Test",
                    1 => "Test match (1)",
                    10 => "Tiny (10)",
                    50 => "Small (50)",
                    250 => "Medium (250)",
                    500 => "Full (500)",
                    _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
                })
        );
    }

    private void OnMatchFinished(MiniChallengeManager.MatchResult matchResult)
    {
        string testId = GetTestId(matchResult.newBot,  matchResult.baseBot);
        string directoryPath = Path.Combine(FileHelper.AppDataPath, "TestResults");
        Directory.CreateDirectory(directoryPath);
        string fileName = testId + ".txt";
        string fullPath = Path.Combine(directoryPath, fileName);
        File.AppendAllText(fullPath, "\n" + matchResult.PGN);
    }

    public override SuiteResult RunSuiteGroup(BotReference newBot, BotReference baseBot, string groupId)
    {
        int suiteSize = AskSuiteSize();
        if (suiteSize == -1)
        {
            return SPRTTest(newBot, baseBot, groupId);
        }
        
        string[] subFenList = new string[suiteSize];
        Array.Copy(fenList, subFenList, suiteSize);

        MiniChallengeManager manager = new MiniChallengeManager(newBot, baseBot);

        Stopwatch stopwatch = Stopwatch.StartNew();
        MiniChallengeManager.MatchResult[] results = manager.PlayMatches(subFenList, false, OnMatchFinished);
        stopwatch.Stop();
        
        PrintMatchStatistics(results, newBot, baseBot);

        return CreateSuiteResult(groupId, results, stopwatch.Elapsed);
    }

    private SuiteResult SPRTTest(BotReference newBot, BotReference baseBot, string groupId)
    {
        MiniChallengeManager manager = new MiniChallengeManager(newBot, baseBot)
        {
            GameDurationMilliseconds = 10000,
            IncrementMilliseconds = 100
        };

        MiniChallengeManager.SPRTOptions options = new MiniChallengeManager.SPRTOptions();
        options.elo1 = 1;
        Stopwatch stopwatch = Stopwatch.StartNew();
        MiniChallengeManager.SPRTResult result = manager.RunSPRT(fenList, options);
        stopwatch.Stop();
        
        PrintMatchStatistics(result.Results, newBot, baseBot);
        DebugLog($"LLR: {result.LLR}");
        
        return CreateSuiteResult(groupId, result.Results, stopwatch.Elapsed);
    } 

    private void PrintMatchStatistics(MiniChallengeManager.MatchResult[] results, BotReference newBot,
        BotReference baseBot)
    {
        int won = results.Count(result => Arbiter.IsWinResult(result.Result) && result.TesterPlayedWhite == Arbiter.IsWhiteWinsResult(result.Result));
        int lost = results.Count(result => Arbiter.IsWinResult(result.Result) && result.TesterPlayedWhite != Arbiter.IsWhiteWinsResult(result.Result));
        int draw = results.Count(result => Arbiter.IsDrawResult(result.Result));
        int total = won + lost + draw;
        double score = won + (double)draw / 2;
        double percentage = score / total;
        double elo = Stats.Elo(percentage);
        TokenCount newTokens = newBot.GetTokenCount();
        TokenCount baseTokens = baseBot.GetTokenCount();
        int tokenDifference = newTokens.total - newTokens.debug - (baseTokens.total - baseTokens.debug);
        
        DebugLog("");
        DebugLog($"{newBot} vs {baseBot}");
        DebugLog($"Tokens: {newTokens.total - newTokens.debug}");
        DebugLog($"Token Diff: {(tokenDifference > 0 ? "+" : tokenDifference < 0 ? "-" : "")}{Math.Abs(tokenDifference)}");
        DebugLog($"W: {won} L: {lost} D: {draw} T: {total}");
        DebugLog($"S: {score:F1} E: {(int)elo}");
    }

    private SuiteResult CreateSuiteResult(string groupId, MiniChallengeManager.MatchResult[] results,
        TimeSpan elapsed)
    {
        int gamesWon = 0, gamesLost = 0;
        foreach (MiniChallengeManager.MatchResult matchResult in results)
        {
            switch (matchResult.TesterPlayedWhite)
            {
                case true:
                {
                    if (Arbiter.IsWhiteWinsResult(matchResult.Result))
                        gamesWon++;
                    if (Arbiter.IsBlackWinsResult(matchResult.Result))
                        gamesLost++;
                    break;
                }
                case false:
                {
                    if (Arbiter.IsWhiteWinsResult(matchResult.Result))
                        gamesLost++;
                    if (Arbiter.IsBlackWinsResult(matchResult.Result))
                        gamesWon++;
                    break;
                }
            }
        }
        return new SuiteResult(groupId, results.Length, gamesWon, gamesLost, Array.Empty<SuiteResult>(),
            elapsed);
    }

    private string GetTestId(BotReference newBot, BotReference baseBot)
    {
        return _testDate + $"-{newBot}-{baseBot}";
    }

    public override SuiteGroup[] GetAllGroups()
    {
        return new[]
        {
            new SuiteGroup("match", "Match")
        };
    }
}