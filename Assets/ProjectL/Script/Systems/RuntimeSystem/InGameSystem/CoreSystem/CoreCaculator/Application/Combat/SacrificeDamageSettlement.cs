using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    internal readonly struct SacrificeHitUpgrade
    {
        internal EntityId RecipientId { get; }
        internal bool PreventsLethalSacrificeDamage { get; }
        internal int ShieldOnActivation { get; }

        internal SacrificeHitUpgrade(EntityId recipientId, bool preventsLethalSacrificeDamage, int shieldOnActivation)
        {
            RecipientId = recipientId;
            PreventsLethalSacrificeDamage = preventsLethalSacrificeDamage;
            ShieldOnActivation = shieldOnActivation;
        }
    }

    internal static class SacrificeDamageSettlement
    {
        internal static SacrificeHitUpgrade Capture(CombatCommandInput input, CharacterCombatFacts protector,
            CombatProfileReadSet profileReads = null)
        {
            var profile = CombatCommandCalculator.RequiredProfile(input, protector, profileReads);
            var rule = CombatCommandEffects.Capture(protector, profile).Select(binding => binding.Rule)
                .OfType<SacrificeEffectRule>().Single();
            return new SacrificeHitUpgrade(protector.EntityId, rule.PreventsLethalSacrificeDamage, rule.ShieldOnActivation);
        }

        internal static int LimitHealthLoss(int currentHealth, int damageAfterShields, in SacrificeHitUpgrade upgrade)
        {
            int limit = upgrade.PreventsLethalSacrificeDamage ? Math.Max(0, currentHealth - 1) : currentHealth;
            return Math.Min(limit, damageAfterShields);
        }

        internal static CharacterCombatFacts GrantShield(CharacterCombatFacts recipient, in SacrificeHitUpgrade upgrade)
        {
            if (recipient.Health.IsDead || upgrade.ShieldOnActivation == 0) return recipient;
            var origin = TraitEffectOrigin.Create(recipient.EntityId, TraitKey.Sacrifice);
            var prior = recipient.StatusEffects.Entries.OfType<ShieldStatus>().SingleOrDefault(status => status.Origin == origin);
            int budget = checked((prior?.RemainingBudget ?? 0) + upgrade.ShieldOnActivation);
            var shield = new ShieldStatus(origin, budget, true, prior?.LastResetControllerTurnCount ?? 0);
            return recipient.WithStatusEffects(ShieldStatusRules.Restore(recipient.StatusEffects, origin, shield));
        }
    }
}
