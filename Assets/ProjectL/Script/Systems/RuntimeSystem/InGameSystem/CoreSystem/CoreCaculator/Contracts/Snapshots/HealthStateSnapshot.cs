using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public readonly struct HealthStateSnapshot
    {
        public int CurrentHealth { get; }
        public int MaxHealth { get; }
        public bool IsDead => CurrentHealth == 0;

        public HealthStateSnapshot(int currentHealth, int maxHealth)
        {
            if (maxHealth < 1)
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            if (currentHealth < 0 || currentHealth > maxHealth)
                throw new ArgumentOutOfRangeException(nameof(currentHealth));

            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
        }
    }
}
