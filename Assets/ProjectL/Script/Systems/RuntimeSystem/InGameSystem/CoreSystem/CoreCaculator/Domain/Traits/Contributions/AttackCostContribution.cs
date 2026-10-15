using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Contributions
{
    internal readonly struct AttackCostContribution
    {
        public TraitKey Source { get; }
        public int FlatReduction { get; }
        public int MinimumCost { get; }
        public bool ConsumeOnSuccessfulAttack { get; }

        public AttackCostContribution(
            TraitKey source,
            int flatReduction,
            int minimumCost,
            bool consumeOnSuccessfulAttack)
        {
            if (source == TraitKey.None || Enum.IsDefined(typeof(TraitKey), source) == false)
            {
                throw new ArgumentOutOfRangeException(nameof(source));
            }

            if (flatReduction < 0)
                throw new ArgumentOutOfRangeException(nameof(flatReduction));
            if (minimumCost < 1)
                throw new ArgumentOutOfRangeException(nameof(minimumCost));

            Source = source;
            FlatReduction = flatReduction;
            MinimumCost = minimumCost;
            ConsumeOnSuccessfulAttack = consumeOnSuccessfulAttack;
        }
    }
}
