using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class SteadfastWillTraitDefinition : TraitDefinition
    {
        public SteadfastWillTraitDefinition() : base(TraitKey.SteadfastWill)
        {
            var spec = new SteadfastWillTraitSpec();
        }
    }
}
