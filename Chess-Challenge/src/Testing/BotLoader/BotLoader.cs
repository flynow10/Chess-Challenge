using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChessChallenge.API;
using ChessChallenge.Application;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;

namespace ChessChallenge.Testing.BotLoader;

public static class BotLoader
{
    public static IChessBot LoadHashBot(string hash)
    {
        return LoadBot(GitLoader.LoadMyBotFromHash(hash));
    }

    public static TokenCount CountHashBotTokens(string hash)
    {
        return TokenCounter.CountTokens(GitLoader.LoadMyBotFromHash(hash));
    }
    
    public static IChessBot LoadBot(string rawBotCode)
    {
        string formattedCode = StripNamespace(rawBotCode) + "\nreturn new MyBot();";
        ScriptOptions options = ScriptOptions.Default.WithReferences(typeof(IChessBot).Assembly);
        Script compiled = CSharpScript.Create(formattedCode, options);
        
        compiled.Compile();
        Task<ScriptState> execute = compiled.RunAsync();
        execute.Wait();
        ScriptState result = execute.Result;
        if (result == null)
        {
            throw new Exception("Bot load failed!", execute.Exception);
        }
        
        if (result.Exception != null)
        {
            throw new Exception(result.Exception.Message);
        }

        IChessBot bot = (IChessBot)result.ReturnValue;
        return bot;
    }
    
    private static string StripNamespace(string code)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        SyntaxNode root = tree.GetRoot();
        var newRoot = SyntaxFactory.CompilationUnit();
        WalkSyntaxNode(ref newRoot, root.ChildNodes());
        return newRoot.ToFullString();
    }

    private static void WalkSyntaxNode(ref CompilationUnitSyntax root, IEnumerable<SyntaxNode> syntaxList)
    {
        foreach (SyntaxNode node in syntaxList)
        {
            if (node.IsKind(SyntaxKind.FileScopedNamespaceDeclaration))
            {
                FileScopedNamespaceDeclarationSyntax ns = (node as FileScopedNamespaceDeclarationSyntax)!;
                WalkSyntaxNode(ref root, ns.Usings);
                WalkSyntaxNode(ref root, ns.Members);
            } else if (node.IsKind(SyntaxKind.NamespaceDeclaration))
            {
                NamespaceDeclarationSyntax ns = (node as NamespaceDeclarationSyntax)!;
                WalkSyntaxNode(ref root, ns.Members);
            }
            else if(node.IsKind(SyntaxKind.UsingDirective))
            {
                root = root.AddUsings((node as UsingDirectiveSyntax)!);
            }
            else if(node.IsKind(SyntaxKind.ClassDeclaration))
            {
                root = root.AddMembers((node as ClassDeclarationSyntax)!);
            }
        }
    }
}