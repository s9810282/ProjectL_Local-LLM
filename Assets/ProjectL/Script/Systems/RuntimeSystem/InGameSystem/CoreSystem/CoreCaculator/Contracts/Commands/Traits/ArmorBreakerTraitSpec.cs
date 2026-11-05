using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class ArmorBreakerTraitSpec : TraitSpec
    {
        public int DefenseReductionPercentPerHit { get; }

        public ArmorBreakerTraitSpec(int defenseReductionPercentPerHit) : base(TraitKey.ArmorBreaker)
        {
            if (defenseReductionPercentPerHit < 0 || defenseReductionPercentPerHit > 100)
                throw new ArgumentOutOfRangeException(nameof(defenseReductionPercentPerHit));
            DefenseReductionPercentPerHit = defenseReductionPercentPerHit;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (ArmorBreakerTraitSpec)other;
            return DefenseReductionPercentPerHit == otherSpec.DefenseReductionPercentPerHit;
        }
    }
}
