using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class IntimidationTraitStatusSnapshot : TraitStatusSnapshot
    {
        public int AttackReductionPercent { get; }

        public IntimidationTraitStatusSnapshot(int attackReductionPercent)
            : base(TraitKey.Intimidation)
        {
            if (attackReductionPercent < 1 || attackReductionPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(attackReductionPercent));

            AttackReductionPercent = attackReductionPercent;
        }
        public override bool SameValues(TraitStatusSnapshot other) =>
            other is IntimidationTraitStatusSnapshot intimidation &&
            AttackReductionPercent == intimidation.AttackReductionPercent;
    }
}
