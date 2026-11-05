using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class CallOfBloodTraitSpec : TraitSpec
    {
        public int EnemyMaxHealthHealPercent { get; }

        public CallOfBloodTraitSpec(int enemyMaxHealthHealPercent) : base(TraitKey.CallOfBlood)
        {
            if (enemyMaxHealthHealPercent < 0 || enemyMaxHealthHealPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(enemyMaxHealthHealPercent));
            EnemyMaxHealthHealPercent = enemyMaxHealthHealPercent;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (CallOfBloodTraitSpec)other;
            return EnemyMaxHealthHealPercent == otherSpec.EnemyMaxHealthHealPercent;
        }
    }
}
