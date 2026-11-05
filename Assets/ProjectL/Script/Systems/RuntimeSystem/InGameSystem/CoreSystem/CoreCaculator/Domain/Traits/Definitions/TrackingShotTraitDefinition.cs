using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class TrackingShotTraitDefinition : TraitDefinition
    {
        public int TargetHealthThresholdPercent { get; }
        public int ExtraDamagePercent { get; }

        public TrackingShotTraitDefinition(int targetHealthThresholdPercent, int extraDamagePercent) : base(TraitKey.TrackingShot)
        {
            var spec = new TrackingShotTraitSpec(targetHealthThresholdPercent, extraDamagePercent);
            TargetHealthThresholdPercent = spec.TargetHealthThresholdPercent;
            ExtraDamagePercent = spec.ExtraDamagePercent;
        }
    }
}
