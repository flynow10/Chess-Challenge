using ChessChallenge.API;

namespace ChessChallenge.Version10;

public static class TimingFunctions
{
    public static bool ThirtiethOfTimeLeft(Timer timer)
    {
        return timer.MillisecondsElapsedThisTurn < timer.MillisecondsRemaining / 30;
    }
}