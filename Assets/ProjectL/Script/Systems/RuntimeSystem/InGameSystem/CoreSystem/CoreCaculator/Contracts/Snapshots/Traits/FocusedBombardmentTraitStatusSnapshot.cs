using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class FocusedBombardmentTraitStatusSnapshot : TraitStatusSnapshot
    {
        public FocusedBombardmentTraitStatusSnapshot() : base(TraitKey.FocusedBombardment)
        {
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is FocusedBombardmentTraitStatusSnapshot;
    }
}
