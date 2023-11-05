using System;

namespace ChessChallenge.Version8;

[Flags]
public enum EvaluationFlags : ulong
{
    UseMobility = 1,
    UseKingShield = 1 << 1,
    UsePawnBonuses = 1 << 2,
    UseKingTropism = 1 << 3,
    UseLowMaterialCutoffs = 1 << 4,
    UseEvalTT = 1 << 5
}