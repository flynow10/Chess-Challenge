using System;
using ChessChallenge.API;

namespace ChessChallenge.MyBot;

public class MyBot : IChessBot
{

    public Move Think(Board board, Timer timer)
    {
        return Move.NullMove;
    }
}