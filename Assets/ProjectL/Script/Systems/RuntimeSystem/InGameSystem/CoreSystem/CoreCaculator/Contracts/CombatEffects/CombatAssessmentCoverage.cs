using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects
{
    /// <summary>관측 가능한 평가에서 아직 확정하지 못한 결과의 이유다. 뒤 단계가 앞 단계의 누락을 지우지 않는다.</summary>
    [Flags]
    public enum CombatAssessmentGap
    {
        None = 0,
        HiddenEnvironment = 1,
        RandomOutcome = 2,
        ActualOnlyReaction = 4,
        UnsupportedEffect = 8,
        FuturePlayerChoice = 16,
        FutureWarning = 32,
        UnknownPosition = 64
    }
}
