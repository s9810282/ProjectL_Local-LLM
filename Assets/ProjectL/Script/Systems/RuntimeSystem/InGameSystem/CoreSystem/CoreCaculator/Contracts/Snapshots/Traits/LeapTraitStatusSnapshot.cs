using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class LeapTraitStatusSnapshot : TraitStatusSnapshot
    {
        public LeapTraitStatusSnapshot() : base(TraitKey.Leap)
        {
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is LeapTraitStatusSnapshot;
    }
}
