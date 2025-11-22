using System;
using System.Diagnostics;
using System.Linq;
using ChessChallenge.API;
using ChessChallenge.Application;
using Spectre.Console;

namespace ChessChallenge.Testing;
public class BotTester
{

    // Selection
    private TestSuiteType selectedSuite = TestSuiteType.MateTests;
    private PlayerType selectedBotType = PlayerType.MyBot;
    private int selectedSkillLevel = 20;
    
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

    private PlayerType AskForBot(string question)
    {
        PlayerType[] playerTypes = Enum.GetValues<PlayerType>().Where(x => x != PlayerType.Human).ToArray();
        return AnsiConsole.Prompt(new SelectionPrompt<PlayerType>()
            .Title(question)
            .PageSize(10)
            .AddChoices(playerTypes)
        );
    }

    private void SelectBot()
    {
        selectedBotType = AskForBot("Which bot would you like to test?");
        if (ChallengeController.UsesSkillLevel.Contains(selectedBotType))
        {
            selectedSkillLevel = AnsiConsole.Prompt(
                new TextPrompt<int>("What skill level would you like to use?")
                    .Validate(level => level switch
                    {
                        < 0 or > 20 => ValidationResult.Error(),
                        _ => ValidationResult.Success()
                    })
            );
        }
        
        SuiteOptions();
    }

    enum SuiteOptionType
    {
        Run,
        ChangeBot,
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
                    SuiteOptionType.ChangeBot => $"Select Bot ([aqua]{selectedBotType}[/])",
                    SuiteOptionType.Run => "Run Suite",
                    SuiteOptionType.Back => "[red]Back[/]",
                    _ => throw new ArgumentOutOfRangeException(nameof(optionType), optionType, null)
                })
            );

        switch (result)
        {
            case SuiteOptionType.ChangeBot:
                SelectBot();
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
        bool requiresOpponent = _suite.SetOpponent(PlayerType.Human);

        TestSuite.SuiteGroup[] groups = _suite.GetAllGroups();

        if (requiresOpponent)
        {
            PlayerType opponent = AskForBot("Select opponent bot");
            _suite.SetOpponent(opponent);
        }

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
                result = _suite.RunFullSuite(selectedBotType);
                break;
            default:
                result = _suite.RunSuiteGroup(selectedBotType, groupId);
                break;
        }
        stopwatch.Stop();
        
        DisplayResults(result);
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

    public IChessBot CreateBot(PlayerType botType)
    {
        return ChallengeController.CreateChessBot(botType, selectedSkillLevel);
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