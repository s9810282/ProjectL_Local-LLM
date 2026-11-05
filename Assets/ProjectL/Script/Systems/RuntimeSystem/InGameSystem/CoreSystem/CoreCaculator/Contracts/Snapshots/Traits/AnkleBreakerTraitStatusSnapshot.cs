using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class AnkleBreakerTraitStatusSnapshot : TraitStatusSnapshot
    {
        public AnkleBreakerTraitStatusSnapshot() : base(TraitKey.AnkleBreaker)
        {
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is AnkleBreakerTraitStatusSnapshot;
    }
}
