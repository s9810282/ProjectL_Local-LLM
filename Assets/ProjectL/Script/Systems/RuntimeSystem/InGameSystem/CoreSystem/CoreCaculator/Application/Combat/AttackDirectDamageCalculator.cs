using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 전달 피격 목록의 직접 피해를 적용한다(S5~S7). 반사 직전과 직접 피해 종료에 대기 체력 사건을 확정하고,
    /// 전달된 장애물 피격은 기존 순서대로 반응·폭발시킨다. 밀치기와 도착은 다루지 않는다.
    /// </summary>
    internal static class AttackDirectDamageCalculator
    {
        internal static bool Apply(CommandAttackPlan plan, CommandAttackProjection world, CommandAttackLedger ledger,
            ISet<EntityId> deliveredVictims, ThornHeartAttackScope reflection = null)
        {
            bool result = false;
            foreach (var phase in ApplySteps(plan, world, ledger, deliveredVictims, reflection, value => result = value)) { }
            return result;
        }

        internal static IEnumerable<string> ApplySteps(CommandAttackPlan plan, CommandAttackProjection world, CommandAttackLedger ledger,
            ISet<EntityId> deliveredVictims, ThornHeartAttackScope reflection, Action<bool> completed)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (ledger == null) throw new ArgumentNullException(nameof(ledger));
            if (deliveredVictims == null) throw new ArgumentNullException(nameof(deliveredVictims));
            ledger.BeginScope(plan, world);
            var actor = plan.DamageActor;

            var structures = plan.Selection.EligibleStructures.ToArray();
            var pendingHealth = new List<(EntityId Id, EntityId Recipient, int Damage, CombatDamageOrigin Origin, bool AppliesDamage,
                CombatDamageCause Cause, int Index, IReadOnlyList<TraitTrailFact> Dust, int Numeric, bool HeldProtected,
                SacrificeHitUpgrade? Sacrifice, EffectOriginKey? EffectOrigin)>();
            var obstacleHits = new List<(EntityId Id, CombatDamageOrigin Origin)>();
            bool reflectedDeath = false;
            int actualRecipientIndex = 0;
            int deliveryIndex = 0;
            int projectileEncounter = 0;
            IReadOnlyDictionary<int, EntityId> redirects = null;
            using (var planning = SacrificeDamagePlanner.PlanSteps(plan, world, value => redirects = value, reflection).GetEnumerator())
            {
                while (true)
                {
                    bool more;
                    using (CoreProfiling.Sample(CoreProfileSection.AttackSacrificePlanning))
                        more = planning.MoveNext();
                    if (!more) break;
                    yield return planning.Current;
                }
            }
            yield return "SacrificePlan";
            bool requiresOrderedHits = TraitPresenceReconciler.RequiresOrderedDamage(world.Characters.Values) ||
                plan.Engine.RequiresOrderedHits(plan.Bindings);
            // 전달 종류와 무관하게 전달 순서대로 피해를 계산한다. 사건 순서도 이 순서를 따른다.
            foreach (var hit in plan.Delivery.Hits)
            {
                yield return "DirectHit";
                if (requiresOrderedHits && pendingHealth.Count > 0) foreach (var phase in FlushHealthSteps(plan, world, ledger, pendingHealth)) yield return phase;
                int index = deliveryIndex++;
                var id = hit.TargetId;
                var recipientId = id;
                // 전달 단계가 대상 자격을 확정합니다. 여기서는 현재 투영에서 대상의 종류와 생존 여부를 읽습니다.
                world.Characters.TryGetValue(id, out var victim);
                var beforeHit = victim;
                // 같은 공격의 앞선 타격(희생으로 대신 맞은 피해 포함)으로 이미 죽은 캐릭터는 맞지 않는다. 희생 계획의 모의 계산과 같은 규칙이다.
                if (!CanDeliverHit(plan, world, hit)) continue;
                if (plan.ProjectileTargetBudget.HasValue && projectileEncounter >= plan.ProjectileTargetBudget.Value) break;
                var currentActor = world.Characters[actor.EntityId];
                actor = currentActor.Health.IsDead ? plan.DamageActor : plan.TraitScope.ForDamage(currentActor);
                var origin = new CombatDamageOrigin(hit.ImpactOrigin, actor.EntityId, plan.AttackGroup);
                bool forcedInstant = hit.AppliesDamage && hit.Cause == CombatDamageCause.InstantDefeat;
                SacrificeHitUpgrade? sacrificeUpgrade = null;
                // 희생 넘기기를 먼저 정한다. 즉사 여부는 넘기기 결과를 본 FiresOnHit 하나가 정한다.
                if (!forcedInstant && redirects.TryGetValue(index, out var source) &&
                    SacrificeDamagePlanner.TryConsume(plan.Input, world.Characters, actor, id, source, world.Events, origin))
                {
                    recipientId = source;
                    sacrificeUpgrade = SacrificeDamageSettlement.Capture(plan.Input, world.Characters[source], world.Damage.ProfileReads);
                }
                if (victim != null && hit.Landed && CombatDamageKinds.IsAttack(plan.DamageKind))
                    world.ThornBombs.RecordAttackHit(plan.AttackGroup, id, recipientId);
                bool obstacle = victim == null && world.Obstacles.HasProfile(id);
                bool finishing = !obstacle && hit.AppliesDamage && FinishingShotApplication.FiresOnHit(plan.Input, actor, id, recipientId,
                    beforeHit, victim == null ? world.Structures[id] : null);
                int contactIndex = plan.ProjectileTargetBudget.HasValue ? projectileEncounter : index;
                bool instant = forcedInstant || finishing;
                var receiver = victim == null ? null : world.Characters[recipientId];
                // 다단 전달도 같은 공격 원인이므로 대상당 한 번만 센다(D7).
                // 위압·아이템 발동 후보는 실제로 맞은 유닛이다. 희생이 대신 맞으면 수호자다(2026-11-02 결정).
                if (victim != null)
                {
                    deliveredVictims.Add(recipientId);
                    if (plan.DamageKind == CombatDamageKind.OrdinaryAttack && receiver.Anchor.HasValue &&
                        !world.ItemDirectHits.ContainsKey(recipientId))
                        world.ItemDirectHits.Add(recipientId, new ItemAttackCandidate(recipientId, receiver.Anchor.Value));
                    RoarHitCounter.Record(world.Characters, id, world.HitCause, plan.Input.State.InCombat);
                }
                if (obstacle)
                {
                    ledger.RecordContact(id, id, contactIndex, hit.Role, hit.AppliedPercent, origin);
                    if (hit.Landed)
                    {
                        obstacleHits.Add((id, origin));
                        actualRecipientIndex++;
                    }
                }
                else if (instant)
                {
                    int healthLoss = victim == null ? world.Structures[id].Health.CurrentHealth : beforeHit.Health.CurrentHealth;
                    EffectOriginKey? finishSource = finishing ? TraitEffectOrigin.Create(actor.EntityId, TraitKey.FinishingShot) : null;
                    if (finishing) FinishingShotApplication.Record(actor, id, healthLoss,
                        beforeHit?.OccupiedCells ?? world.Structures[id].OccupiedCells, origin, world.Events);
                    ledger.RecordContact(id, id, contactIndex, hit.Role, hit.AppliedPercent, origin);
                    ledger.RecordDirectHit(id, hit.Role, TargetDamageBreakdown.CreatePassthrough(0), healthLoss);
                    if (victim != null)
                    {
                        // 재구성이 즉사를 버티면 살아 있으므로 사망 위치를 남기지 않는다.
                        if (world.Damage.Defeat(id, origin, finishSource).After.Health.IsDead) world.DeathAnchors[id] = beforeHit.Anchor.Value;
                    }
                    else pendingHealth.Add((id, id, 0, origin, true, CombatDamageCause.InstantDefeat, index, Array.Empty<TraitTrailFact>(),
                        0, false, null, finishSource));
                    actualRecipientIndex++;
                }
                else
                {
                    var current = CombatAttackDelivery.CurrentDamage(plan, actor, index,
                        plan.ProjectileFalloff ? projectileEncounter : null);
                    var scale = CombatDamageCalculator.ScaleByTargetDistanceDetailed(plan, actor, id, victim, structures, current.Damage);
                    int scaled = scale.Damage;
                    var deliveredTraits = current.Traits.Concat(scale.Traits).ToArray();
                    // 앞선 조우와 남은 생존 후보를 현재 예산 안에서 센다. 이미 정산한 피해는 재계산하지 않는다.
                    var damageScope = plan.ProjectileTargetBudget.HasValue
                        ? plan.TraitScope.WithCapturedEnemyCount(CountProjectileEnemies(plan, world, ledger, index, projectileEncounter))
                        : plan.TraitScope;
                    var damage = CombatDamageCalculator.Attack(plan.Input, plan.Engine, plan.Pipeline, actor, receiver, scaled, hit.Role,
                        hit.AppliedPercent, hit.ImpactOrigin, null, plan.Environment,
                        CombatDamageCalculator.Distance(actor, receiver?.OccupiedCells ?? structures.Single(s => s.EntityId == id).OccupiedCells), actualRecipientIndex,
                        world.Characters.Values.ToArray(), plan.DamageKind, damageScope, victim == null ? world.Structures[id].Health : null,
                        deliveredTraits, plan.BypassPhysicalDefense);
                    if (hit.AppliesDamage)
                        CombatCommandEvents.DamageTraits(world.Events, damage.TraitContributions, recipientId,
                            receiver?.OccupiedCells ?? world.Structures[id].OccupiedCells, origin);
                    if (hit.AppliesDamage) actualRecipientIndex++;
                    // 배지 소비 사건의 기존 순서는 유지하고, 수신 경계는 흡수 전 수치로 노출을 평가합니다.
                    int finalDamage = CombatDamageCalculator.ApplyHeldDefense(world.AttackGroups, world.Characters, recipientId,
                        damage.FinalDamage, origin, world.Events);
                    ledger.RecordContact(recipientId, id, contactIndex, hit.Role, hit.AppliedPercent, origin);
                    ledger.RecordDirectHit(recipientId, hit.Role, damage, finalDamage);
                    pendingHealth.Add((id, recipientId, finalDamage, origin, hit.AppliesDamage, CombatDamageCause.NumericDamage, index,
                        hit.AppliesDamage ? damage.UsedDustSources : Array.Empty<TraitTrailFact>(), hit.AppliesDamage ? damage.FinalDamage : 0,
                        world.AttackGroups.IsProtected(recipientId, origin.AttackGroup), sacrificeUpgrade, null));
                }

                projectileEncounter++;
                if (plan.ProjectileTargetBudget.HasValue)
                {
                    // 투사체는 앞선 구조물의 폭발까지 반영한 생존 상태로 다음 조우를 정한다.
                    foreach (var phase in FlushHealthSteps(plan, world, ledger, pendingHealth)) yield return phase;
                    foreach (var obstacleHit in obstacleHits)
                    {
                        world.Obstacles.ApplyHit(obstacleHit.Id, obstacleHit.Origin);
                        foreach (var phase in world.Obstacles.ResolveExplosionSteps()) yield return phase;
                    }
                    obstacleHits.Clear();
                }
                // 피격 전 아군 사실을 보존하고, 반사 직전에만 지금까지의 체력 사건을 확정합니다.
                if (beforeHit != null && reflection != null && reflection.CanReflect(world.Characters[actor.EntityId], beforeHit))
                {
                    foreach (var phase in FlushHealthSteps(plan, world, ledger, pendingHealth)) yield return phase;
                    if (reflection.TryReflect(world.Characters[actor.EntityId], beforeHit, world.AttackGroups.Next(),
                        world.Damage, out var reflected))
                    {
                        // 반사 피해는 공유 적용 경계가 공격자 사실에 이미 반영했다.
                        var change = reflected.TargetChange;
                        if (change.Final.Health.IsDead)
                        {
                            world.DeathAnchors[actor.EntityId] = change.Expected.Anchor.Value;
                            reflectedDeath = true;
                            // 이미 발사된 관통은 공격 시작 사실로 남은 조우의 피해를 계속 계산합니다.
                            if (plan.Delivery.PiercingShot == null && !plan.ProjectileFalloff) break;
                        }
                    }
                }
            }
            foreach (var phase in FlushHealthSteps(plan, world, ledger, pendingHealth)) yield return phase;
            if (plan.DamageKind == CombatDamageKind.PlayerSkillAttack) ledger.CaptureDirectKills(plan, world);
            ledger.CompleteDirectStage();
            yield return "DirectStageComplete";

            // 장애물 반응은 실제로 전달된 타격 순서로 정산한다.
            foreach (var hit in obstacleHits)
            {
                world.Obstacles.ApplyHit(hit.Id, hit.Origin);
                yield return "ObstacleHit";
                foreach (var phase in world.Obstacles.ResolveExplosionSteps()) yield return phase;
            }
            foreach (var blast in world.Obstacles.BlastDamage)
            {
                ledger.AddBlastIfHit(blast.Key, blast.Value);
                yield return "BlastEvidence";
            }
            world.Obstacles.ResetHitSummaries();
            completed(reflectedDeath);
        }

        private static bool CanDeliverHit(CommandAttackPlan plan, CommandAttackProjection world, DeliveredAttackHit hit) =>
            world.Characters.TryGetValue(hit.TargetId, out var victim)
                ? !victim.Health.IsDead || hit.AppliesDamage && hit.Cause == CombatDamageCause.InstantDefeat
                : !plan.ProjectileTargetBudget.HasValue || !world.Structures[hit.TargetId].Health.IsDead;

        private static int CountProjectileEnemies(CommandAttackPlan plan, CommandAttackProjection world,
            CommandAttackLedger ledger, int hitIndex, int encounters)
        {
            var enemies = ledger.HitIds.SelectMany(id => ledger.Contacts(id))
                .Select(contact => contact.OriginalTargetId).Where(plan.CapturedEnemyIds.Contains).ToHashSet();
            int remaining = plan.ProjectileTargetBudget.Value - encounters;
            for (int i = hitIndex; i < plan.Delivery.Hits.Count && remaining > 0; i++)
            {
                var hit = plan.Delivery.Hits[i];
                if (!CanDeliverHit(plan, world, hit)) continue;
                if (plan.CapturedEnemyIds.Contains(hit.TargetId)) enemies.Add(hit.TargetId);
                remaining--;
            }
            return enemies.Count;
        }

        private static IEnumerable<string> FlushHealthSteps(CommandAttackPlan plan, CommandAttackProjection world, CommandAttackLedger ledger,
            List<(EntityId Id, EntityId Recipient, int Damage, CombatDamageOrigin Origin, bool AppliesDamage, CombatDamageCause Cause, int Index,
                IReadOnlyList<TraitTrailFact> Dust, int Numeric, bool HeldProtected, SacrificeHitUpgrade? Sacrifice, EffectOriginKey? EffectOrigin)> pending)
        {
            foreach (var hit in pending)
            {
                if (world.Characters.ContainsKey(hit.Recipient))
                {
                    CombatDamageOutcome outcome = default;
                    foreach (var phase in world.Damage.ApplySteps(hit.Recipient, plan.DamageKind, hit.Origin, hit.Numeric,
                        value => outcome = value,
                        usedDustSources: hit.Dust, traitScope: hit.AppliesDamage ? plan.TraitScope : null,
                        damageActor: plan.DamageActor, numericInput: hit.Numeric, resolvedHeldProtection: hit.HeldProtected,
                        sacrificeUpgrade: hit.Sacrifice))
                        yield return phase;
                    if (hit.AppliesDamage)
                        TraitContactApplication.Apply(plan, world, hit.Id, hit.Recipient, hit.Origin);
                    ledger.AdjustDirectDamage(hit.Recipient, outcome.RemainingDamage - hit.Damage, outcome.DamageBeforeItems);
                    if (hit.Recipient != hit.Id) ledger.Reactions.Add(SacrificeDamagePlanner.Record(world.Events, hit.Recipient, hit.Id, outcome.ActualHealthLoss, hit.Origin, hit.Index));
                }
                else world.Obstacles.DamageStructure(hit.Id, hit.Damage, hit.Origin, hit.Cause, effectOrigin: hit.EffectOrigin);
                yield return "HealthFlush";
            }
            pending.Clear();
        }


    }
}
