using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class CleanSweepTraitStatusSnapshot : TraitStatusSnapshot
    {
        public CleanSweepTraitStatusSnapshot() : base(TraitKey.CleanSweep)
        {
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is CleanSweepTraitStatusSnapshot;
    }
}
