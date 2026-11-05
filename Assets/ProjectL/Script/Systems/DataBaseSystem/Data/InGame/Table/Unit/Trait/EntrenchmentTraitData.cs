using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class EntrenchmentTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.Entrenchment;
        public int RadiusCells { get; }
        public int DamageReductionPercent { get; }

        public EntrenchmentTraitData(int radiusCells, int damageReductionPercent)
        {
            RadiusCells = radiusCells;
            DamageReductionPercent = damageReductionPercent;
        }

        public override void Validate()
        {
            try
            {
                _ = new EntrenchmentTraitSpec(RadiusCells, DamageReductionPercent);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"Entrenchment.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
