using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 처치 연쇄 공격(난사)을 정산한다. 이 공격 명령으로 처치된 적 1명당 맵 전체의 랜덤한 적 캐릭터 하나를 추가 공격하고,
    /// 추가 공격으로 처치하면 1회가 더 생긴다. 기본 공격 결과(AttackResult)는 바꾸지 않는다.
    /// 영혼·사망·승패 정산은 하지 않으며 호출자의 최종 게시(Finish)가 투영 상태로 한 번 처리한다.
    /// </summary>
    internal static class KillChainAttackCalculator
    {
        /// <summary>
        /// 강제 도착 처리가 끝난 투영에서 추가 공격을 진행한다. 미리보기에서는 호출하지 않는다.
        /// 시작 전과 매 타격 전에 공격자 생존·필드 존재·전투 종료·후보 유무를 확인하고, 하나라도 어긋나면 남은 횟수를 취소한다.
        /// 타격마다 발동(CombatEffectsEvent) → 보유 방어 소모 → 체력 사건 순서로 기록한다.
        /// </summary>
        /// <param name="actorId">추가 공격의 공격자다. 반응 공격(포효)에서는 원래 명령의 행동자와 다르다.</param>
        /// <param name="damage">기본 공격과 같은 공통 피해(특성 가산·영혼 공격력 포함)다.</param>
        /// <param name="projection">강제 도착을 처리한 투영이다. 승패 판정·전투 난수·현재 환경을 제공하며 사건 목록을 공유한다.</param>
        /// <param name="projected">투영과 같은 캐릭터 사전이다. 추가 공격의 체력·보유 방어 변경을 여기에 반영한다.</param>
        internal static void Resolve(CombatCommandInput input, EntityId actorId, CombatEffectEngine engine, TargetDamagePipeline pipeline,
            IReadOnlyList<EffectRuleBinding> bindings, int damage, MovementProjection projection,
            Dictionary<EntityId, CharacterCombatFacts> projected, CommandAttackGroups attackGroups,
            Dictionary<EntityId, CellCoord> deathAnchors, CombatHitCause hitCause, ISet<EntityId> deliveredVictims,
            CommandAttackPlan parent = null, CommandAttackProjection world = null)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            if (projected == null) throw new ArgumentNullException(nameof(projected));
            if (attackGroups == null) throw new ArgumentNullException(nameof(attackGroups));
            if (deathAnchors == null) throw new ArgumentNullException(nameof(deathAnchors));
            if (hitCause == null) throw new ArgumentNullException(nameof(hitCause));
            if (deliveredVictims == null) throw new ArgumentNullException(nameof(deliveredVictims));
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            if (!engine.EvaluateKillChainAttack(bindings).HasContribution) return;

            var hostility = new HostilityPolicy();
            Owner owner = projected[actorId].Owner;
            // 미정(설계 §5.1 R9): 끌어오기 중 위험 요소로 죽은 적도 여기서는 처치로 센다(후속 공격은 세지 않음).
            // 끌어오기와 처치 연쇄를 함께 가진 유닛이 생기기 전에 이 귀속을 정한다.
            int remaining = input.Characters.Count(c => !c.Health.IsDead && hostility.AreHostile(owner, c.Owner) &&
                projected[c.EntityId].Health.IsDead);
            var events = projection.Events;
            if (!ReferenceEquals(projected, projection.Damage.Characters))
                throw new ArgumentException("처치 연쇄는 도착 투영과 같은 캐릭터 사전을 써야 합니다.", nameof(projected));

            while (remaining > 0)
            {
                var actor = projected[actorId];
                if (actor.Health.IsDead || actor.Area != CharacterRuntimeArea.Field || projection.Terminal) return;
                // 처치 연쇄는 벽과 무관한 적군 전용 추가 공격이다. 최초 공격의 벽 판정을 다시 적용하지 않는다.
                var candidates = projected.Values.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead &&
                    hostility.AreHostile(actor.Owner, c.Owner)).OrderBy(c => c.EntityId).ToArray();
                if (candidates.Length == 0) return;

                var victim = candidates[projection.ChooseRandom(candidates.Length)];
                deliveredVictims.Add(victim.EntityId);
                remaining--;
                // 처치 연쇄는 원래 공격과 같은 원인이다(G14).
                RoarHitCounter.Record(projected, victim.EntityId, hitCause, input.State.InCombat);
                // 추가 공격은 적 1명만 노리므로 범위 안 적 수를 1로 다시 센다(일점 포격). 가속 포착값은 원래 공격 것을 쓴다.
                var scope = parent?.TraitScope?.WithCapturedEnemyCount(1);
                // 추가 공격 한 번도 공격 행동 한 번이다: 진영 공격 횟수를 올리고 깔끔한 뒷정리·선봉장을 평가한다(2026-11-02 결정).
                if (world != null && scope != null)
                {
                    TraitAttackNotifications.Apply(input, world, scope);
                    actor = projected[actorId];
                }
                var origin = new CombatDamageOrigin(actor.Anchor, actor.EntityId, attackGroups.Next());
                // 처치 중 새 특성 기여를 얻었으면 다음 타격은 현재 투영에서 공격력을 읽습니다.
                int currentDamage = parent != null
                    ? CombatAttackCalculator.CommonAttackDamage(projection.CurrentInput(), actor, parent.Profile, traitScope: scope) : damage;
                actor = scope?.ForDamage(actor) ?? actor;
                var numeric = CombatDamageCalculator.Attack(input, engine, pipeline, actor, victim, currentDamage,
                    AttackTargetRole.Designated, 100, actor.Anchor, null, projection.Environment,
                    currentCharacters: projected.Values.ToArray(), damageKind: CombatDamageKind.ExtraAttack, traitScope: scope);
                int dealt = numeric.FinalDamage;

                int activationIndex = events.Count;
                var recoveryBefore = projection.Damage.CaptureRecovery();
                // 희생 넘기기를 먼저 정한다. 즉사 여부는 넘기기 결과를 본 FiresOnHit 하나가 정한다.
                var sacrificeUpgrade = SacrificeDamagePlanner.ResolveSingle(input, projected, actor, victim.EntityId, currentDamage,
                    AttackTargetRole.Designated, 100, origin, CombatDamageKind.ExtraAttack, projection.Environment,
                    engine, pipeline, attackGroups, scope, events, projection.ProfileReads);
                var recipientId = sacrificeUpgrade?.RecipientId ?? victim.EntityId;
                bool instant = FinishingShotApplication.FiresOnHit(input, actor, victim.EntityId, recipientId, victim);
                if (recipientId != victim.EntityId)
                    numeric = CombatDamageCalculator.Attack(input, engine, pipeline, actor, projected[recipientId], currentDamage,
                        AttackTargetRole.Designated, 100, actor.Anchor, null, projection.Environment,
                        currentCharacters: projected.Values.ToArray(), damageKind: CombatDamageKind.ExtraAttack, traitScope: scope);
                dealt = numeric.FinalDamage;

                // 추가 공격의 발동 위치는 희생 소비를 포함한 모든 방어 정산보다 먼저 포착합니다.
                if (instant) FinishingShotApplication.Record(actor, victim.EntityId, victim.Health.CurrentHealth, victim.OccupiedCells, origin, events);
                else CombatCommandEvents.DamageTraits(events, numeric.TraitContributions, recipientId, projected[recipientId].OccupiedCells, origin);
                if (instant) dealt = victim.Health.CurrentHealth;
                events.Insert(activationIndex, new CombatEffectsEvent(
                    new[]
                    {
                        new TraitActivationSnapshot(actor.EntityId, victim.EntityId, TraitKey.KillChainAttack,
                            TraitActivationKind.KillChainAttack, dealt, targetCells: victim.OccupiedCells, resultOrigin: origin,
                                attackDirection: CombatCommandCalculator.AttackRotation(AttackAim.ForCell(victim.Anchor.Value), actor.Anchor.Value),
                                targetCell: victim.Anchor),
                    },
                    Array.Empty<CategoryActivationSnapshot>()));

                var startEvent = (CombatEffectsEvent)events[activationIndex];
                var before = projected[recipientId];
                var applied = instant ? projection.Damage.Defeat(recipientId, origin) : projection.Damage.Apply(recipientId, CombatDamageKind.ExtraAttack, origin, dealt,
                    usedDustSources: numeric.UsedDustSources, traitScope: scope, damageActor: actor, numericInput: numeric.FinalDamage, heldDefenseGroups: attackGroups,
                    sacrificeUpgrade: sacrificeUpgrade);
                if (!instant && applied.DamageBeforeItems != dealt)
                {
                    var value = startEvent.Traits.Single();
                    var updated = new CombatEffectsEvent(new[] { new TraitActivationSnapshot(value.SourceUnitId, value.TargetId,
                        value.TraitKey, value.TraitActivationKind, applied.DamageBeforeItems, value.TargetIds, value.TargetCells,
                        value.ResultOrigin, value.AttackDirection, value.TargetCell) }, startEvent.Categories);
                    events[events.IndexOf(startEvent)] = updated;
                    startEvent = updated;
                }
                var after = applied.After;
                ArmorBreakerApplication.AfterHit(input, actor, recipientId, projected, events, origin);
                var reaction = recipientId != victim.EntityId ? SacrificeDamagePlanner.Record(events, recipientId, victim.EntityId, applied.ActualHealthLoss, origin) : null;
                int recovery = projection.Damage.CaptureRecovery().GetValueOrDefault(recipientId) - recoveryBefore.GetValueOrDefault(recipientId);
                AttackResultFactory.AttachChild(events, startEvent, AttackResultFactory.SingleOutcome(recipientId, victim.EntityId,
                    before.Health.CurrentHealth, after.Health.CurrentHealth, recovery, origin, reaction));
                WeaknessExposureApplication.CompleteHit(actor, CombatCommandCalculator.RequiredProfile(input, actor),
                    CombatDamageKind.ExtraAttack, projected, events, victim.EntityId, origin);
                TraitContactApplication.ApplyBleeding(actor, bindings, CombatDamageKind.ExtraAttack, projected, events, recipientId, origin);
                LavaCannonApplication.ApplyCaptured(actor, bindings, CombatDamageKind.ExtraAttack, input.State.Turn,
                    projected, events, origin, new[] { victim.EntityId });
                if (after.Health.IsDead)
                {
                    deathAnchors[recipientId] = before.Anchor.Value;
                    remaining++;
                }
            }
        }
    }
}
