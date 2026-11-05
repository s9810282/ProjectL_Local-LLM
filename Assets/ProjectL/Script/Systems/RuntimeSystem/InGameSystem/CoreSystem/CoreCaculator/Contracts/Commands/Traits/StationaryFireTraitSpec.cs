using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class StationaryFireTraitSpec : TraitSpec
    {
        public int ExtraDamagePercent { get; }

        public StationaryFireTraitSpec(int extraDamagePercent) : base(TraitKey.StationaryFire)
        {
            if (extraDamagePercent < 0)
                throw new ArgumentOutOfRangeException(nameof(extraDamagePercent));
            ExtraDamagePercent = extraDamagePercent;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (StationaryFireTraitSpec)other;
            return ExtraDamagePercent == otherSpec.ExtraDamagePercent;
        }
    }
}
