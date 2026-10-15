using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions
{
    /// <summary>
    /// 실제 상태 변경 계획
    /// </summary>
    internal readonly struct EffectStateChange
    {
        public EffectOriginKey Origin { get; }
        public IEffectStateSnapshot ExpectedState { get; }
        public IEffectStateSnapshot FinalState { get; }

        public EffectStateChange(EffectOriginKey origin, IEffectStateSnapshot expectedState, IEffectStateSnapshot finalState)
        {
            if (origin.IsValid == false) throw new ArgumentException(nameof(origin));
            if (expectedState == null) throw new ArgumentNullException(nameof(expectedState));
            if (finalState == null) throw new ArgumentNullException(nameof(finalState));

            Origin = origin;
            ExpectedState = expectedState;
            FinalState = finalState;
        }
    }
}
