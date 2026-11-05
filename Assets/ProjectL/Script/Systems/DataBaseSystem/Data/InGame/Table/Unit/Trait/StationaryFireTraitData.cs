using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class StationaryFireTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.StationaryFire;
        public int ExtraDamagePercent { get; }

        public StationaryFireTraitData(int extraDamagePercent)
        {
            ExtraDamagePercent = extraDamagePercent;
        }

        public override void Validate()
        {
            try
            {
                _ = new StationaryFireTraitSpec(ExtraDamagePercent);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"StationaryFire.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
