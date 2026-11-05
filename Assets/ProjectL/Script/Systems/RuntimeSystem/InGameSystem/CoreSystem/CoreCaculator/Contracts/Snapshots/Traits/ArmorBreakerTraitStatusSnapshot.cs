using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class ArmorBreakerTraitStatusSnapshot : TraitStatusSnapshot
    {
        public ArmorBreakerTraitStatusSnapshot() : base(TraitKey.ArmorBreaker)
        {
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is ArmorBreakerTraitStatusSnapshot;
    }
}
