using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class SoloActionTraitStatusSnapshot : TraitStatusSnapshot
    {
        public SoloActionTraitStatusSnapshot() : base(TraitKey.SoloAction)
        {
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is SoloActionTraitStatusSnapshot;
    }
}
