﻿using Raylib_cs;
using System.Numerics;
using System;
using System.IO;
using System.Linq;

namespace ChessChallenge.Application
{
    public static class MenuUI
    {
        public static ChallengeController.PlayerType Bot1Type = ChallengeController.PlayerType.MyBot;
        public static ChallengeController.PlayerType Bot2Type = ChallengeController.PlayerType.MyBot;
        public static ChallengeController.PlayerType HumanOpponent = ChallengeController.PlayerType.MyBot;

        public static void DrawButtons(ChallengeController controller)
        {
            Vector2 buttonPos = UIHelper.Scale(new Vector2(260, 50));
            Vector2 buttonSize = UIHelper.Scale(new Vector2(260, 55));
            float spacing = buttonSize.Y * 1.2f;
            float breakSpacing = spacing * 0.6f;

            buttonPos.X -= buttonSize.X / 2;
            // Game Buttons
            if (NextButtonInRow("Human", ref buttonPos, spacing, buttonSize with {X = buttonSize.X * ((float)4 / 5)}))
            {
                
                var whiteType = controller.HumanWasWhiteLastGame
                    ? HumanOpponent
                    : ChallengeController.PlayerType.Human;
                var blackType = !controller.HumanWasWhiteLastGame
                    ? HumanOpponent
                    : ChallengeController.PlayerType.Human;
                controller.StartNewGame(whiteType, blackType);
            }
            buttonPos.X += buttonSize.X / 2;

            UIHelper.DrawText("Bot vs Bot Match", buttonPos, UIHelper.ScaleInt(32), 1, Color.WHITE,
                UIHelper.AlignH.Centre);
            buttonPos.Y += spacing;

            DrawBotPickers(controller, ref buttonPos, spacing, buttonSize);

            controller.FenInputBox.Draw(buttonPos, buttonSize with { X = buttonSize.X * (float)1.5 });
            buttonPos.Y += spacing;

            if(NextButtonInRow("Clear Fen", ref buttonPos, spacing, buttonSize))
            {
                controller.FenInputBox.value = "";
            }
            UIHelper.DrawText($"Current Game Duration: {controller.GameDurationMilliseconds / (float)60000} mins", buttonPos, UIHelper.ScaleInt(32), 1, Color.WHITE, UIHelper.AlignH.Centre);
            buttonPos.Y += spacing;

            if (NextButtonInRow("Set new Game Duration", ref buttonPos, spacing, buttonSize with {X = buttonSize.X * (float)1.5}))
            {
                controller.SetNewGameDuration();
            }

            // Page buttons
            buttonPos.Y += breakSpacing;

            if (NextButtonInRow("Save Games", ref buttonPos, spacing, buttonSize))
            {
                string pgns = controller.AllPGNs;
                string directoryPath = Path.Combine(FileHelper.AppDataPath, "Games");
                Directory.CreateDirectory(directoryPath);
                string fileName = FileHelper.GetUniqueFileName(directoryPath, "games", ".txt");
                string fullPath = Path.Combine(directoryPath, fileName);
                File.WriteAllText(fullPath, pgns);
                ConsoleHelper.Log("Saved games to " + fullPath, false, ConsoleColor.Blue);
            }

            if (NextButtonInRow("Rules & Help", ref buttonPos, spacing, buttonSize))
            {
                FileHelper.OpenUrl("https://github.com/SebLague/Chess-Challenge");
            }

            if (NextButtonInRow("Documentation", ref buttonPos, spacing, buttonSize))
            {
                FileHelper.OpenUrl("https://seblague.github.io/chess-coding-challenge/documentation/");
            }

            if (NextButtonInRow("Submission Page", ref buttonPos, spacing, buttonSize))
            {
                FileHelper.OpenUrl("https://forms.gle/6jjj8jxNQ5Ln53ie6");
            }

            // Window and quit buttons
            buttonPos.Y += breakSpacing;

            bool isBigWindow = Raylib.GetScreenWidth() > Settings.ScreenSizeSmall.X;
            string windowButtonName = isBigWindow ? "Smaller Window" : "Bigger Window";
            if (NextButtonInRow(windowButtonName, ref buttonPos, spacing, buttonSize))
            {
                Program.SetWindowSize(isBigWindow ? Settings.ScreenSizeSmall : Settings.ScreenSizeBig);
            }

            if (NextButtonInRow("Exit (ESC)", ref buttonPos, spacing, buttonSize))
            {
                Environment.Exit(0);
            }

            bool NextButtonInRow(string name, ref Vector2 pos, float spacingY, Vector2 size)
            {
                bool pressed = UIHelper.Button(name, pos, size);
                pos.Y += spacingY;
                return pressed;
            }
            DrawRightButtons(controller);
        }

