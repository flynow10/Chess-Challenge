using System;

namespace ChessChallenge.MyBot;

[Flags]
public enum SearchFlags: ulong
{
    UseAlphaBetaPruning = 1,
    UseIterativeDeepening = 1 << 1,
    UseQuiescenceSearch = 1 << 2,
    UseMoveOrdering = 1 << 3,
    UseTranspositionTable = 1 << 4,
    UseNullMoveObservation = 1 << 5,
    UseKillerMoveOrdering = 1 << 6
}