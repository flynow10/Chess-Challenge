using System;
using System.IO;
using System.Linq;
using ChessChallenge.API;
using ChessChallenge.Example;
using ChessChallenge.Testing.BotLoader;

namespace ChessChallenge.Application;

public record struct BotReference(PlayerType Type, int SkillLevel = 20, string CommitHash = "")
{
    public readonly TokenCount GetTokenCount()
    {
        
        return Type switch
        {
            PlayerType.MyBot => GetSrcDirTokenCount("My Bot", "MyBot.cs"),
            PlayerType.HashBot => BotLoader.CountHashBotTokens(CommitHash),
            PlayerType.Boychesser => GetSrcDirTokenCount("Boychesser", "MyBot.cs"),
            PlayerType.EvilBot =>  GetSrcDirTokenCount("EvilBot", "EvilBot.cs"),
            PlayerType.ExtraEvilBot =>   GetSrcDirTokenCount("ExtraEvilBot", "ExtraEvilBot.cs"),
            _ => new TokenCount(0,0)
        };
        
        TokenCount GetSrcDirTokenCount(params string[] srcPath)
        {
            string? srcDir = FileHelper.GetSrcDir();
            if (srcDir == null)
            {
                return new TokenCount(0, 0);
            }
            using StreamReader reader = new(Path.Combine(srcDir, Path.Combine(srcPath)));
            string txt = reader.ReadToEnd();
            return TokenCounter.CountTokens(txt);
        }
    }
    public readonly IChessBot CreateBot()
    {
        return Type switch
        {
            PlayerType.MyBot => new MyBot.MyBot(),
            PlayerType.HashBot => BotLoader.LoadHashBot(CommitHash),
            PlayerType.EvilBot => new EvilBot(),
            PlayerType.ExtraEvilBot => new ExtraEvilBot(),
            PlayerType.PawnBot => new PawnBot(),
            PlayerType.StockFish => new UCIBot.UCIBot("/opt/homebrew/bin/stockfish", false, SkillLevel),
            PlayerType.StockFishV3 => new UCIBot.UCIBot(Path.Combine(FileHelper.GetSrcDir()!, "StockfishV3",
                "stockfish")),
            PlayerType.Boychesser => new Boychesser.MyBot(),
            PlayerType.Comet => new UCIBot.UCIBot(Path.Combine(FileHelper.GetSrcDir()!, "Comet", "chess_bot")),
            PlayerType.MyBotV1 => new Version1.MyBot(),
            PlayerType.MyBotV2 => new Version2.MyBot(),
            PlayerType.MyBotV3 => new Version3.MyBot(),
            PlayerType.MyBotV4 => new Version4.MyBot(),
            PlayerType.MyBotV5 => new Version5.MyBot(),
            PlayerType.MyBotV6 => new Version6.MyBot(),
            PlayerType.MyBotV7 => new Version7.MyBot(),
            PlayerType.MyBotV8 => new Version8.MyBot(SkillLevel),
            PlayerType.MyBotV9 => new Version9.MyBot(SkillLevel),
            PlayerType.MyBotV10 => new Version10.MyBot(SkillLevel),
            _ => throw new ArgumentOutOfRangeException(nameof(Type), Type, null)
        };
    }

    public override string ToString()
    {
        if (Type == PlayerType.HashBot)
        {
            return $"{Type}({CommitHash.Substring(0, 7)})";
        }

        if (ChallengeController.UsesSkillLevel.Contains(Type))
        {
            return $"{Type}({SkillLevel})";
        }
        return $"{Type}";
    }
}