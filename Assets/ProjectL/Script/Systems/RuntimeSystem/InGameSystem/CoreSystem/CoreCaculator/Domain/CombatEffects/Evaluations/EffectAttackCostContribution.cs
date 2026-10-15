using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    internal readonly struct EffectAttackCostContribution
    {
        public EffectOriginKey Origin { get; }
        public int FlatReduction { get; }
        public int MinimumCost { get; }
        public bool ConsumeOnSuccessfulAttack { get; }

        public EffectAttackCostContribution(
            EffectOriginKey origin,
            int flatReduction,
            int minimumCost,
            bool consumeOnSuccessfulAttack)
        {
            if (origin.IsValid == false) throw new ArgumentException(nameof(origin));
            if (flatReduction < 0) throw new ArgumentOutOfRangeException(nameof(flatReduction));
            if (minimumCost < 1) throw new ArgumentOutOfRangeException(nameof(minimumCost));

            Origin = origin;
            FlatReduction = flatReduction;
            MinimumCost = minimumCost;
            ConsumeOnSuccessfulAttack = consumeOnSuccessfulAttack;
        }
    }
}
