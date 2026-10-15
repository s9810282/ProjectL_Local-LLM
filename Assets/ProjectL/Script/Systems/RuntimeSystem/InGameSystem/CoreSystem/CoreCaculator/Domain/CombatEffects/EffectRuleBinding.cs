using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects
{
    /// <summary>
    /// 실행할 Rule을 준비
    /// </summary>
    internal sealed class EffectRuleBinding
    {
        public EffectOriginKey Origin { get; }
        public ICombatEffectRule Rule { get; }
        public IEffectStateSnapshot ExpectedState { get; }

        public EffectRuleBinding(EffectOriginKey origin, ICombatEffectRule rule, IEffectStateSnapshot expectedState)
        {
            if (origin.IsValid == false) throw new ArgumentException(nameof(origin));
            Origin = origin;
            Rule = rule ?? throw new ArgumentNullException(nameof(rule));
            ExpectedState = expectedState ?? throw new ArgumentNullException(nameof(expectedState));
        }
    }
}
