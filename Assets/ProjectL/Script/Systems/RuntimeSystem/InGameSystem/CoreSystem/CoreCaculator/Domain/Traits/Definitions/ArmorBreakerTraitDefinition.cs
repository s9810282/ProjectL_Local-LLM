using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class ArmorBreakerTraitDefinition : TraitDefinition
    {
        public int DefenseReductionPercentPerHit { get; }

        public ArmorBreakerTraitDefinition(int defenseReductionPercentPerHit) : base(TraitKey.ArmorBreaker)
        {
            var spec = new ArmorBreakerTraitSpec(defenseReductionPercentPerHit);
            DefenseReductionPercentPerHit = spec.DefenseReductionPercentPerHit;
        }
    }
}
