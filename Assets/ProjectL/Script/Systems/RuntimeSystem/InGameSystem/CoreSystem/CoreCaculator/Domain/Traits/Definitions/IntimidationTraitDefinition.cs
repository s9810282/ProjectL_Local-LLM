using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class IntimidationTraitDefinition : TraitDefinition
    {
        public int AttackReductionPercent { get; }

        public IntimidationTraitDefinition(int attackReductionPercent)
            : base(TraitKey.Intimidation)
        {
            if (attackReductionPercent < 1 || attackReductionPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(attackReductionPercent));

            AttackReductionPercent = attackReductionPercent;
        }

    }
}
