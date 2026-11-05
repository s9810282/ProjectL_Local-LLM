using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items
{
    public sealed class ItemDamageContext
    {
        public CombatDamageKind Kind { get; }
        public CharacterCombatFacts Attacker { get; }
        public CharacterCombatFacts Recipient { get; }
        public int DamageBeforeItems { get; }
        public int PreparationIndex { get; }

        public ItemDamageContext(
            CombatDamageKind kind,
            CharacterCombatFacts attacker,
            CharacterCombatFacts recipient,
            int damageBeforeItems,
            int preparationIndex)
        {
            if (!Enum.IsDefined(typeof(CombatDamageKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (recipient == null) throw new ArgumentNullException(nameof(recipient));
            if (damageBeforeItems < 0) throw new ArgumentOutOfRangeException(nameof(damageBeforeItems));
            if (preparationIndex < 0) throw new ArgumentOutOfRangeException(nameof(preparationIndex));
            Kind = kind;
            Attacker = attacker;
            Recipient = recipient;
            DamageBeforeItems = damageBeforeItems;
            PreparationIndex = preparationIndex;
        }
    }
}
