﻿using ChessChallenge.Chess;
using ChessChallenge.Example;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ChessChallenge.Application.Settings;
using static ChessChallenge.Application.ConsoleHelper;

namespace ChessChallenge.Application
{
    public class ChallengeController
    {
        public enum PlayerType
        {
            Human,
            MyBot,
            EvilBot,
            ExtraEvilBot,
            PawnBot,
            StockFish,
            Comet,
            MyBotV1,
            MyBotV2,
            MyBotV3,
            MyBotV4,
            MyBotV5,
            MyBotV6,
            MyBotV7,
            MyBotV8,
            MyBotV9,
        }

        public static readonly PlayerType[] UsesNewTokenLimit =
            { PlayerType.MyBot, PlayerType.MyBotV8, PlayerType.MyBotV9 };

        public static readonly PlayerType[] UsesSkillLevel =
            { PlayerType.MyBot, PlayerType.StockFish, PlayerType.MyBotV8, PlayerType.MyBotV9 };

        // Game state
        readonly Random rng;
        int gameID;
        bool isPlaying;
        Board board;
        public ChessPlayer PlayerWhite { get; private set; }
        public ChessPlayer PlayerBlack { get; private set; }

        float lastMoveMadeTime;
        bool isWaitingToPlayMove;
        Move moveToPlay;
        float playMoveTime;
        public bool HumanWasWhiteLastGame { get; private set; }
        public int GameDurationMilliseconds { get; private set; } = Settings.GameDurationMilliseconds;

        // Bot match state
        readonly string[] botMatchStartFens;
        int botMatchGameIndex;
        public BotMatchStats BotStatsA { get; private set; }
        public BotMatchStats BotStatsB { get; private set; }
        bool botAPlaysWhite;
        bool startedFromCustomFen = false;


        // Bot task
        AutoResetEvent botTaskWaitHandle;
        bool hasBotTaskException;
        ExceptionDispatchInfo botExInfo;

        // Other
        readonly BoardUI boardUI;
        readonly MoveGenerator moveGenerator;
        readonly Dictionary<PlayerType, TokenCount> tokenCounts = new();
        readonly StringBuilder pgns;
        public readonly UIHelper.InputBox FenInputBox = new("Click to Paste Fen String");
        public int botSkill = 20;

        public ChallengeController()
        {
            Log($"Launching Chess-Challenge version {Settings.Version}");
            GetAllTokenCounts(ref tokenCounts);
            Warmer.Warm();

            rng = new Random();
            moveGenerator = new();
            boardUI = new BoardUI();
            board = new Board();
            pgns = new();

            BotStatsA = new BotMatchStats("IBot");
            BotStatsB = new BotMatchStats("IBot");
            botMatchStartFens = FileHelper.ReadResourceFile("Fens.txt").Split('\n').Where(fen => fen.Length > 0)
                .ToArray();
            botTaskWaitHandle = new AutoResetEvent(false);

            StartNewGame(PlayerType.Human, PlayerType.MyBot);
        }

        public void StartNewGame(PlayerType whiteType, PlayerType blackType, bool autoStarted = false)
        {
            // End any ongoing game
            EndGame(GameResult.DrawByArbiter, log: false, autoStartNextBotMatch: false);
            gameID = rng.Next();

            // Stop prev task and create a new one
            if (RunBotsOnSeparateThread)
            {
                // Allow task to terminate
                botTaskWaitHandle.Set();
                // Create new task
                botTaskWaitHandle = new AutoResetEvent(false);
                Task.Factory.StartNew(BotThinkerThread, TaskCreationOptions.LongRunning);
            }

            // Board Setup
            board = new Board();
            if(autoStarted || FenInputBox.value == "")
            {
                bool isGameWithHuman = whiteType is PlayerType.Human || blackType is PlayerType.Human;
                int fenIndex = isGameWithHuman ? 0 : botMatchGameIndex / 2;
                board.LoadPosition(botMatchStartFens[fenIndex]);
                startedFromCustomFen = false;
            } else {
                board.LoadPosition(FenInputBox.value);
                startedFromCustomFen = true;
            }

            // Player Setup
            PlayerWhite = CreatePlayer(whiteType);
            PlayerBlack = CreatePlayer(blackType);
            PlayerWhite.SubscribeToMoveChosenEventIfHuman(OnMoveChosen);
            PlayerBlack.SubscribeToMoveChosenEventIfHuman(OnMoveChosen);

            // UI Setup
            boardUI.UpdatePosition(board);
            boardUI.ResetSquareColours();
            SetBoardPerspective();

            // Start
            isPlaying = true;
            NotifyTurnToMove();
        }

