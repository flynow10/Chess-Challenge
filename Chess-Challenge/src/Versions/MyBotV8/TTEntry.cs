using ChessChallenge.API;

namespace ChessChallenge.Version8;

public record struct TTEntry(ulong Key, Move Move, int Depth, int Eval, NodeType NodeType);

public enum NodeType
{
    PV = 1,
    Cut = 2,
    All = 3
}