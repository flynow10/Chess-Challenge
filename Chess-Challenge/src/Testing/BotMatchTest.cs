using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChessChallenge.Application;
using ChessChallenge.Chess;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace ChessChallenge.Testing;

public class BotMatchTest(BotTester botTester) : TestSuite(botTester)
{
    private static readonly string[] fenList =
        FileHelper.ReadResourceFile("Fens.txt").Split('\n').Where(fen => fen.Length > 0).ToArray();

    private string _testDate = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");

    private ProgressTask _progressTask;

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

    private (int, int) AskTimeControl()
    {
        string tcString = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("What time control would you like to run?")
            .AddChoices("SSTC (0.16+0.1)", "STC (1+0)", "LTC (10+1)", "Custom"));
        switch (tcString)
        {
            case "SSTC (0.16+0.1)":
                return (10 * 1000, 100);
            case "STC (1+0)":
                return (60 * 1000, 0);
            case "LTC (10+1)":
                return (10 * 60 * 1000, 1000);
            default:
                string customStr = AnsiConsole.Prompt(
                    new TextPrompt<string>("Enter custom time control (eg. 10+0.1 - 10m with .1s inc):")
                        .Validate(str =>
                        {
                            var tc = ParseTimeControl(str);
                            return tc is { Item1: > 0, Item2: >= 0 } && tc.Item1 >= tc.Item2;
                        }));
                return ParseTimeControl(customStr);
        }
    }

    private (int, int) ParseTimeControl(string timeControl)
    {
        string[] chunks = timeControl.Split('+');
        if (chunks.Length != 2)
            return (0, 0);
        if (!double.TryParse(chunks[0], out double minutes))
            return (0, 0);
        if (!double.TryParse(chunks[1], out double increment))
            return (0, 0);
        return ((int)(minutes * 60 * 1000), (int)(increment * 1000));
    }

    private void SaveMatchResult(MiniChallengeManager.MatchResult matchResult, string prefix)
    {
        string testId = prefix + GetTestId(matchResult.newBot, matchResult.baseBot);
        string directoryPath = Path.Combine(FileHelper.AppDataPath, "TestResults");
        Directory.CreateDirectory(directoryPath);
        string fileName = testId + ".txt";
        string fullPath = Path.Combine(directoryPath, fileName);
        File.AppendAllText(fullPath, "\n" + matchResult.PGN);
    }

    private void OnMatchFinished(MiniChallengeManager.MatchResult matchResult)
    {
        SaveMatchResult(matchResult, "");
        _progressTask.Increment(1);
    }

    private void OnSPRTMatchFinished(MiniChallengeManager.MatchResult matchResult)
    {
        SaveMatchResult(matchResult, "sprt-");
    }

    public override SuiteResult RunSuiteGroup(BotReference newBot, BotReference baseBot, string groupId)
    {
        int suiteSize = AskSuiteSize();

        var timeControl = AskTimeControl();
        if (suiteSize == -1)
        {
            return SPRTTest(newBot, baseBot, groupId, timeControl);
        }

        string[] subFenList = new string[suiteSize];
        int numGames = subFenList.Length * 2;
        Array.Copy(fenList, subFenList, suiteSize);
        Progress progress = AnsiConsole.Progress();

        (MiniChallengeManager.MatchResult[] results, TimeSpan elapsed) = progress.Start(ctx =>
        {
            _progressTask = ctx.AddTask("Games");
            _progressTask.MaxValue = numGames;
            MiniChallengeManager manager = new MiniChallengeManager(newBot, baseBot)
            {
                GameDurationMilliseconds = timeControl.Item1,
                IncrementMilliseconds = timeControl.Item2
            };

            Stopwatch stopwatch = Stopwatch.StartNew();
            MiniChallengeManager.MatchResult[] results = manager.PlayMatches(subFenList, false, OnMatchFinished);
            stopwatch.Stop();
            return (results, stopwatch.Elapsed);
        });

        PrintMatchStatistics(results, newBot, baseBot);

        return CreateSuiteResult(groupId, results, elapsed);
    }

    private SuiteResult SPRTTest(BotReference newBot, BotReference baseBot, string groupId, (int, int) timeControl)
    {
        MiniChallengeManager manager = new MiniChallengeManager(newBot, baseBot)
        {
            GameDurationMilliseconds = timeControl.Item1,
            IncrementMilliseconds = timeControl.Item2
        };

        SPRT.SPRTOptions options = new SPRT.SPRTOptions
        {
            Elo1 = 30
        };
        Stopwatch stopwatch = Stopwatch.StartNew();
        MiniChallengeManager.SPRTResult result = manager.RunSPRT(fenList, options, OnSPRTMatchFinished);
        stopwatch.Stop();

        PrintMatchStatistics(result.Results, newBot, baseBot, new Markup($"LLR: {result.LLR}"));

        return CreateSuiteResult(groupId, result.Results, stopwatch.Elapsed);
    }

    private void PrintMatchStatistics(MiniChallengeManager.MatchResult[] results, BotReference newBot,
        BotReference baseBot, IRenderable? additional = null)
    {
        int won = results.Count(result =>
            Arbiter.IsWinResult(result.Result) && result.TesterPlayedWhite == Arbiter.IsWhiteWinsResult(result.Result));
        int lost = results.Count(result =>
            Arbiter.IsWinResult(result.Result) && result.TesterPlayedWhite != Arbiter.IsWhiteWinsResult(result.Result));
        int draw = results.Count(result => Arbiter.IsDrawResult(result.Result));
        int total = won + lost + draw;
        double score = won + (double)draw / 2;
        double percentage = score / total;
        double elo = Stats.Elo(percentage);
        double eloMargin = Stats.CalculateEloErrorMargin(won, draw, lost);

        TokenCount newTokens = newBot.GetTokenCount();
        TokenCount baseTokens = baseBot.GetTokenCount();
        int newTokensPure = newTokens.total - newTokens.debug;
        int baseTokensPure = baseTokens.total - baseTokens.debug;
        int tokenDifference = newTokensPure - baseTokensPure;

        string newBotMarkedUp = $"[{Color.DodgerBlue1.ToMarkup()}]{newBot}[/]";
        string baseBotMarkedUp = $"[{Color.IndianRed.ToMarkup()}]{baseBot}[/]";


        Table tokenTable = new Table
        {
            Title = new TableTitle("Tokens"),
            Border = TableBorder.Rounded
        };
        tokenTable.AddColumns(new TableColumn("Diff"), new TableColumn(new Markup(newBotMarkedUp)),
            new TableColumn(new Markup(baseBotMarkedUp)));
        tokenTable.AddRow(
            new Markup(
                $"[{(tokenDifference < 0 ? "red" : "green")}]{(tokenDifference > 0 ? "+" : tokenDifference < 0 ? "-" : "")}{Math.Abs(tokenDifference)}[/]"),
            new Markup(newTokensPure.ToString()), new Markup(baseTokensPure.ToString()));

        int wldWidth = 60;
        BreakdownChart wldChart = new BreakdownChart();
        wldChart.Width(wldWidth);
        wldChart.AddItem("Wins", won, Color.Green3_1);
        wldChart.AddItem("Draws", draw, Color.Gray);
        wldChart.AddItem("Losses", lost, Color.Red3_1);

        Table scoreTable = new Table
        {
            Title = new TableTitle("Score"),
            Border = TableBorder.Rounded
        };
        scoreTable.AddColumns($"Score: [{Color.DodgerBlue1.ToMarkup()}]{score:F1}[/]",
            $"Elo: [{(elo < 0 ? "red" : "green")}]{elo:0.00}[/] +/-[{Color.DodgerBlue1.ToMarkup()}]{eloMargin}[/]");

        List<IRenderable> rows =
        [
            tokenTable,
            scoreTable,
            wldChart
        ];
        if (additional != null)
        {
            rows.Add(additional);
        }

        Panel panel = new Panel(new Rows(rows).Expand());
        panel.Header(new PanelHeader($"{newBotMarkedUp} vs {baseBotMarkedUp}"));
        panel.Border = BoxBorder.Rounded;
        panel.Collapse();
        AnsiConsole.Write(panel);
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