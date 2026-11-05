using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class SteadfastWillTraitStatusSnapshot : TraitStatusSnapshot
    {
        public SteadfastWillTraitStatusSnapshot() : base(TraitKey.SteadfastWill)
        {
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is SteadfastWillTraitStatusSnapshot;
    }
}
