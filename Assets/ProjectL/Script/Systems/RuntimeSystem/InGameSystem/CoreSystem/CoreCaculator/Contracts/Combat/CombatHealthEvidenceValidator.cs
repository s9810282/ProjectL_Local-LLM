using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>실행자가 확정한 체력 사실의 연결·보존·선언 범위를 검증합니다. 피해 공식이나 특성 규칙을 다시 계산하지 않습니다.</summary>
    public static class CombatHealthEvidenceValidator
    {
        public static void Validate(CombatCommandTransition transition)
        {
            if (transition == null) throw new ArgumentNullException(nameof(transition));
            if (transition.Accepted) new Validation(transition).Run();
        }

        private sealed class Validation
        {
            private readonly CombatCommandTransition _transition;
            private readonly Dictionary<CombatEvent, int> _eventOrder = new();
            private readonly HashSet<CombatHealthStep> _steps = new();
            private readonly HashSet<CombatHealthClaim> _checkedClaims = new();
            private readonly HashSet<CombatHealthStep> _attackClaimedSteps = new();
            private readonly Dictionary<EntityId, HealthStateSnapshot> _health = new();
            private readonly Dictionary<(CombatEvent, EntityId), CombatHealthStep[]> _anchored = new();
            private readonly Dictionary<(CombatHealthClaimKind, CombatEvent, int, int, EntityId), CombatHealthClaim> _claims = new();

            internal Validation(CombatCommandTransition transition) => _transition = transition;

            internal void Run()
            {
                for (int i = 0; i < _transition.Events.Count; i++)
                    Require(_eventOrder.TryAdd(_transition.Events[i], i), "중복 사건");
                foreach (var execution in _transition.HealthExecutions)
                    foreach (var step in execution.Steps)
                    {
                        Require(_steps.Add(step), "여러 실행이 같은 체력 단계를 중복 소유함", step.RecipientId);
                        Require(_eventOrder.ContainsKey(step.EvidenceEvent), "외부 근거 사건", step.RecipientId);
                        ValidateSource(step);
                    }
                foreach (var group in _steps.GroupBy(step => (step.EvidenceEvent, step.RecipientId)))
                {
                    var ordered = group.OrderBy(step => step.EvidenceOrdinal).ToArray();
                    Require(ordered.Select(step => step.EvidenceOrdinal).Distinct().Count() == ordered.Length,
                        "같은 사건·수신자의 단계 순번 중복", group.Key.RecipientId);
                    _anchored.Add(group.Key, ordered);
                }
                foreach (var claim in _transition.HealthClaims)
                {
                    Require(_claims.TryAdd((claim.Kind, claim.Event, claim.EntryIndex, claim.HitIndex, claim.RecipientId), claim),
                        "같은 체력 선언의 중복 연결", claim.RecipientId);
                    Require(claim.Steps.All(_steps.Contains), "외부 실행 단계의 차용", claim.RecipientId);
                    if (claim.Scope != null)
                        Require(ReferenceEquals(claim.Scope.Event, claim.Event) && claim.Scope.EntryIndex == claim.EntryIndex &&
                            claim.Scope.Steps.All(_steps.Contains) && claim.Scope.Steps
                            .Where(step => step.RecipientId == claim.RecipientId).SequenceEqual(claim.Steps),
                            "공격의 확정 범위 밖 단계 또는 누락", claim.RecipientId);
                }

                ValidateHealthChain();
                ValidateDamageExecutions();
                foreach (var occurrence in _transition.Events) ValidateClaims(occurrence);
                Require(_checkedClaims.Count == _transition.HealthClaims.Count, "지원하는 실제 선언에 연결되지 않은 체력 근거");
            }

            private void ValidateSource(CombatHealthStep step)
            {
                var source = step.Source;
                Require(source.ContextEvent == null || _eventOrder.ContainsKey(source.ContextEvent), "외부 출처 문맥", step.RecipientId);
                var origins = step.EvidenceEvent switch
                {
                    CombatCharacterEvent character => character.DamageOrigins,
                    CombatStructureEvent structure => structure.DamageOrigins,
                    _ => Array.Empty<CombatDamageOrigin>()
                };
                if (source.DamageOrigins.Count != 0)
                    Require(source.DamageOrigins.SequenceEqual(origins),
                        "실행과 사건의 피해 출처 불일치", step.RecipientId);
                if (source.EnvironmentSource != null)
                {
                    var tile = source.EnvironmentSource;
                    Require(_transition.Input.State.Environment.Tiles.Any(tile.SameValues) || _transition.Events
                        .OfType<CombatMovementSourceEvent>().Any(e => _eventOrder[e] <= _eventOrder[step.EvidenceEvent] &&
                            (tile.SameValues(e.Expected) || tile.SameValues(e.Final))), "없는 환경 출처", step.RecipientId);
                }
            }

            private void ValidateHealthChain()
            {
                foreach (var character in _transition.Input.Characters) _health.Add(character.EntityId, character.Health);
                foreach (var structure in _transition.Input.State.Structures)
                    if (structure.ObstacleDurability == null) _health.Add(structure.EntityId, structure.Health);
                var consumed = new HashSet<CombatHealthStep>();
                foreach (var occurrence in _transition.Events)
                {
                    switch (occurrence)
                    {
                        case CombatSummonEvent summon:
                            Require(_health.TryAdd(summon.Created.EntityId, summon.Created.Health), "중복 생성 개체", summon.Created.EntityId);
                            break;
                        case CombatReinforcementEvent reinforcement:
                            Require(_health.TryAdd(reinforcement.Created.EntityId, reinforcement.Created.Health), "중복 증원 개체", reinforcement.Created.EntityId);
                            break;
                        case CombatCharacterEvent character:
                            Apply(occurrence, character.Final.EntityId, character.Expected.Health, character.Final.Health,
                                character.Kind == CombatEventKind.Reconstructed, consumed);
                            break;
                        case CombatStructureEvent structure when structure.Expected.ObstacleDurability == null &&
                            structure.Final.ObstacleDurability == null:
                            Apply(occurrence, structure.Final.EntityId, structure.Expected.Health, structure.Final.Health, false, consumed);
                            break;
                        case CombatTraitStateEvent state:
                            Apply(occurrence, state.Final.EntityId, state.Expected.Health, state.Final.Health, false, consumed);
                            break;
                        case CombatSoulEvent soul when soul.Characters != null:
                            foreach (var change in soul.Characters.Characters)
                                Apply(occurrence, change.Final.EntityId, change.Expected.Health, change.Final.Health, false, consumed);
                            break;
                    }
                }
                Require(consumed.Count == _steps.Count, "실제 체력 변경에 대응하지 않는 실행 단계");
                var finalCharacters = _transition.Input.Characters.ToDictionary(c => c.EntityId, c => c.Health);
                foreach (var change in _transition.Characters.Characters) finalCharacters[change.Final.EntityId] = change.Final.Health;
                foreach (var created in _transition.Characters.Created) finalCharacters.Add(created.EntityId, created.Health);
                foreach (var pair in finalCharacters)
                    Require(_health.TryGetValue(pair.Key, out var actual) && actual.Equals(pair.Value), "게시할 캐릭터 체력과 실행 결과 불일치", pair.Key);
                foreach (var structure in _transition.FinalState.Structures)
                    if (structure.ObstacleDurability == null)
                        Require(_health.TryGetValue(structure.EntityId, out var actual) && actual.Equals(structure.Health),
                            "게시할 구조물 체력과 실행 결과 불일치", structure.EntityId);
            }

            private void Apply(CombatEvent occurrence, EntityId recipient, HealthStateSnapshot before, HealthStateSnapshot after,
                bool composite, HashSet<CombatHealthStep> consumed)
            {
                _anchored.TryGetValue((occurrence, recipient), out var steps);
                if (steps == null)
                {
                    Require(!composite && before.Equals(after), "체력 변경의 실행 근거 누락", recipient);
                    return;
                }
                Require(_health.TryGetValue(recipient, out var current) && current.Equals(before), "체력 실행 시작 상태 불일치", recipient);
                foreach (var step in steps)
                {
                    Require(current.Equals(step.Before), "체력 단계 순서 또는 원본 불일치", recipient);
                    long remaining = (long)current.CurrentHealth - step.ActualLoss - step.NormalizationLoss;
                    Require(remaining >= 0 && remaining + step.ActualRecovery == step.After.CurrentHealth, "체력 보존식 불일치", recipient);
                    if (occurrence.Kind == CombatEventKind.HealthNormalized)
                        Require(step.ActualLoss == 0 && step.ActualRecovery == 0, "상한 보정을 피해·회복으로 기록함", recipient);
                    current = step.After;
                    consumed.Add(step);
                }
                Require(current.Equals(after), "근거 사건과 실행의 최종 체력 불일치", recipient);
                _health[recipient] = current;
            }

            private void ValidateDamageExecutions()
            {
                var evidence = _transition.HealthExecutions.SelectMany(execution => execution.Evidence).ToHashSet();
                foreach (var fact in _transition.Events)
                {
                    bool absorbed = fact switch
                    {
                        CombatHeldEnvironmentEvent held => held.Origin != null && held.Expected.BadgeAvailable && !held.Final.BadgeAvailable,
                        CombatItemEvent item => item.Stage == ItemEventStage.BeforeDamage && item.DamageKind.HasValue,
                        CombatTraitStateEvent state => state.Origin != null && state.Expected.StatusEffects.Entries.OfType<ShieldStatus>().Any(old =>
                            state.Final.StatusEffects.Entries.OfType<ShieldStatus>().Any(next => next.Origin == old.Origin && next.RemainingBudget < old.RemainingBudget)),
                        _ => false
                    };
                    Require(!absorbed || evidence.Contains(fact), "피해 흡수 사건의 실행 근거 누락");
                }
            }

            private void ValidateClaims(CombatEvent occurrence)
            {
                switch (occurrence)
                {
                    case CombatAttackEvent attack:
                        ValidateAttack(occurrence, attack.Attack.Hits.ToArray(), -1);
                        ValidateSummaryTraits(attack.Attack.TraitActivations.ToArray());
                        break;
                    case CombatPlayerSkillAttackEvent skill:
                        ValidateAttack(occurrence, skill.Attack.Hits, -1);
                        ValidateSummaryTraits(skill.Attack.TraitActivations);
                        break;
                    case CombatRoarEvent roar when roar.Attack.HasValue:
                        ValidateAttack(occurrence, roar.Attack.Value.Hits.ToArray(), -1);
                        ValidateSummaryTraits(roar.Attack.Value.TraitActivations.ToArray());
                        break;
                    case CombatEffectsEvent effects:
                        for (int i = 0; i < effects.Traits.Count; i++)
                        {
                            var trait = effects.Traits[i];
                            if (trait.ChildAttack != null) ValidateAttack(occurrence, trait.ChildAttack.Hits, i);
                            if (trait.TraitActivationKind != TraitActivationKind.HealthRecovered) continue;
                            foreach (var recipient in trait.TargetIds)
                            {
                                var claim = Claim(CombatHealthClaimKind.TraitRecovery, occurrence, i, -1, recipient);
                                ValidateRecovery(claim, origin => origin.Kind == EffectOwnerKind.Trait &&
                                    origin.OwnerInstanceId == trait.SourceUnitId.Value && origin.DefinitionKey == (int)trait.TraitKey);
                                Require(claim.Steps.Sum(step => (long)step.ActualRecovery) > 0, "특성 회복의 실제 회복 누락", recipient);
                            }
                        }
                        break;
                    case CombatArtifactEvent artifact when artifact.Purpose is CombatArtifactPurpose.Reflection or CombatArtifactPurpose.Healing:
                        for (int i = 0; i < artifact.Activations.Count; i++)
                        {
                            var activation = artifact.Activations[i];
                            bool healing = artifact.Purpose == CombatArtifactPurpose.Healing;
                            var claim = Claim(healing ? CombatHealthClaimKind.ArtifactRecovery : CombatHealthClaimKind.ArtifactDamage,
                                occurrence, i, -1, activation.TargetId);
                            if (healing) ValidateRecovery(claim, origin => origin == activation.Origin);
                            else foreach (var step in claim.Steps)
                                Require(step.Source.Kind == CombatHealthOperationKind.Damage && step.Source.EffectOrigin == activation.Origin &&
                                    step.Source.DamageOrigins.Any(origin => ReferenceEquals(origin, claim.DamageOrigin)) && step.Source.DamageOrigins.All(origin =>
                                        origin.AttackGroup == artifact.Reflection.ReflectionAttackGroup), "반사 선언과 실제 피해 출처 불일치", claim.RecipientId);
                            long amount = claim.Steps.Sum(step => (long)(healing ? step.ActualRecovery : step.ActualLoss));
                            Require(amount == activation.AppliedAmount, "아티팩트 선언과 실제 적용량 불일치", claim.RecipientId);
                        }
                        break;
                    case CombatItemEvent item when item.Activated && item.ActivationKind == ItemActivationKind.Healing:
                        var recovery = Claim(CombatHealthClaimKind.ItemRecovery, occurrence, -1, -1, item.HolderId);
                        ValidateRecovery(recovery, origin => origin.Kind == EffectOwnerKind.Item &&
                            origin.OwnerInstanceId == item.HolderId.Value && origin.DefinitionKey == item.ItemKey);
                        Require(recovery.Steps.Sum(step => (long)step.ActualRecovery) == item.AppliedMagnitude,
                            "아이템 선언과 실제 회복량 불일치", item.HolderId);
                        break;
                }
            }

            private void ValidateAttack(CombatEvent occurrence, IReadOnlyList<AttackHitResult> hits, int entry)
            {
                var kind = entry < 0 ? CombatHealthClaimKind.AttackHit : CombatHealthClaimKind.ChildAttackHit;
                CombatHealthScope scope = null;
                for (int i = 0; i < hits.Count; i++)
                {
                    var hit = hits[i];
                    var claim = Claim(kind, occurrence, entry, i, hit.TargetId);
                    Require(claim.Scope != null && (scope == null || ReferenceEquals(scope, claim.Scope)), "같은 공격의 확정 범위 불일치", hit.TargetId);
                    scope = claim.Scope;
                    var steps = claim.Steps.OrderBy(step => _eventOrder[step.EvidenceEvent]).ThenBy(step => step.EvidenceOrdinal).ToArray();
                    bool obstacle = _transition.Input.EnvironmentBindings.ObstacleEntityIds.Contains(hit.TargetId);
                    Require(hit.IsObstacle == obstacle, "공격 대상과 타수 내구도 분류 불일치", hit.TargetId);
                    if (obstacle)
                    {
                        Require(steps.Length == 0, "장애물 타수 내구도에 체력 실행을 사용함", hit.TargetId);
                        continue;
                    }
                    int health = hit.TargetHealthBefore;
                    foreach (var step in steps)
                    {
                        Require(_attackClaimedSteps.Add(step), "서로 다른 공격 선언이 같은 체력 단계를 중복 사용함", hit.TargetId);
                        if (occurrence is CombatPlayerSkillAttackEvent skill)
                        {
                            Require(_eventOrder[step.EvidenceEvent] > _eventOrder[skill], "스킬 발생보다 앞선 체력 단계의 차용", hit.TargetId);
                            var source = step.Source;
                            // 환경·반사는 자신의 문맥 또는 Artifact 근거가 있습니다. 직접 타격은 장부에 포착한 같은 접촉 출처를 써야 합니다.
                            if (source.Kind == CombatHealthOperationKind.Damage && source.ContextEvent == null &&
                                source.EnvironmentSource == null && source.EffectOrigin?.Kind != EffectOwnerKind.Artifact &&
                                (!source.DamageKind.HasValue || source.DamageKind == CombatDamageKind.PlayerSkillAttack))
                                Require(source.DamageOrigins.All(origin => origin.AttackGroup == skill.Attack.AttackGroup &&
                                    origin.CreditedCharacterId == skill.Attack.ActorId &&
                                    hit.Contacts != null && hit.Contacts.Any(contact => ReferenceEquals(contact.Origin, origin))),
                                    "스킬 직접 피해와 실제 접촉 출처 불일치", hit.TargetId);
                        }
                        Require(health == step.Before.CurrentHealth, "공격 범위의 체력 연속성 불일치", hit.TargetId);
                        health = step.After.CurrentHealth;
                    }
                    Require(health == hit.TargetHealthAfter && steps.Sum(step => (long)step.ActualLoss) == hit.DamageDealt &&
                        steps.Sum(step => (long)step.ActualRecovery) == hit.HealthRecovery &&
                        steps.Sum(step => (long)step.NormalizationLoss) == hit.HealthNormalizationLoss,
                        "공격 선언과 확정 범위의 피해·회복·보정 불일치", hit.TargetId);
                }
            }

            private void ValidateSummaryTraits(IEnumerable<Snapshots.Traits.TraitActivationSnapshot> traits)
            {
                // 공격 요약은 실제 회복 발동의 같은 스냅샷을 공유하며, 별도 실행 근거를 만들지 않습니다.
                foreach (var trait in traits.Where(t => t.TraitActivationKind == TraitActivationKind.HealthRecovered))
                    Require(_transition.Events.OfType<CombatEffectsEvent>().Any(e => e.Traits.Any(t => ReferenceEquals(t, trait))),
                        "공격 요약의 회복 발동과 실제 사건 참조 불일치", trait.SourceUnitId);
            }

            private CombatHealthClaim Claim(CombatHealthClaimKind kind, CombatEvent occurrence, int entry, int hit, EntityId recipient)
            {
                Require(_claims.TryGetValue((kind, occurrence, entry, hit, recipient), out var claim), $"{kind} 선언의 실행 연결 누락", recipient);
                _checkedClaims.Add(claim);
                return claim;
            }

            private static void ValidateRecovery(CombatHealthClaim claim, Func<EffectOriginKey, bool> matchesOrigin)
            {
                foreach (var step in claim.Steps)
                    Require(step.Source.Kind == CombatHealthOperationKind.EffectRecovery && step.Source.EffectOrigin.HasValue &&
                        matchesOrigin(step.Source.EffectOrigin.Value) && ReferenceEquals(step.Source.ContextEvent, claim.Event),
                        $"{claim.Kind} 선언과 회복의 실제 출처 불일치", claim.RecipientId);
            }
        }

        private static void Require(bool condition, string detail, EntityId recipient = default)
        {
            if (!condition) throw new InvalidOperationException($"체력 실행 근거 불일치: {detail} (수신자 {recipient}).");
        }
    }
}
