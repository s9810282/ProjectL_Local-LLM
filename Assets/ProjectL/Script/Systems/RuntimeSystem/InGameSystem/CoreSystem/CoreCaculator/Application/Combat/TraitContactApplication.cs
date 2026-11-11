using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    internal static class TraitContactApplication
    {
        /// <summary>피해가 정산된 접촉의 특성을 적용합니다. 원래 지정 대상과 실제 피격자를 각각 전달합니다.</summary>
        internal static void Apply(CommandAttackPlan plan, CommandAttackProjection world, EntityId targetId,
            EntityId recipientId, CombatDamageOrigin origin)
        {
            WeaknessExposureApplication.CompleteHit(plan, world, recipientId, origin);
            TraitRestrictionApplication.AfterContact(plan, world, targetId, recipientId, origin);
            ArmorBreakerApplication.AfterHit(plan.Input, plan.DamageActor, recipientId, world.Characters, world.Events, origin);
            ApplyBleeding(plan.Actor, plan.Bindings, plan.DamageKind, world.Characters, world.Events, recipientId, origin);
        }

        internal static void ApplyBleeding(CharacterCombatFacts attacker, IReadOnlyList<EffectRuleBinding> bindings, CombatDamageKind kind,
            IDictionary<EntityId, CharacterCombatFacts> characters, List<CombatEvent> events, EntityId recipientId, CombatDamageOrigin origin)
        {
            if (!characters.TryGetValue(recipientId, out var recipient)) return;
            foreach (var binding in bindings)
            {
                if (binding.Rule is not BleedingEffectRule rule) continue;
                var statuses = rule.Apply(new EffectRuleInput(binding.Origin, binding.ExpectedState), attacker, recipient, kind);
                if (ReferenceEquals(statuses, recipient.StatusEffects)) continue;
                var final = recipient.WithStatusEffects(statuses);
                characters[recipientId] = final;
                CombatCommandEvents.Trait(events, attacker.EntityId, TraitKey.Bleeding, TraitActivationKind.StatusApplied,
                    statuses.Entries.OfType<BleedingStatus>().Single().Stacks.Count, new[] { recipientId }, origin: origin);
                CombatCommandEvents.TraitState(events, recipient, final, origin);
                recipient = final;
            }
        }
    }
}
