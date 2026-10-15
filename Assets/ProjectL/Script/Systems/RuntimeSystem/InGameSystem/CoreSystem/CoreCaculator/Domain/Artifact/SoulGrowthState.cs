using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact
{
    internal readonly struct SoulGrowthState : IEquatable<SoulGrowthState>
    {
        internal static SoulGrowthState Empty => default;

        internal int AttackBonus { get; }
        internal int MaxHealthBonus { get; }
        internal long Revision { get; }

        internal SoulGrowthState(int attackBonus, int maxHealthBonus, long revision)
        {
            if (attackBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(attackBonus));
            if (maxHealthBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(maxHealthBonus));
            if (revision < 0)
                throw new ArgumentOutOfRangeException(nameof(revision));

            AttackBonus = attackBonus;
            MaxHealthBonus = maxHealthBonus;
            Revision = revision;
        }

        public bool Equals(SoulGrowthState other)
        {
            return AttackBonus == other.AttackBonus &&
                   MaxHealthBonus == other.MaxHealthBonus &&
                   Revision == other.Revision;
        }

        public override bool Equals(object obj)
        {
            return obj is SoulGrowthState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(AttackBonus, MaxHealthBonus, Revision);
        }

        public static bool operator ==(SoulGrowthState left, SoulGrowthState right) =>
            left.Equals(right);

        public static bool operator !=(SoulGrowthState left, SoulGrowthState right) =>
            left.Equals(right) == false;
    }

    internal static class SoulGrowthRules
    {
        internal const int AttackBonusPerSoul = 1;
        internal const int MaxHealthBonusPerSoul = 1;
    }

    internal static class SoulGrowthMath
    {
        internal static (HealthStateSnapshot Health, int Count) Collect(
            HealthStateSnapshot health, int count, int baseMaxHealth, int temporaryMaxHealth,
            int healAmount, int positionMaxHealth = 0)
        {
            if (count < 0 || baseMaxHealth < 1 || temporaryMaxHealth < 0 || healAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (health.MaxHealth != checked(baseMaxHealth + count * SoulGrowthRules.MaxHealthBonusPerSoul +
                                           temporaryMaxHealth + positionMaxHealth) || health.IsDead)
                throw new InvalidOperationException("영혼 수집 체력이 기준값 또는 생존 조건과 맞지 않습니다.");
            int finalCount = checked(count + 1);
            int maxHealth = checked(baseMaxHealth + finalCount * SoulGrowthRules.MaxHealthBonusPerSoul +
                                    temporaryMaxHealth + positionMaxHealth);
            int currentHealth = (int)Math.Min(maxHealth, (long)health.CurrentHealth + healAmount);
            return (new HealthStateSnapshot(currentHealth, maxHealth), finalCount);
        }

        internal static (HealthStateSnapshot Health, SoulGrowthState Growth) Collect(
            HealthStateSnapshot health,
            SoulGrowthState growth,
            SoulGrowthAmounts amounts)
        {
            if (health.MaxHealth < 1 || health.CurrentHealth < 0 ||
                health.CurrentHealth > health.MaxHealth)
            {
                throw new ArgumentException("A valid health snapshot is required.", nameof(health));
            }
            if (health.IsDead)
                throw new InvalidOperationException("A dead Unit cannot collect a soul.");
            if (amounts.attackBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(amounts), "Attack bonus cannot be negative.");
            if (amounts.maxHealthBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(amounts), "Max-health bonus cannot be negative.");
            if (amounts.healAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(amounts), "Heal amount cannot be negative.");

            int attackBonus = checked(growth.AttackBonus + amounts.AttackBonus);
            int maxHealthBonus = checked(growth.MaxHealthBonus + amounts.MaxHealthBonus);
            int maxHealth = checked(health.MaxHealth + amounts.MaxHealthBonus);
            int currentHealth = (int)Math.Min(
                (long)maxHealth,
                (long)health.CurrentHealth + amounts.HealAmount);
            long revision = checked(growth.Revision + 1L);

            return (
                new HealthStateSnapshot(currentHealth, maxHealth),
                new SoulGrowthState(attackBonus, maxHealthBonus, revision));
        }
    }
}
