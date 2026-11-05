using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class MoraleBoostTraitStatusSnapshot : TraitStatusSnapshot
    {
        public MoraleBoostTraitStatusSnapshot() : base(TraitKey.MoraleBoost)
        {
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is MoraleBoostTraitStatusSnapshot;
    }
}
