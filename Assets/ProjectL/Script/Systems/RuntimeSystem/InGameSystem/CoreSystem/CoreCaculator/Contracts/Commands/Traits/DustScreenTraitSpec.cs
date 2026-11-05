using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class DustScreenTraitSpec : TraitSpec
    {
        public int AttackRangeReduction { get; }
        public int AttackDamageReductionPercent { get; }
        public int OpposingTurnEndsToExpire { get; }

        public DustScreenTraitSpec(int attackRangeReduction, int attackDamageReductionPercent, int opposingTurnEndsToExpire) : base(TraitKey.DustScreen)
        {
            if (attackRangeReduction < 0)
                throw new ArgumentOutOfRangeException(nameof(attackRangeReduction));
            if (attackDamageReductionPercent < 0 || attackDamageReductionPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(attackDamageReductionPercent));
            if (opposingTurnEndsToExpire < 1)
                throw new ArgumentOutOfRangeException(nameof(opposingTurnEndsToExpire));
            AttackRangeReduction = attackRangeReduction;
            AttackDamageReductionPercent = attackDamageReductionPercent;
            OpposingTurnEndsToExpire = opposingTurnEndsToExpire;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (DustScreenTraitSpec)other;
            return AttackRangeReduction == otherSpec.AttackRangeReduction &&
                AttackDamageReductionPercent == otherSpec.AttackDamageReductionPercent &&
                OpposingTurnEndsToExpire == otherSpec.OpposingTurnEndsToExpire;
        }
    }
}
