using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class AccelerationTraitSpec : TraitSpec
    {
        public int AttackPerMovedCell { get; }

        public AccelerationTraitSpec(int attackPerMovedCell) : base(TraitKey.Acceleration)
        {
            if (attackPerMovedCell < 0)
                throw new ArgumentOutOfRangeException(nameof(attackPerMovedCell));
            AttackPerMovedCell = attackPerMovedCell;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (AccelerationTraitSpec)other;
            return AttackPerMovedCell == otherSpec.AttackPerMovedCell;
        }
    }
}
