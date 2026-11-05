using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class LeapTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.Leap;

        public LeapTraitData()
        {
        }

        public override void Validate()
        {
            try
            {
                _ = new LeapTraitSpec();
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"Leap.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
