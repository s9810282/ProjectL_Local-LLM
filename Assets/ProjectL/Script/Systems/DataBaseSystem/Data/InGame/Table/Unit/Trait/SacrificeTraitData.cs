using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class SacrificeTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.Sacrifice;
        public int RadiusCells { get; }
        public int MaxTriggersPerEnemyTurn { get; }

        public SacrificeTraitData(int radiusCells, int maxTriggersPerEnemyTurn)
        {
            RadiusCells = radiusCells;
            MaxTriggersPerEnemyTurn = maxTriggersPerEnemyTurn;
        }

        public override void Validate()
        {
            try
            {
                _ = new SacrificeTraitSpec(RadiusCells, MaxTriggersPerEnemyTurn);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"Sacrifice.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
