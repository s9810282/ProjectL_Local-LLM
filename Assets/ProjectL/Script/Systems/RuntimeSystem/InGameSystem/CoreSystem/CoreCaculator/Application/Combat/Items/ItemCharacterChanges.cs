using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items
{
    public static class ItemCharacterChanges
    {
        internal static IReadOnlyList<CharacterStateChange> ApplyDecision(CharacterCombatFacts holder,
            CharacterCombatFacts target, ItemRuleDecision decision, out bool effectAdded)
        {
            if (holder == null) throw new ArgumentNullException(nameof(holder));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (decision == null) throw new ArgumentNullException(nameof(decision));
            effectAdded = false;
            if (!decision.Activated) return Array.Empty<CharacterStateChange>();
            if (!holder.Items.EquippedItemKey.HasValue || decision.TargetId != target.EntityId)
                throw new InvalidOperationException("발동 결정과 현재 소지자·대상이 일치해야 합니다.");
            var consumed = WithItems(holder, new CharacterItemState(null, holder.Items.Effects));
            var effect = decision.TemporaryEffect;
            if (effect != null && !target.Items.Effects.Any(e => e.ItemKey == effect.ItemKey && e.Stat == effect.Stat))
            {
                effectAdded = true;
                var updatedTarget = WithItems(target, new CharacterItemState(target.Items.EquippedItemKey,
                    target.Items.Effects.Append(effect).ToArray()));
                if (holder.EntityId == target.EntityId)
                    consumed = WithItems(consumed, new CharacterItemState(null, updatedTarget.Items.Effects));
                else
                    return new[] { new CharacterStateChange(holder, consumed), new CharacterStateChange(target, updatedTarget) };
            }
            return new[] { new CharacterStateChange(holder, consumed) };
        }

        /// <summary>발동 결정을 아이템 사건으로 기록한다. 이미 걸린 같은 효과를 다시 건 경우는 중복 소모로 구분한다.</summary>
        internal static void AppendDecisionEvent(List<CombatEvent> events, ItemDefinition definition, EntityId holderId,
            ItemRuleDecision decision, ItemEventStage stage, CombatDamageKind? cause, int appliedMagnitude, bool effectAdded)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (decision == null) throw new ArgumentNullException(nameof(decision));
            if (!decision.Activated) return;
            events.Add(new CombatItemEvent(definition.Key, holderId, decision.TargetId, stage,
                decision.TemporaryEffect != null && !effectAdded ? ItemEventReason.DuplicateEffect : ItemEventReason.Activated,
                appliedMagnitude, definition.Key, null, cause, decision.TemporaryEffect != null ? ItemActivationKind.TemporaryEffect :
                    decision.RemainingDamage.HasValue ? ItemActivationKind.DamageMitigation : ItemActivationKind.Healing));
        }

        public static CharacterCombatFacts WithItems(CharacterCombatFacts c, CharacterItemState items)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            if (items == null) throw new ArgumentNullException(nameof(items));
            return c.With(items: items);
        }
    }
}
