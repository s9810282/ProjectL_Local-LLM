using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    internal static class CombatCommandEvents
    {
        internal static void Health(List<CombatEvent> events, CharacterCombatFacts before, CharacterCombatFacts after,
            IReadOnlyList<CombatDamageOrigin> damageOrigins = null)
        {
            if (before.Health.CurrentHealth != after.Health.CurrentHealth || before.Health.MaxHealth != after.Health.MaxHealth)
                events.Add(new CombatCharacterEvent(after.Health.IsDead ? CombatEventKind.Died : CombatEventKind.HealthChanged,
                    before, after, damageOrigins));
        }
        internal static void Trait(List<CombatEvent> events, EntityId source, TraitKey key, TraitActivationKind kind,
            int magnitude, IReadOnlyList<EntityId> targets = null, IReadOnlyList<CellCoord> cells = null,
            CombatDamageOrigin origin = null, Rotation4? direction = null, CellCoord? selectedCell = null)
        {
            if (events == null) return;
            events.Add(new CombatEffectsEvent(new[] { new TraitActivationSnapshot(source, null, key, kind, magnitude,
                targets, cells, origin, direction, selectedCell) }, Array.Empty<CategoryActivationSnapshot>()));
        }

        internal static void DamageTraits(List<CombatEvent> events, IReadOnlyList<TraitActivationSnapshot> contributions,
            EntityId target, IReadOnlyList<CellCoord> cells, CombatDamageOrigin origin)
        {
            if (events == null || contributions.Count == 0) return;
            var traits = contributions.Select(c => new TraitActivationSnapshot(c.SourceUnitId, target, c.TraitKey,
                c.TraitActivationKind, c.Magnitude, targetCells: cells, resultOrigin: origin)).ToArray();
            events.Add(new CombatEffectsEvent(traits, Array.Empty<CategoryActivationSnapshot>()));
        }

        internal static void TraitState(List<CombatEvent> events, CharacterCombatFacts before, CharacterCombatFacts after,
            CombatDamageOrigin origin = null)
        {
            if (events != null && !before.SameValues(after)) events.Add(new CombatTraitStateEvent(before, after, origin,
                CombatEffectiveStatsCalculator.Snapshot(before), CombatEffectiveStatsCalculator.Snapshot(after)));
        }

        internal static CombatDamageOrigin FindOrigin(IEnumerable<CombatEvent> events, EntityId creditor)
        {
            foreach (var value in events)
            {
                if (value is CombatCharacterEvent character)
                {
                    var origin = character.DamageOrigins.FirstOrDefault(o => o.CreditedCharacterId == creditor);
                    if (origin != null) return origin;
                }
                if (value is CombatStructureEvent structure)
                {
                    var origin = structure.DamageOrigins.FirstOrDefault(o => o.CreditedCharacterId == creditor);
                    if (origin != null) return origin;
                }
                var single = value is CombatTraitStateEvent state ? state.Origin
                    : value is CombatHeldEnvironmentEvent held ? held.Origin
                    : value is CombatObstacleReactionEvent obstacle ? obstacle.Origin : null;
                if (single?.CreditedCharacterId == creditor) return single;
            }
            return null;
        }

        internal static void Effects(List<CombatEvent> events, CombatEffectPlan plan)
        {
            var traits = new List<TraitActivationSnapshot>(); var categories = new List<CategoryActivationSnapshot>();
            CombatCommandEffects.AppendActivations(plan, traits, categories);
            if (traits.Count + categories.Count != 0) events.Add(new CombatEffectsEvent(traits, categories));
        }
    }
}
