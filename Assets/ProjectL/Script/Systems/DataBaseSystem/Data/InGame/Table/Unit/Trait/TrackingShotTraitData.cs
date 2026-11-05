using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class TrackingShotTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.TrackingShot;
        public int TargetHealthThresholdPercent { get; }
        public int ExtraDamagePercent { get; }

        public TrackingShotTraitData(int targetHealthThresholdPercent, int extraDamagePercent)
        {
            TargetHealthThresholdPercent = targetHealthThresholdPercent;
            ExtraDamagePercent = extraDamagePercent;
        }

        public override void Validate()
        {
            try
            {
                _ = new TrackingShotTraitSpec(TargetHealthThresholdPercent, ExtraDamagePercent);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"TrackingShot.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
