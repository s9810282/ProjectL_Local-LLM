using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts
{
    internal readonly struct PierceFalloffEffectContext
    {
        public int DamageAfterBonuses { get; }
        public int EncounterIndex { get; }

        public PierceFalloffEffectContext(int damageAfterBonuses, int encounterIndex)
        {
            if (damageAfterBonuses < 0)
                throw new ArgumentOutOfRangeException(nameof(damageAfterBonuses));
            if (encounterIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(encounterIndex));

            DamageAfterBonuses = damageAfterBonuses;
            EncounterIndex = encounterIndex;
        }
    }
}
