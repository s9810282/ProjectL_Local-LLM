using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class VanguardTraitSpec : TraitSpec
    {
        public int AttackBonus { get; }
        public int DurationControllerTurnEnds { get; }

        public VanguardTraitSpec(int attackBonus, int durationControllerTurnEnds) : base(TraitKey.Vanguard)
        {
            if (attackBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(attackBonus));
            if (durationControllerTurnEnds < 1)
                throw new ArgumentOutOfRangeException(nameof(durationControllerTurnEnds));
            AttackBonus = attackBonus;
            DurationControllerTurnEnds = durationControllerTurnEnds;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (VanguardTraitSpec)other;
            return AttackBonus == otherSpec.AttackBonus &&
                DurationControllerTurnEnds == otherSpec.DurationControllerTurnEnds;
        }
    }
}
