using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts
{
    internal readonly struct AttackDamageEffectContext
    {
        public int BaseDamage { get; }
        public int AllyUnitCount { get; }
        public int EnemyUnitCount { get; }

        public AttackDamageEffectContext(int baseDamage)
            : this(baseDamage, 0, 0)
        {
        }

        public AttackDamageEffectContext(
            int baseDamage,
            int allyUnitCount,
            int enemyUnitCount)
        {
            if (baseDamage < 0) throw new ArgumentOutOfRangeException(nameof(baseDamage));
            if (allyUnitCount < 0) throw new ArgumentOutOfRangeException(nameof(allyUnitCount));
            if (enemyUnitCount < 0) throw new ArgumentOutOfRangeException(nameof(enemyUnitCount));

            BaseDamage = baseDamage;
            AllyUnitCount = allyUnitCount;
            EnemyUnitCount = enemyUnitCount;
        }
    }
}
