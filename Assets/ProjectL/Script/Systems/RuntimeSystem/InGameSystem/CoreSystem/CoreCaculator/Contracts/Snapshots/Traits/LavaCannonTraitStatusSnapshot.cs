using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class LavaCannonTraitStatusSnapshot : TraitStatusSnapshot
    {
        public LavaCannonTraitStatusSnapshot() : base(TraitKey.LavaCannon)
        {
        }

        public override bool SameValues(TraitStatusSnapshot other) => other is LavaCannonTraitStatusSnapshot;
    }
}
