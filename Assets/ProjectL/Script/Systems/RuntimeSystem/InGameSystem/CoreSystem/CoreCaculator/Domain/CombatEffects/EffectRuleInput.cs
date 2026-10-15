using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects
{
    /// <summary>
    /// Rule을 호출
    /// </summary>
    internal readonly struct EffectRuleInput
    {
        public EffectOriginKey Origin { get; }
        public IEffectStateSnapshot State { get; }
        public EffectRuleInput(EffectOriginKey origin, IEffectStateSnapshot state)
        {
            if (origin.IsValid == false) throw new ArgumentException(nameof(origin));
            if (state == null) throw new ArgumentNullException(nameof(state));

            Origin = origin;
            State = state;
        }
    }
}
