using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class EntrenchmentTraitSpec : TraitSpec
    {
        public int RadiusCells { get; }
        public int DamageReductionPercent { get; }

        public EntrenchmentTraitSpec(int radiusCells, int damageReductionPercent) : base(TraitKey.Entrenchment)
        {
            if (radiusCells < 1)
                throw new ArgumentOutOfRangeException(nameof(radiusCells));
            if (damageReductionPercent < 0 || damageReductionPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(damageReductionPercent));
            RadiusCells = radiusCells;
            DamageReductionPercent = damageReductionPercent;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (EntrenchmentTraitSpec)other;
            return RadiusCells == otherSpec.RadiusCells &&
                DamageReductionPercent == otherSpec.DamageReductionPercent;
        }
    }
}
