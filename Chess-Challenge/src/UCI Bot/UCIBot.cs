using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ChessChallenge.API;

namespace ChessChallenge.UCIBot;

public class UCIBot : IChessBot
{
    private Process botProcess;

    public static List<Process> Processes = new();

    private StreamWriter Ins() => botProcess.StandardInput;

    private StreamReader Outs() => botProcess.StandardOutput;

    /// <summary>
    /// The skill level of stockfish. Max is 20, min is 0.
    /// </summary>
    private const int SKILL_LEVEL = 20;

    public UCIBot(String pathToExecutable)
    {
        var botExecutable = pathToExecutable;

        botProcess = new();
        Processes.Add(botProcess);
        botProcess.StartInfo.RedirectStandardOutput = true;
        botProcess.StartInfo.RedirectStandardInput = true;
        botProcess.StartInfo.FileName = botExecutable;
        botProcess.Start();

        Ins().WriteLine("uci");
        string? line;
        var isOk = false;

        while ((line = Outs().ReadLine()) != null)
        {
            if (line == "uciok")
            {
                isOk = true;
                break;
            }
        }

        if (!isOk)
        {
            throw new Exception("Failed to communicate with stockfish");
        }

        Ins().WriteLine($"setoption name Skill Level value {SKILL_LEVEL}");
        Ins().WriteLine("ucinewgame");
    }

    public Move Think(Board board, Timer timer)
    {
        Ins().WriteLine($"position fen {board.GetFenString()}");

        string me = "w",
            other = "b";
        if (!board.IsWhiteToMove)
        {
            (me, other) = (other, me);
        }
        Ins()
            .WriteLine(
                $"go {me}time {timer.MillisecondsRemaining} {other}time {timer.OpponentMillisecondsRemaining}"
            );
        /* Ins().WriteLine($"go movetime 100"); */

        string? line;
        Move? move = null;

        while ((line = Outs().ReadLine()) != null)
        {
            Console.WriteLine(line.Substring(0, Math.Min(72, line.Length)));
            if (line.StartsWith("bestmove"))
            {
                var moveStr = line.Split()[1];
                move = new Move(moveStr, board);

                break;
            }
        }

        if (move == null)
        {
            throw new Exception("Engine crashed");
        }

        return (Move)move;
    }
}