using System;
using System.Collections.Generic;
using System.Diagnostics;
using ChessChallenge.API;
using ChessChallenge.Application;

namespace ChessChallenge.Testing;

public abstract class TestSuite(BotTester botTester)
{
    public abstract string Name { get; }

    public SuiteResult RunFullSuite(BotReference newBot, BotReference baseBot)
    {
        SuiteGroup[] groups = GetAllGroups();
        List<SuiteResult> results = new List<SuiteResult>();
        SuiteResult fullResult = new ("full");
        Stopwatch sw = Stopwatch.StartNew();
        foreach (SuiteGroup suiteGroup in groups)
        {
            SuiteResult partialResult = RunSuiteGroup(newBot, baseBot, suiteGroup.GroupId);
            fullResult.TotalTests += partialResult.TotalTests;
            fullResult.TestsPassed += partialResult.TestsPassed;
            fullResult.TestsFailed += partialResult.TestsFailed;
            results.Add(partialResult);
        }
        sw.Stop();
        fullResult.SuiteGroups = results.ToArray();
        fullResult.Elapsed = sw.Elapsed;
        return fullResult;
    }
    public abstract SuiteResult RunSuiteGroup(BotReference newBot, BotReference baseBot, string groupId);
    public abstract SuiteGroup[] GetAllGroups();

    protected void DebugLog(string message, bool isError = false)
    {
        botTester.PrintTestDebug(message, isError);
    }

    public record SuiteGroup(string GroupId, string Name);
}