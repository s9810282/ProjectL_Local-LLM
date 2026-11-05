using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class DisarmTraitStatusSnapshot : TraitStatusSnapshot
    {
        public DisarmTraitStatusSnapshot() : base(TraitKey.Disarm)
        {
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is DisarmTraitStatusSnapshot;
    }
}
