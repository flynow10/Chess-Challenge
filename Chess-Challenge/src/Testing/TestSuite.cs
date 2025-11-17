using System;
using System.Collections.Generic;
using ChessChallenge.API;

namespace ChessChallenge.Testing;

public abstract class TestSuite
{
    public abstract string Name { get; }
    private BotTester _botTester;

    public TestSuite(BotTester botTester)
    {
        _botTester = botTester;
    }

    public SuiteResult RunFullSuite(IChessBot bot)
    {
        SuiteGroup[] groups = GetAllGroups();
        List<SuiteResult> results = new List<SuiteResult>();
        SuiteResult fullResult = new ("full");
        foreach (SuiteGroup suiteGroup in groups)
        {
            SuiteResult partialResult = RunSuiteGroup(bot, suiteGroup.GroupId);
            fullResult.TotalTests += partialResult.TotalTests;
            fullResult.TestsPassed += partialResult.TestsPassed;
            fullResult.TestsFailed += partialResult.TestsFailed;
            results.Add(partialResult);
        }
        fullResult.SuiteGroups = results.ToArray();
        return fullResult;
    }
    public abstract SuiteResult RunSuiteGroup(IChessBot bot, string groupId);
    public abstract SuiteGroup[] GetAllGroups();
    public abstract bool SetOpponent(IChessBot opponent);

    protected void DebugLog(string message, bool isError = false)
    {
        _botTester.PrintTestDebug(message, isError);
    }

    public record SuiteGroup(string GroupId, string Name);
}