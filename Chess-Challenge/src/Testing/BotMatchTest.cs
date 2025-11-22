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
                .AddChoices(1, 10, 50, 250, 500)
                .UseConverter(size => size switch
                {
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
        string[] subFenList = new string[suiteSize];
        Array.Copy(fenList, subFenList, suiteSize);

        MiniChallengeManager manager = new MiniChallengeManager(newBot, baseBot);

        Stopwatch stopwatch = Stopwatch.StartNew();
        MiniChallengeManager.MatchResult[] results = manager.PlayMatches(subFenList, false, OnMatchFinished);
        stopwatch.Stop();

        int gamesWon = 0, gamesLost = 0;
        foreach (MiniChallengeManager.MatchResult matchResult in results)
        {
            string botResult = matchResult switch
            {
                { Result: var result } when !Arbiter.IsWinResult(result) => $"{newBot} drew",
                { Result: var result } when matchResult.TesterPlayedWhite == Arbiter.IsWhiteWinsResult(result) =>
                    $"{newBot} won",
                _ => $"{newBot} lost"
            };
            DebugLog($"{botResult}, {matchResult.Result}: {matchResult.Position.fen}");
            switch (matchResult.TesterPlayedWhite)
            {
                case true:
                {
                    if (Arbiter.IsWhiteWinsResult(matchResult.Result))
                    {
                        gamesWon++;
                    }

                    if (Arbiter.IsBlackWinsResult(matchResult.Result))
                    {
                        gamesLost++;
                    }

                    break;
                }
                case false:
                {
                    if (Arbiter.IsWhiteWinsResult(matchResult.Result))
                    {
                        gamesLost++;
                    }

                    if (Arbiter.IsBlackWinsResult(matchResult.Result))
                    {
                        gamesWon++;
                    }

                    break;
                }
            }
        }

        return new SuiteResult(groupId, results.Length, gamesWon, gamesLost, Array.Empty<SuiteResult>(),
            stopwatch.Elapsed);
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