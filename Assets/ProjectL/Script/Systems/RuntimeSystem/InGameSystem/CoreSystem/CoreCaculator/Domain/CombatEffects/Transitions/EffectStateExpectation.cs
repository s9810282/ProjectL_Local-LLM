using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions
{
    /// <summary>
    /// 해당 Rule을 계산할 당시 Origin 상태
    /// </summary>
    internal readonly struct EffectStateExpectation
    {
        public EffectOriginKey Origin { get; }
        public IEffectStateSnapshot ExpectedState { get; }

        public EffectStateExpectation(EffectOriginKey origin, IEffectStateSnapshot expectedState)
        {
            if (origin.IsValid == false) throw new ArgumentException(nameof(origin));
            if (expectedState == null) throw new ArgumentNullException(nameof(expectedState));

            Origin = origin;
            ExpectedState = expectedState;
        }
    }
}
