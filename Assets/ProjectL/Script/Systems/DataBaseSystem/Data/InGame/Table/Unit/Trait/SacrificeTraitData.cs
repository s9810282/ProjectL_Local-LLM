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
        public bool PreventsLethalSacrificeDamage { get; }
        public int ShieldOnActivation { get; }

        public SacrificeTraitData(int radiusCells, int maxTriggersPerEnemyTurn,
            bool preventsLethalSacrificeDamage = false, int shieldOnActivation = 0)
        {
            RadiusCells = radiusCells;
            MaxTriggersPerEnemyTurn = maxTriggersPerEnemyTurn;
            PreventsLethalSacrificeDamage = preventsLethalSacrificeDamage;
            ShieldOnActivation = shieldOnActivation;
        }

        public override void Validate()
        {
            try
            {
                _ = new SacrificeTraitSpec(RadiusCells, MaxTriggersPerEnemyTurn, PreventsLethalSacrificeDamage, ShieldOnActivation);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"Sacrifice.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
