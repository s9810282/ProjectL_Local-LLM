using System;
using System.Linq;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items
{
    public static class ItemEquipmentCalculator
    {
        public static CombatCommandTransition Calculate(CombatCommandInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.State.Phase == CombatFlowPhase.Finished) return CombatCommandCalculator.Reject(input, "StageEnded");
            if (input.State.Turn.StagePhase != StagePhase.Deployment || input.State.Phase != CombatFlowPhase.PlayerInput)
                return CombatCommandCalculator.Reject(input, "WrongPhase");
            var actor = input.Characters.SingleOrDefault(c => c.EntityId == input.ActorId);
            if (actor == null || actor.Owner != Owner.Player || actor.Health.IsDead || actor.Area != CharacterRuntimeArea.Field ||
                (actor.OwnershipRole != CharacterOwnershipRole.Main && actor.OwnershipRole != CharacterOwnershipRole.Sub))
                return CombatCommandCalculator.Reject(input, "InvalidItemTarget");
            int? old = actor.Items.EquippedItemKey;
            bool unequip = input.Kind == CombatCommandKind.UnequipItem;
            if (input.Kind != CombatCommandKind.EquipItem && input.Kind != CombatCommandKind.ReplaceItem && !unequip)
                throw new ArgumentException("아이템 장착 명령이 필요합니다.", nameof(input));
            if (input.Kind == CombatCommandKind.EquipItem && old.HasValue)
                return CombatCommandCalculator.Reject(input, "OccupiedItemSlot");
            if ((unequip || input.Kind == CombatCommandKind.ReplaceItem) && !old.HasValue)
                return CombatCommandCalculator.Reject(input, "EmptyItemSlot");
            int? next = unequip ? null : input.ItemKey;
            if ((!unequip && (!next.HasValue || !input.ItemDefinitions.Any(d => d.Key == next))) ||
                (old.HasValue && !input.ItemDefinitions.Any(d => d.Key == old)))
                return CombatCommandCalculator.Reject(input, "UnknownItem");
            if (next.HasValue && !ItemRuleRegistry.CreateDefault().IsAvailable(input.ItemDefinitions.Single(d => d.Key == next.Value).Effect))
                return CombatCommandCalculator.Reject(input, "DeferredUnitTurnTiming");
            if (old == next)
                return Accepted(input, Array.Empty<CharacterStateChange>(), null, Array.Empty<CombatEvent>());
            if (next.HasValue && input.ItemInventory.GetCount(next.Value) < 1)
                return CombatCommandCalculator.Reject(input, "ItemUnavailable");

            var counts = input.ItemInventory.Counts.ToDictionary(p => p.Key, p => p.Value);
            if (old.HasValue)
            {
                try { counts[old.Value] = checked(input.ItemInventory.GetCount(old.Value) + 1); }
                catch (OverflowException) { return CombatCommandCalculator.Reject(input, "QuantityOverflow"); }
            }
            if (next.HasValue) counts[next.Value] = checked(input.ItemInventory.GetCount(next.Value) - 1);
            var final = ItemCharacterChanges.WithItems(actor, new CharacterItemState(next, actor.Items.Effects));
            var reason = unequip ? ItemEventReason.Unequipped : old.HasValue ? ItemEventReason.Replaced : ItemEventReason.Equipped;
            return Accepted(input, new[] { new CharacterStateChange(actor, final) },
                new ItemInventoryChange(input.ItemInventory, new ItemInventorySnapshot(counts)), new CombatEvent[] {
                    new CombatItemEvent((next ?? old).Value, actor.EntityId, null, ItemEventStage.Equipment, reason, 0, old, next) });
        }

        internal static CombatCommandTransition RefundReturn(CombatCommandInput input, CharacterCombatFacts actor,
            CharacterCombatFacts returned, List<CombatEvent> events)
        {
            ItemInventoryChange inventory = null;
            if (actor.Items.EquippedItemKey is int key)
            {
                var grant = ItemInventoryCalculator.Grant(input.ItemInventory, input.ItemDefinitions, key, 1);
                if (!grant.Accepted) return CombatCommandCalculator.Reject(input, grant.Reason);
                inventory = grant.Change;
                returned = ItemCharacterChanges.WithItems(returned, new CharacterItemState(null, returned.Items.Effects));
                events.Add(new CombatItemEvent(key, actor.EntityId, null, ItemEventStage.Equipment,
                    ItemEventReason.ReturnedToHand, 0, key, null));
            }
            return Accepted(input, new[] { new CharacterStateChange(actor, returned) }, inventory, events);
        }

        private static CombatCommandTransition Accepted(CombatCommandInput input, IReadOnlyList<CharacterStateChange> changes,
            ItemInventoryChange inventory, IReadOnlyList<CombatEvent> events) => new(input, true, "",
                new CharacterChanges(changes, Array.Empty<SoulCountChange>()), new SoulFieldChange(input.Souls, input.Souls),
                input.State, new StageOutcomeSnapshot(StageOutcome.InProgress, StageOutcomeReason.None), events: events, inventoryChange: inventory);
    }
}