        void BotThinkerThread()
        {
            int threadID = gameID;
            //Console.WriteLine("Starting thread: " + threadID);

            while (true)
            {
                // Sleep thread until notified
                botTaskWaitHandle.WaitOne();
                // Get bot move
                if (threadID == gameID)
                {
                    var move = GetBotMove();

                    if (threadID == gameID)
                    {
                        OnMoveChosen(move);
                    }
                }

                // Terminate if no longer playing this game
                if (threadID != gameID)
                {
                    break;
                }
            }
            //Console.WriteLine("Exitting thread: " + threadID);
        }

        Move GetBotMove()
        {
            API.Board botBoard = new(board);
            try
            {
                API.Timer timer = new(PlayerToMove.TimeRemainingMs, PlayerNotOnMove.TimeRemainingMs, GameDurationMilliseconds, IncrementMilliseconds);
                API.Move move = PlayerToMove.Bot.Think(botBoard, timer);
                return new Move(move.RawValue);
            }
            catch (Exception e)
            {
                Log("An error occurred while bot was thinking.\n" + e.ToString(), true, ConsoleColor.Red);
                hasBotTaskException = true;
                botExInfo = ExceptionDispatchInfo.Capture(e);
            }

            return Move.NullMove;
        }


        void NotifyTurnToMove()
        {
            //playerToMove.NotifyTurnToMove(board);
            if (PlayerToMove.IsHuman)
            {
                PlayerToMove.Human.SetPosition(FenUtility.CurrentFen(board));
                PlayerToMove.Human.NotifyTurnToMove();
            }
            else
            {
                if (RunBotsOnSeparateThread)
                {
                    botTaskWaitHandle.Set();
                }
                else
                {
                    double startThinkTime = Raylib.GetTime();
                    var move = GetBotMove();
                    double thinkDuration = Raylib.GetTime() - startThinkTime;
                    PlayerToMove.UpdateClock(thinkDuration);
                    OnMoveChosen(move);
                }
            }
        }

        void SetBoardPerspective()
        {
            // Board perspective
            if (PlayerWhite.IsHuman || PlayerBlack.IsHuman)
            {
                boardUI.SetPerspective(PlayerWhite.IsHuman);
                HumanWasWhiteLastGame = PlayerWhite.IsHuman;
            }
            else if (PlayerWhite.Bot is MyBot.MyBot && PlayerBlack.Bot is MyBot.MyBot)
            {
                boardUI.SetPerspective(true);
            }
            else
            {
                boardUI.SetPerspective(PlayerWhite.Bot is MyBot.MyBot);
            }
        }

        public void FlipPerspective() {
            boardUI.SetPerspective(!boardUI.whitePerspective);
        }

        ChessPlayer CreatePlayer(PlayerType type)
        {
            return type switch
            {
                PlayerType.MyBot => new ChessPlayer(new MyBot.MyBot(botSkill), type, GameDurationMilliseconds),
                PlayerType.EvilBot => new ChessPlayer(new EvilBot(), type, GameDurationMilliseconds),
                PlayerType.ExtraEvilBot => new ChessPlayer(new ExtraEvilBot(), type, GameDurationMilliseconds),
                PlayerType.PawnBot => new ChessPlayer(new PawnBot(), type, GameDurationMilliseconds),
                PlayerType.StockFish => new ChessPlayer(new UCIBot.UCIBot("/opt/homebrew/bin/stockfish", false, botSkill), type, GameDurationMilliseconds),
                PlayerType.Comet => new ChessPlayer(new UCIBot.UCIBot(Path.Combine(FileHelper.GetSrcDir()!, "Comet", "chess_bot")), type, GameDurationMilliseconds),
                PlayerType.MyBotV1 => new ChessPlayer(new Version1.MyBot(), type, GameDurationMilliseconds),
                PlayerType.MyBotV2 => new ChessPlayer(new Version2.MyBot(), type, GameDurationMilliseconds),
                PlayerType.MyBotV3 => new ChessPlayer(new Version3.MyBot(), type, GameDurationMilliseconds),
                PlayerType.MyBotV4 => new ChessPlayer(new Version4.MyBot(), type, GameDurationMilliseconds),
                PlayerType.MyBotV5 => new ChessPlayer(new Version5.MyBot(), type, GameDurationMilliseconds),
                PlayerType.MyBotV6 => new ChessPlayer(new Version6.MyBot(), type, GameDurationMilliseconds),
                PlayerType.MyBotV7 => new ChessPlayer(new Version7.MyBot(), type, GameDurationMilliseconds),
                PlayerType.MyBotV8 => new ChessPlayer(new Version8.MyBot(botSkill), type, GameDurationMilliseconds),
                PlayerType.MyBotV9 => new ChessPlayer(new Version9.MyBot(botSkill), type, GameDurationMilliseconds),
                _ => new ChessPlayer(new HumanPlayer(boardUI), type)
            };
        }

