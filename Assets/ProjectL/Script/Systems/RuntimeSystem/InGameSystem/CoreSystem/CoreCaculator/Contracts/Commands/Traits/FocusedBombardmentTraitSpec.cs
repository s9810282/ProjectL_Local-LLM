using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class FocusedBombardmentTraitSpec : TraitSpec
    {
        public int RequiredEnemyCount { get; }
        public int ExtraDamagePercent { get; }

        public FocusedBombardmentTraitSpec(int requiredEnemyCount, int extraDamagePercent) : base(TraitKey.FocusedBombardment)
        {
            if (requiredEnemyCount < 1)
                throw new ArgumentOutOfRangeException(nameof(requiredEnemyCount));
            if (extraDamagePercent < 0)
                throw new ArgumentOutOfRangeException(nameof(extraDamagePercent));
            RequiredEnemyCount = requiredEnemyCount;
            ExtraDamagePercent = extraDamagePercent;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (FocusedBombardmentTraitSpec)other;
            return RequiredEnemyCount == otherSpec.RequiredEnemyCount &&
                ExtraDamagePercent == otherSpec.ExtraDamagePercent;
        }
    }
}
