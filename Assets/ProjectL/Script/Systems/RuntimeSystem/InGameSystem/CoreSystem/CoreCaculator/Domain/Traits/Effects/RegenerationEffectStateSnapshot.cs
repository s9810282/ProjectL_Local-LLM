using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    /// <summary>재생 생산자의 주기 진행값을 불변으로 전달합니다.</summary>
    internal sealed class RegenerationEffectStateSnapshot : IEffectStateSnapshot
    {
        public int CompletedEligibleTurnEnds { get; }

        internal RegenerationEffectStateSnapshot(int completedEligibleTurnEnds = 0)
        {
            if (completedEligibleTurnEnds < 0) throw new ArgumentOutOfRangeException(nameof(completedEligibleTurnEnds));
            CompletedEligibleTurnEnds = completedEligibleTurnEnds;
        }

        public bool ValueEquals(IEffectStateSnapshot other) =>
            other is RegenerationEffectStateSnapshot regeneration && CompletedEligibleTurnEnds == regeneration.CompletedEligibleTurnEnds;
    }
}
