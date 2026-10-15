using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    internal readonly struct EffectAttackDamageContribution
    {
        public EffectOriginKey Origin { get; }
        public int FlatBonus { get; }

        public EffectAttackDamageContribution(EffectOriginKey origin, int flatBonus)
        {
            if (origin.IsValid == false) throw new ArgumentException(nameof(origin));
            if (flatBonus < 0) throw new ArgumentOutOfRangeException(nameof(flatBonus));

            Origin = origin;
            FlatBonus = flatBonus;
        }
    }
}
