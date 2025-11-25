using System;
using System.Diagnostics;
using System.Linq;
using ChessChallenge.Application;
using ChessChallenge.Testing.Utils;
using Spectre.Console;

namespace ChessChallenge.Testing;
public class BotTester
{

    // Selection
    private TestSuiteType selectedSuite = TestSuiteType.MateTests;
    private BotReference newBot = new(PlayerType.MyBot);
    private BotReference baseBot = new(PlayerType.MyBot);
    
    // Running
    private TestSuite _suite;

    enum TestSuiteType
    {
        BotMatchTest,
        MateTests,
        PieceSquareTables
    }

    public void Run()
    {
        AnsiConsole.MarkupLine("[teal]Welcome to the BotTester[/]");
        SelectSuite();
    }

    private void SelectSuite()
    {
        selectedSuite = AnsiConsole.Prompt(new SelectionPrompt<TestSuiteType>()
            .Title("Which test suite would you like to run?")
            .PageSize(10)
            .AddChoices(Enum.GetValues<TestSuiteType>())
        );
        SuiteOptions();
    }

    private BotReference AskForBot(string question)
    {
        BotReference bot = new(PlayerType.MyBot);
        PlayerType[] playerTypes = Enum.GetValues<PlayerType>().Where(x => x != PlayerType.Human).ToArray();
        bot.Type = AnsiConsole.Prompt(new SelectionPrompt<PlayerType>()
            .Title(question)
            .PageSize(10)
            .AddChoices(playerTypes)
        );
        
        if (ChallengeController.UsesSkillLevel.Contains(bot.Type))
        {
            bot.SkillLevel = AnsiConsole.Prompt(
                new TextPrompt<int>("What skill level would you like to use?")
                    .Validate(level => level switch
                    {
                        < 0 or > 20 => ValidationResult.Error(),
                        _ => ValidationResult.Success()
                    })
            );
        }

        if (bot.Type == PlayerType.HashBot)
        {
            bot.CommitHash = GitLoader.AskCommitHash();
        }

        return bot;
    }

    


    enum SuiteOptionType
    {
        Run,
        ChangeNewBot,
        ChangeBaseBot,
        Back,
    }

    private void SuiteOptions()
    {
        SuiteOptionType result = AnsiConsole.Prompt(
            new SelectionPrompt<SuiteOptionType>()
                .Title($"Test suite options ([aqua]{selectedSuite}[/])")
                .AddChoices(Enum.GetValues<SuiteOptionType>())
                .UseConverter(optionType => optionType switch
                {
                    SuiteOptionType.ChangeNewBot => $"Select New Bot ([aqua]{newBot.Type}[/])",
                    SuiteOptionType.ChangeBaseBot => $"Select Base Bot ([aqua]{baseBot.Type}[/])",
                    SuiteOptionType.Run => "Run Suite",
                    SuiteOptionType.Back => "[red]Back[/]",
                    _ => throw new ArgumentOutOfRangeException(nameof(optionType), optionType, null)
                })
            );

        switch (result)
        {
            case SuiteOptionType.ChangeNewBot:
                newBot = AskForBot("Which bot would you like to test?");
                SuiteOptions();
                break;
            case SuiteOptionType.ChangeBaseBot:
                baseBot = AskForBot("Which bot would you like to use as the base?");
                SuiteOptions();
                break;
            case SuiteOptionType.Run:
                RunSuite();
                break;
            case SuiteOptionType.Back:
                SelectSuite();
                break;
        }
    }

    private void RunSuite()
    {
        _suite = CreateTestSuite(selectedSuite);

        TestSuite.SuiteGroup[] groups = _suite.GetAllGroups();

        string groupId = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Which tests would you like to run?")
                .AddChoices("full")
                .AddChoices(groups.Select(group => group.GroupId))
                .AddChoices("back")
                .UseConverter(prompt => prompt switch
                {
                    "full" => "[green]Run all tests[/]",
                    "back" => "[red]Back[/]",
                    _ => groups.First(group => group.GroupId == prompt)?.Name
                } ?? throw new InvalidOperationException())
        );
        Stopwatch stopwatch = Stopwatch.StartNew();
        SuiteResult result;
        switch (groupId)
        {
            case "back":
                SuiteOptions();
                return;
            case "full":
                result = _suite.RunFullSuite(newBot, baseBot);
                break;
            default:
                result = _suite.RunSuiteGroup(newBot, baseBot, groupId);
                break;
        }
        stopwatch.Stop();

        if (selectedSuite is not TestSuiteType.BotMatchTest)
        {
            DisplayResults(result);
        }
        SuiteOptions();
    }

    private void DisplayResults(SuiteResult result)
    {
        Table table = new Table
        {
            Title = new TableTitle($"Test Results ([teal]{_suite.Name}[/])"),
            ShowRowSeparators = true
        };
        table.AddColumns("Group", "Total Tests", "Tests Passed", "Tests Failed", "Time Spent");
        TestSuite.SuiteGroup[] suiteGroups = _suite.GetAllGroups();
        
        // Add top level
        AddResult(result);
        
        foreach (SuiteResult resultGroup in result.SuiteGroups)
        {
            AddResult(resultGroup);
        }
        
        AnsiConsole.Write(table);

        void AddResult(SuiteResult result)
        {
            string groupName;
            if (result.GroupId == "full")
            {
                groupName = "Full";
            } else {
                groupName = suiteGroups.First(group => group.GroupId == result.GroupId).Name;
            }
            table.AddRow(groupName, $"[blue]{result.TotalTests}[/]", $"[green]{result.TestsPassed}[/]", $"[red]{result.TestsFailed}[/]", result.Elapsed.ToString());
        }
    }

    public void PrintTestDebug(string message, bool isError)
    {
        if (isError)
        {
            AnsiConsole.MarkupLine($"[red]{message}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"{message}");
        }
    }

    TestSuite CreateTestSuite(TestSuiteType type)
    {
        return type switch
        {
            TestSuiteType.BotMatchTest => new BotMatchTest(this),
            TestSuiteType.MateTests => new MateTests(this),
            TestSuiteType.PieceSquareTables => new PieceSquareTable(this),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}