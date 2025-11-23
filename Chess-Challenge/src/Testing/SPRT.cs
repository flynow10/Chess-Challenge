using System;
using System.Linq;

namespace ChessChallenge.Testing;

public class SPRT
{
    public enum Status
    {
        H1,
        H0,
        None
    }

    public Status CurrentStatus { get; private set; } = Status.None;
    public double Alpha = 0;
    public double Beta = 0;
    public double Elo0 = 0;
    public double Elo1 = 0;
    public double LLR { get; private set; }
    
    public double LA => Math.Log(Beta / (1 - Alpha));
    public double LB => Math.Log((1 - Beta) / Alpha);
    public int Wins => _results[2];
    public int Losses => _results[0];
    public int Draws => _results[1];
    public double LowerBound => LA + o0;
    public double UpperBound => LB - o1;
    public double Total => _results.Sum();

    private double maxLLR;
    private double minLLR;
    private double o0;
    private double o1;
    private double sq0;
    private double sq1;
    private readonly int[] _results = new int[3];

    public SPRT(SPRTOptions options) : this(options.Elo0, options.Elo1, options.Alpha, options.Beta) {}
    public SPRT(double elo0, double elo1, double alpha, double beta)
    {
        Elo0 = elo0;
        Elo1 = elo1;
        Alpha = alpha;
        Beta = beta;
    }

    public Status Record(int result)
    {
        if (CurrentStatus != Status.None)
            return CurrentStatus;
        _results[result]++;
        LLR = Stats.LLRLogistic(_results, Elo0, Elo1);

        if (LLR > maxLLR)
        {
            sq1 += Math.Pow(LLR - maxLLR, 2);
            maxLLR = LLR;
            o1 = sq1 / LLR / 2;
        }
        if (LLR < minLLR)
        {
            sq0 += Math.Pow(LLR - minLLR, 2);
            minLLR = LLR;
            o0 = -sq0 / LLR / 2;
        }

        if (LLR > LB - o1)
        {
            CurrentStatus = Status.H1;
        } else if (LLR < LA + o0)
        {
            CurrentStatus = Status.H0;
        }

        return CurrentStatus;
    }

    public record struct SPRTOptions
    {
        public double Elo0 = 0;
        public double Elo1 = 10;
        public double Alpha = 0.05;
        public double Beta = 0.05;
        public SPRTOptions() : this(0, 10, 0.05, 0.05) {}
        public SPRTOptions(double Elo0, double Elo1, double Alpha, double Beta)
        {
            this.Elo0 = Elo0;
            this.Elo1 = Elo1;
            this.Alpha = Alpha;
            this.Beta = Beta;
        }
    }
}