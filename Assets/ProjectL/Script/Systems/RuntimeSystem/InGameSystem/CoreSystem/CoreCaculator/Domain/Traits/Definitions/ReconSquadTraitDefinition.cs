using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class ReconSquadTraitDefinition : TraitDefinition
    {
        public int IncreasePercent { get; }
        public int Radius { get; }

        public ReconSquadTraitDefinition(int increasePercent, int radius) : base(TraitKey.ReconSquad)
        {
            var spec = new ReconSquadTraitSpec(increasePercent, radius);
            IncreasePercent = spec.IncreasePercent;
            Radius = spec.Radius;
        }
    }
}
