using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>환경 종료 동작 뒤, 다음 진영 입력 전에 특성 수명만 정산합니다.</summary>
    internal static class TraitTurnLifecycle
    {
        internal static void BeginControllerTurn(CombatCommandInput input, IDictionary<EntityId, CharacterCombatFacts> projected, Owner side)
            => BeginAndRecord(input, projected, side, null);

        internal static void BeginAndRecord(CombatCommandInput input, IDictionary<EntityId, CharacterCombatFacts> projected,
            Owner side, List<CombatEvent> events)
        {
            foreach (var character in input.Characters.Where(c => c.Owner == side))
            {
                var current = projected.TryGetValue(character.EntityId, out var value) ? value : character;
                var expired = current.StatusEffects.Entries.OfType<ShieldStatus>().Where(s => s.ExpiresAtSideTurnStart == side).ToArray();
                if (expired.Length > 0)
                {
                    var statuses = current.StatusEffects;
                    foreach (var shield in expired)
                    {
                        var origin = shield.Origin.Value;
                        statuses = ShieldStatusRules.Restore(statuses, origin, null);
                        CombatCommandEvents.Trait(events, new EntityId(origin.OwnerInstanceId), (TraitKey)origin.DefinitionKey,
                            TraitActivationKind.ShieldRemoved, shield.RemainingBudget, new[] { current.EntityId });
                    }
                    var cleared = current.WithStatusEffects(statuses);
                    CombatCommandEvents.TraitState(events, current, cleared);
                    projected[current.EntityId] = current = cleared;
                }
                var stats = StationaryFireTraitRule.BeginOwnTurn(current.Stats);
                if (stats.SameValues(current.Stats)) continue;
                var final = current.With(stats: stats);
                projected[current.EntityId] = final;
                CombatCommandEvents.Trait(events, current.EntityId, TraitKey.StationaryFire, TraitActivationKind.StatusReset, 0,
                    new[] { current.EntityId });
                CombatCommandEvents.TraitState(events, current, final);
            }
        }

        internal static void EndControllerTurn(CombatCommandInput input, IDictionary<EntityId, CharacterCombatFacts> projected, Owner endedSide)
            => EndAndRecord(input, projected, endedSide, null);

        internal static void EndAndRecord(CombatCommandInput input, IDictionary<EntityId, CharacterCombatFacts> projected,
            Owner endedSide, List<CombatEvent> events)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (projected == null) throw new ArgumentNullException(nameof(projected));
            if (endedSide != Owner.Player && endedSide != Owner.Enemy) throw new ArgumentOutOfRangeException(nameof(endedSide));
            var expired = new Dictionary<(EffectOriginKey Origin, TraitActivationKind Kind), List<EntityId>>();
            var changes = new List<CharacterStateChange>();
            void Record(EffectOriginKey origin, TraitActivationKind kind, EntityId target)
            {
                var key = (origin, kind);
                if (!expired.TryGetValue(key, out var targets)) expired.Add(key, targets = new List<EntityId>());
                targets.Add(target);
            }
            foreach (var character in projected.Values.ToArray())
            {
                var current = character;
                if (character.Owner == endedSide)
                    foreach (var restriction in character.StatusEffects.Entries.OfType<ActionRestrictionStatus>())
                        if (restriction.Origin.Value.Kind == EffectOwnerKind.Trait)
                            Record(restriction.Origin.Value, restriction.RemainingAffectedSideTurnEnds == 1
                                ? TraitActivationKind.StatusExpired : TraitActivationKind.StatusConsumed, character.EntityId);
                if (character.Owner == endedSide)
                    foreach (var exposure in character.StatusEffects.Entries.OfType<WeaknessExposureStatus>())
                        if (exposure.Origin.Value.Kind == EffectOwnerKind.Trait && exposure.LifetimeKind == WeaknessExposureLifetimeKind.AffectedSideTurnEnds)
                            Record(exposure.Origin.Value, exposure.RemainingAffectedSideTurnEnds == 1
                                ? TraitActivationKind.StatusExpired : TraitActivationKind.StatusConsumed, character.EntityId);
                var statuses = AppliedStatusRules.EndControllerTurn(current.StatusEffects, current.Owner, endedSide);
                statuses = PeriodicStatusRules.ExpireSlow(statuses, input.State.Turn.CompletedControllerTurnCount);
                foreach (var slow in current.StatusEffects.Entries.OfType<MovementRangeDecreaseStatus>())
                    if (!statuses.Entries.Contains(slow) && slow.Origin.Value.Kind == EffectOwnerKind.Trait)
                        Record(slow.Origin.Value, TraitActivationKind.StatusExpired, character.EntityId);
                if (!ReferenceEquals(statuses, current.StatusEffects))
                {
                    foreach (var grant in current.StatusEffects.Entries.OfType<AttackIncreaseStatus>()
                        .Where(g => g.Amount > 0 && g.Origin.Value.Kind == EffectOwnerKind.Trait))
                        Record(grant.Origin.Value, grant.RemainingControllerTurnEnds == 1 ? TraitActivationKind.StatusExpired : TraitActivationKind.StatusConsumed, character.EntityId);
                    current = current.WithStatusEffects(statuses);
                    int hunter = current.OwnedId.HasValue ? input.Owned.Single(o => o.Id == current.OwnedId.Value).HunterAttackBonus : 0;
                    current = CombatEffectiveStatsCalculator.ProjectTraitAttack(current, hunter);
                }
                if (!ReferenceEquals(current, character))
                {
                    projected[character.EntityId] = current;
                    changes.Add(new CharacterStateChange(character, current));
                }
            }
            foreach (var entry in expired.OrderBy(p => p.Key.Origin).ThenBy(p => p.Key.Kind))
                CombatCommandEvents.Trait(events, new EntityId(entry.Key.Origin.OwnerInstanceId), (TraitKey)entry.Key.Origin.DefinitionKey,
                    entry.Key.Kind, entry.Key.Kind == TraitActivationKind.StatusConsumed ? 1 : 0,
                    entry.Value.Distinct().OrderBy(id => id).ToArray());
            foreach (var change in changes) CombatCommandEvents.TraitState(events, change.Expected, change.Final);
            TraitPresenceReconciler.EndAndRecord(input, projected, endedSide, events);
        }
    }
}
