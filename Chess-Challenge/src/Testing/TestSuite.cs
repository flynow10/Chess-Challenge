using System;
using System.Collections.Generic;
using ChessChallenge.API;
using ChessChallenge.Application;

namespace ChessChallenge.Testing;

public abstract class TestSuite
{
    public abstract string Name { get; }
    protected BotTester _botTester;

    public TestSuite(BotTester botTester)
    {
        _botTester = botTester;
    }

    public SuiteResult RunFullSuite(PlayerType botType)
    {
        SuiteGroup[] groups = GetAllGroups();
        List<SuiteResult> results = new List<SuiteResult>();
        SuiteResult fullResult = new ("full");
        foreach (SuiteGroup suiteGroup in groups)
        {
            SuiteResult partialResult = RunSuiteGroup(botType, suiteGroup.GroupId);
            fullResult.TotalTests += partialResult.TotalTests;
            fullResult.TestsPassed += partialResult.TestsPassed;
            fullResult.TestsFailed += partialResult.TestsFailed;
            results.Add(partialResult);
        }
        fullResult.SuiteGroups = results.ToArray();
        return fullResult;
    }
    public abstract SuiteResult RunSuiteGroup(PlayerType botType, string groupId);
    public abstract SuiteGroup[] GetAllGroups();
    public abstract bool SetOpponent(PlayerType opponent);

    protected void DebugLog(string message, bool isError = false)
    {
        _botTester.PrintTestDebug(message, isError);
    }

    public record SuiteGroup(string GroupId, string Name);
}