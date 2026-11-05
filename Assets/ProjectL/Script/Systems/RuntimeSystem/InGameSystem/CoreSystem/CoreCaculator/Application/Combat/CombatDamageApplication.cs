using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 한 명령 투영이 공유하는 캐릭터 피해 적용 경계다. 아이템 단계(피해 직전·직후), 체력 적용, 사망 때 슬롯 폐기,
    /// 회복 기록을 맡고 바뀐 사실을 공유 사전에 직접 반영한다. 피해량 계산과 호출 순서는 피해 경로가 정한다.
    /// 같은 사전을 쓰는 투영(공격·도착·장애물 반응)은 한 인스턴스를 공유해야 회복 기록이 갈라지지 않는다.
    /// 스테이지 기록의 받은·넣은 피해 원천이기도 하다. 이 경계를 지나지 않는 HP 감소(추락 등)는 기록되지 않는다.
    /// </summary>
    internal sealed class CombatDamageApplication
    {
        private readonly IReadOnlyList<ItemDefinition> _definitions;
        private readonly CombatCommandInput _traitInput;
        private readonly Dictionary<CharacterInstanceId, OwnedCharacterChange> _ownedChanges = new();
        internal ISet<EntityId> SettledTraitDeaths { get; } = new HashSet<EntityId>();
        private readonly Dictionary<EntityId, long> _recovery = new();
        private readonly Dictionary<EntityId, long> _appliedDamage = new();
        private readonly List<CombatStageDamageEntry> _stageDamage = new();

        private readonly bool _captureMovementUses;
        internal List<MovementDamageExecution> DamageExecutions { get; } = new();
        internal IReadOnlyList<MovementEffectUse> MovementEffectUses => MovementDamageExecution.Collect(DamageExecutions);
        internal CombatProfileReadSet ProfileReads { get; }
        internal int PreparationIndex { get; }
        internal Dictionary<EntityId, CharacterCombatFacts> Characters { get; }
        internal List<CombatEvent> Events { get; }

        internal CombatDamageApplication(IReadOnlyList<ItemDefinition> definitions, int preparationIndex,
            Dictionary<EntityId, CharacterCombatFacts> characters, List<CombatEvent> events, CombatCommandInput traitInput = null,
            bool captureMovementUses = true, CombatProfileReadSet profileReads = null)
        {
            _traitInput = traitInput;
            _captureMovementUses = captureMovementUses;
            ProfileReads = profileReads;
            if (preparationIndex < 0) throw new ArgumentOutOfRangeException(nameof(preparationIndex));
            _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            PreparationIndex = preparationIndex;
            Characters = characters ?? throw new ArgumentNullException(nameof(characters));
            Events = events ?? throw new ArgumentNullException(nameof(events));
        }

        /// <summary>명령 입력의 정의 목록과 준비 순번으로 만든다. 입력과 다른 상태로 진행하는 투영은 생성자를 직접 쓴다.</summary>
        internal static CombatDamageApplication For(CombatCommandInput input,
            Dictionary<EntityId, CharacterCombatFacts> characters, List<CombatEvent> events, bool captureMovementUses = true,
            CombatProfileReadSet profileReads = null)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            return new CombatDamageApplication(input.ItemDefinitions, input.State.Turn.PreparationIndex, characters, events, input, captureMovementUses, profileReads);
        }

        /// <summary>
        /// 아이템 외 계산을 모두 마친 피해 1회를 적용한다. 공격자는 출처의 가해 캐릭터다.
        /// 소지자와 효과 대상의 최종 사실은 공유 사전에 반영되며, 호출자는 결과의 After만 읽는다.
        /// </summary>
        internal CombatDamageOutcome Apply(EntityId recipientId, CombatDamageKind kind, CombatDamageOrigin origin,
            int damageBeforeItems, IReadOnlyList<CombatDamageOrigin> eventOrigins = null, bool deferTraitKills = false,
            IReadOnlyList<TraitTrailFact> usedDustSources = null, AttackTraitScope traitScope = null, CharacterCombatFacts damageActor = null,
            int? numericInput = null, CommandAttackGroups heldDefenseGroups = null, bool resolvedHeldProtection = false,
            IReadOnlyList<(CombatDamageOrigin Origin, int Input)> resolvedHeldAbsorptions = null)
        {
            if (origin == null) throw new ArgumentNullException(nameof(origin));
            if (_traitInput != null) TraitPresenceReconciler.ApplyAndRecord(_traitInput, Characters, Events, origin, ProfileReads);
            int eventStart = Events.Count;
            var before = Characters[recipientId];
            int numeric = numericInput ?? damageBeforeItems;
            CharacterCombatFacts attacker = origin.CreditedCharacterId is EntityId attackerId &&
                Characters.TryGetValue(attackerId, out var source) ? source : null;
            if (before.Health.IsDead) return new CombatDamageOutcome(before, before, damageBeforeItems, 0, 0);

            // 공격은 대상 산식의 비율 단계에서 처리합니다. 환경·반사는 같은 엔진을 이 공통 경계에서 한 번 통과합니다.
            if (_traitInput != null && kind != CombatDamageKind.OrdinaryAttack && kind != CombatDamageKind.ExtraAttack && kind != CombatDamageKind.ReactionAttack)
            {
                var incoming = new CombatEffectEngine().EvaluateIncomingDamage(
                    CombatCommandEffects.CaptureIncoming(_traitInput, Characters.Values.ToArray(), before, profileReads: ProfileReads),
                    new IncomingDamageEffectContext(damageBeforeItems, kind, before, Characters.Values.ToArray(), origin.CreditedCharacterId));
                CombatCommandEvents.DamageTraits(Events, CombatDamageCalculator.IncomingEvidence(incoming, damageBeforeItems),
                    recipientId, before.OccupiedCells, origin);
                damageBeforeItems = incoming.FinalDamage;
            }
            // 실제 수신자가 정해지고 비아이템 방어가 끝난 뒤 한 번만 증폭합니다.
            int exposureInput = damageBeforeItems;
            var exposure = WeaknessExposureRules.Evaluate(before.StatusEffects);
            bool usesExposure = damageBeforeItems > 0;
            damageBeforeItems = exposure.Amplify(damageBeforeItems);
            if (heldDefenseGroups != null)
            {
                damageBeforeItems = CombatDamageCalculator.ApplyHeldDefense(heldDefenseGroups, Characters, recipientId, damageBeforeItems, origin, Events);
                before = Characters[recipientId];
            }
            else if (resolvedHeldProtection) damageBeforeItems = 0;
            var context = new ItemDamageContext(kind, attacker, before, damageBeforeItems, PreparationIndex);
            int remainingDamage = damageBeforeItems;
            var recipient = before;
            CharacterStateChange affectedTarget = null;
            var definition = CombatItemResolver.EquippedDefinition(_definitions, recipient);
            if (definition != null)
            {
                var decision = CombatItemResolver.BeforeDamage(definition, context);
                if (decision.Activated)
                {
                    remainingDamage = decision.RemainingDamage ?? damageBeforeItems;
                    recipient = ItemCharacterChanges.ApplyDecision(recipient, recipient, decision, out bool added)[0].Final;
                    ItemCharacterChanges.AppendDecisionEvent(Events, definition, recipientId, decision, ItemEventStage.BeforeDamage,
                        kind, damageBeforeItems - remainingDamage, added);
                }
            }

            var protectedStatuses = ShieldStatusRules.Absorb(recipient.StatusEffects, remainingDamage, out remainingDamage);
            var protectedEffects = CharacterStatusCompatibility.ProjectTraitEffects(protectedStatuses, recipient.TraitEffects.CohesionDefenseBonus);
            if (!protectedEffects.SameValues(recipient.TraitEffects))
            {
                foreach (var prior in recipient.TraitEffects.Shields)
                {
                    var final = protectedEffects.Shields.Single(g => g.Origin == prior.Origin);
                    int absorbed = prior.RemainingBudget - final.RemainingBudget;
                    if (absorbed > 0 && prior.Origin.Kind == EffectOwnerKind.Trait)
                        CombatCommandEvents.Trait(Events, new EntityId(prior.Origin.OwnerInstanceId), (TraitKey)prior.Origin.DefinitionKey,
                            TraitActivationKind.ShieldAbsorbed, absorbed, new[] { recipientId }, origin: origin);
                }
                var protectedRecipient = recipient.WithStatusEffects(protectedStatuses);
                CombatCommandEvents.TraitState(Events, recipient, protectedRecipient, origin);
                recipient = protectedRecipient;
            }
            int actualHealthLoss = Math.Min(recipient.Health.CurrentHealth, remainingDamage);
            var damaged = actualHealthLoss == 0 ? recipient
                : actualHealthLoss == recipient.Health.CurrentHealth ? CombatCommandCalculator.Hurt(recipient, remainingDamage)
                : CharacterSettlementCalculator.Copy(recipient,
                    new HealthStateSnapshot(recipient.Health.CurrentHealth - actualHealthLoss, recipient.Health.MaxHealth), recipient.Stats);
            var healthOrigins = eventOrigins ?? new[] { origin };
            CombatCommandEvents.Health(Events, recipient, damaged, healthOrigins);
            // 체력 사건과 같은 출처 목록을 남겨, 넣은 피해의 귀속이 처치 귀속과 같은 출처를 보게 한다(스테이지 기록 설계 D4).
            if (actualHealthLoss > 0)
                _stageDamage.Add(new CombatStageDamageEntry(recipientId, healthOrigins, actualHealthLoss));
            definition = CombatItemResolver.EquippedDefinition(_definitions, damaged);
            recipient = damaged;
            int recovery = 0;
            if (definition != null)
            {
                var decision = CombatItemResolver.AfterDamage(definition, new ItemAfterDamageContext(context, damaged, actualHealthLoss));
                if (decision.Activated)
                {
                    var target = decision.TargetId == recipientId ? recipient : Characters[decision.TargetId.Value];
                    var changes = ItemCharacterChanges.ApplyDecision(recipient, target, decision, out bool added);
                    foreach (var change in changes)
                        if (change.Final.EntityId == recipientId) recipient = change.Final;
                        else affectedTarget = change;
                    recipient = CharacterSettlementCalculator.Recover(recipient, decision.HealAmount, out recovery);
                    var finalTarget = decision.TargetId == recipientId ? recipient : affectedTarget?.Final ?? target;
                    int magnitude = decision.TemporaryEffect == null ? recovery :
                        CombatEffectiveStatsCalculator.ItemReduction(target, finalTarget, decision.TemporaryEffect.Stat);
                    ItemCharacterChanges.AppendDecisionEvent(Events, definition, recipientId, decision, ItemEventStage.AfterDamage,
                        kind, magnitude, added);
                    CombatCommandEvents.Health(Events, damaged, recipient);
                }
            }
            recipient = DiscardOnDeath(recipient);

            if (!ReferenceEquals(before, recipient)) Characters[recipientId] = recipient;
            if (affectedTarget != null) Characters[affectedTarget.Final.EntityId] = affectedTarget.Final;
            RecordRecovery(recipientId, recovery);
            if (remainingDamage > 0) _appliedDamage[recipientId] = checked(_appliedDamage.GetValueOrDefault(recipientId) + remainingDamage);
            // 후속 귀속 문맥을 만드는 호출자는 문맥 설치 뒤 같은 정산 경계를 호출합니다.
            if (!deferTraitKills) SettleTraitKills(eventStart);
            if (_traitInput != null) TraitPresenceReconciler.ApplyAndRecord(_traitInput, Characters, Events, origin, ProfileReads);
            RecordMovementUses(before, origin, kind, numeric, exposureInput, exposure, usedDustSources,
                traitScope, damageActor, eventStart);
            // 여러 가시 출처는 기존의 개별 배지 처리와 단일 합산 피해를 유지합니다. 막힌 양수 입력도 같은 노출 선택 근거를 사용합니다.
            foreach (var absorbed in resolvedHeldAbsorptions ?? Array.Empty<(CombatDamageOrigin Origin, int Input)>())
                RecordMovementUses(before, absorbed.Origin, kind, absorbed.Input, absorbed.Input, exposure, null, null, null, eventStart);
            return new CombatDamageOutcome(before, Characters[recipientId], remainingDamage, actualHealthLoss, recovery,
                damageBeforeItems, usesExposure ? exposure : null);
        }


        private void RecordMovementUses(CharacterCombatFacts recipient, CombatDamageOrigin origin, CombatDamageKind kind,
            int numeric, int exposureInput, WeaknessExposureEvaluation exposure, IReadOnlyList<TraitTrailFact> dust,
            AttackTraitScope scope, CharacterCombatFacts actor, int eventStart)
        {
            if (!_captureMovementUses || _traitInput == null) return;
            var execution = Events.Skip(eventStart).FirstOrDefault(e => MovementUndoPolicy.MatchesDamage(e, origin, recipient.EntityId)) ??
                Events.LastOrDefault(e => MovementUndoPolicy.MatchesDamage(e, origin, recipient.EntityId));
            if (execution == null) return;
            var uses = new List<MovementEffectUse>();
            if (exposureInput > 0)
                foreach (var contribution in exposure.UsedSources.Where(s => s.Movement.HasValue))
                    uses.Add(new MovementEffectUse(contribution.Movement, contribution.Origin, recipient.EntityId,
                        MovementEffectUseKind.ExposureDamage, execution, origin, consumerBefore: recipient,
                        numericInput: exposureInput, damageKind: kind, capturedAmount: contribution.IncreasePercent));
            foreach (var trail in dust ?? Array.Empty<TraitTrailFact>())
                if (numeric > 0) uses.Add(new MovementEffectUse(MovementUndoPolicy.TrailMovement(_traitInput, trail.Id), trail.Origin,
                    recipient.EntityId, MovementEffectUseKind.DustDefense, execution, origin, trail.Id, recipient, numeric, kind));
            if (scope?.CapturedAcceleration > 0 && actor != null && numeric > 0 &&
                _traitInput.State.MovementUndo.TryGet(actor.EntityId, out var checkpoint) &&
                checkpoint.Effects.Any(e => e is MovementAccelerationReceipt))
                uses.Add(new MovementEffectUse(checkpoint.Movement,
                    new EffectOriginKey(EffectOwnerKind.Trait, actor.EntityId.Value, (int)TraitKey.Acceleration, "main"),
                    actor.EntityId, MovementEffectUseKind.AccelerationDamage, execution, origin, consumerBefore: actor,
                    numericInput: numeric, damageKind: kind, capturedAmount: scope.CapturedAcceleration));
            var evidence = Events.Skip(eventStart).Where(e => MovementUndoPolicy.MatchesDamage(e, origin, recipient.EntityId))
                .Concat(Events.Take(eventStart).Where(e => e is not CombatItemEvent &&
                MovementUndoPolicy.MatchesDamage(e, origin, recipient.EntityId))).Distinct().ToArray();
            if (!evidence.Contains(execution)) evidence = evidence.Append(execution).ToArray();
            DamageExecutions.Add(new MovementDamageExecution(evidence, uses));
        }

        /// <summary>숫자 피해·아이템 피격·보호막을 거치지 않고 실제 HP를 사망 사실로 바꿉니다.</summary>
        internal CombatDamageOutcome Defeat(EntityId recipientId, CombatDamageOrigin origin)
        {
            if (origin == null) throw new ArgumentNullException(nameof(origin));
            var before = Characters[recipientId];
            if (before.Health.IsDead) return new CombatDamageOutcome(before, before, 0, 0, 0);
            int eventStart = Events.Count;
            var final = DiscardOnDeath(CombatCommandCalculator.Hurt(before, before.Health.CurrentHealth));
            Characters[recipientId] = final;
            Events.Add(new CombatCharacterEvent(CombatEventKind.Died, before, final, new[] { origin }, damageCause: CombatDamageCause.InstantDefeat));
            _stageDamage.Add(new CombatStageDamageEntry(recipientId, new[] { origin }, before.Health.CurrentHealth));
            SettleTraitKills(eventStart);
            if (_traitInput != null) TraitPresenceReconciler.ApplyAndRecord(_traitInput, Characters, Events, origin, ProfileReads);
            return new CombatDamageOutcome(before, Characters[recipientId], 0, before.Health.CurrentHealth, 0);
        }

        private CharacterCombatFacts DiscardOnDeath(CharacterCombatFacts recipient)
        {
            if (!recipient.Health.IsDead || recipient.Items.EquippedItemKey is not int discarded) return recipient;
            var final = ItemLifetimeCalculator.DiscardEquipment(recipient);
            Events.Add(new CombatItemEvent(discarded, recipient.EntityId, null, ItemEventStage.Lifecycle, ItemEventReason.Death, 0, discarded, null));
            return final;
        }

        /// <summary>한 명령의 보유 변경 제안입니다. 실제 설치는 기존 Buffer 게시 경계만 합니다.</summary>
        internal IReadOnlyList<OwnedCharacterChange> OwnedChanges => _ownedChanges.Values.OrderBy(c => c.Final.Id).ToArray();
        internal IReadOnlyList<OwnedCharacterRecord> ProjectOwned(IReadOnlyList<OwnedCharacterRecord> original) => original
            .Select(o => _ownedChanges.TryGetValue(o.Id, out var change) ? change.Final : o).ToArray();
        internal OwnedCharacterRecord OwnedRecord(CharacterInstanceId id) => _ownedChanges.TryGetValue(id, out var change) ? change.Final :
            _traitInput?.Owned.Single(o => o.Id == id) ?? throw new InvalidOperationException("보유 변경에는 명령 입력 원본이 필요합니다.");
        internal void ProposeOwned(OwnedCharacterChange change)
        {
            if (change == null) throw new ArgumentNullException(nameof(change));
            if (!OwnedRecord(change.Expected.Id).SameValues(change.Expected)) throw new InvalidOperationException("보유 변경 제안의 기대값이 다릅니다.");
            var first = _ownedChanges.TryGetValue(change.Expected.Id, out var previous) ? previous.Expected : change.Expected;
            _ownedChanges[change.Expected.Id] = new OwnedCharacterChange(first, change.Final);
        }

        /// <summary>이미 확정된 사망 사건만 같은 처치 소비자에 넘깁니다.</summary>
        internal void SettleTraitKills(int eventStart = 0)
        {
            if (_traitInput != null) TraitKillSettlement.Apply(_traitInput, this, eventStart);
        }

        /// <summary>지금까지 이 경계가 적용한 대상별 회복량의 복사본이다. 이후 적용은 복사본에 반영되지 않는다.</summary>
        internal IReadOnlyDictionary<EntityId, int> CaptureRecovery() =>
            _recovery.ToDictionary(pair => pair.Key, pair => checked((int)pair.Value));

        /// <summary>아이템·오브젝트·영혼 도착이 실제로 회복한 체력을 기록합니다. 요청값이나 정규화는 더하지 않습니다.</summary>
        internal void RecordRecovery(EntityId id, int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount > 0) _recovery[id] = checked(_recovery.GetValueOrDefault(id) + amount);
        }

        /// <summary>이 경계가 적용한 아이템 보정 후 피해 요청의 누계입니다. 낙사·회복·위치 정규화는 포함하지 않습니다.</summary>
        internal IReadOnlyDictionary<EntityId, long> CaptureAppliedDamage() => new Dictionary<EntityId, long>(_appliedDamage);

        /// <summary>지금까지 이 경계가 적용한, 실제 HP를 줄인 피해 1회들의 복사본이다(적용 순서). 스테이지 기록 정산이 읽는다.</summary>
        internal IReadOnlyList<CombatStageDamageEntry> CaptureStageDamage() => _stageDamage.ToArray();
    }

    /// <summary>피해 1회의 스테이지 기록 재료다. 받은 개체, 체력 사건과 같은 출처 목록, 실제 HP 감소(1 이상)를 가진다.</summary>
    internal readonly struct CombatStageDamageEntry
    {
        internal EntityId RecipientId { get; }
        internal IReadOnlyList<CombatDamageOrigin> Origins { get; }
        internal int HealthLoss { get; }

        internal CombatStageDamageEntry(EntityId recipientId, IReadOnlyList<CombatDamageOrigin> origins, int healthLoss)
        {
            if (healthLoss < 1) throw new ArgumentOutOfRangeException(nameof(healthLoss));
            RecipientId = recipientId;
            Origins = origins ?? throw new ArgumentNullException(nameof(origins));
            HealthLoss = healthLoss;
        }
    }

    /// <summary>피해 1회의 적용 결과다. 요청 피해·실제 HP 손실·회복을 구분한다.</summary>
    internal readonly struct CombatDamageOutcome
    {
        internal CharacterCombatFacts Before { get; }
        internal CharacterCombatFacts After { get; }
        /// <summary>아이템 단계 뒤 남은 요청 피해다. HP 상한으로 자르기 전 값이다.</summary>
        internal int RemainingDamage { get; }
        internal int DamageBeforeItems { get; }
        internal int ActualHealthLoss { get; }
        internal int Recovery { get; }
        internal WeaknessExposureEvaluation Exposure { get; }

        internal CombatDamageOutcome(CharacterCombatFacts before, CharacterCombatFacts after,
            int remainingDamage, int actualHealthLoss, int recovery, int? damageBeforeItems = null, WeaknessExposureEvaluation exposure = null)
        {
            Before = before;
            After = after;
            RemainingDamage = remainingDamage;
            DamageBeforeItems = damageBeforeItems ?? remainingDamage;
            ActualHealthLoss = actualHealthLoss;
            Recovery = recovery;
            Exposure = exposure;
        }
    }
}
