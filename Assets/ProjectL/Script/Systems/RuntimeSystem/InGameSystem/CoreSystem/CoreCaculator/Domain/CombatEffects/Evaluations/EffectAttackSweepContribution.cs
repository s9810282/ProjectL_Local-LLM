using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    internal readonly struct EffectAttackSweepContribution
    {
        public EffectOriginKey Origin { get; }
        public int DamagePercent { get; }

        public EffectAttackSweepContribution(EffectOriginKey origin, int damagePercent)
        {
            if (!origin.IsValid) throw new ArgumentException(nameof(origin));
            if (damagePercent < 0) throw new ArgumentOutOfRangeException(nameof(damagePercent));
            Origin = origin;
            DamagePercent = damagePercent;
        }
    }
}
