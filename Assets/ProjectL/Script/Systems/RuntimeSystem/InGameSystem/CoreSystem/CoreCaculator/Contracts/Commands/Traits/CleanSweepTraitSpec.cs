using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class CleanSweepTraitSpec : TraitSpec
    {
        public int AttackBonusPerAllyAttack { get; }
        public int DurationControllerTurnEnds { get; }

        public CleanSweepTraitSpec(int attackBonusPerAllyAttack, int durationControllerTurnEnds) : base(TraitKey.CleanSweep)
        {
            if (attackBonusPerAllyAttack < 0)
                throw new ArgumentOutOfRangeException(nameof(attackBonusPerAllyAttack));
            if (durationControllerTurnEnds < 1)
                throw new ArgumentOutOfRangeException(nameof(durationControllerTurnEnds));
            AttackBonusPerAllyAttack = attackBonusPerAllyAttack;
            DurationControllerTurnEnds = durationControllerTurnEnds;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (CleanSweepTraitSpec)other;
            return AttackBonusPerAllyAttack == otherSpec.AttackBonusPerAllyAttack &&
                DurationControllerTurnEnds == otherSpec.DurationControllerTurnEnds;
        }
    }
}
