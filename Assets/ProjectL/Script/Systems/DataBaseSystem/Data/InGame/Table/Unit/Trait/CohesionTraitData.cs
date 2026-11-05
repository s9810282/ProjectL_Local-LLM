using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class CohesionTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.Cohesion;
        public int DefensePerAdjacentAlly { get; }
        public int ShieldAmountPerAlly { get; }
        public int RadiusCells { get; }
        public int ResetEveryControllerTurnEnds { get; }

        public CohesionTraitData(int defensePerAdjacentAlly, int shieldAmountPerAlly, int radiusCells, int resetEveryControllerTurnEnds)
        {
            DefensePerAdjacentAlly = defensePerAdjacentAlly;
            ShieldAmountPerAlly = shieldAmountPerAlly;
            RadiusCells = radiusCells;
            ResetEveryControllerTurnEnds = resetEveryControllerTurnEnds;
        }

        public override void Validate()
        {
            try
            {
                _ = new CohesionTraitSpec(DefensePerAdjacentAlly, ShieldAmountPerAlly, RadiusCells, ResetEveryControllerTurnEnds);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"Cohesion.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
