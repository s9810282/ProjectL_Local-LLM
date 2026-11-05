using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class GroundCompactionTraitStatusSnapshot : TraitStatusSnapshot
    {
        public GroundCompactionTraitStatusSnapshot() : base(TraitKey.GroundCompaction)
        {
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is GroundCompactionTraitStatusSnapshot;
    }
}
