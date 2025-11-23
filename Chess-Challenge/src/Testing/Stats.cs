using System;

namespace ChessChallenge.Testing;

public static class Stats
{
    public static double LL(double x) => 1/(1+Math.Pow(10, -x/400));

    public static double LLR(int wins, int losses, int draws, double elo0, double elo1)
    {
        if (wins == 0 || losses == 0 || draws == 0)
        {
            return 0;
        }

        int N = wins + draws + losses;
        double w = (double)wins/N, d = (double)draws/N, l = (double)losses/N;

        double s = w + d / 2;
        double m2 = w + d / 2;
        double variance = m2 - Math.Pow(s, 2);;
        double variance_s = variance / N;
        double s0 = LL(elo0);
        double s1 = LL(elo1);
        return (s1 - s0) * (2 * s - s0 - s1) / variance_s / 2;
    }

    public static int SPRT(int wins, int losses, int draws, double elo0, double elo1, double alpha, double beta)
    {
        double LLR_ = LLR(wins, losses, draws, elo0, elo1);
        (double LA, double LB) = AlphaBetaWindow(alpha, beta);
        if (LLR_ > LB)
            return 1;
        if (LLR_ < LA)
            return -1;
        return 0;
    }

    public static (double, double) AlphaBetaWindow(double alpha, double beta)
    {
        double LA = Math.Log(beta / (1 - alpha));
        double LB = Math.Log((1-beta)/alpha);
        return (LA, LB);
    }

    public static double Elo(double percentage)
    {
        double epsilon = 1e-3;
        percentage = Math.Clamp(percentage, epsilon, 1-epsilon);
        return -400 * Math.Log(1 / percentage - 1);
    }
}