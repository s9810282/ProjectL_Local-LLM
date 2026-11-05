using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class EntrenchmentTraitStatusSnapshot : TraitStatusSnapshot
    {
        public EntrenchmentTraitStatusSnapshot() : base(TraitKey.Entrenchment)
        {
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is EntrenchmentTraitStatusSnapshot;
    }
}
