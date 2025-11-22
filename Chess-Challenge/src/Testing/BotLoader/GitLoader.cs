using System;
using System.IO;
using System.Linq;
using LibGit2Sharp;

namespace ChessChallenge.Testing.BotLoader;

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
    public static string LoadHash(string hash)
    {
        using var repo = new Repository(GetRepositoryPath());
        Commit commit = repo.Commits.First(c => c.Sha == hash);
        if (commit == null)
        {
            throw new Exception($"Couldn't find commit with hash {hash}");
        }
        var file = commit["Chess-Challenge/src/My Bot/MyBot.cs"];
        var blob = file.Target as Blob;
        using var stream = new StreamReader(blob.GetContentStream());
        return stream.ReadToEnd();
    }
}