using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class LeapTraitDefinition : TraitDefinition
    {
        public LeapTraitDefinition() : base(TraitKey.Leap)
        {
            var spec = new LeapTraitSpec();
        }
    }
}
