using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using ChessChallenge.API;

namespace ChessChallenge.UCIBot;

public class UCIBot : IChessBot
{
    private Process botProcess;

    private bool displayOutput;

    public static string STOCKFISH_PATH = "/opt/homebrew/bin/stockfish";

    public static List<Process> Processes = new();

    private StreamWriter Ins() => botProcess.StandardInput;

    private StreamReader Outs() => botProcess.StandardOutput;

    /// <summary>
    /// The skill level of stockfish. Max is 20, min is 0.
    /// </summary>
    private int _skillLevel = 20;

    public UCIBot(String pathToExecutable, bool displayOutput = true, int skillLevel = 20)
    {
        var botExecutable = pathToExecutable;

        _skillLevel = skillLevel;
        this.displayOutput = displayOutput;

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

        Ins().WriteLine($"setoption name Skill Level value {_skillLevel}");
        Ins().WriteLine("ucinewgame");
    }

    public int EvaluatePosition(Board board)
    {
        Ins().WriteLine("ucinewgame");
        Ins().WriteLine($"position fen {board.GetFenString()}");
        Ins()
            .WriteLine(
                $"go depth 1"
            );
        string? line;
        int eval = 0;
        while ((line = Outs().ReadLine()) != null)
        {
            if (displayOutput)
            {
                Console.WriteLine(line);
            }
            if (line.Contains("score"))
            {
                Match mateMatch = Regex.Match(line, "\\smate\\s(-?\\d*)");
                if (mateMatch.Success)
                {
                    int mateDistance = int.Parse(mateMatch.Groups[1].ToString());
                    eval = 50000 - Math.Abs(mateDistance);
                    if (mateDistance < 0)
                        eval = -eval;
                    return eval;
                }

                Match scoreMatch = Regex.Match(line, "\\scp\\s(-?\\d*)");
                if (scoreMatch.Success)
                {
                    int score = int.Parse(scoreMatch.Groups[1].ToString());
                    eval = score;
                }
                break;
            }
        }
        return eval;
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
            if (displayOutput)
            {
                Console.WriteLine(line.Substring(0, Math.Min(72, line.Length)));
            }
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