using System;
using System.Linq;
using ChessChallenge.Chess;

namespace ChessChallenge.Version6;

public static class OpeningBook
{
    public static Board board;
    public static void Run()
    {
        var moveBook = CreateBook();
        Console.WriteLine($"[{string.Join(", ", moveBook.Select(ulongToString))}]");
        // Console.WriteLine(MoveUtility.GetMoveNameUCI(GetRefutationFromMove(moveBook, MoveUtility.GetMoveFromUCIName("e2e4", board))));
        Console.WriteLine($"[{string.Join(", ", moveBook)}]");
    }

    public static Move GetRefutationFromMove(ulong[] moveBook, Move firstMove)
    {
        foreach (var bookPart in moveBook)
            foreach (var movePair in new [] {(uint)bookPart, (uint)(bookPart >> 32)})
                if ((movePair & 0xFFFF) == firstMove.Value)
                    return new Move((ushort)(movePair >> 16));
        return Move.NullMove;
    }

    public static ulong[] CreateBook()
    {
        board = new();
        board.LoadStartPosition();
        MovePair[] bookMovePairs =
        {
            new("e2e4", "e7e5"),
            new("d2d4", "d7d5"),
            new("g1f3", "d7d5"),
            new("c2c4", "e7e5"),
            new("b2b3", "e7e5"),
            new("g2g3", "d7d5"),
            new("e2e3", "g7g6"),
            new("b1c3", "c7c5"),
            new("d2d3", "d7d5"),
            new("f2f4", "e7e5"),
            new("c2c3", "e7e5"),
            new("a2a3", "d7d5")
        };
        ulong[] moveBook = new ulong[(int)Math.Ceiling(bookMovePairs.Length/(double)2)];
        for (int i = 0; i < bookMovePairs.Length; i++)
        {
            MovePair movePair = bookMovePairs[i];
            moveBook[(int)Math.Floor(i / (double)2)] |= (ulong)movePair.Value << (i % 2)*32;
        }

        return moveBook;
    }

    static string ulongToString(ulong arg) => Convert.ToString((long)arg, 2);

    struct MovePair
    {
        public Move FirstMove;
        public Move Refutation;

        public uint Value => FirstMove.Value | (uint)Refutation.Value << 16;
        public MovePair(string firstMoveUCI, string refutationUCI)
        {
            Move firstMove = MoveUtility.GetMoveFromUCIName(firstMoveUCI, board);
            board.MakeMove(firstMove);
            Move refutation = MoveUtility.GetMoveFromUCIName(refutationUCI, board);
            board.UndoMove(firstMove);
            FirstMove = firstMove;
            Refutation = refutation;
        }

        public MovePair(Move firstMove, Move refutation)
        {
            FirstMove = firstMove;
            Refutation = refutation;
        }
    }
}