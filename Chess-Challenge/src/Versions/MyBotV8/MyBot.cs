using System;
using ChessChallenge.API;

namespace ChessChallenge.Version8;

public class MyBot : IChessBot
{
    private bool Debug = false;
    private readonly Search _search;

    public MyBot(int skillLevel = 20)
    {
        SearchFlags searchFlags = SearchFlags.UseAlphaBetaPruning | SearchFlags.UseIterativeDeepening |
                                  SearchFlags.UseQuiescenceSearch | SearchFlags.UseMoveOrdering |
                                  SearchFlags.UseTranspositionTable | SearchFlags.UseKillerMoveOrdering;
        EvaluationFlags evaluationFlags = EvaluationFlags.UseMobility | EvaluationFlags.UseKingShield |
                                          EvaluationFlags.UsePawnBonuses | EvaluationFlags.UseKingTropism |
                                          EvaluationFlags.UseLowMaterialCutoffs | EvaluationFlags.UseEvalTT;
        Evaluate evaluate = new Evaluate(evaluationFlags);
        _search = new Search(searchFlags, evaluate, TimingFunctions.ThirtiethOfTimeLeft, skillLevel, Debug);
        if (Debug)
            Console.WriteLine(
                $"Search Flags: {searchFlags.ToString()}\nEvaluation Flags: {evaluationFlags.ToString()}");
    }

    public Move Think(Board board, Timer timer)
    {
        return _search.DepthFirstSearch(board, timer);
    }
}