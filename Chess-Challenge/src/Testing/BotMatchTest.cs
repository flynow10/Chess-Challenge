using System;
using System.Diagnostics;
using System.Linq;
using ChessChallenge.Application;
using ChessChallenge.Chess;
using Spectre.Console;

namespace ChessChallenge.Testing;

public class BotMatchTest : TestSuite
{
    private static readonly string[] fenList = FileHelper.ReadResourceFile("Fens.txt").Split('\n').Where(fen => fen.Length > 0).ToArray();
    public override string Name => "Bot Match Test";
    private PlayerType _opponent;
    public BotMatchTest(BotTester botTester) : base(botTester)
    {
    }

    private int AskSuiteSize()
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<int>()
                .Title("What size suite would you like to run?")
                .AddChoices(2, 10, 50, 250, 500)
                .UseConverter(size => size switch
                {
                    2 => "Test set (2)",
                    10 => "Tiny (10)",
                    50 => "Small (50)",
                    250 => "Medium (250)",
                    500 => "Full (500)",
                    _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
                })
        );
    }

    public override SuiteResult RunSuiteGroup(PlayerType botType, string groupId)
    {
        int suiteSize = AskSuiteSize();
        string[] subFenList = new string[suiteSize];
        Array.Copy(fenList, subFenList, suiteSize);
        
        MiniChallengeManager manager = new MiniChallengeManager(botType, _opponent, _botTester.CreateBot);
        
        Stopwatch stopwatch = Stopwatch.StartNew();
        MiniChallengeManager.MatchResult[] results = manager.PlayMatches(subFenList);
        stopwatch.Stop();
        
        int gamesWon = 0, gamesLost = 0;
        foreach (MiniChallengeManager.MatchResult matchResult in results)
        {
            DebugLog($"{(Arbiter.IsWinResult(matchResult.Result) && matchResult.TesterPlayedWhite == Arbiter.IsWhiteWinsResult(matchResult.Result) ? $"{botType} won" : $"{botType} lost")}, {matchResult.Result}: {matchResult.Position.fen}");
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
        return new SuiteResult(groupId, results.Length, gamesWon, gamesLost, Array.Empty<SuiteResult>(), stopwatch.Elapsed);
    }

    public override SuiteGroup[] GetAllGroups()
    {
        return new []
        {
            new SuiteGroup("match", "Match")
        };
    }

    public override bool SetOpponent(PlayerType opponent)
    {
        _opponent = opponent;
        return true;
    }
}