using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class AccelerationTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.Acceleration;
        public int AttackPerMovedCell { get; }

        public AccelerationTraitData(int attackPerMovedCell)
        {
            AttackPerMovedCell = attackPerMovedCell;
        }

        public override void Validate()
        {
            try
            {
                _ = new AccelerationTraitSpec(AttackPerMovedCell);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"Acceleration.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
