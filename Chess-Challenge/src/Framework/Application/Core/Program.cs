using System;
using System.Collections.Generic;
using System.Diagnostics;
using Raylib_cs;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using ChessChallenge.Chess;
using ChessChallenge.MyBot;

namespace ChessChallenge.Application
{
    static class Program
    {
        const bool hideRaylibLogs = true;
        static Camera2D cam;

        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "bot":
                        bool humanOpponent = false;
                        if (args.Length > 1)
                        {
                            if ((new List<string> { "human" }).Contains(args[1].ToLower()))
                            {
                                humanOpponent = true;
                            }
                        }
                        MyBot.Tester.Run(humanOpponent);
                        return;
                    case "fens":
                        foreach (var moveCount in new[] {15,25,50})
                        {
                            List<string> fenList = new();
                            for (int i = 0; i < 2000; i++)
                            {
                                string fen = FenUtility.CurrentFen(MyBot.Tester.RandomBoard(moveCount));
                                if (!fenList.Contains(fen))
                                {
                                    fenList.Add(fen);
                                }
                            }

                            string fileName;
                            switch (moveCount)
                            {
                                case 15:
                                    fileName = "earlyFens";
                                    break;
                                case 25:
                                    fileName = "midFens";
                                    break;
                                case 50:
                                    fileName = "endFens";
                                    break;
                                default:
                                    fileName = "fens";
                                    break;
                            }
                            string fullPath = Path.Combine(Environment.CurrentDirectory, "TestData", fileName + ".txt");
                            File.WriteAllText(fullPath, String.Join("\n", fenList));
                        }

                        return;
                    case "program":
                        Tester.Run(false);
                        return;
                    case "book":
                        OpeningBook.Run();
                        return;
                }
            }
            Vector2 loadedWindowSize = GetSavedWindowSize();
            int screenWidth = (int)loadedWindowSize.X;
            int screenHeight = (int)loadedWindowSize.Y;

            if (hideRaylibLogs)
            {
                unsafe
                {
                    Raylib.SetTraceLogCallback(&LogCustom);
                }
            }

            Raylib.InitWindow(screenWidth, screenHeight, "Chess Coding Challenge");
            Raylib.SetTargetFPS(60);

            UpdateCamera(screenWidth, screenHeight);

            ChallengeController controller = new();

            while (!Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(22, 22, 22, 255));
                Raylib.BeginMode2D(cam);

                controller.Update();
                controller.Draw();

                Raylib.EndMode2D();

                controller.DrawOverlay();

                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();

            controller.Release();
            UIHelper.Release();
            foreach (Process process in UCIBot.UCIBot.Processes)
            {
                process.Kill();
            }
        }

        public static void SetWindowSize(Vector2 size)
        {
            Raylib.SetWindowSize((int)size.X, (int)size.Y);
            UpdateCamera((int)size.X, (int)size.Y);
            SaveWindowSize();
        }

        public static Vector2 ScreenToWorldPos(Vector2 screenPos) => Raylib.GetScreenToWorld2D(screenPos, cam);

        static void UpdateCamera(int screenWidth, int screenHeight)
        {
            cam = new Camera2D();
            cam.target = new Vector2(0, 15);
            cam.offset = new Vector2(screenWidth / 2f, screenHeight / 2f);
            cam.zoom = screenWidth / 1280f * 0.7f;
        }


        [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static unsafe void LogCustom(int logLevel, sbyte* text, sbyte* args)
        {
        }

        static Vector2 GetSavedWindowSize()
        {
            if (File.Exists(FileHelper.PrefsFilePath))
            {
                string prefs = File.ReadAllText(FileHelper.PrefsFilePath);
                if (!string.IsNullOrEmpty(prefs))
                {
                    if (prefs[0] == '0')
                    {
                        return Settings.ScreenSizeSmall;
                    }
                    else if (prefs[0] == '1')
                    {
                        return Settings.ScreenSizeBig;
                    }
                }
            }
            return Settings.ScreenSizeSmall;
        }

        static void SaveWindowSize()
        {
            Directory.CreateDirectory(FileHelper.AppDataPath);
            bool isBigWindow = Raylib.GetScreenWidth() > Settings.ScreenSizeSmall.X;
            File.WriteAllText(FileHelper.PrefsFilePath, isBigWindow ? "1" : "0");
        }

      

    }


}