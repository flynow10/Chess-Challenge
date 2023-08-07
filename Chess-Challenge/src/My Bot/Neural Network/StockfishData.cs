using System;
using System.Diagnostics;
using System.IO;
using ChessChallenge.Chess;

namespace ChessChallenge.MyBot.Neural_Network;

public static class StockfishData
{
    private static Process stockfishProcess;

    private static StreamWriter Ins() => stockfishProcess.StandardInput;

    private static StreamReader Outs() => stockfishProcess.StandardOutput;
    public static void Init()
    {
        stockfishProcess = new Process();
        stockfishProcess.StartInfo.RedirectStandardOutput = true;
        stockfishProcess.StartInfo.RedirectStandardInput = true;
        stockfishProcess.StartInfo.FileName = "/opt/homebrew/bin/stockfish";
        stockfishProcess.Start();
        
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

        Ins().WriteLine("ucinewgame");
    }

    public static void Run()
    {
        Init();
        string fileText = "";
        for (int i = 0; i < 10000; i++)
        {
            string fen = GetRandomFenString();
            int evaluation = GetEvaluation(fen, 5);
            string line = fen + ":" + evaluation + Environment.NewLine;
            fileText += line;
        }
        File.WriteAllText("./datapoints2.txt", fileText);
    }

    public static string GetRandomFenString(int maxDepth = 200)
    {
        Random rng = new ();
        Board board = new ();
        board.LoadStartPosition();
        MoveGenerator moveGenerator = new ();

        int depth = rng.Next(maxDepth);

        for (int i = 0; i < depth; i++)
        {
            Span<Move> moves = moveGenerator.GenerateMoves(board);
            var move = moves[rng.Next(moves.Length)];
            board.MakeMove(move, false);

            if (Arbiter.GetGameState(board) != GameResult.InProgress)
            {
                board.UndoMove(move, false);
                break;
            }
        }

        return FenUtility.CurrentFen(board);
    }

    public static int GetEvaluation(string fen, int depth = 20)
    {
        Ins().WriteLine($"position fen {fen}");
        Ins().WriteLine($"go depth {depth}");

        int? lastScore = null;
        string? line;
        while ((line = Outs().ReadLine()) != null)
        {
            if (line.StartsWith("info"))
            {
                string[] splitLine = line.Split(" ");
                int centiPawnIndex = Array.IndexOf(splitLine, "cp");
                if (centiPawnIndex != -1)
                {
                    int scoreIndex = centiPawnIndex + 1;
                    lastScore = int.Parse(splitLine[scoreIndex]);
                }
                else
                {
                    int mateIndex = Array.IndexOf(splitLine, "mate");
                    if (mateIndex != -1)
                    {
                        int mateDistance = int.Parse(splitLine[mateIndex + 1]);
                        lastScore = Math.Sign(mateDistance) * (50000 - Math.Abs(mateDistance));
                    }
                }
            }

            if (line.StartsWith("bestmove"))
            {
                break;
            }
        }

        if (lastScore == null)
        {
            throw new Exception($"No score found for position {fen}");
        }

        if (fen.Contains("b"))
        {
            lastScore = -lastScore;
        }
        return (int)lastScore;
    }
}