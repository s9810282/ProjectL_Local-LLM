using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class BleedingTraitStatusSnapshot : TraitStatusSnapshot
    {
        public BleedingTraitStatusSnapshot() : base(TraitKey.Bleeding)
        {
        }

        public override bool SameValues(TraitStatusSnapshot other) => other is BleedingTraitStatusSnapshot;
    }
}
