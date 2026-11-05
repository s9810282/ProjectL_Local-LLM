using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class DustScreenTraitStatusSnapshot : TraitStatusSnapshot
    {
        public DustScreenTraitStatusSnapshot() : base(TraitKey.DustScreen)
        {
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is DustScreenTraitStatusSnapshot;
    }
}
