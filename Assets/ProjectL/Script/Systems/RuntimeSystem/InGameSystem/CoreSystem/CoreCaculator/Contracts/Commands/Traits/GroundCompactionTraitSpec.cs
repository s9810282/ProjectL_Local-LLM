using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class GroundCompactionTraitSpec : TraitSpec
    {
        public int MovementRangeBonus { get; }
        public int OpposingTurnEndsToExpire { get; }

        public GroundCompactionTraitSpec(int movementRangeBonus, int opposingTurnEndsToExpire) : base(TraitKey.GroundCompaction)
        {
            if (movementRangeBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(movementRangeBonus));
            if (opposingTurnEndsToExpire < 1)
                throw new ArgumentOutOfRangeException(nameof(opposingTurnEndsToExpire));
            MovementRangeBonus = movementRangeBonus;
            OpposingTurnEndsToExpire = opposingTurnEndsToExpire;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (GroundCompactionTraitSpec)other;
            return MovementRangeBonus == otherSpec.MovementRangeBonus &&
                OpposingTurnEndsToExpire == otherSpec.OpposingTurnEndsToExpire;
        }
    }
}
