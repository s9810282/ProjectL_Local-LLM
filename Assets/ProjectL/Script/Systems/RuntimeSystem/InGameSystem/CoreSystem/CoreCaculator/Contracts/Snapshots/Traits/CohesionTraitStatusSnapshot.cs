using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class CohesionTraitStatusSnapshot : TraitStatusSnapshot
    {
        public CohesionTraitStatusSnapshot() : base(TraitKey.Cohesion)
        {
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is CohesionTraitStatusSnapshot;
    }
}
