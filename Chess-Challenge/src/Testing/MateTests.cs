using System;
using System.Diagnostics;
using ChessChallenge.API;
using ChessChallenge.Application;
using ChessChallenge.Chess;

namespace ChessChallenge.Testing;

public class MateTests : TestSuite
{
    public override string Name => "Mate in X";
    SuiteResult result;

    readonly string[] _mateInTwoFens =
    {
        "r2qkb1r/pp2nppp/3p4/2pNN1B1/2BnP3/3P4/PPP2PPP/R2bK2R w KQkq - 1 25",
        "1rb4r/pkPp3p/1b1P3n/1Q6/N3Pp2/8/P1P3PP/7K w - - 1 25",
        "4kb1r/p2n1ppp/4q3/4p1B1/4P3/1Q6/PPP2PPP/2KR4 w k - 1 25",
        "r1b2k1r/ppp1bppp/8/1B1Q4/5q2/2P5/PPP2PPP/R3R1K1 w - - 1 25",
        "5rkr/pp2Rp2/1b1p1Pb1/3P2Q1/2n3P1/2p5/P4P2/4R1K1 w - - 1 25",
        "6k1/pp4p1/2p5/2bp4/8/P5Pb/1P3rrP/2BRRN1K b - - 0 25",
        "rnbqkbn1/ppppp3/7r/6pp/3P1p2/3BP1B1/PPP2PPP/RN1QK1NR w - - 1 25"
    };

    readonly string[] _mateInThreeFens =
    {
        "r1b1kb1r/pppp1ppp/5q2/4n3/3KP3/2N3PN/PPP4P/R1BQ1B1R b kq - 0 25",
        "r3k2r/ppp2Npp/1b5n/4p2b/2B1P2q/BQP2P2/P5PP/RN5K w kq - 1 25",
        "r1b3kr/ppp1Bp1p/1b6/n2P4/2p3q1/2Q2N2/P4PPP/RN2R1K1 w - - 1 25",
        "r2n1rk1/1ppb2pp/1p1p4/3Ppq1n/2B3P1/2P4P/PP1N1P1K/R2Q1RN1 b - - 0 25",
        "3q1r1k/2p4p/1p1pBrp1/p2Pp3/2PnP3/5PP1/PP1Q2K1/5R1R w - - 1 25",
        "6k1/ppp2ppp/8/2n2K1P/2P2P1P/2Bpr3/PP4r1/4RR2 b - - 0 25",
        "rn3rk1/p5pp/2p5/3Ppb2/2q5/1Q6/PPPB2PP/R3K1NR b - - 0 25",
        "N1bk4/pp1p1Qpp/8/2b5/3n3q/8/PPP2RPP/RNB1rBK1 b - - 0 25",
        "8/2p3N1/6p1/5PB1/pp2Rn2/7k/P1p2K1P/3r4 w - - 1 25",
        "r1b1k1nr/p2p1ppp/n2B4/1p1NPN1P/6P1/3P1Q2/P1P1K3/q5b1 w - - 1 25"
    };

    readonly string[] _mateInFourFens =
    {
        "r5rk/2p1Nppp/3p3P/pp2p1P1/4P3/2qnPQK1/8/R6R w - - 1 25",
        "1r2k1r1/pbppnp1p/1b3P2/8/Q7/B1PB1q2/P4PPP/3R2K1 w - - 1 25",
        "Q7/p1p1q1pk/3p2rp/4n3/3bP3/7b/PP3PPK/R1B2R2 b - - 0 25",
        "r1bqr3/ppp1B1kp/1b4p1/n2B4/3PQ1P1/2P5/P4P2/RN4K1 w - - 1 25",
        "r1b3kr/3pR1p1/ppq4p/5P2/4Q3/B7/P5PP/5RK1 w - - 1 25",
        "2k4r/1r1q2pp/QBp2p2/1p6/8/8/P4PPP/2R3K1 w - - 1 25"
    };

    public MateTests(BotTester botTester) : base(botTester)
    {
    }

    public override SuiteResult RunSuiteGroup(PlayerType botType, string groupId)
    {
        result = new(groupId);
        string[] fens = Array.Empty<string>();
        int mateDepth;

        switch (groupId)
        {
            case "mate2":
                fens = _mateInTwoFens;
                mateDepth = 2;
                break;
            case "mate3":
                fens = _mateInThreeFens;
                mateDepth = 3;
                break;
            case "mate4":
                fens = _mateInFourFens;
                mateDepth = 4;
                break;
            default:
                throw new Exception("Unknown group id " + groupId);
        }

        Stopwatch sw = Stopwatch.StartNew();
        MateTest(botType, fens, mateDepth);
        sw.Stop();
        result.Elapsed = sw.Elapsed;
        return result;
    }

    public override bool SetOpponent(PlayerType opponent)
    {
        return false;
    }

    public override SuiteGroup[] GetAllGroups()
    {
        return new[]
        {
            new SuiteGroup("mate2", "Mate in two"),
            new SuiteGroup("mate3", "Mate in three"),
            new SuiteGroup("mate4", "Mate in four")
        };
    }

    private void MateTest(PlayerType botType, string[] fens, int mateDistance)
    {
        MiniChallengeManager controller = new MiniChallengeManager(botType, _botTester.CreateBot);
        controller.MaxMoves = mateDistance;
        foreach (string fenString in fens)
        {
            result.TotalTests++;
            DebugLog($"Mate in {mateDistance} test:");
            DebugLog($"Fen: {fenString}");
            MiniChallengeManager.MatchResult matchResult = controller.PlayMatch(fenString);
            if (matchResult.PlyCount / 2 >= mateDistance)
            {
                DebugLog("Mate move limit reached", true);
                continue;
            }

            if (matchResult.Result is GameResult.WhiteIllegalMove or GameResult.BlackIllegalMove)
            {
                DebugLog("Illegal move made", true);
                result.TestsFailed++;
                continue;
            }

            if (!Arbiter.IsWinResult(matchResult.Result) ||
                matchResult is { TesterPlayedWhite: true, Result: GameResult.WhiteIsMated } || matchResult is
                    { TesterPlayedWhite: false, Result: GameResult.BlackIsMated })
            {
                DebugLog("Mate was not found", true);
                result.TestsFailed++;
                continue;
            }

            result.TestsPassed++;
        }
    }
}