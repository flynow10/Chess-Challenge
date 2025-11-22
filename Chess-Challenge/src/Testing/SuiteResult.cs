using System;

namespace ChessChallenge.Testing;

public record struct SuiteResult(string GroupId, int TotalTests, int TestsPassed, int TestsFailed, SuiteResult[] SuiteGroups, TimeSpan Elapsed)
{
    public SuiteResult(string GroupId) : this(GroupId, 0, 0, 0, Array.Empty<SuiteResult>(), TimeSpan.Zero)
    {}
}