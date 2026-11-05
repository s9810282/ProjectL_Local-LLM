using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class CohesionTraitSpec : TraitSpec
    {
        public int DefensePerAdjacentAlly { get; }
        public int ShieldAmountPerAlly { get; }
        public int RadiusCells { get; }
        public int ResetEveryControllerTurnEnds { get; }

        public CohesionTraitSpec(int defensePerAdjacentAlly, int shieldAmountPerAlly, int radiusCells, int resetEveryControllerTurnEnds) : base(TraitKey.Cohesion)
        {
            if (defensePerAdjacentAlly < 0)
                throw new ArgumentOutOfRangeException(nameof(defensePerAdjacentAlly));
            if (shieldAmountPerAlly < 0)
                throw new ArgumentOutOfRangeException(nameof(shieldAmountPerAlly));
            if (radiusCells < 1)
                throw new ArgumentOutOfRangeException(nameof(radiusCells));
            if (resetEveryControllerTurnEnds < 1)
                throw new ArgumentOutOfRangeException(nameof(resetEveryControllerTurnEnds));
            DefensePerAdjacentAlly = defensePerAdjacentAlly;
            ShieldAmountPerAlly = shieldAmountPerAlly;
            RadiusCells = radiusCells;
            ResetEveryControllerTurnEnds = resetEveryControllerTurnEnds;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (CohesionTraitSpec)other;
            return DefensePerAdjacentAlly == otherSpec.DefensePerAdjacentAlly &&
                ShieldAmountPerAlly == otherSpec.ShieldAmountPerAlly &&
                RadiusCells == otherSpec.RadiusCells &&
                ResetEveryControllerTurnEnds == otherSpec.ResetEveryControllerTurnEnds;
        }
    }
}
