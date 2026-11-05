using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class TrackingShotTraitStatusSnapshot : TraitStatusSnapshot
    {
        public TrackingShotTraitStatusSnapshot() : base(TraitKey.TrackingShot)
        {
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is TrackingShotTraitStatusSnapshot;
    }
}
