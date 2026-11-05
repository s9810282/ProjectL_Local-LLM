using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class TauntTraitStatusSnapshot : TraitStatusSnapshot
    {
        public TauntTraitStatusSnapshot() : base(TraitKey.Taunt)
        {
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is TauntTraitStatusSnapshot;
    }
}
