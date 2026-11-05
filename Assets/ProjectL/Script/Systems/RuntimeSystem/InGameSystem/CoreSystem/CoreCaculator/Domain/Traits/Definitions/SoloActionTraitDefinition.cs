using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class SoloActionTraitDefinition : TraitDefinition
    {
        public int ShieldAmount { get; }
        public int RadiusCells { get; }
        public int RequiredOccupiedNeighborCount { get; }
        public int ResetEveryControllerTurnEnds { get; }

        public SoloActionTraitDefinition(int shieldAmount, int radiusCells, int requiredOccupiedNeighborCount, int resetEveryControllerTurnEnds) : base(TraitKey.SoloAction)
        {
            var spec = new SoloActionTraitSpec(shieldAmount, radiusCells, requiredOccupiedNeighborCount, resetEveryControllerTurnEnds);
            ShieldAmount = spec.ShieldAmount;
            RadiusCells = spec.RadiusCells;
            RequiredOccupiedNeighborCount = spec.RequiredOccupiedNeighborCount;
            ResetEveryControllerTurnEnds = spec.ResetEveryControllerTurnEnds;
        }
    }
}
