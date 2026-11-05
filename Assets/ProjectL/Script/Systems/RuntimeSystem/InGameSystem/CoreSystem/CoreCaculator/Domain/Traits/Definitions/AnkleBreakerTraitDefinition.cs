using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class AnkleBreakerTraitDefinition : TraitDefinition
    {
        public int DurationAffectedSideTurnEnds { get; }

        public AnkleBreakerTraitDefinition(int durationAffectedSideTurnEnds) : base(TraitKey.AnkleBreaker)
        {
            var spec = new AnkleBreakerTraitSpec(durationAffectedSideTurnEnds);
            DurationAffectedSideTurnEnds = spec.DurationAffectedSideTurnEnds;
        }
    }
}
