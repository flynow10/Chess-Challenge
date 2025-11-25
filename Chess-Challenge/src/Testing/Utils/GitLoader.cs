using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using Spectre.Console;

namespace ChessChallenge.Testing.Utils;

public static class GitLoader
{
    private static string GetRepositoryPath()
    {
        string? root = Directory.GetParent(Environment.CurrentDirectory).FullName;
        if (root == null)
        {
            throw new Exception("Couldn't find root directory");
        }
        return Path.Combine(root, ".git");
    }
    public static string LoadMyBotFromHash(string hash)
    {
        using var repo = new Repository(GetRepositoryPath());
        Commit commit = repo.Lookup<Commit>(hash);
        if (commit == null)
        {
            throw new Exception($"Couldn't find commit with hash {hash}");
        }
        var file = commit["Chess-Challenge/src/My Bot/MyBot.cs"];
        var blob = file.Target as Blob;
        using var stream = new StreamReader(blob.GetContentStream());
        return stream.ReadToEnd();
    }

    public static List<string> GetRecentCommits()
    {
        using var repo = new Repository(GetRepositoryPath());
        return repo.Head.Commits.Take(10).Select(c => c.Sha).ToList();
    }

    public static string GetMainCommit()
    {
        using var repo = new Repository(GetRepositoryPath());
        return repo.Branches["main"].Tip.Sha;
    }

    public static string GetMainMyBot()
    {
        using var repo = new Repository(GetRepositoryPath());
        Branch branch = repo.Branches["main"];
        return LoadMyBotFromHash(branch.Tip.Sha);
    }

    public static string GetCommitMessage(string hash, bool shortMessage = false)
    {
        using var repo = new Repository(GetRepositoryPath());
        Commit commit = repo.Lookup<Commit>(hash);
        if (commit == null)
        {
            throw new Exception($"Couldn't find commit with hash {hash}");
        }
        if(shortMessage)
            return commit.MessageShort;
        return commit.Message;
    }
    
    public static string AskCommitHash()
    {
        List<string> commits = GitLoader.GetRecentCommits();
        string mainHash = GitLoader.GetMainCommit();
        
        commits.Insert(0, "main");

        string hash = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Which commit would you like to test against?")
            .AddChoices(commits)
            .UseConverter(hash =>
            {
                if (hash == "main")
                {
                    return "main - " + GitLoader.GetCommitMessage(mainHash, true);
                }
                
                return hash.Substring(0, 7) + " - " + GitLoader.GetCommitMessage(hash, true);
            })
        );

        if (hash == "main")
        {
            return mainHash;
        }

        return hash;
    }
}