        public static void DrawRightButtons(ChallengeController controller) {
            Vector2 buttonPos = UIHelper.Scale(new Vector2(1630, 950));
            Vector2 buttonSize = UIHelper.Scale(new Vector2(260, 55));
            float spacing = buttonSize.Y * 1.2f;
            float breakSpacing = spacing * 0.6f;

            if(NextButtonInRow("Flip Board", ref buttonPos, spacing, buttonSize))
            {
                controller.FlipPerspective();
            }
            
            Vector2 skillLevelPos = UIHelper.Scale(new Vector2(1630, 50));
            bool rightClickSkill = UIHelper.RightClickButton(skillLevelPos, buttonSize);
            if (UIHelper.Button($"Skill Level: {controller.botSkill}", skillLevelPos, buttonSize) || rightClickSkill)
            {
                if (rightClickSkill)
                    controller.botSkill = controller.botSkill == 0 ? 20 : controller.botSkill - 1;
                else
                    controller.botSkill = (controller.botSkill + 1) % 21;
                ChallengeController.PlayerType white = controller.PlayerWhite.PlayerType;
                ChallengeController.PlayerType black = controller.PlayerBlack.PlayerType;
                if (ChallengeController.UsesSkillLevel.Contains(white) || ChallengeController.UsesSkillLevel.Contains(black))
                {
                    controller.StartNewGame(white, black);
                }
            }

            bool NextButtonInRow(string name, ref Vector2 pos, float spacingY, Vector2 size)
            {
                bool pressed = UIHelper.Button(name, pos, size);
                pos.Y -= spacingY;
                return pressed;
            }
        }

        public static void DrawBotPickers(ChallengeController challengeController, ref Vector2 pos, float spacingY,
            Vector2 size)
        {
            Vector2 botPos = pos with { X = pos.X - size.X / 2 };
            Vector2 humanOpponentPos = pos with { Y = pos.Y - 2 * spacingY, X = pos.X + size.X / 2 };
            DrawBotPicker(3, humanOpponentPos);
            ChallengeController.PlayerType bot1 = DrawBotPicker(1, botPos);
            botPos.X += size.X;
            ChallengeController.PlayerType bot2 = DrawBotPicker(2, botPos);
            pos.Y += spacingY;

            if (UIHelper.Button("Start", pos, size))
            {
                challengeController.StartNewBotMatch(bot1, bot2);
            }

            pos.Y += spacingY;

            ChallengeController.PlayerType DrawBotPicker(int botNumber, Vector2 pos)
            {
                ref ChallengeController.PlayerType botType = ref Bot1Type;
                if (botNumber == 2)
                {
                    botType = ref Bot2Type;
                }

                if (botNumber == 3)
                {
                    botType = ref HumanOpponent;
                }

                if (botNumber is < 1 or > 3)
                {
                    throw new Exception("Invalid bot number!");
                }

                Vector2 buttonSize = size with { X = size.X * ((float)4 / 5) };
                if (UIHelper.Button(botType.ToString(), pos, buttonSize))
                {
                    botType = NextBotType(botType);
                }

                if (UIHelper.RightClickButton(pos, buttonSize))
                {
                    botType = NextBotType(botType, -1);
                }

                return botType;
            }

            ChallengeController.PlayerType NextBotType(ChallengeController.PlayerType currentType, int direction = 1)
            {
                ChallengeController.PlayerType nextType = currentType;
                while (nextType == currentType || nextType == ChallengeController.PlayerType.Human)
                {
                    ChallengeController.PlayerType[] types = Enum.GetValues<ChallengeController.PlayerType>();
                    int index = Array.IndexOf(types, nextType) + direction;
                    if (index == -1)
                        nextType = types[^1];
                    else if (index == types.Length)
                        nextType = types[0];
                    else 
                        nextType = types[index];
                }

                return nextType;
            }
        }
    }
}