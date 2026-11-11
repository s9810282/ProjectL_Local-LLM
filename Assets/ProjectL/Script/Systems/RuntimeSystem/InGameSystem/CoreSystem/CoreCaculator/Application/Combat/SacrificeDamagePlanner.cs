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
            IReadOnlyDictionary<int, EntityId> result = null;
            foreach (var phase in PlanSteps(plan, world, value => result = value, reflection)) { }
            return result;
        }

        /// <summary>폐기할 시뮬레이션을 보관하고 보호 예약이 완성된 뒤에만 호출자에게 넘긴다.</summary>
        internal static IEnumerable<string> PlanSteps(CommandAttackPlan plan, CommandAttackProjection world,
            Action<IReadOnlyDictionary<int, EntityId>> completed, ThornHeartAttackScope reflection = null)
        {
            bool available = false;
            if (plan.Input.State.Turn.ControllerPhase == CombatControllerPhase.Enemy)
            {
                foreach (var character in world.Characters.Values)
                {
                    available = IsSource(character);
                    yield return "SacrificeSource";
                    if (available) break;
                }
            }
            if (!available)
            {
                completed(new ReadOnlyDictionary<int, EntityId>(new Dictionary<int, EntityId>()));
                yield break;
            }
            var hits = new List<Hit>();
            int ordinal = 0;
            for (int i = 0; i < plan.Delivery.Hits.Count; i++)
            {
                yield return "SacrificeHitScan";
                var delivered = plan.Delivery.Hits[i];
                int currentOrdinal = ordinal;
                if (delivered.AppliesDamage) ordinal++;
                if (!delivered.AppliesDamage || delivered.Cause != CombatDamageCause.NumericDamage ||
                    !world.Characters.TryGetValue(delivered.TargetId, out var victim) || victim.Health.IsDead || victim.Area != CharacterRuntimeArea.Field) continue;
                int raw = CombatDamageCalculator.ScaleByTargetDistance(plan.Engine, plan.Bindings, plan.DamageActor, victim.EntityId,
                    victim, world.Structures.Values.ToArray(), delivered.BaseDamage);
                hits.Add(new Hit(i, victim.EntityId, raw, delivered.Role, delivered.AppliedPercent, delivered.ImpactOrigin, currentOrdinal));
            }
            foreach (var phase in ChooseSteps(plan.Input, world.Characters, plan.DamageActor, hits, plan.DamageKind, plan.AttackGroup,
                plan.Environment, plan.Engine, plan.Pipeline, world.AttackGroups, completed, reflection,
                plan.Delivery.PiercingShot != null, plan.TraitScope, plan))
                yield return phase;
        }

        private static bool IsSource(CharacterCombatFacts character) =>
            !character.Health.IsDead && character.Area == CharacterRuntimeArea.Field &&
            character.Stats.TraitStatuses.ToArray().Any(t => t.Key == TraitKey.Sacrifice);

        internal static SacrificeHitUpgrade? ResolveSingle(CombatCommandInput input, Dictionary<EntityId, CharacterCombatFacts> characters,
            CharacterCombatFacts actor, EntityId original, int raw, AttackTargetRole role, int percent, CombatDamageOrigin origin,
            CombatDamageKind kind, MovementEnvironmentFacts environment, CombatEffectEngine engine, TargetDamagePipeline pipeline, CommandAttackGroups groups, AttackTraitScope traitScope = null, List<CombatEvent> events = null,
            CombatProfileReadSet profileReads = null)
        {
            if (!characters.ContainsKey(original)) return null;
            var plan = Choose(input, characters, actor, new[] { new Hit(0, original, raw, role, percent, origin.PhysicalCell, 0) },
                kind, origin.AttackGroup, environment, engine, pipeline, groups, traitScope: traitScope);
            if (!plan.TryGetValue(0, out var source) || !TryConsume(input, characters, actor, original, source, events, origin))
                return null;
            return SacrificeDamageSettlement.Capture(input, characters[source], profileReads);
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
            IReadOnlyDictionary<int, EntityId> result = null;
            foreach (var phase in ChooseSteps(input, characters, actor, hits, kind, group, environment, engine, pipeline,
                groups, value => result = value, reflection, continueAfterReflectionDeath, traitScope, directPlan)) { }
            return result;
        }

        private static IEnumerable<string> ChooseSteps(CombatCommandInput input, Dictionary<EntityId, CharacterCombatFacts> characters,
            CharacterCombatFacts actor, IReadOnlyList<Hit> hits, CombatDamageKind kind, long group, MovementEnvironmentFacts environment,
            CombatEffectEngine engine, TargetDamagePipeline pipeline, CommandAttackGroups groups, Action<IReadOnlyDictionary<int, EntityId>> completed,
            ThornHeartAttackScope reflection = null, bool continueAfterReflectionDeath = false, AttackTraitScope traitScope = null,
            CommandAttackPlan directPlan = null)
        {
            var selected = new Dictionary<int, EntityId>();
            bool available = false;
            if (input.State.Turn.ControllerPhase == CombatControllerPhase.Enemy &&
                (kind == CombatDamageKind.OrdinaryAttack || kind == CombatDamageKind.ExtraAttack || kind == CombatDamageKind.ReactionAttack))
            {
                foreach (var character in characters.Values)
                {
                    available = IsSource(character);
                    yield return "SacrificeEligibility";
                    if (available) break;
                }
            }
            if (!available)
            {
                completed(new ReadOnlyDictionary<int, EntityId>(selected));
                yield break;
            }

            IEnumerable<string> Evaluate(Dictionary<int, EntityId> proposal, Action<Simulation> evaluated)
            {
                Simulation result = null;
                foreach (var phase in SimulateSteps(input, characters, actor, hits, proposal, kind, group, environment, engine,
                    pipeline, groups, reflection, continueAfterReflectionDeath, traitScope, directPlan, value => result = value))
                    yield return phase;
                yield return "SacrificeSimulationComplete";
                // 앞선 보호가 방어 출처를 살리면 뒤의 불필요한 예약을 제거하고 같은 배치를 다시 평가합니다.
                while (result.Valid && result.UnnecessaryRedirects.Count != 0)
                {
                    foreach (int index in result.UnnecessaryRedirects)
                    {
                        proposal.Remove(index);
                        yield return "SacrificeRemoveReservation";
                    }
                    foreach (var phase in SimulateSteps(input, characters, actor, hits, proposal, kind, group, environment,
                        engine, pipeline, groups, reflection, continueAfterReflectionDeath, traitScope, directPlan,
                        value => result = value))
                        yield return phase;
                    yield return "SacrificeRevalidationComplete";
                }
                evaluated(result);
            }

            Simulation baseline = null;
            foreach (var phase in Evaluate(selected, value => baseline = value)) yield return phase;
            yield return "SacrificeBaselineComplete";
            while (true)
            {
                Candidate chosen = null;
                foreach (var hit in hits)
                {
                    yield return "SacrificeCandidateHit";
                    if (selected.ContainsKey(hit.Index) || !baseline.Lethal.Contains(hit.Index)) continue;
                    var before = baseline.Before[hit.Index];
                    var recipient = before.Single(c => c.EntityId == hit.Target);
                    foreach (var source in before)
                    {
                        yield return "SacrificeCandidateSource";
                        if (!IsSource(source)) continue;
                        var binding = Binding(input, source);
                        var bound = new EffectRuleInput(binding.Origin, binding.ExpectedState);
                        int reserved = 0;
                        foreach (var sourceId in selected.Values)
                        {
                            if (sourceId == source.EntityId) reserved++;
                            yield return "SacrificeReservedCount";
                        }
                        if (!((SacrificeEffectRule)binding.Rule).CanProtect(bound, actor, source, recipient,
                            input.State.Turn.ControllerPhase, input.State.Turn.ControllerTurnSequence, reserved)) continue;
                        var proposal = new Dictionary<int, EntityId>();
                        foreach (var entry in selected)
                        {
                            proposal.Add(entry.Key, entry.Value);
                            yield return "SacrificeCopyReservation";
                        }
                        proposal[hit.Index] = source.EntityId;
                        Simulation result = null;
                        foreach (var phase in Evaluate(proposal, value => result = value)) yield return phase;
                        yield return "SacrificeCandidateComplete";
                        if (!result.Valid || !proposal.ContainsKey(hit.Index)) continue;
                        var candidate = new Candidate(hit, source.EntityId, SacrificeTraitRule.Distance(source, recipient),
                            recipient.OwnershipRole == CharacterOwnershipRole.Main ? 0 : 1,
                            result.Final[source.EntityId].Health.CurrentHealth, proposal, result);
                        if (chosen == null || candidate.CompareTo(chosen) < 0) chosen = candidate;
                    }
                }
                if (chosen == null) break;
                selected = chosen.Plan;
                baseline = chosen.Result;
                yield return "SacrificeChoose";
            }
            completed(new ReadOnlyDictionary<int, EntityId>(selected));
        }

        private static IEnumerable<string> SimulateSteps(CombatCommandInput input, Dictionary<EntityId, CharacterCombatFacts> characters,
            CharacterCombatFacts actor, IReadOnlyList<Hit> hits, IReadOnlyDictionary<int, EntityId> redirects, CombatDamageKind kind,
            long group, MovementEnvironmentFacts environment, CombatEffectEngine engine, TargetDamagePipeline pipeline, CommandAttackGroups groups,
            ThornHeartAttackScope reflection, bool continueAfterReflectionDeath, AttackTraitScope traitScope, CommandAttackPlan directPlan,
            Action<Simulation> completed)
        {
            var facts = new Dictionary<EntityId, CharacterCombatFacts>();
            foreach (var entry in characters)
            {
                facts.Add(entry.Key, entry.Value);
                yield return "SacrificeCopyFacts";
            }
            var damage = CombatDamageApplication.For(input, facts, new List<CombatEvent>(), captureMovementUses: false);
            var defenses = groups.Clone();
            var reflected = reflection?.Clone();
            var result = new Simulation(facts);
            foreach (var hit in hits.OrderBy(h => h.Index))
            {
                using (var presence = damage.BeginReconcilePresence())
                {
                    while (presence != null && !presence.IsComplete)
                    {
                        presence.Step();
                        yield return "SacrificePresence";
                    }
                }
                var before = new List<CharacterCombatFacts>();
                foreach (var character in facts.Values)
                {
                    before.Add(character);
                    yield return "SacrificeBeforeHit";
                }
                result.Before.Add(hit.Index, before.ToArray());
                var original = facts[hit.Target];
                bool redirected = redirects.TryGetValue(hit.Index, out var source);
                var currentActor = facts[actor.EntityId].Health.IsDead ? actor : traitScope?.ForDamage(facts[actor.EntityId]) ?? actor;
                // 지키지 않으면 즉사로 죽을 대상도 희생 후보다(치명 타격으로 센다). 실제 즉사 여부는 아래 FiresOnHit가 정한다.
                bool finishingTarget = FinishingShotApplication.Qualifies(input, currentActor, original);
                if (redirected && (original.Health.IsDead || facts[source].Health.IsDead))
                {
                    result.Valid = false;
                    completed(result);
                    yield break;
                }
                if (original.Health.IsDead) continue;
                // 즉사가 걸릴 대상은 숫자 피해로 살아남아도 지켜야 하므로 불필요 판정을 하지 않는다.
                if (redirected && !finishingTarget)
                {
                    var probeFacts = new Dictionary<EntityId, CharacterCombatFacts>();
                    foreach (var entry in facts)
                    {
                        probeFacts.Add(entry.Key, entry.Value);
                        yield return "SacrificeProbeFacts";
                    }
                    var probe = CombatDamageApplication.For(input, probeFacts, new List<CombatEvent>(), captureMovementUses: false);
                    CombatDamageOutcome probed = default;
                    foreach (var phase in ApplyHitSteps(input, currentActor, hit, hit.Target, kind, group, environment, engine,
                        pipeline, defenses.Clone(), probe, traitScope, directPlan, value => probed = value))
                        yield return phase;
                    yield return "SacrificeProbeComplete";
                    if (!probed.After.Health.IsDead)
                    {
                        result.UnnecessaryRedirects.Add(hit.Index);
                        redirected = false;
                    }
                }
                CombatDamageOutcome applied = default;
                if (FinishingShotApplication.FiresOnHit(input, currentActor, hit.Target, redirected ? source : hit.Target, original))
                {
                    foreach (var phase in damage.DefeatSteps(hit.Target, new CombatDamageOrigin(hit.Origin, actor.EntityId, group),
                        value => applied = value))
                        yield return phase;
                }
                else
                {
                    SacrificeHitUpgrade? upgrade = redirected ? SacrificeDamageSettlement.Capture(input, facts[source]) : null;
                    foreach (var phase in ApplyHitSteps(input, currentActor, hit, redirected ? source : hit.Target, kind, group,
                        environment, engine, pipeline, defenses, damage, traitScope, directPlan, value => applied = value, upgrade))
                        yield return phase;
                }
                yield return "SacrificeApplied";
                if (!redirected && applied.After.Health.IsDead) result.Lethal.Add(hit.Index);
                // 실제 공격의 반사 경계와 관통 예외를 같은 폐기 투영에서 사용합니다.
                if (reflected != null && reflected.CanReflect(facts[actor.EntityId], original) &&
                    reflected.TryReflect(facts[actor.EntityId], original, defenses.Next(), damage, out var reflectedHit) &&
                    reflectedHit.TargetChange.Final.Health.IsDead && !continueAfterReflectionDeath) break;
            }
            foreach (int index in redirects.Keys)
            {
                if (!result.Before.ContainsKey(index)) result.UnnecessaryRedirects.Add(index);
                yield return "SacrificeUndelivered";
            }
            completed(result);
        }

        private static IEnumerable<string> ApplyHitSteps(CombatCommandInput input, CharacterCombatFacts actor, Hit hit,
            EntityId recipientId, CombatDamageKind kind, long group, MovementEnvironmentFacts environment,
            CombatEffectEngine engine, TargetDamagePipeline pipeline, CommandAttackGroups defenses, CombatDamageApplication damage, AttackTraitScope traitScope,
            CommandAttackPlan directPlan, Action<CombatDamageOutcome> completed, SacrificeHitUpgrade? sacrificeUpgrade = null)
        {
            var facts = damage.Characters;
            int raw = directPlan == null ? hit.Raw : CombatDamageCalculator.ScaleByTargetDistance(directPlan.Engine, directPlan.Bindings,
                actor, hit.Target, facts[hit.Target], directPlan.Selection.EligibleStructures.ToArray(),
                CombatAttackDelivery.CurrentBaseDamage(directPlan, actor, hit.Index));
            yield return "SacrificeRawDamage";
            var calculated = CombatDamageCalculator.Attack(input, engine, pipeline, actor, facts[recipientId], raw,
                hit.Role, hit.Percent, hit.Origin, null, environment, actualRecipientIndex: hit.Ordinal,
                currentCharacters: facts.Values.ToArray(), damageKind: kind, traitScope: traitScope);
            // 피해와 함께 붙는 효과(방어구 부수기·표식)도 실제 수신자가 받습니다(2026-11-02 결정).
            int amount = calculated.FinalDamage;
            var origin = new CombatDamageOrigin(hit.Origin, actor.EntityId, group);
            yield return "SacrificeDamageCalculation";
            CombatDamageOutcome applied = default;
            foreach (var phase in damage.ApplySteps(recipientId, kind, origin, amount, value => applied = value,
                heldDefenseGroups: defenses, sacrificeUpgrade: sacrificeUpgrade))
                yield return phase;
            ArmorBreakerApplication.AfterHit(input, actor, recipientId, facts, damage.Events, origin);
            WeaknessExposureApplication.CompleteHit(actor, CombatCommandCalculator.RequiredProfile(input, actor), kind, facts, damage.Events, recipientId, origin);
            completed(applied);
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

        /// <summary>기존 거리·메인·보호자 체력·대상·보호자·타격 순번의 우선순위를 유지한다.</summary>
        private sealed class Candidate
        {
            private readonly Hit _hit;
            private readonly EntityId _source;
            private readonly long _distance;
            private readonly int _main;
            private readonly int _hp;
            internal Dictionary<int, EntityId> Plan { get; }
            internal Simulation Result { get; }

            internal Candidate(Hit hit, EntityId source, long distance, int main, int hp,
                Dictionary<int, EntityId> plan, Simulation result)
            {
                _hit = hit;
                _source = source;
                _distance = distance;
                _main = main;
                _hp = hp;
                Plan = plan;
                Result = result;
            }

            internal int CompareTo(Candidate other)
            {
                int comparison = _distance.CompareTo(other._distance);
                if (comparison != 0) return comparison;
                comparison = _main.CompareTo(other._main);
                if (comparison != 0) return comparison;
                comparison = other._hp.CompareTo(_hp);
                if (comparison != 0) return comparison;
                comparison = _hit.Target.CompareTo(other._hit.Target);
                if (comparison != 0) return comparison;
                comparison = _source.CompareTo(other._source);
                return comparison != 0 ? comparison : _hit.Index.CompareTo(other._hit.Index);
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
