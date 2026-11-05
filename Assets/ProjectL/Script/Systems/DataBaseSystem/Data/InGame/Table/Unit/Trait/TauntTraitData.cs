using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class TauntTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.Taunt;
        public int RadiusCells { get; }

        public int DamageReductionPercent { get; }

        public TauntTraitData(int radiusCells) : this(radiusCells, 0) { }

        public TauntTraitData(int radiusCells, int damageReductionPercent)
        {
            DamageReductionPercent = damageReductionPercent;
            RadiusCells = radiusCells;
        }

        public override void Validate()
        {
            try
            {
                _ = new TauntTraitSpec(RadiusCells, DamageReductionPercent);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"Taunt.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
