using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    public static class TraitPresenceReconciler
    {
        public static CharacterChanges Calculate(CombatCommandInput input, IReadOnlyList<CharacterCombatFacts> projected)
            => Calculate(input, projected, null, null);

        private static CharacterChanges Calculate(CombatCommandInput input, IReadOnlyList<CharacterCombatFacts> projected,
            Owner? endedSide, CombatProfileReadSet profileReads)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (projected == null) throw new ArgumentNullException(nameof(projected));
            var sources = projected.Where(c => !c.Health.IsDead && c.Stats.TraitStatuses.ToArray()
                .Any(t => t.Key == TraitKey.SoloAction || t.Key == TraitKey.Cohesion))
                .SelectMany(c => CombatCommandEffects.Capture(c, CombatCommandCalculator.RequiredProfile(input, c, profileReads))
                    .Where(b => b.Rule is SoloActionEffectRule || b.Rule is CohesionEffectRule).Select(b => (Source: c, Binding: b))).ToArray();
            bool anyTaunter = projected.Any(c => !c.Health.IsDead && c.Area == CharacterRuntimeArea.Field &&
                c.Stats.TraitStatuses.ToArray().Any(t => t.Key == TraitKey.Taunt));
            var changes = new List<CharacterStateChange>();
            foreach (var recipient in projected)
            {
                var shields = recipient.Health.IsDead ? new List<TraitShieldGrant>() : recipient.TraitEffects.Shields
                    .Where(g => !IsPresenceManagedShield(g.Origin)).ToList();
                if (!recipient.Health.IsDead)
                {
                    foreach (var entry in sources)
                    {
                        var prior = recipient.TraitEffects.Shields.SingleOrDefault(g => g.Origin == entry.Binding.Origin);
                        var bound = new EffectRuleInput(entry.Binding.Origin, entry.Binding.ExpectedState);
                        var grant = entry.Binding.Rule switch
                        {
                            SoloActionEffectRule rule => rule.Evaluate(bound, entry.Source, recipient, projected, prior,
                                input.State.Turn.CompletedControllerTurnCount,
                                entry.Source.Owner == Owner.Player ? input.State.Turn.CompletedPlayerTurnCount
                                    : input.State.Turn.CompletedControllerTurnCount - input.State.Turn.CompletedPlayerTurnCount,
                                entry.Source.Owner == endedSide),
                            CohesionEffectRule rule => rule.Evaluate(bound, entry.Source, recipient, prior,
                                input.State.Turn.CompletedControllerTurnCount, endedSide.HasValue),
                            _ => null
                        };
                        if (grant != null) shields.Add(grant);
                    }
                }
                var effects = new CharacterTraitEffects(recipient.TraitEffects.AttackGrants, shields.OrderBy(g => g.Origin).ToArray(),
                    CombatEffectiveStatsCalculator.CohesionDefenseBonus(input, recipient, projected, profileReads));
                var final = effects.SameValues(recipient.TraitEffects) ? recipient : recipient.With(traitEffects: effects);
                // 도발당함은 특성 효과를 맞춘 뒤에 맞춘다: With(traitEffects:)가 정규 목록의 특성 효과 항목을 다시 놓기 때문이다.
                var statuses = AppliedStatusRules.ReplaceTaunted(final.StatusEffects, TauntedOrigins(input, projected, recipient, anyTaunter, profileReads));
                if (!ReferenceEquals(statuses, final.StatusEffects)) final = final.WithStatusEffects(statuses);
                if (!ReferenceEquals(final, recipient)) changes.Add(new CharacterStateChange(recipient, final));
            }
            return new CharacterChanges(changes, Array.Empty<SoulCountChange>());
        }

        /// <summary>
        /// 받는 쪽에 걸려야 할 도발당함 출처입니다(도발 디버프 설계 TT2, Q-TT1 A). AI가 행동을 정하는 진영(적)의 필드 유닛만 받고,
        /// 판정은 적 AI와 같은 <see cref="TauntTargeting.Sources(CombatCommandInput, IEnumerable{CharacterCombatFacts}, CharacterCombatFacts, CombatProfileReadSet)"/>입니다.
        /// </summary>
        private static IEnumerable<EffectOriginKey> TauntedOrigins(CombatCommandInput input, IReadOnlyList<CharacterCombatFacts> projected,
            CharacterCombatFacts recipient, bool anyTaunter, CombatProfileReadSet profileReads) =>
            !anyTaunter || recipient.Owner != Owner.Enemy || recipient.Health.IsDead || recipient.Area != CharacterRuntimeArea.Field
                ? Array.Empty<EffectOriginKey>()
                : TauntTargeting.Sources(input, projected, recipient, profileReads).Select(source => TraitEffectOrigin.Create(source.EntityId, TraitKey.Taunt));

        private static bool IsPresenceManagedShield(EffectOriginKey origin) =>
            origin.Kind == EffectOwnerKind.Trait && origin.OwnerInstanceId > 0 &&
            (origin.DefinitionKey == (int)TraitKey.SoloAction || origin.DefinitionKey == (int)TraitKey.Cohesion) &&
            origin == TraitEffectOrigin.Create(new EntityId(origin.OwnerInstanceId), (TraitKey)origin.DefinitionKey);

        internal static bool RequiresOrderedDamage(IEnumerable<CharacterCombatFacts> characters) =>
            characters.Any(c => !c.Health.IsDead && c.Area == CharacterRuntimeArea.Field &&
                (c.StatusEffects.Entries.Any(s => s is ShieldStatus) || c.Stats.TraitStatuses.ToArray().Any(t =>
                    t.Key == TraitKey.SoloAction || t.Key == TraitKey.Cohesion || t.Key == TraitKey.Entrenchment ||
                    t.Key == TraitKey.Sacrifice || t.Key == TraitKey.Taunt)));

        internal static void EndControllerTurn(CombatCommandInput input, IDictionary<EntityId, CharacterCombatFacts> projected, Owner endedSide)
            => EndAndRecord(input, projected, endedSide, null);

        internal static void Apply(CombatCommandInput input, IDictionary<EntityId, CharacterCombatFacts> projected)
            => ApplyAndRecord(input, projected, null);

        internal static void ApplyAndRecord(CombatCommandInput input, IDictionary<EntityId, CharacterCombatFacts> projected,
            List<CombatEvent> events, CombatDamageOrigin origin = null, CombatProfileReadSet profileReads = null)
            => ReconcileAndRecord(input, projected, null, events, origin, profileReads);

        internal static void EndAndRecord(CombatCommandInput input, IDictionary<EntityId, CharacterCombatFacts> projected,
            Owner endedSide, List<CombatEvent> events)
        {
            if (endedSide != Owner.Player && endedSide != Owner.Enemy) throw new ArgumentOutOfRangeException(nameof(endedSide));
            ReconcileAndRecord(input, projected, endedSide, events, null, null);
        }

        private static void ReconcileAndRecord(CombatCommandInput input, IDictionary<EntityId, CharacterCombatFacts> projected,
            Owner? endedSide, List<CombatEvent> events, CombatDamageOrigin origin, CombatProfileReadSet profileReads)
        {
            var changes = Calculate(input, projected.Values.ToArray(), endedSide, profileReads).Characters;
            RecordShieldActivations(events, changes, origin);
            RecordTauntActivations(events, changes, origin);
            foreach (var change in changes)
            {
                if (change.Expected.TraitEffects.CohesionDefenseBonus != change.Final.TraitEffects.CohesionDefenseBonus)
                    CombatCommandEvents.Trait(events, change.Final.EntityId, TraitKey.Cohesion, TraitActivationKind.DefenseChanged,
                        Math.Abs(change.Final.TraitEffects.CohesionDefenseBonus - change.Expected.TraitEffects.CohesionDefenseBonus),
                        new[] { change.Final.EntityId }, change.Final.OccupiedCells, origin);
                projected[change.Final.EntityId] = change.Final;
                CombatCommandEvents.TraitState(events, change.Expected, change.Final, origin);
            }
        }

        internal static void RecordShieldActivations(List<CombatEvent> events, IEnumerable<CharacterStateChange> changes, CombatDamageOrigin origin = null)
        {
            if (events == null) return;
            var outcomes = new List<(EffectOriginKey Origin, TraitActivationKind Kind, int Amount, EntityId Recipient)>();
            foreach (var change in changes)
            {
                var before = change.Expected.TraitEffects.Shields;
                var after = change.Final.TraitEffects.Shields;
                foreach (var key in before.Select(g => g.Origin).Concat(after.Select(g => g.Origin)).Distinct().OrderBy(k => k))
                {
                    if (key.Kind != EffectOwnerKind.Trait) continue;
                    var old = before.SingleOrDefault(g => g.Origin == key);
                    var next = after.SingleOrDefault(g => g.Origin == key);
                    if (old == null ? next == null : old.SameValues(next)) continue;
                    TraitActivationKind kind;
                    int amount;
                    if (next == null || old?.Eligible == true && !next.Eligible)
                    {
                        kind = TraitActivationKind.ShieldRemoved;
                        amount = old.RemainingBudget;
                    }
                    else if (old == null || !old.Eligible && next.Eligible)
                    {
                        if (!next.Eligible || next.RemainingBudget == 0) continue;
                        kind = TraitActivationKind.ShieldGranted;
                        amount = next.RemainingBudget;
                    }
                    else if (next.LastResetControllerTurnCount > old.LastResetControllerTurnCount)
                    {
                        kind = TraitActivationKind.ShieldReset;
                        amount = Math.Max(0, next.RemainingBudget - old.RemainingBudget);
                    }
                    else continue;
                    outcomes.Add((key, kind, amount, change.Final.EntityId));
                }
            }
            foreach (var group in outcomes.GroupBy(o => (o.Origin, o.Kind)).OrderBy(g => g.Key.Origin).ThenBy(g => g.Key.Kind))
            {
                var amounts = group.Select(o => o.Amount).Distinct().ToArray();
                CombatCommandEvents.Trait(events, new EntityId(group.Key.Origin.OwnerInstanceId), (TraitKey)group.Key.Origin.DefinitionKey,
                    group.Key.Kind, amounts.Length == 1 ? amounts[0] : 0, group.Select(o => o.Recipient).Distinct().OrderBy(id => id).ToArray(), origin: origin);
            }
        }

        /// <summary>
        /// 도발당함이 걸리고 풀린 받는 쪽을 도발 보유자(출처)마다 묶어 <see cref="TraitActivationKind.StatusApplied"/>·
        /// <see cref="TraitActivationKind.StatusRemoved"/>로 냅니다(양 0, 도발 디버프 설계 TT3). 상태 사건은 부르는 쪽이 바뀐 유닛마다 냅니다.
        /// </summary>
        private static void RecordTauntActivations(List<CombatEvent> events, IEnumerable<CharacterStateChange> changes, CombatDamageOrigin origin)
        {
            if (events == null) return;
            var outcomes = new List<(EffectOriginKey Origin, TraitActivationKind Kind, EntityId Recipient)>();
            foreach (var change in changes)
            {
                var before = TauntedOriginsOf(change.Expected);
                var after = TauntedOriginsOf(change.Final);
                foreach (var key in after.Except(before)) outcomes.Add((key, TraitActivationKind.StatusApplied, change.Final.EntityId));
                foreach (var key in before.Except(after)) outcomes.Add((key, TraitActivationKind.StatusRemoved, change.Final.EntityId));
            }
            foreach (var group in outcomes.GroupBy(o => (o.Origin, o.Kind)).OrderBy(g => g.Key.Origin).ThenBy(g => g.Key.Kind))
                CombatCommandEvents.Trait(events, new EntityId(group.Key.Origin.OwnerInstanceId), TraitKey.Taunt, group.Key.Kind, 0,
                    group.Select(o => o.Recipient).Distinct().OrderBy(id => id).ToArray(), origin: origin);
        }

        private static EffectOriginKey[] TauntedOriginsOf(CharacterCombatFacts character) =>
            character.StatusEffects.Entries.OfType<TauntedStatus>().Select(s => s.Origin.Value).ToArray();

        internal static CombatCommandTransition AfterTransition(CombatCommandTransition transition)
        {
            if (!transition.Accepted || transition.Outcome.IsStageEnded) return transition;
            var after = CombatCommandComposition.Project(transition);
            var characters = after.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();
            ApplyAndRecord(after, characters, events);
            if (after.Characters.All(c => ReferenceEquals(c, characters[c.EntityId]))) return transition;
            return CombatCommandComposition.Merge(transition.Input, new[]
            {
                transition, CombatTurnCalculator.Changed(after, characters, after.State, events)
            });
        }

        internal static IReadOnlyList<MovementEffectReceipt> CaptureMovementEffects(MovementEffectId movement,
            IReadOnlyList<CharacterCombatFacts> before, IReadOnlyList<CharacterCombatFacts> after)
        {
            var receipts = new List<MovementEffectReceipt>();
            var swapOrigin = TraitEffectOrigin.Create(movement.ActorId, TraitKey.PositionSwap);
            bool Tracked(ShieldStatus s) => IsPresenceManagedShield(s.Origin.Value) || s.Origin == swapOrigin;
            foreach (var previous in before)
            {
                var next = after.Single(c => c.EntityId == previous.EntityId);
                var oldShields = previous.StatusEffects.Entries.OfType<ShieldStatus>().Where(Tracked).ToDictionary(s => s.Origin.Value);
                var newShields = next.StatusEffects.Entries.OfType<ShieldStatus>().Where(Tracked).ToDictionary(s => s.Origin.Value);
                bool defenseChanged = previous.TraitEffects.CohesionDefenseBonus != next.TraitEffects.CohesionDefenseBonus;
                var defenseOrigin = TraitEffectOrigin.Create(previous.EntityId, TraitKey.Cohesion);
                var keys = oldShields.Keys.Concat(newShields.Keys).Concat(defenseChanged ? new[] { defenseOrigin } : Array.Empty<EffectOriginKey>()).Distinct().OrderBy(k => k);
                foreach (var origin in keys)
                {
                    oldShields.TryGetValue(origin, out var old);
                    newShields.TryGetValue(origin, out var current);
                    if (origin == swapOrigin && (current?.RemainingBudget ?? 0) <= (old?.RemainingBudget ?? 0)) continue;
                    bool defense = defenseChanged && origin == defenseOrigin;
                    if (!defense && (old == null ? current == null : old.SameValues(current))) continue;
                    receipts.Add(new MovementShieldReceipt(movement, previous.EntityId, origin, old, current,
                        defense ? previous.TraitEffects.CohesionDefenseBonus : (int?)null,
                        defense ? next.TraitEffects.CohesionDefenseBonus : (int?)null));
                }
            }
            return receipts;
        }

        internal static bool NormalizeMovementUndo(CombatCommandInput input, IDictionary<EntityId, CharacterCombatFacts> projected,
            IReadOnlyList<MovementEffectReceipt> receipts)
        {
            var changes = Calculate(input, projected.Values.ToArray()).Characters;
            foreach (var change in changes)
            {
                if (!change.Expected.Health.Equals(change.Final.Health)) return false;
                var before = change.Expected.StatusEffects.Entries.OfType<ShieldStatus>().ToDictionary(s => s.Origin.Value);
                var after = change.Final.StatusEffects.Entries.OfType<ShieldStatus>().ToDictionary(s => s.Origin.Value);
                foreach (var origin in before.Keys.Union(after.Keys))
                {
                    before.TryGetValue(origin, out var old);
                    after.TryGetValue(origin, out var next);
                    if (old == null ? next == null : old.SameValues(next)) continue;
                    if (old == null || next == null || old.RemainingBudget != next.RemainingBudget ||
                        old.LastResetControllerTurnCount != next.LastResetControllerTurnCount ||
                        !receipts.OfType<MovementShieldReceipt>().Any(r => r.RecipientId == change.Final.EntityId && r.Origin == origin && r.PreviousStatus != null))
                        return false;
                }
                projected[change.Final.EntityId] = change.Final;
            }
            return true;
        }
    }
}
