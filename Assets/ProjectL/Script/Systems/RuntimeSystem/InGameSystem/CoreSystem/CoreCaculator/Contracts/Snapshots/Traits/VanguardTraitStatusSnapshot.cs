using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class VanguardTraitStatusSnapshot : TraitStatusSnapshot
    {
        public VanguardTraitStatusSnapshot() : base(TraitKey.Vanguard)
        {
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is VanguardTraitStatusSnapshot;
    }
}
