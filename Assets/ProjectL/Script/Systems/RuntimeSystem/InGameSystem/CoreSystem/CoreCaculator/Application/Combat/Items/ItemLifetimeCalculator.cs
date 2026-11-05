using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items
{
    public static class ItemLifetimeCalculator
    {
        public static CharacterCombatFacts ClearRoundItems(CharacterCombatFacts source) =>
            ClearRoundItems(source, null, ItemEventReason.RoundEnded);

        internal static CharacterCombatFacts ClearRoundItems(CharacterCombatFacts source,
            List<CombatEvent> events, ItemEventReason reason)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source.Items.SameValues(CharacterItemState.Empty)) return source;
            if (source.Items.EquippedItemKey is int discarded)
                events?.Add(new CombatItemEvent(discarded, source.EntityId, null, ItemEventStage.Lifecycle,
                    reason, 0, discarded, null));
            return ItemCharacterChanges.WithItems(source, CharacterItemState.Empty);
        }

        internal static CharacterCombatFacts DiscardEquipment(CharacterCombatFacts source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return source.Items.EquippedItemKey.HasValue
                ? ItemCharacterChanges.WithItems(source, new CharacterItemState(null, source.Items.Effects)) : source;
        }
    }
}
