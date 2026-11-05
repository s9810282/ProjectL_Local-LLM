using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class TauntTraitSpec : TraitSpec
    {
        public int RadiusCells { get; }

        public int DamageReductionPercent { get; }

        public TauntTraitSpec(int radiusCells) : this(radiusCells, 0) { }

        public TauntTraitSpec(int radiusCells, int damageReductionPercent) : base(TraitKey.Taunt)
        {
            if (radiusCells < 1)
                throw new ArgumentOutOfRangeException(nameof(radiusCells));
            if (damageReductionPercent < 0 || damageReductionPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(damageReductionPercent));
            DamageReductionPercent = damageReductionPercent;
            RadiusCells = radiusCells;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (TauntTraitSpec)other;
            return RadiusCells == otherSpec.RadiusCells &&
                DamageReductionPercent == otherSpec.DamageReductionPercent;
        }
    }
}
