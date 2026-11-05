using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class SoloActionTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.SoloAction;
        public int ShieldAmount { get; }
        public int RadiusCells { get; }
        public int RequiredOccupiedNeighborCount { get; }
        public int ResetEveryControllerTurnEnds { get; }

        public SoloActionTraitData(int shieldAmount, int radiusCells, int requiredOccupiedNeighborCount, int resetEveryControllerTurnEnds)
        {
            ShieldAmount = shieldAmount;
            RadiusCells = radiusCells;
            RequiredOccupiedNeighborCount = requiredOccupiedNeighborCount;
            ResetEveryControllerTurnEnds = resetEveryControllerTurnEnds;
        }

        public override void Validate()
        {
            try
            {
                _ = new SoloActionTraitSpec(ShieldAmount, RadiusCells, RequiredOccupiedNeighborCount, ResetEveryControllerTurnEnds);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"SoloAction.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
