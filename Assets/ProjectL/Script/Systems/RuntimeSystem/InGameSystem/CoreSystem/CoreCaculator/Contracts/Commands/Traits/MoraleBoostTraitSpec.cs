using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class MoraleBoostTraitSpec : TraitSpec
    {
        public int DefeatedEnemyBaseAttackPercent { get; }
        public int DurationControllerTurnEnds { get; }

        public MoraleBoostTraitSpec(int defeatedEnemyBaseAttackPercent, int durationControllerTurnEnds) : base(TraitKey.MoraleBoost)
        {
            if (defeatedEnemyBaseAttackPercent < 0)
                throw new ArgumentOutOfRangeException(nameof(defeatedEnemyBaseAttackPercent));
            if (durationControllerTurnEnds < 1)
                throw new ArgumentOutOfRangeException(nameof(durationControllerTurnEnds));
            DefeatedEnemyBaseAttackPercent = defeatedEnemyBaseAttackPercent;
            DurationControllerTurnEnds = durationControllerTurnEnds;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (MoraleBoostTraitSpec)other;
            return DefeatedEnemyBaseAttackPercent == otherSpec.DefeatedEnemyBaseAttackPercent &&
                DurationControllerTurnEnds == otherSpec.DurationControllerTurnEnds;
        }
    }
}
