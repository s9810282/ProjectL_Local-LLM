using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class StationaryFireTraitDefinition : TraitDefinition
    {
        public int ExtraDamagePercent { get; }

        public StationaryFireTraitDefinition(int extraDamagePercent) : base(TraitKey.StationaryFire)
        {
            var spec = new StationaryFireTraitSpec(extraDamagePercent);
            ExtraDamagePercent = spec.ExtraDamagePercent;
        }
    }
}
