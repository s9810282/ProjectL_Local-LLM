using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class PositionSwapTraitStatusSnapshot : TraitStatusSnapshot
    {
        public PositionSwapTraitStatusSnapshot() : base(TraitKey.PositionSwap)
        {
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is PositionSwapTraitStatusSnapshot;
    }
}
