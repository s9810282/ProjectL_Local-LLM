using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class CallOfBloodTraitStatusSnapshot : TraitStatusSnapshot
    {
        public CallOfBloodTraitStatusSnapshot() : base(TraitKey.CallOfBlood)
        {
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is CallOfBloodTraitStatusSnapshot;
    }
}
