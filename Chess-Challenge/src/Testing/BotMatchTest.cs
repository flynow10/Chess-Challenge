using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ChessChallenge.API;
using ChessChallenge.Application;
using ChessChallenge.Chess;
using ChessChallenge.Testing.Utils;
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

    private string TimeControlToString((int, int) timeControl)
    {
        return $"{(double)timeControl.Item1 / 60000:0.00}+{(double)timeControl.Item2 / 1000:0.00}";
    }

    private void SaveMatchResult(MiniChallengeManager.MatchResult matchResult, string prefix)
    {
        string testId = prefix + GetTestId(matchResult.newBot, matchResult.baseBot);
        AppendToFile(testId, matchResult.PGN);
    }

    private void SaveFullTestResult(MiniChallengeManager.MatchResult[] matchResult, BotReference newBot,
        BotReference baseBot, TimeSpan elapsed, MiniChallengeManager.SPRTResult? sprtResult = null)
    {
        MatchStatistics ms = new MatchStatistics(matchResult, newBot, baseBot);
        
        string testId = GetTestId(newBot, baseBot);
        string matchStats = $"""
                             {newBot} vs {baseBot}
                             Tokens
                               Diff: {ms.TokenDifference}
                               New: {ms.NewBotTokens}
                               Base: {ms.BaseBotTokens}
                             Score
                               W: {ms.GamesWon} D: {ms.GamesDrawn} L: {ms.GamesLost} T: {ms.TotalGames}
                               Elo: {ms.Elo} +/-{ms.EloMargin} LOS: {ms.LOS}
                             """;
        if (sprtResult.HasValue)
        {
            testId = "sprt-" + testId;
            matchStats += $"\nLLR: {sprtResult.Value.LLR} ({sprtResult.Value.LowerBound},{sprtResult.Value.UpperBound}) Status: {(sprtResult.Value.Status == SPRT.Status.H1 ? "passed" : "failed")}";
        }

        matchStats += $"\nElapsed: {elapsed.ToString()}";
        AppendToFile(testId, matchStats);
    }

    private void AppendToFile(string testId, string text)
    {
        string directoryPath = Path.Combine(FileHelper.AppDataPath, "TestResults");
        Directory.CreateDirectory(directoryPath);
        string fileName = testId + ".txt";
        string fullPath = Path.Combine(directoryPath, fileName);
        File.AppendAllText(fullPath, "\n" + text);
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
        Logger.Level = Logger.LogLevel.None;
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

        PrintMatchStatistics(results, newBot, baseBot, elapsed);
        SaveFullTestResult(results, newBot, baseBot, elapsed);
        Logger.Level = Logger.LogLevel.Full;

        return CreateSuiteResult(groupId, results, elapsed);
    }

    private SuiteResult SPRTTest(BotReference newBot, BotReference baseBot, string groupId, (int, int) timeControl)
    {
        MiniChallengeManager manager = new MiniChallengeManager(newBot, baseBot)
        {
            GameDurationMilliseconds = timeControl.Item1,
            IncrementMilliseconds = timeControl.Item2,
            Debug = true
        };
        
        SPRT.SPRTOptions options = new SPRT.SPRTOptions
        {
            Elo1 = 10
        };
        
        string status = $"Running SPRT Test... TC: ({TimeControlToString(timeControl)}) Options: {options}";
        (MiniChallengeManager.SPRTResult result, TimeSpan elapsed) = AnsiConsole.Status().Start(status, ctx =>
        {
            ctx.Spinner(Spinner.Known.BouncingBar);

            Stopwatch stopwatch = Stopwatch.StartNew();
            MiniChallengeManager.SPRTResult result = manager.RunSPRT(fenList, options, OnSPRTMatchFinished);
            stopwatch.Stop();
            return (result, stopwatch.Elapsed);
        });
        
        
        PrintMatchStatistics(result.Results, newBot, baseBot, elapsed, CreateSPRTTable(result));
        SaveFullTestResult(result.Results, newBot, baseBot, elapsed, result);
        Logger.Level = Logger.LogLevel.Full;

        return CreateSuiteResult(groupId, result.Results, elapsed);
    }

    private IRenderable CreateSPRTTable(MiniChallengeManager.SPRTResult result)
    {
        List<IRenderable> rows =
        [
            new Markup($"LLR: [{Color.DodgerBlue1.ToMarkup()}]{result.LLR:0.000}[/] Bounds: ([{Color.DodgerBlue1.ToMarkup()}]{result.LowerBound:0.000}[/], [{Color.DodgerBlue1.ToMarkup()}]{result.UpperBound:0.000}[/])"),
            new Markup($"Elo: ([{Color.DodgerBlue1.ToMarkup()}]{result.Options.Elo0}[/], [{Color.DodgerBlue1.ToMarkup()}]{result.Options.Elo1}[/]) Status: [{(result.Status == SPRT.Status.H1 ? "green]passed" : "red]failed")}[/]")
        ];
        Panel panel = new Panel(new Rows(rows));
        panel.Header = new PanelHeader("SPRT");
        panel.RoundedBorder();
        return panel;
    }

    private void PrintMatchStatistics(MiniChallengeManager.MatchResult[] results, BotReference newBot,
        BotReference baseBot, TimeSpan elapsed, IRenderable? additional = null)
    {
        MatchStatistics ms = new MatchStatistics(results, newBot, baseBot);

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
                $"[{(ms.TokenDifference < 0 ? "red" : "green")}]{(ms.TokenDifference > 0 ? "+" : ms.TokenDifference < 0 ? "-" : "")}{Math.Abs(ms.TokenDifference)}[/]"),
            new Markup(ms.NewBotTokens.ToString()), new Markup(ms.BaseBotTokens.ToString()));

        int wldWidth = 60;
        BreakdownChart wldChart = new BreakdownChart();
        wldChart.Width(wldWidth);
        wldChart.AddItem("Wins", ms.GamesWon, Color.Green3_1);
        wldChart.AddItem("Draws", ms.GamesDrawn, Color.Gray);
        wldChart.AddItem("Losses", ms.GamesLost, Color.Red3_1);

        Table scoreTable = new Table
        {
            Title = new TableTitle("Score"),
            Border = TableBorder.Rounded
        };
        scoreTable.AddColumns($"Score: [{Color.DodgerBlue1.ToMarkup()}]{ms.Score:F1}[/]",
            $"Elo: [{(ms.Elo < 0 ? "red" : "green")}]{ms.Elo:0.00}[/] +/-[{Color.DodgerBlue1.ToMarkup()}]{ms.EloMargin:0.0}[/]",
            $"LOS: [{Color.DodgerBlue1.ToMarkup()}]{ms.LOS:P2}[/]");

        List<IRenderable> rows =
        [
            tokenTable,
            scoreTable,
        ];
        
        if (additional != null)
            rows.Add(additional);

        Table timeTable = new Table()
        {
            Title = new TableTitle("Time"),
            Border = TableBorder.Rounded
        };
        timeTable.AddColumns($"Elapsed: {elapsed:g}");
        rows.Add(timeTable);
        
        rows.Add(wldChart);

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

    private readonly struct MatchStatistics
    {
        public int GamesWon { get; }
        public int GamesLost { get; }
        public int GamesDrawn { get; }
        
        public double Elo { get; }
        public double EloMargin { get; }
        public double LOS { get; }
        public int NewBotTokens { get; }
        public int BaseBotTokens { get; }

        public int TotalGames => GamesWon + GamesLost + GamesDrawn;
        public double Score => GamesWon + (double)GamesDrawn / 2;
        public double Percentage => Score / TotalGames;
        public int TokenDifference => NewBotTokens - BaseBotTokens;
        public MatchStatistics(MiniChallengeManager.MatchResult[] results, BotReference newBot, BotReference baseBot)
        {
            GamesWon = results.Count(result =>
                Arbiter.IsWinResult(result.Result) && result.TesterPlayedWhite == Arbiter.IsWhiteWinsResult(result.Result));
            GamesLost = results.Count(result =>
                Arbiter.IsWinResult(result.Result) && result.TesterPlayedWhite != Arbiter.IsWhiteWinsResult(result.Result));
            GamesDrawn = results.Count(result => Arbiter.IsDrawResult(result.Result));
            
            LOS = Stats.LOS([GamesLost, GamesDrawn, GamesWon]);
            
            Elo = Stats.Elo(Percentage);
            EloMargin = Stats.CalculateEloErrorMargin(GamesWon, GamesDrawn, GamesLost);

            TokenCount newTokens = newBot.GetTokenCount();
            TokenCount baseTokens = baseBot.GetTokenCount();
            NewBotTokens = newTokens.total - newTokens.debug;
            BaseBotTokens = baseTokens.total - baseTokens.debug;
        }
    }
}