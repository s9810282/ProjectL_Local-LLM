using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    internal readonly struct EffectKillChainAttackContribution
    {
        public EffectOriginKey Origin { get; }

        public EffectKillChainAttackContribution(EffectOriginKey origin)
        {
            if (origin.IsValid == false)
                throw new ArgumentException(nameof(origin));

            Origin = origin;
        }
    }
}
