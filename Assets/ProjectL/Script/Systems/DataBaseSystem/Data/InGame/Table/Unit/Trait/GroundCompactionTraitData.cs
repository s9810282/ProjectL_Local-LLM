using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class GroundCompactionTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.GroundCompaction;
        public int MovementRangeBonus { get; }
        public int OpposingTurnEndsToExpire { get; }

        public GroundCompactionTraitData(int movementRangeBonus, int opposingTurnEndsToExpire)
        {
            MovementRangeBonus = movementRangeBonus;
            OpposingTurnEndsToExpire = opposingTurnEndsToExpire;
        }

        public override void Validate()
        {
            try
            {
                _ = new GroundCompactionTraitSpec(MovementRangeBonus, OpposingTurnEndsToExpire);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"GroundCompaction.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
