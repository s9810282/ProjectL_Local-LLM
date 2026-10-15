using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    /// <summary>공격 대상 정책에 대한 규칙 하나의 기여와 출처다.</summary>
    internal readonly struct EffectAttackTargetingContribution
    {
        public EffectOriginKey Origin { get; }
        public AttackTargetingPolicy Policy { get; }

        public EffectAttackTargetingContribution(EffectOriginKey origin, AttackTargetingPolicy policy)
        {
            if (!origin.IsValid) throw new ArgumentException(nameof(origin));
            Origin = origin;
            Policy = policy;
        }
    }
}
