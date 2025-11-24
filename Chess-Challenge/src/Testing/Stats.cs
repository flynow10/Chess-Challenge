using System;
using System.Linq;

namespace ChessChallenge.Testing;

public static class Stats
{
    public static double LL(double x) => 1/(1+Math.Pow(10, -x/400));

    // public static (double, double)[] MLE((double, double)[] pdf, double s)
    // {
    //     double epsilon = 1e-9;
    //     double v = pdf[0].Item1, w =  pdf.Last().Item2;
    //     if (v >= s || s >= w)
    //     {
    //         return [(Double.NaN, Double.NaN)];
    //     }
    //
    //     double l = -1 / (w - s), u = 1 / (s - v);
    //
    //     double F(double x)
    //     {
    //         
    //     }
    // }

    public static double LLR((double, double)[] pdf, double s0, double s1)
    {
        double r0 = pdf.Sum(item => item.Item2 * Math.Pow(item.Item1 - s0, 2));
        double r1 = pdf.Sum(item => item.Item2 * Math.Pow(item.Item1 - s1, 2));
        return 0.5 * Math.Log(r0 / r1);
    }

    public static double LLRLogistic(int[] results, double elo0, double elo1) 
    {
        double s0 = LL(elo0);
        double s1 = LL(elo1);
        (double N, (double, double)[] pdf) = ResultsToPdf(results);
        // Console.WriteLine(string.Join(", ", pdf.Select(r => $"({r.Item1}, {r.Item2})")));
        return N * LLR(pdf, s0, s1);
    }

    public static double[] Regularize(int[] results)
    {
        double epsilon = 1e-3;
        double[] regularized = new double[results.Length];
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i] == 0)
            {
                regularized[i] = epsilon;
            }
            else
            {
                regularized[i] = results[i];
            }
        }

        return regularized;
    }

    private static (double, (double, double)[]) ResultsToPdf(int[] results)
    {
        double[] regularized = Regularize(results);
        double N = regularized.Sum();
        double l = regularized.Length;
        return (N, regularized.Select((result, i) => (i / (l - 1), result / N)).ToArray());
    }
    
    // public static (double, double) LLRDriftVariance((double, double)[] pdf, double s0, double s1, double? s = null)
    // {
    //     (double s_, double v_) = stats(pdf);
    //     double v;
    //
    //     if (!s.HasValue)
    //     {
    //         s = s_;
    //         v = v_;
    //     }
    //     else
    //     {
    //         v = v_ + Math.Pow(s.Value - s_, 2);
    //     }
    //
    //     double mu = (s.Value - (s0 + s1) / 2) * (s1 - s0) / v;
    //     double var = Math.Pow(s1 - s0, 2) / v;
    //     return (mu, var);
    // }
    
    // private static (double, double) stats((double, double)[] pdf)
    // {
    //     double epsilon = 1e-6;
    //     foreach ((double, double) item in pdf)
    //     {
    //         if(-epsilon > item.Item2 || item.Item2 > 1 + epsilon)
    //             return (Double.NaN, double.NaN);
    //     }
    //     double n = pdf.Sum(i => i.Item2);
    //     if (Math.Abs(n - 1) >= epsilon)
    //         return (Double.NaN, Double.NaN);
    //     
    //     double s = pdf.Sum(i => i.Item1 * i.Item2);
    //     double var = pdf.Sum(i => i.Item1 * Math.Pow(i.Item2 - s, 2));
    //
    //     return (s, var);
    // }

    public static double LOS(int[] results)
    {
        double[] regularized = Regularize(results);
        double games = regularized.Sum();
        double mu = regularized.Select((score, i) => score * ((double)i / 2)).Sum() / games;
        double mu_ = (double)(results.Length - 1) / 2 * mu;
        double var = regularized.Select((score, i) => score * Math.Pow((double)i / 2 - mu_, 2)).Sum() / games;
        double stdev = Math.Sqrt(var);

        return Phi((mu - 0.5) / (stdev / Math.Sqrt(games)));
    }
    
    public static double Elo(double percentage)
    {
        double epsilon = 1e-3;
        percentage = Math.Clamp(percentage, epsilon, 1-epsilon);
        return -400 * Math.Log10(1 / percentage - 1);
    }
    
    public static double CalculateEloErrorMargin(int wins, int draws, int losses)
    {
        double total = wins + draws + losses;
        double winP = wins / total;
        double drawP = draws / total;
        double lossP = losses / total;

        double percentage = (wins + (double)draws / 2) / total;
        double winDev = winP * Math.Pow(1 - percentage, 2);
        double drawsDev = drawP * Math.Pow(0.5 - percentage, 2);
        double lossesDev = lossP * Math.Pow(0 - percentage, 2);

        double stdDeviation = Math.Sqrt(winDev + drawsDev + lossesDev) / Math.Sqrt(total);

        double confidenceP = 0.95;
        double minConfidenceP = (1 - confidenceP) / 2;
        double maxConfidenceP = 1 - minConfidenceP;
        double devMin = percentage + PhiInv(minConfidenceP) * stdDeviation;
        double devMax = percentage + PhiInv(maxConfidenceP) * stdDeviation;

        double difference = Elo(devMax) - Elo(devMin);
        double margin = Math.Round(difference / 2);
        return margin;
    }

    public static double Phi(double x)
    {
        // constants
        double a1 = 0.254829592;
        double a2 = -0.284496736;
        double a3 = 1.421413741;
        double a4 = -1.453152027;
        double a5 = 1.061405429;
        double p = 0.3275911;
        
        // Save the sign of x
        int sign = 1;
        if (x < 0)
            sign = -1;
        x = Math.Abs(x) / Math.Sqrt(2.0);
        
        // A&S formula 7.1.26
        double t = 1.0 / (1.0 + p*x);
        double y = 1.0 - (((((a5*t + a4)*t) + a3)*t + a2)*t + a1)*t * Math.Exp(-x*x);
        
        return 0.5 * (1.0 + sign*y);
    }
    
    public static double Erf(double x)
    {
        // constants
        double a1 = 0.254829592;
        double a2 = -0.284496736;
        double a3 = 1.421413741;
        double a4 = -1.453152027;
        double a5 = 1.061405429;
        double p = 0.3275911;

        // Save the sign of x
        int sign = 1;
        if (x < 0)
            sign = -1;
        x = Math.Abs(x);

        // A&S formula 7.1.26
        double t = 1.0 / (1.0 + p*x);
        double y = 1.0 - (((((a5*t + a4)*t) + a3)*t + a2)*t + a1)*t*Math.Exp(-x*x);

        return sign*y;
    }

    public static double PhiInv(double p)
    {
        return Math.Sqrt(2) * CalculateInverseErrorFunction(2 * p - 1);
    }
    
    public static double CalculateInverseErrorFunction(double x)
    {
        double a = 8 * (Math.PI - 3) / (3 * Math.PI * (4 - Math.PI));
        double y = Math.Log(1 - x * x);
        double z = 2 / (Math.PI * a) + y / 2;

        double ret = Math.Sqrt(Math.Sqrt(z * z - y / a) - z);
        if (x < 0) return -ret;
        return ret;
    }
}