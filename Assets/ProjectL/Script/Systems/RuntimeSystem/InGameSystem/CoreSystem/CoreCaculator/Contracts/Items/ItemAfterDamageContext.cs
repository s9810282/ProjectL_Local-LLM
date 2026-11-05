using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items
{
    public sealed class ItemAfterDamageContext
    {
        public ItemDamageContext Damage { get; }
        public CharacterCombatFacts RecipientAfterDamage { get; }
        public int ActualHealthLoss { get; }

        public ItemAfterDamageContext(ItemDamageContext damage, CharacterCombatFacts recipientAfterDamage, int actualHealthLoss)
        {
            if (damage == null) throw new ArgumentNullException(nameof(damage));
            if (recipientAfterDamage == null) throw new ArgumentNullException(nameof(recipientAfterDamage));
            if (actualHealthLoss < 0) throw new ArgumentOutOfRangeException(nameof(actualHealthLoss));
            if (damage.Recipient.EntityId != recipientAfterDamage.EntityId ||
                damage.Recipient.Health.MaxHealth != recipientAfterDamage.Health.MaxHealth ||
                actualHealthLoss != checked(damage.Recipient.Health.CurrentHealth - recipientAfterDamage.Health.CurrentHealth) ||
                actualHealthLoss > damage.DamageBeforeItems)
                throw new ArgumentException("피해 전후 대상과 실제 체력 감소량이 일치해야 합니다.");
            Damage = damage;
            RecipientAfterDamage = recipientAfterDamage;
            ActualHealthLoss = actualHealthLoss;
        }
    }
}
