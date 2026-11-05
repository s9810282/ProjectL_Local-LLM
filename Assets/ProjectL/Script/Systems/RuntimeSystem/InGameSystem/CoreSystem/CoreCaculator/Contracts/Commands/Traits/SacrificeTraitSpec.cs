using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class SacrificeTraitSpec : TraitSpec
    {
        public int RadiusCells { get; }
        public int MaxTriggersPerEnemyTurn { get; }

        public SacrificeTraitSpec(int radiusCells, int maxTriggersPerEnemyTurn) : base(TraitKey.Sacrifice)
        {
            if (radiusCells < 1)
                throw new ArgumentOutOfRangeException(nameof(radiusCells));
            if (maxTriggersPerEnemyTurn < 1)
                throw new ArgumentOutOfRangeException(nameof(maxTriggersPerEnemyTurn));
            RadiusCells = radiusCells;
            MaxTriggersPerEnemyTurn = maxTriggersPerEnemyTurn;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (SacrificeTraitSpec)other;
            return RadiusCells == otherSpec.RadiusCells &&
                MaxTriggersPerEnemyTurn == otherSpec.MaxTriggersPerEnemyTurn;
        }
    }
}
