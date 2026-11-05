using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>기존 피해 경계를 폐기할 투영에서 모의 실행하고, 실제 방어 소모 전 수신자만 예약합니다.</summary>
    internal static class SacrificeDamagePlanner
    {
        internal static IReadOnlyDictionary<int, EntityId> Plan(CommandAttackPlan plan, CommandAttackProjection world, ThornHeartAttackScope reflection = null)
        {
            if (plan.Input.State.Turn.ControllerPhase != CombatControllerPhase.Enemy || !world.Characters.Values.Any(c => !c.Health.IsDead && c.Area == CharacterRuntimeArea.Field && c.Stats.TraitStatuses.ToArray().Any(t => t.Key == TraitKey.Sacrifice)))
                return new ReadOnlyDictionary<int, EntityId>(new Dictionary<int, EntityId>());
            var hits = new List<Hit>();
            int ordinal = 0;
            for (int i = 0; i < plan.Delivery.Hits.Count; i++)
            {
                var delivered = plan.Delivery.Hits[i];
                int currentOrdinal = ordinal;
                if (delivered.AppliesDamage) ordinal++;
                if (!delivered.AppliesDamage || delivered.Cause != CombatDamageCause.NumericDamage ||
                    !world.Characters.TryGetValue(delivered.TargetId, out var victim) || victim.Health.IsDead || victim.Area != CharacterRuntimeArea.Field) continue;
                int raw = CombatDamageCalculator.ScaleByTargetDistance(plan.Engine, plan.Bindings, plan.DamageActor, victim.EntityId,
                    victim, world.Structures.Values.ToArray(), delivered.BaseDamage);
                hits.Add(new Hit(i, victim.EntityId, raw, delivered.Role, delivered.AppliedPercent, delivered.ImpactOrigin, currentOrdinal));
            }
            return Choose(plan.Input, world.Characters, plan.DamageActor, hits, plan.DamageKind, plan.AttackGroup,
                plan.Environment, plan.Engine, plan.Pipeline, world.AttackGroups, reflection, plan.Delivery.PiercingShot != null, plan.TraitScope, plan);
        }

        internal static EntityId ResolveSingle(CombatCommandInput input, Dictionary<EntityId, CharacterCombatFacts> characters,
            CharacterCombatFacts actor, EntityId original, int raw, AttackTargetRole role, int percent, CombatDamageOrigin origin,
            CombatDamageKind kind, MovementEnvironmentFacts environment, CombatEffectEngine engine, TargetDamagePipeline pipeline, CommandAttackGroups groups, AttackTraitScope traitScope = null, List<CombatEvent> events = null)
        {
            if (!characters.ContainsKey(original)) return original;
            var plan = Choose(input, characters, actor, new[] { new Hit(0, original, raw, role, percent, origin.PhysicalCell, 0) },
                kind, origin.AttackGroup, environment, engine, pipeline, groups, traitScope: traitScope);
            return plan.TryGetValue(0, out var source) && TryConsume(input, characters, actor, original, source, events, origin) ? source : original;
        }

        internal static bool TryConsume(CombatCommandInput input, IDictionary<EntityId, CharacterCombatFacts> characters,
            CharacterCombatFacts actor, EntityId original, EntityId sourceId, List<CombatEvent> events = null, CombatDamageOrigin origin = null)
        {
            if (!characters.TryGetValue(sourceId, out var source) || !characters.TryGetValue(original, out var recipient)) return false;
            if (source.Health.IsDead || source.Area != CharacterRuntimeArea.Field || !source.Stats.TraitStatuses.ToArray().Any(t => t.Key == TraitKey.Sacrifice)) return false;
            var binding = Binding(input, source);
            var bound = new EffectRuleInput(binding.Origin, binding.ExpectedState);
            var rule = (SacrificeEffectRule)binding.Rule;
            if (!rule.CanProtect(bound, actor, source, recipient, input.State.Turn.ControllerPhase, input.State.Turn.ControllerTurnSequence)) return false;
            var status = rule.Consume(bound, input.State.Turn.ControllerTurnSequence);
            characters[sourceId] = source.With(stats: source.Stats.With(traitStatuses: source.Stats.TraitStatuses.ToArray()
                .Select(t => t.Key == TraitKey.Sacrifice ? status : t).ToArray()));
            if (events != null) CombatCommandEvents.TraitState(events, source, characters[sourceId], origin);
            return true;
        }

        internal static TraitActivationSnapshot Record(List<CombatEvent> events, EntityId source, EntityId original, int hpLoss, CombatDamageOrigin origin, int deliveryIndex = 0)
        {
            int index = events.FindLastIndex(value => value is CombatTraitStateEvent state && state.Final.EntityId == source &&
                ReferenceEquals(state.Origin, origin) &&
                state.Expected.Stats.TraitStatuses.ToArray().OfType<SacrificeTraitStatusSnapshot>().SingleOrDefault() is { } expected &&
                state.Final.Stats.TraitStatuses.ToArray().OfType<SacrificeTraitStatusSnapshot>().SingleOrDefault() is { } final &&
                !expected.SameValues(final));
            if (index < 0) throw new InvalidOperationException("희생 적용에는 동일한 소비 상태 결과가 필요합니다.");
            return new TraitActivationSnapshot(source, original, TraitKey.Sacrifice,
                TraitActivationKind.DamageRedirected, hpLoss, resultOrigin: origin)
                .WithInterception(new AttackInterceptionSnapshot(source, original, source, deliveryIndex));
        }

        private static IReadOnlyDictionary<int, EntityId> Choose(CombatCommandInput input, Dictionary<EntityId, CharacterCombatFacts> characters,
            CharacterCombatFacts actor, IReadOnlyList<Hit> hits, CombatDamageKind kind, long group, MovementEnvironmentFacts environment,
            CombatEffectEngine engine, TargetDamagePipeline pipeline, CommandAttackGroups groups,
            ThornHeartAttackScope reflection = null, bool continueAfterReflectionDeath = false, AttackTraitScope traitScope = null,
            CommandAttackPlan directPlan = null)
        {
            var selected = new Dictionary<int, EntityId>();
            if (input.State.Turn.ControllerPhase != CombatControllerPhase.Enemy ||
                (kind != CombatDamageKind.OrdinaryAttack && kind != CombatDamageKind.ExtraAttack && kind != CombatDamageKind.ReactionAttack) ||
                !characters.Values.Any(c => !c.Health.IsDead && c.Area == CharacterRuntimeArea.Field &&
                    c.Stats.TraitStatuses.ToArray().Any(t => t.Key == TraitKey.Sacrifice)))
                return new ReadOnlyDictionary<int, EntityId>(selected);

            Simulation Evaluate(Dictionary<int, EntityId> proposal)
            {
                var result = Simulate(input, characters, actor, hits, proposal, kind, group, environment, engine, pipeline, groups,
                    reflection, continueAfterReflectionDeath, traitScope, directPlan);
                // 앞선 보호가 방어 출처를 살리면 뒤의 불필요한 예약을 제거하고 같은 배치를 다시 평가합니다.
                while (result.Valid && result.UnnecessaryRedirects.Count != 0)
                {
                    foreach (int index in result.UnnecessaryRedirects) proposal.Remove(index);
                    result = Simulate(input, characters, actor, hits, proposal, kind, group, environment, engine, pipeline, groups,
                        reflection, continueAfterReflectionDeath, traitScope, directPlan);
                }
                return result;
            }

            var baseline = Evaluate(selected);
            while (true)
            {
                var candidates = new List<(Hit Hit, EntityId Source, long Distance, int Main, int Hp, Dictionary<int, EntityId> Plan, Simulation Result)>();
                foreach (var hit in hits.Where(h => !selected.ContainsKey(h.Index) && baseline.Lethal.Contains(h.Index)))
                {
                    var before = baseline.Before[hit.Index];
                    var recipient = before.Single(c => c.EntityId == hit.Target);
                    foreach (var source in before.Where(c => !c.Health.IsDead && c.Area == CharacterRuntimeArea.Field &&
                        c.Stats.TraitStatuses.ToArray().Any(t => t.Key == TraitKey.Sacrifice)))
                    {
                        var binding = Binding(input, source);
                        var bound = new EffectRuleInput(binding.Origin, binding.ExpectedState);
                        int reserved = selected.Values.Count(id => id == source.EntityId);
                        if (!((SacrificeEffectRule)binding.Rule).CanProtect(bound, actor, source, recipient,
                            input.State.Turn.ControllerPhase, input.State.Turn.ControllerTurnSequence, reserved)) continue;
                        var proposal = new Dictionary<int, EntityId>(selected) { [hit.Index] = source.EntityId };
                        var result = Evaluate(proposal);
                        if (!result.Valid || !proposal.ContainsKey(hit.Index)) continue;
                        candidates.Add((hit, source.EntityId, SacrificeTraitRule.Distance(source, recipient),
                            recipient.OwnershipRole == CharacterOwnershipRole.Main ? 0 : 1,
                            result.Final[source.EntityId].Health.CurrentHealth, proposal, result));
                    }
                }
                if (candidates.Count == 0) break;
                var chosen = candidates.OrderBy(c => c.Distance).ThenBy(c => c.Main).ThenByDescending(c => c.Hp)
                    .ThenBy(c => c.Hit.Target).ThenBy(c => c.Source).ThenBy(c => c.Hit.Index).First();
                selected = chosen.Plan;
                baseline = chosen.Result;
            }
            return new ReadOnlyDictionary<int, EntityId>(selected);
        }

        private static Simulation Simulate(CombatCommandInput input, Dictionary<EntityId, CharacterCombatFacts> characters,
            CharacterCombatFacts actor, IReadOnlyList<Hit> hits, IReadOnlyDictionary<int, EntityId> redirects, CombatDamageKind kind,
            long group, MovementEnvironmentFacts environment, CombatEffectEngine engine, TargetDamagePipeline pipeline, CommandAttackGroups groups,
            ThornHeartAttackScope reflection, bool continueAfterReflectionDeath, AttackTraitScope traitScope, CommandAttackPlan directPlan)
        {
            var facts = characters.ToDictionary(p => p.Key, p => p.Value);
            var damage = CombatDamageApplication.For(input, facts, new List<CombatEvent>(), captureMovementUses: false);
            var defenses = groups.Clone();
            var reflected = reflection?.Clone();
            var result = new Simulation(facts);
            foreach (var hit in hits.OrderBy(h => h.Index))
            {
                TraitPresenceReconciler.Apply(input, facts);
                result.Before.Add(hit.Index, facts.Values.ToArray());
                var original = facts[hit.Target];
                bool redirected = redirects.TryGetValue(hit.Index, out var source);
                var currentActor = facts[actor.EntityId].Health.IsDead ? actor : traitScope?.ForDamage(facts[actor.EntityId]) ?? actor;
                bool instant = FinishingShotApplication.Qualifies(input, currentActor, original);
                if (instant && redirected) { result.UnnecessaryRedirects.Add(hit.Index); redirected = false; }
                if (redirected && (original.Health.IsDead || facts[source].Health.IsDead))
                {
                    result.Valid = false;
                    return result;
                }
                if (original.Health.IsDead) continue;
                if (redirected)
                {
                    var probe = CombatDamageApplication.For(input, facts.ToDictionary(p => p.Key, p => p.Value), new List<CombatEvent>(), captureMovementUses: false);
                    if (!ApplyHit(input, currentActor, hit, hit.Target, kind, group, environment, engine, pipeline, defenses.Clone(), probe, traitScope, directPlan).After.Health.IsDead)
                    {
                        result.UnnecessaryRedirects.Add(hit.Index);
                        redirected = false;
                    }
                }
                var applied = instant ? damage.Defeat(hit.Target, new CombatDamageOrigin(hit.Origin, actor.EntityId, group)) :
                    ApplyHit(input, currentActor, hit, redirected ? source : hit.Target, kind, group, environment, engine, pipeline, defenses, damage, traitScope, directPlan);
                if (!instant && !redirected && applied.After.Health.IsDead) result.Lethal.Add(hit.Index);
                // 실제 공격의 반사 경계와 관통 예외를 같은 폐기 투영에서 사용합니다.
                if (reflected != null && reflected.CanReflect(facts[actor.EntityId], original) &&
                    reflected.TryReflect(facts[actor.EntityId], original, defenses.Next(), damage, out var reflectedHit) &&
                    reflectedHit.TargetChange.Final.Health.IsDead && !continueAfterReflectionDeath) break;
            }
            foreach (int index in redirects.Keys.Where(index => !result.Before.ContainsKey(index))) result.UnnecessaryRedirects.Add(index);
            return result;
        }

        private static CombatDamageOutcome ApplyHit(CombatCommandInput input, CharacterCombatFacts actor, Hit hit,
            EntityId recipientId, CombatDamageKind kind, long group, MovementEnvironmentFacts environment,
            CombatEffectEngine engine, TargetDamagePipeline pipeline, CommandAttackGroups defenses, CombatDamageApplication damage, AttackTraitScope traitScope,
            CommandAttackPlan directPlan)
        {
            var facts = damage.Characters;
            int raw = directPlan == null ? hit.Raw : CombatDamageCalculator.ScaleByTargetDistance(directPlan.Engine, directPlan.Bindings,
                actor, hit.Target, facts[hit.Target], directPlan.Selection.EligibleStructures.ToArray(),
                CombatAttackDelivery.CurrentBaseDamage(directPlan, actor, hit.Index));
            var calculated = CombatDamageCalculator.Attack(input, engine, pipeline, actor, facts[recipientId], raw,
                hit.Role, hit.Percent, hit.Origin, null, environment, actualRecipientIndex: hit.Ordinal,
                currentCharacters: facts.Values.ToArray(), damageKind: kind, traitScope: traitScope);
            // 피해와 함께 붙는 효과(방어구 부수기·표식)도 실제 수신자가 받습니다(2026-11-02 결정).
            int amount = calculated.FinalDamage;
            var origin = new CombatDamageOrigin(hit.Origin, actor.EntityId, group);
            var applied = damage.Apply(recipientId, kind, origin, amount, heldDefenseGroups: defenses);
            ArmorBreakerApplication.AfterHit(input, actor, recipientId, facts, damage.Events, origin);
            WeaknessExposureApplication.CompleteHit(actor, CombatCommandCalculator.RequiredProfile(input, actor), kind, facts, damage.Events, recipientId, origin);
            return applied;
        }

        private static EffectRuleBinding Binding(CombatCommandInput input, CharacterCombatFacts source) =>
            CombatCommandEffects.Capture(source, CombatCommandCalculator.RequiredProfile(input, source)).Single(b => b.Rule is SacrificeEffectRule);

        private readonly struct Hit
        {
            internal int Index { get; }
            internal EntityId Target { get; }
            internal int Raw { get; }
            internal AttackTargetRole Role { get; }
            internal int Percent { get; }
            internal CellCoord? Origin { get; }
            internal int Ordinal { get; }

            internal Hit(int index, EntityId target, int raw, AttackTargetRole role, int percent, CellCoord? origin, int ordinal)
            {
                Index = index;
                Target = target;
                Raw = raw;
                Role = role;
                Percent = percent;
                Origin = origin;
                Ordinal = ordinal;
            }
        }

        private sealed class Simulation
        {
            internal Dictionary<EntityId, CharacterCombatFacts> Final { get; }
            internal Dictionary<int, CharacterCombatFacts[]> Before { get; } = new();
            internal HashSet<int> Lethal { get; } = new();
            internal HashSet<int> UnnecessaryRedirects { get; } = new();
            internal bool Valid { get; set; } = true;

            internal Simulation(Dictionary<EntityId, CharacterCombatFacts> final)
            {
                Final = final;
            }
        }
    }
}
