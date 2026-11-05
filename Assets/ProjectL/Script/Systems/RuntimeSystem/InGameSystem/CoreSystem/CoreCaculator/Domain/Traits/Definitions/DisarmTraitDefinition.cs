using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class DisarmTraitDefinition : TraitDefinition
    {
        public int DurationAffectedSideTurnEnds { get; }

        public DisarmTraitDefinition(int durationAffectedSideTurnEnds) : base(TraitKey.Disarm)
        {
            var spec = new DisarmTraitSpec(durationAffectedSideTurnEnds);
            DurationAffectedSideTurnEnds = spec.DurationAffectedSideTurnEnds;
        }
    }
}
