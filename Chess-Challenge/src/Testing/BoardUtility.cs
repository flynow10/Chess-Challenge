using System;
using ChessChallenge.Chess;

namespace ChessChallenge.Testing;

public static class BoardUtility
{
    public static Board RandomBoard(int moveCount = 20)
    {
        Board board = new Board();
        board.LoadStartPosition();
        MoveGenerator moveGenerator = new MoveGenerator();
        Random random = new Random();
        for (int i = 0; i < moveCount; i++)
        {
            Span<Move> legalMoves = moveGenerator.GenerateMoves(board);
            if (legalMoves.Length != 0)
            {
                Move nextMove = legalMoves[random.Next(legalMoves.Length)];
                board.MakeMove(nextMove, false);
                if (Arbiter.GetGameState(board) != GameResult.InProgress)
                {
                    board.UndoMove(nextMove, false);
                    break;
                }
            }
            else break;
        }

        return board;
    }
}