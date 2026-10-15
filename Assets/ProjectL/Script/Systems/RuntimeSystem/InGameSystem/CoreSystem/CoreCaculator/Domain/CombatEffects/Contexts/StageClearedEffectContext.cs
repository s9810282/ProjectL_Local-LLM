using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts
{
    internal readonly struct StageClearedEffectContext
    {
        internal EntityId MainCharacterId { get; }
        internal int ProjectedCurrentHealth { get; }
        internal int MaxHealth { get; }

        internal StageClearedEffectContext(
            EntityId mainCharacterId,
            int projectedCurrentHealth,
            int maxHealth)
        {
            if (mainCharacterId.IsEmpty) throw new ArgumentException(nameof(mainCharacterId));
            if (maxHealth <= 0) throw new ArgumentOutOfRangeException(nameof(maxHealth));
            if (projectedCurrentHealth <= 0 || projectedCurrentHealth > maxHealth)
                throw new ArgumentOutOfRangeException(nameof(projectedCurrentHealth));

            MainCharacterId = mainCharacterId;
            ProjectedCurrentHealth = projectedCurrentHealth;
            MaxHealth = maxHealth;
        }
    }
}
