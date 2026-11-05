using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class FinishingShotTraitStatusSnapshot : TraitStatusSnapshot
    {
        public FinishingShotTraitStatusSnapshot() : base(TraitKey.FinishingShot)
        {
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is FinishingShotTraitStatusSnapshot;
    }
}
