using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class IntimidationTraitSpec : TraitSpec
    {
        public int AttackReductionPercent { get; }

        public IntimidationTraitSpec(int attackReductionPercent)
            : base(TraitKey.Intimidation)
        {
            if (attackReductionPercent < 1 || attackReductionPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(attackReductionPercent));

            AttackReductionPercent = attackReductionPercent;
        }


        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (IntimidationTraitSpec)other;
            return AttackReductionPercent == otherSpec.AttackReductionPercent;
        }
    }
}
