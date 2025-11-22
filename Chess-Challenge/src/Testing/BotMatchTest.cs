using System;
using ChessChallenge.Application;
using ChessChallenge.Chess;

namespace ChessChallenge.Testing;

public class BotMatchTest : TestSuite
{
    public override string Name => "Bot Match Test";
    private PlayerType _opponent;
    public BotMatchTest(BotTester botTester) : base(botTester)
    {
    }

    public override SuiteResult RunSuiteGroup(PlayerType botType, string groupId)
    {
        MiniChallengeManager manager = new MiniChallengeManager(botType, _opponent, _botTester.CreateBot);
        MiniChallengeManager.MatchResult[] results = manager.PlayMatches(new[]
        {
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            "r1bq1rk1/pp3ppp/2n1p3/3n4/1b1P4/2N2N2/PP2BPPP/R1BQ1RK1 w - - 0 10"
        });
        int gamesWon = 0, gamesLost = 0;
        foreach (MiniChallengeManager.MatchResult matchResult in results)
        {
            DebugLog($"{matchResult.Result}: {matchResult.Position.fen}");
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
        return new SuiteResult(groupId, results.Length, gamesWon, gamesLost, Array.Empty<SuiteResult>());
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