        public void SetNewGameDuration()
        {
            try
            {
                Console.Write("Set bot time (mins): ");
                string? newMinutesString = Console.ReadLine();
                if (newMinutesString != null)
                {
                    float minutes = float.Parse(newMinutesString);
                    GameDurationMilliseconds = (int)(minutes * 60 * 1000);
                }
                StartNewGame(PlayerWhite.PlayerType, PlayerBlack.PlayerType);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static TokenCount GetMyBotTokenCount()
        {
            string? srcDir = FileHelper.GetSrcDir();
            if (srcDir == null)
            {
                return new TokenCount(0, 0);
            }
            string path = Path.Combine(srcDir, "My Bot", "MyBot.cs");
            return GetTokenCount(path);
        }

        static TokenCount GetExtraEvilBotTokenCount()
        {
            string? srcDir = FileHelper.GetSrcDir();
            if (srcDir == null)
            {
                return new TokenCount(0, 0);
            }
            string path = Path.Combine(srcDir, "Extra Evil Bot", "ExtraEvilBot.cs");
            return GetTokenCount(path);
        }

        static TokenCount GetVersionTokenCount(int version)
        {
            string? srcDir = FileHelper.GetSrcDir();
            if (srcDir == null)
            {
                return new TokenCount(0, 0);
            }
            string path = Path.Combine(srcDir, "Versions", "MyBotV" + version,
                "MyBot.cs");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException();
            }

            return GetTokenCount(path);
        }

        static void GetAllTokenCounts(ref Dictionary<PlayerType, TokenCount> tokenCounts)
        {
            foreach (PlayerType playerType in Enum.GetValues<PlayerType>())
            {
                if (new [] {PlayerType.Human, PlayerType.EvilBot, PlayerType.Comet, PlayerType.StockFish, PlayerType.PawnBot}.Contains(playerType))
                {
                    tokenCounts[playerType] = new TokenCount(0,0);
                    continue;
                }

                if (playerType == PlayerType.MyBot)
                {
                    tokenCounts[playerType] = GetMyBotTokenCount();
                }
                else if (playerType == PlayerType.ExtraEvilBot)
                {
                    tokenCounts[playerType] = GetExtraEvilBotTokenCount();
                }
                else
                {
                    tokenCounts[playerType] = GetVersionTokenCount(int.Parse(playerType.ToString().Substring(6)));
                }
            }

            Log("Bot Brain Capacity List", false, ConsoleColor.Blue);
            foreach (KeyValuePair<PlayerType, TokenCount> bot in tokenCounts)
            {
                if (bot.Value.total != 0)
                {
                    Log(string.Format("Bot: {0}, Brain Capacity: {1}, ({2} with debugs)", bot.Key, bot.Value.total - bot.Value.debug, bot.Value.total));
                }
            }
        }

        static TokenCount GetTokenCount(string path)
        {
            using StreamReader reader = new(path);
            string txt = reader.ReadToEnd();
            return TokenCounter.CountTokens(txt);
        }

        void OnMoveChosen(Move chosenMove)
        {
            if (IsLegal(chosenMove))
            {
                PlayerToMove.AddIncrement(IncrementMilliseconds);
                if (PlayerToMove.IsBot)
                {
                    moveToPlay = chosenMove;
                    isWaitingToPlayMove = true;
                    playMoveTime = lastMoveMadeTime + MinMoveDelay;
                }
                else
                {
                    PlayMove(chosenMove);
                }
            }
            else
            {
                string moveName = MoveUtility.GetMoveNameUCI(chosenMove);
                string log = $"Illegal move: {moveName} in position: {FenUtility.CurrentFen(board)}";
                Log(log, true, ConsoleColor.Red);
                GameResult result = PlayerToMove == PlayerWhite
                    ? GameResult.WhiteIllegalMove
                    : GameResult.BlackIllegalMove;
                EndGame(result);
            }
        }

        void PlayMove(Move move)
        {
            if (isPlaying)
            {
                bool animate = PlayerToMove.IsBot;
                lastMoveMadeTime = (float)Raylib.GetTime();

                board.MakeMove(move, false);
                boardUI.UpdatePosition(board, move, animate);

                GameResult result = Arbiter.GetGameState(board);
                if (result == GameResult.InProgress)
                {
                    NotifyTurnToMove();
                }
                else
                {
                    EndGame(result);
                }
            }
        }

        void EndGame(GameResult result, bool log = true, bool autoStartNextBotMatch = true)
        {
            if (isPlaying)
            {
                isPlaying = false;
                isWaitingToPlayMove = false;
                gameID = -1;

                if (log)
                {
                    Log("Game Over: " + result, false, ConsoleColor.Blue);
                }

                string pgn = PGNCreator.CreatePGN(board, result, GetPlayerName(PlayerWhite),
                    GetPlayerName(PlayerBlack));
                pgns.AppendLine(pgn);

                // If 2 bots playing each other, start next game automatically.
                if (PlayerWhite.IsBot && PlayerBlack.IsBot && !startedFromCustomFen)
                {
                    UpdateBotMatchStats(result);
                    botMatchGameIndex++;
                    int numGamesToPlay = botMatchStartFens.Length * 2;

                    if (botMatchGameIndex < numGamesToPlay && autoStartNextBotMatch)
                    {
                        botAPlaysWhite = !botAPlaysWhite;
                        const int startNextGameDelayMs = 600;
                        System.Timers.Timer autoNextTimer = new(startNextGameDelayMs);
                        int originalGameID = gameID;
                        autoNextTimer.Elapsed += (s, e) => AutoStartNextBotMatchGame(originalGameID, autoNextTimer);
                        autoNextTimer.AutoReset = false;
                        autoNextTimer.Start();
                    }
                    else if (autoStartNextBotMatch)
                    {
                        Log("Match finished", false, ConsoleColor.Blue);
                    }
                }
            }
        }

        private void AutoStartNextBotMatchGame(int originalGameID, System.Timers.Timer timer)
        {
            if (originalGameID == gameID)
            {
                StartNewGame(PlayerBlack.PlayerType, PlayerWhite.PlayerType);
            }

            timer.Close();
        }


        void UpdateBotMatchStats(GameResult result)
        {
            UpdateStats(BotStatsA, botAPlaysWhite);
            UpdateStats(BotStatsB, !botAPlaysWhite);

            void UpdateStats(BotMatchStats stats, bool isWhiteStats)
            {
                // Draw
                if (Arbiter.IsDrawResult(result))
                {
                    stats.NumDraws++;
                }
                // Win
                else if (Arbiter.IsWhiteWinsResult(result) == isWhiteStats)
                {
                    stats.NumWins++;
                }
                // Loss
                else
                {
                    stats.NumLosses++;
                    stats.NumTimeouts += (result is GameResult.WhiteTimeout or GameResult.BlackTimeout) ? 1 : 0;
                    stats.NumIllegalMoves +=
                        (result is GameResult.WhiteIllegalMove or GameResult.BlackIllegalMove) ? 1 : 0;
                }
            }
        }

        public void Update()
        {
            if (isPlaying)
            {
                PlayerWhite.Update();
                PlayerBlack.Update();

                PlayerToMove.UpdateClock(Raylib.GetFrameTime());
                if (PlayerToMove.TimeRemainingMs <= 0)
                {
                    EndGame(PlayerToMove == PlayerWhite ? GameResult.WhiteTimeout : GameResult.BlackTimeout);
                }
                else
                {
                    if (isWaitingToPlayMove && Raylib.GetTime() > playMoveTime)
                    {
                        isWaitingToPlayMove = false;
                        PlayMove(moveToPlay);
                    }
                }
            }

            if (hasBotTaskException)
            {
                hasBotTaskException = false;
                botExInfo.Throw();
            }
        }

        public void Draw()
        {
            boardUI.Draw();
            string nameW = GetPlayerName(PlayerWhite);
            string nameB = GetPlayerName(PlayerBlack);
            if (UsesSkillLevel.Contains(PlayerWhite.PlayerType))
                nameW += $" - (Level: {botSkill})";
            if (UsesSkillLevel.Contains(PlayerBlack.PlayerType))
                nameB += $" - (Level: {botSkill})";
            boardUI.DrawPlayerNames(nameW, nameB, PlayerWhite.TimeRemainingMs, PlayerBlack.TimeRemainingMs, isPlaying);
        }

        public void DrawOverlay()
        {
            ChessPlayer firstPlayer =
                (int)PlayerWhite.PlayerType < (int)PlayerBlack.PlayerType ? PlayerWhite : PlayerBlack;

            ChessPlayer secondPlayer = firstPlayer.PlayerType == PlayerWhite.PlayerType ? PlayerBlack : PlayerWhite;
            TokenCount firstTokens = tokenCounts[firstPlayer.PlayerType];
            TokenCount secondTokens = tokenCounts[secondPlayer.PlayerType];
            int totalCapacityBars = (firstTokens.total == 0 ? 0 : 1) +
                                    (secondTokens.total == 0 || PlayerWhite.PlayerType == PlayerBlack.PlayerType ? 0 : 1);
            int drawnCapacityBars = 0;
            if (firstTokens.total != 0)
            {
                BotBrainCapacityUI.Draw(GetPlayerName(firstPlayer), firstTokens, UsesNewTokenLimit.Contains(firstPlayer.PlayerType) ? SecondMaxTokenCount : MaxTokenCount, totalCapacityBars,
                    drawnCapacityBars++);
            }

            if (secondTokens.total != 0 && PlayerWhite.PlayerType != PlayerBlack.PlayerType)
            {
                BotBrainCapacityUI.Draw(GetPlayerName(secondPlayer), secondTokens, UsesNewTokenLimit.Contains(secondPlayer.PlayerType) ? SecondMaxTokenCount : MaxTokenCount, totalCapacityBars,
                    drawnCapacityBars);
            }

            MenuUI.DrawButtons(this);
            MatchStatsUI.DrawMatchStats(this);
        }

        static string GetPlayerName(ChessPlayer player) => GetPlayerName(player.PlayerType);
        static string GetPlayerName(PlayerType type) => type.ToString();

        public void StartNewBotMatch(PlayerType botTypeA, PlayerType botTypeB)
        {
            EndGame(GameResult.DrawByArbiter, log: false, autoStartNextBotMatch: false);
            botMatchGameIndex = 0;
            string nameA = GetPlayerName(botTypeA);
            string nameB = GetPlayerName(botTypeB);
            if (nameA == nameB)
            {
                nameA += " (A)";
                nameB += " (B)";
            }

            BotStatsA = new BotMatchStats(nameA);
            BotStatsB = new BotMatchStats(nameB);
            botAPlaysWhite = true;
            Log($"Starting new match: {nameA} vs {nameB}", false, ConsoleColor.Blue);
            StartNewGame(botTypeA, botTypeB);
        }


        ChessPlayer PlayerToMove => board.IsWhiteToMove ? PlayerWhite : PlayerBlack;
        ChessPlayer PlayerNotOnMove => board.IsWhiteToMove ? PlayerBlack : PlayerWhite;

        public int TotalGameCount => botMatchStartFens.Length * 2;
        public int CurrGameNumber => Math.Min(TotalGameCount, botMatchGameIndex + 1);
        public string AllPGNs => pgns.ToString();


        bool IsLegal(Move givenMove)
        {
            var moves = moveGenerator.GenerateMoves(board);
            foreach (var legalMove in moves)
            {
                if (givenMove.Value == legalMove.Value)
                {
                    return true;
                }
            }

            return false;
        }

        public class BotMatchStats
        {
            public string BotName;
            public int NumWins;
            public int NumLosses;
            public int NumDraws;
            public int NumTimeouts;
            public int NumIllegalMoves;

            public BotMatchStats(string name) => BotName = name;
        }

        public void Release()
        {
            boardUI.Release();
        }
    }
}
