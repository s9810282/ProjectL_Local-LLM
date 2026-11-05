using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public static class CombatChangeEvidenceReader
    {
        public static IReadOnlyList<CombatChangeEvidence> ReadAll(IReadOnlyList<CombatEvent> facts)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            if (facts.Any(f => f == null)) throw Invalid("null 사건");
            return Array.AsReadOnly(facts.Select((fact, index) => Read(fact, facts, index)).ToArray());
        }

        public static CombatChangeEvidence ReadCharacterChanges(IReadOnlyList<CharacterCombatFacts> before,
            IReadOnlyList<CharacterCombatFacts> after)
        {
            if (before == null || after == null) throw new ArgumentNullException(before == null ? nameof(before) : nameof(after));
            if (before.Any(c => c == null) || after.Any(c => c == null) ||
                before.Select(c => c.EntityId).Distinct().Count() != before.Count || after.Select(c => c.EntityId).Distinct().Count() != after.Count)
                throw Invalid("캐릭터 범위의 null 또는 중복 식별자");
            var a = before.ToDictionary(c => c.EntityId);
            var b = after.ToDictionary(c => c.EntityId);
            return Evidence(CombatChangeEvidenceKind.Effect, a.Keys.Union(b.Keys).Where(id =>
                !a.TryGetValue(id, out var old) || !b.TryGetValue(id, out var next) || !old.SameValues(next)),
                changes: a.Keys.Intersect(b.Keys).Where(id => !a[id].SameValues(b[id])).Select(id => new CharacterStateChange(a[id], b[id])));
        }

        public static CombatChangeEvidence ReadChanges(SoulFieldChange change)
        {
            if (change == null) throw new ArgumentNullException(nameof(change));
            var a = change.Expected.Entries.ToDictionary(s => s.Id);
            var b = change.Final.Entries.ToDictionary(s => s.Id);
            var cells = new List<CellCoord>();
            foreach (var id in a.Keys.Union(b.Keys))
            {
                a.TryGetValue(id, out var old); b.TryGetValue(id, out var next);
                if (old != null && old.SameValues(next)) continue;
                if (old != null) cells.Add(old.Cell);
                if (next != null) cells.Add(next.Cell);
            }
            return Evidence(CombatChangeEvidenceKind.Effect, cells: cells);
        }

        private static CombatChangeEvidence Read(CombatEvent fact, IReadOnlyList<CombatEvent> stream, int index)
        {
            switch (fact)
            {
                case CombatCharacterEvent c when c.Kind is CombatEventKind.Moved or CombatEventKind.Placed or CombatEventKind.Returned or
                    CombatEventKind.HealthChanged or CombatEventKind.HealthNormalized or CombatEventKind.Died or CombatEventKind.Relocated:
                    var kind = c.Kind == CombatEventKind.Returned ? CombatChangeEvidenceKind.Return : CombatChangeEvidenceKind.Effect;
                    if (c.Kind == CombatEventKind.Moved && c.MovementCause?.Agency != CombatMovementAgency.Forced &&
                        c.Expected.WithPlacement(c.Final.Area, c.Final.ReserveSlotIndex, c.Final.Anchor, c.Final.Rotation,
                            c.Final.OccupiedCells).SameValues(c.Final)) kind = CombatChangeEvidenceKind.OrdinaryMove;
                    return Evidence(kind, new[] { c.Final.EntityId }, changes: new[] { new CharacterStateChange(c.Expected, c.Final) });
                case CombatStructureEvent s when s.Kind is CombatEventKind.HealthChanged or CombatEventKind.Died:
                    return Evidence(CombatChangeEvidenceKind.Effect, new[] { s.Final.EntityId });
                case CombatAttackEvent a when a.Kind == CombatEventKind.Attacked:
                    return ReadAttack(a.Attack, stream, CombatChangeEvidenceKind.Action);
                case CombatRoarEvent r when r.Kind == CombatEventKind.Roared:
                    return r.Attack.HasValue ? ReadAttack(r.Attack.Value, stream, CombatChangeEvidenceKind.Effect) :
                        Evidence(CombatChangeEvidenceKind.Effect, new[] { r.BossId });
                case CombatEffectsEvent e when e.Kind == CombatEventKind.EffectsActivated:
                    if (e.Categories.Count != 0) throw Invalid("지원하지 않는 Category 발동");
                    if (e.Traits.Count == 0) throw Invalid("비어 있는 효과 발동");
                    var traits = e.Traits.Select(t => ReadTrait(t, stream)).ToArray();
                    return Evidence(traits.Any(t => t.Kind != CombatChangeEvidenceKind.Observation) ? CombatChangeEvidenceKind.Effect : CombatChangeEvidenceKind.Observation,
                        traits.SelectMany(t => t.AffectedEntityIds), traits.SelectMany(t => t.ChangedSourceCells));
                case CombatTraitStateEvent t when t.Kind == CombatEventKind.TraitStateChanged:
                    return Evidence(CombatChangeEvidenceKind.Effect, new[] { t.Final.EntityId }, changes: new[] { new CharacterStateChange(t.Expected, t.Final) });
                case CombatHeldEnvironmentEvent h when h.Kind == CombatEventKind.HeldEnvironmentChanged:
                    return Evidence(CombatChangeEvidenceKind.Effect, new[] { h.CharacterId });
                case CombatHuntersMarkEvent m when m.Kind == CombatEventKind.HuntersMarkChanged:
                    return Evidence(CombatChangeEvidenceKind.Effect, new[] { m.TargetId });
                case CombatPullEvent p when p.Kind == CombatEventKind.Pulled:
                    return Evidence(CombatChangeEvidenceKind.Effect, new[] { p.Movement.TargetId });
                case CombatFallEvent f when f.Kind == CombatEventKind.Fell:
                    return Evidence(CombatChangeEvidenceKind.Effect, new[] { f.EntityId });
                case CombatMovementSourceEvent s when s.Kind == CombatEventKind.MovementSourceChanged:
                    return Source(s.Expected, s.Final);
                case CombatObjectActivationEvent o when o.Kind == CombatEventKind.ObjectActivated:
                    Require(stream.OfType<CombatMovementSourceEvent>().Any(s => s.Expected != null && s.Expected.SameValues(o.Source) && !s.Final.Active),
                        "Object 발동의 같은 원본 소비 사실");
                    return Evidence(CombatChangeEvidenceKind.Effect, new[] { o.ActivatorId }, new[] { o.Source.Cell });
                case CombatObjectCreatedEvent o when o.Kind == CombatEventKind.ObjectCreated:
                    return Evidence(CombatChangeEvidenceKind.Effect, cells: new[] { o.Source.Cell });
                case CombatTornadoEvent t when t.Kind == CombatEventKind.TornadoActivated:
                    return Source(t.Expected, t.Final);
                case CombatThornConsumedEvent t when t.Kind == CombatEventKind.ThornConsumed:
                    return Evidence(CombatChangeEvidenceKind.Effect, cells: new[] { t.Source.Cell });
                case CombatSoulEvent s when s.Kind is CombatEventKind.SoulsCollected or CombatEventKind.SoulFieldSettled:
                    if (s.Kind == CombatEventKind.SoulFieldSettled) return ReadChanges(s.Field);
                    Require(!s.CollectorId.IsEmpty && s.Characters != null && s.ConsumedIds.Count > 0, "영혼 수집자와 실제 소비 사실");
                    try { _ = new SoulActionChanges(s.Characters, s.Field, s.ConsumedIds); }
                    catch (ArgumentException exception) { throw new InvalidOperationException("영혼 사건의 범위가 불완전합니다.", exception); }
                    return Evidence(CombatChangeEvidenceKind.Effect, s.Characters.Characters.Where(c => !c.Expected.SameValues(c.Final))
                        .Select(c => c.Final.EntityId).Append(s.CollectorId), ReadChanges(s.Field).ChangedSourceCells);
                case CombatArtifactEvent a when a.Kind == CombatEventKind.ArtifactActivated:
                    return ReadArtifact(a, stream);
                case CombatObstacleReactionEvent o when o.Kind == CombatEventKind.ObstacleReacted:
                    Require(stream.OfType<CombatStructureEvent>().Any(s => s.Final.EntityId == o.ObstacleId &&
                        s.Expected.Health.CurrentHealth > s.Final.Health.CurrentHealth), "장애물 반응의 실제 체력 변경");
                    return Evidence(CombatChangeEvidenceKind.Effect, new[] { o.ObstacleId });
                case CombatGimmickEvent g when g.Kind == CombatEventKind.GimmickActivated:
                    if (g.Activation is not RailwayGimmickActivationSnapshot rail) throw Invalid("지원하지 않는 기믹 형식");
                    foreach (var id in rail.KilledUnitIds.ToArray())
                        Require(!id.IsEmpty && stream.OfType<CombatCharacterEvent>().Any(c => c.Kind == CombatEventKind.Died &&
                            c.Final.EntityId == id && !c.Expected.Health.IsDead && c.Final.Health.IsDead), "철도 피해자의 사망 사실");
                    return Evidence(CombatChangeEvidenceKind.Effect, rail.KilledUnitIds.ToArray());
                case CombatHaboobEvent h when h.Kind == CombatEventKind.HaboobAdvanced:
                    return Evidence(CombatChangeEvidenceKind.Effect, cells: h.Expected.Cells.Concat(h.Final.Cells));
                case CombatTornadoGimmickEvent storm when storm.Kind == CombatEventKind.TornadoAdvanced:
                    return Evidence(CombatChangeEvidenceKind.Effect, cells: storm.Advances.SelectMany(a =>
                        a.Path.SelectMany(c => TornadoGeometry.Footprint(c, a.SideLength))));
                case CombatTornadoCenterEvent center when center.Kind == CombatEventKind.TornadoCenterReached:
                    return Evidence(CombatChangeEvidenceKind.Observation);
                case CombatTornadoEntryEvent entry when entry.Kind == CombatEventKind.TornadoEntryStarted:
                    return Evidence(CombatChangeEvidenceKind.Observation);
                case CombatTornadoMemberEvent member when member.Kind == CombatEventKind.TornadoMemberAdvanced:
                    return Evidence(CombatChangeEvidenceKind.Observation);
                case CombatRockfallEvent r when r.Kind == CombatEventKind.RockfallAdvanced:
                    if (r.Final == null) Require(stream.OfType<CombatOutcomeEvent>().Any(o => o.Outcome.IsStageEnded), "종료 낙석의 승패 사실");
                    if (r.Step == RockfallGimmickStep.Impact) ValidateRockfallRecipients(r, stream, index + 1);
                    return Evidence(CombatChangeEvidenceKind.Effect, r.RecipientIds,
                        r.Step == RockfallGimmickStep.Warning ? r.Final.WarnedCells : r.Expected.WarnedCells);
                case CombatSummonEvent s when s.Kind == CombatEventKind.Summoned:
                    return Evidence(CombatChangeEvidenceKind.Effect, new[] { s.Created.EntityId });
                case CombatItemEvent i when i.Kind is CombatEventKind.ItemActivated or CombatEventKind.ItemEquipmentChanged or CombatEventKind.ItemDiscarded:
                    return ReadItem(i, stream);
                case CombatExperienceEvent e when e.Kind is CombatEventKind.ExperienceEarned or CombatEventKind.ExperienceApplied:
                    return Evidence(CombatChangeEvidenceKind.Effect, new[] { e.RecipientId });
                case CombatTurnEvent t when t.Kind is CombatEventKind.CombatEntered or CombatEventKind.ControllerTurnStarted or
                    CombatEventKind.PlayerTurnEnded or CombatEventKind.EnemyPhaseEnded or CombatEventKind.NightEnded:
                    return Evidence(CombatChangeEvidenceKind.Lifecycle);
                case CombatOutcomeEvent o when o.Kind == CombatEventKind.StageEnded:
                    return Evidence(CombatChangeEvidenceKind.Lifecycle);
                default:
                    throw Invalid(fact.GetType().Name + "/" + fact.Kind);
            }
        }

        private static CombatChangeEvidence Source(MovementTileFact before, MovementTileFact after) =>
            Evidence(CombatChangeEvidenceKind.Effect, cells: after is TraitTrailFact || before != null && before.SameValues(after) ? Array.Empty<CellCoord>() :
                before == null ? new[] { after.Cell } : new[] { before.Cell, after.Cell },
                sourceIds: before != null && before.SameValues(after) ? Array.Empty<long>() : new[] { after.Id });

        private static bool HasRockfallBind(CharacterCombatFacts character, RockfallGimmickState rock) =>
            character.StatusEffects.Entries.OfType<ActionRestrictionStatus>().Any(s => s.Key == StatusEffectKey.Bind && s.Origin == rock.BindOrigin);

        private static void ValidateRockfallRecipients(CombatRockfallEvent rock, IReadOnlyList<CombatEvent> stream, int next)
        {
            // 충돌 바로 뒤에 수신자 순서대로 체력 변화(있을 때)와 생존자의 속박이 기록된다.
            var cells = rock.Expected.WarnedCells.ToHashSet();
            foreach (var id in rock.RecipientIds)
            {
                CharacterCombatFacts hurt = null;
                if (next < stream.Count && stream[next] is CombatCharacterEvent hp && hp.Final.EntityId == id)
                {
                    Require(hp.Expected.Area == CharacterRuntimeArea.Field && hp.Expected.OccupiedCells.Any(cells.Contains) &&
                        hp.Expected.Health.CurrentHealth > hp.Final.Health.CurrentHealth &&
                        hp.Kind == (hp.Final.Health.IsDead ? CombatEventKind.Died : CombatEventKind.HealthChanged), "낙석 충돌 직후의 체력 변화");
                    next++;
                    hurt = hp.Final;
                    if (hurt.Health.IsDead) continue;
                }
                Require(next < stream.Count && stream[next] is CombatTraitStateEvent grant && grant.Final.EntityId == id &&
                    grant.Expected.Area == CharacterRuntimeArea.Field && grant.Expected.OccupiedCells.Any(cells.Contains) &&
                    (hurt == null || hurt.SameValues(grant.Expected)) && !grant.Final.Health.IsDead &&
                    HasRockfallBind(grant.Final, rock.Expected), "낙석 생존자의 충돌 직후 속박");
                next++;
            }
        }

        /// <summary>저장된 기믹 상태를 사건 순서대로 대조한다. 선정·피해 공식은 다시 계산하지 않는다.</summary>
        public static void ValidateGimmickChanges(CombatCommandTransition transition)
        {
            TornadoChangeEvidenceValidator.Validate(transition);
            var state = transition.Input.State.Gimmicks;
            var characters = transition.Input.Characters.ToDictionary(c => c.EntityId);
            var seen = new HashSet<StageGimmickId>();
            CombatRockfallEvent impact = null;
            foreach (var fact in transition.Events)
            {
                if (fact is CombatCharacterEvent character) characters[character.Final.EntityId] = character.Final;
                else if (fact is CombatTraitStateEvent status) characters[status.Final.EntityId] = status.Final;
                else if (fact is CombatTornadoGimmickEvent storm)
                {
                    Require(seen.Add(storm.Expected.Id) && state.Get(storm.Expected.Id).SameValues(storm.Expected), "토네이도의 유일한 현재 기대값");
                    state = state.Replace(storm.Final);
                }
                else if (fact is CombatHaboobEvent cloud)
                {
                    Require(seen.Add(cloud.Expected.Id) && state.Get(cloud.Expected.Id).SameValues(cloud.Expected), "하부브의 유일한 현재 기대값");
                    Require(transition.Events.OfType<CombatTurnEvent>().Any(e =>
                        (e.Kind == CombatEventKind.PlayerTurnEnded || e.Kind == CombatEventKind.EnemyPhaseEnded) &&
                        e.Final.CompletedControllerTurnCount == cloud.Expected.NextBoundaryCount &&
                        e.Final.CompletedControllerTurnCount == transition.FinalState.Turn.CompletedControllerTurnCount), "하부브의 실제 진영 종료 경계");
                    Require(cloud.Final.Cells.All(c => c.X < transition.FinalState.BoardWidth && c.Y < transition.FinalState.BoardHeight), "하부브 영역의 보드 범위");
                    state = state.Replace(cloud.Final);
                }
                else if (fact is CombatRockfallEvent rock)
                {
                    Require(seen.Add(rock.Expected.Id) && state.Get(rock.Expected.Id).SameValues(rock.Expected), "낙석의 유일한 현재 기대값");
                    Require(transition.Events.OfType<CombatTurnEvent>().Any(e =>
                        (e.Kind == CombatEventKind.PlayerTurnEnded || e.Kind == CombatEventKind.EnemyPhaseEnded) &&
                        e.Final.CompletedControllerTurnCount == rock.Expected.NextBoundaryCount &&
                        e.Final.CompletedControllerTurnCount == transition.FinalState.Turn.CompletedControllerTurnCount), "낙석의 실제 진영 종료 경계");
                    if (rock.Step == RockfallGimmickStep.Impact)
                    {
                        impact = rock;
                        var victims = characters.Values.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead &&
                            c.OccupiedCells.Intersect(rock.Expected.WarnedCells).Any()).Select(c => c.EntityId).ToHashSet();
                        Require(victims.SetEquals(rock.RecipientIds), "낙석 충돌 직전의 전체 수신자");
                    }
                    Require(rock.Final != null || transition.Outcome.IsStageEnded, "낙석 상태 삭제의 종료 승패");
                    var cells = rock.Final?.WarnedCells ?? Array.Empty<CellCoord>();
                    Require(cells.All(c => c.X < transition.FinalState.BoardWidth && c.Y < transition.FinalState.BoardHeight), "낙석 경고의 보드 범위");
                    state = rock.Final == null ? state.Without(rock.Expected.Id) : state.Replace(rock.Final);
                }
            }
            Require(state.SameValues(transition.FinalState.Gimmicks), "기믹 사건과 최종 상태의 대응");
            long count = transition.FinalState.Turn.CompletedControllerTurnCount;
            if (!transition.Outcome.IsStageEnded && count > transition.Input.State.Turn.CompletedControllerTurnCount)
            {
                foreach (var entry in transition.Input.State.Gimmicks.Entries)
                    Require(entry.NextBoundaryCount > count || seen.Contains(entry.Id), "예정된 기믹의 진영 종료 사건");
            }
            if (impact != null)
            {
                // 이후 턴 정리·회수까지 반영된 사건 기록과 실제 설치할 캐릭터의 속박을 대조한다.
                var published = transition.Input.Characters.ToDictionary(c => c.EntityId);
                foreach (var change in transition.Characters.Characters) published[change.Final.EntityId] = change.Final;
                foreach (var id in impact.RecipientIds)
                    Require(HasRockfallBind(characters[id], impact.Expected) == HasRockfallBind(published[id], impact.Expected),
                        "낙석 속박의 사건과 게시 상태");
            }
        }

        private static CombatChangeEvidence ReadAttack(AttackResult attack, IReadOnlyList<CombatEvent> stream, CombatChangeEvidenceKind kind)
        {
            Require(attack.Succeeded && !attack.AttackerId.IsEmpty, "수락된 공격 사실");
            var actors = new List<EntityId> { attack.AttackerId };
            actors.AddRange(ReadHits(attack.Hits.ToArray(), stream));
            foreach (var trait in attack.TraitActivations.ToArray()) actors.AddRange(ReadTrait(trait, stream).AffectedEntityIds);
            Require(attack.CategoryActivations.Length == 0, "지원하지 않는 Category 발동");
            if (attack.Pull != null)
                Require(stream.OfType<CombatPullEvent>().Any(p => SamePull(p.Movement, attack.Pull)), "공격의 끌어오기 사실");
            foreach (var knockback in attack.Knockbacks.Where(k => k.Path.Count > 0))
                Require(HasMovement(stream, knockback.TargetId), "공격의 넉백 이동 사실");
            return Evidence(kind, actors);
        }

        private static IEnumerable<EntityId> ReadHits(IReadOnlyList<AttackHitResult> hits, IReadOnlyList<CombatEvent> stream)
        {
            foreach (var hit in hits)
            {
                Require(!hit.TargetId.IsEmpty, "공격의 실제 수신자");
                if (hit.DamageDealt == 0 && hit.HealthRecovery == 0 && hit.HealthNormalizationLoss == 0) continue;
                Require(HasHealthChain(stream, hit), "공격 수신자의 체력·회복·상한 정규화 사실 " + hit.TargetId);
                yield return hit.TargetId;
            }
        }

        private static CombatChangeEvidence ReadArtifact(CombatArtifactEvent artifact, IReadOnlyList<CombatEvent> stream)
        {
            Require(artifact.Activations.Count > 0 && artifact.Activations.All(a => a != null), "Artifact 발동 값");
            if (artifact.Purpose == CombatArtifactPurpose.AttackPowerContribution)
                return Evidence(CombatChangeEvidenceKind.Observation);
            Require(artifact.Purpose is CombatArtifactPurpose.Healing or CombatArtifactPurpose.Reflection, "Artifact 발동 목적");
            foreach (var activation in artifact.Activations.Where(a => a.AppliedAmount > 0))
            {
                long amount = stream.OfType<CombatCharacterEvent>().Where(c => c.Final.EntityId == activation.TargetId)
                    .Sum(c => artifact.Purpose == CombatArtifactPurpose.Healing
                        ? (long)Math.Max(0, c.Final.Health.CurrentHealth - c.Expected.Health.CurrentHealth)
                        : Math.Max(0, c.Expected.Health.CurrentHealth - c.Final.Health.CurrentHealth));
                Require(amount >= activation.AppliedAmount, "Artifact 실제 수신자의 체력 사실");
            }
            return Evidence(CombatChangeEvidenceKind.Effect, artifact.Activations.Where(a => a.AppliedAmount > 0).Select(a => a.TargetId));
        }

        private static CombatChangeEvidence ReadItem(CombatItemEvent item, IReadOnlyList<CombatEvent> stream)
        {
            if (item.Kind != CombatEventKind.ItemActivated) return Evidence(CombatChangeEvidenceKind.Effect, new[] { item.HolderId });
            Require(item.ActivationKind != ItemActivationKind.Unspecified, "아이템 발동 효과 종류");
            if (item.ActivationKind == ItemActivationKind.DamageMitigation)
                Require(item.Stage == ItemEventStage.BeforeDamage && item.TargetId == item.HolderId, "피해 감소의 소지자·단계");
            if (item.ActivationKind == ItemActivationKind.Healing)
            {
                Require(item.Stage == ItemEventStage.AfterDamage && item.TargetId == item.HolderId, "회복의 소지자·단계");
                long recovered = stream.OfType<CombatCharacterEvent>().Where(c => c.Final.EntityId == item.TargetId)
                    .Sum(c => (long)Math.Max(0, c.Final.Health.CurrentHealth - c.Expected.Health.CurrentHealth));
                Require(recovered >= item.AppliedMagnitude, "아이템 회복의 실제 체력 사실");
            }
            // 계산 수치가 0이어도 새 임시 효과 슬롯은 실제 수신자의 상태 변경이다. 중복 소모는 소지자만 바뀐다.
            return Evidence(CombatChangeEvidenceKind.Effect, item.ActivationKind == ItemActivationKind.TemporaryEffect && item.Reason == ItemEventReason.Activated
                ? new[] { item.HolderId, item.TargetId.Value } : new[] { item.HolderId });
        }

        private static CombatChangeEvidence ReadTrait(TraitActivationSnapshot trait, IReadOnlyList<CombatEvent> stream)
        {
            if (trait == null) throw Invalid("null 특성 발동");
            var targets = trait.TargetIds;
            switch (trait.TraitActivationKind)
            {
                case TraitActivationKind.DamageModified:
                case TraitActivationKind.DefenseBypassed:
                case TraitActivationKind.KnockbackPrevented:
                case TraitActivationKind.TauntSelected:
                    return Evidence(CombatChangeEvidenceKind.Observation);
                case TraitActivationKind.MovementChanged when trait.TraitKey == TraitKey.PositionSwap:
                    var passenger = stream.OfType<CombatCharacterEvent>().SingleOrDefault(e => e.Kind == CombatEventKind.Moved &&
                        e.MovementCause?.Agency == CombatMovementAgency.Forced && e.MovementCause.TraitKey == TraitKey.PositionSwap);
                    var initiator = stream.OfType<CombatCharacterEvent>().SingleOrDefault(e => e.Kind == CombatEventKind.Moved &&
                        e.Final.EntityId == trait.SourceUnitId && e.MovementCause?.Agency == CombatMovementAgency.Voluntary &&
                        e.MovementCause.TraitKey == TraitKey.PositionSwap);
                    Require(passenger != null && initiator != null && targets.Count == 2 &&
                        targets.Contains(initiator.Final.EntityId) && targets.Contains(passenger.Final.EntityId) &&
                        initiator.Final.Anchor == passenger.Expected.Anchor && trait.TargetCells.SequenceEqual(new[] {
                            initiator.Final.Anchor.Value, passenger.Final.Anchor.Value }), "위치 교환의 두 참여자 이동 사실");
                    return Evidence(CombatChangeEvidenceKind.Observation);
                case TraitActivationKind.MovementChanged when trait.TraitKey == TraitKey.Leap:
                case TraitActivationKind.RangeChanged when trait.TraitKey == TraitKey.GroundCompaction:
                    Require(targets.Count > 0 && targets.All(id => HasMovement(stream, id)), "이동 계산 기여의 실제 이동");
                    return Evidence(CombatChangeEvidenceKind.Observation);
                case TraitActivationKind.HunterTargetSelected:
                    Require(HasTraitState(stream, trait.SourceUnitId, trait.TraitKey), "사냥꾼 소유자의 상태 변경");
                    return Evidence(CombatChangeEvidenceKind.Effect, new[] { trait.SourceUnitId });
                case TraitActivationKind.StatusApplied:
                case TraitActivationKind.StatusConsumed:
                case TraitActivationKind.StatusRemoved:
                case TraitActivationKind.StatusExpired:
                case TraitActivationKind.StatusReset:
                case TraitActivationKind.ShieldGranted:
                case TraitActivationKind.ShieldAbsorbed:
                case TraitActivationKind.ShieldRemoved:
                case TraitActivationKind.ShieldReset:
                case TraitActivationKind.AttackGranted:
                case TraitActivationKind.DefenseChanged:
                case TraitActivationKind.ArmorBroken:
                    if (targets.Count == 0 && trait.TraitKey == TraitKey.Hunter) targets = new[] { trait.SourceUnitId };
                    Require(targets.Count > 0 && targets.All(id => HasTraitState(stream, id, trait.TraitKey)), "특성 수신자의 상태 변경 " + trait.TraitKey);
                    return Evidence(CombatChangeEvidenceKind.Effect, targets);
                case TraitActivationKind.HealthRecovered:
                    Require(targets.Count > 0 && targets.All(id => stream.OfType<CombatCharacterEvent>().Any(c => c.Final.EntityId == id &&
                        c.Final.Health.CurrentHealth > c.Expected.Health.CurrentHealth)), "특성 회복 수신자의 체력 사실");
                    return Evidence(CombatChangeEvidenceKind.Effect, targets);
                case TraitActivationKind.TrailCreated:
                case TraitActivationKind.TrailChanged:
                case TraitActivationKind.TrailRemoved:
                    var sources = stream.OfType<CombatMovementSourceEvent>().Where(s => s.Final is TraitTrailFact t &&
                        t.Origin.OwnerInstanceId == trait.SourceUnitId.Value && t.Key == trait.TraitKey && trait.TargetCells.Contains(t.Cell) &&
                        (s.Expected == null || !s.Expected.SameValues(s.Final))).ToArray();
                    Require(trait.TargetCells.Count > 0 && trait.TargetCells.All(cell => sources.Any(s => s.Final.Cell == cell)), "흔적 원본의 실제 변경");
                    return Evidence(CombatChangeEvidenceKind.Observation);
                case TraitActivationKind.KnockbackApplied:
                    var attacks = stream.OfType<CombatAttackEvent>().Select(e => e.Attack)
                        .Concat(stream.OfType<CombatRoarEvent>().Where(e => e.Attack.HasValue).Select(e => e.Attack.Value))
                        .Where(a => trait.ResultOrigin == null || a.AttackGroup == trait.ResultOrigin.AttackGroup).ToArray();
                    var paths = attacks.SelectMany(a => a.Knockbacks).Where(k => targets.Contains(k.TargetId)).ToArray();
                    if (paths.Length != 0 || trait.Magnitude == 0)
                    {
                        Require(targets.Count > 0 && targets.All(id => paths.Any(p => p.TargetId == id)), "대상별 넉백 경로 사실");
                        var changed = paths.Where(p => p.Path.Count > 0).Select(p => p.TargetId).Distinct().ToArray();
                        Require(changed.All(id => HasMovement(stream, id)), "넉백 수신자의 실제 이동");
                        // 묶인 대상들의 거리가 서로 다르면 발동 Magnitude는 0이다. 실제 이동 여부는 대상별 경로가 정한다.
                        return Evidence(changed.Length == 0 ? CombatChangeEvidenceKind.Observation : CombatChangeEvidenceKind.Effect, changed);
                    }
                    Require(targets.Count > 0 && targets.All(id => HasMovement(stream, id)), "넉백 수신자의 실제 이동");
                    return Evidence(CombatChangeEvidenceKind.Effect, targets);
                case TraitActivationKind.PullApplied:
                    Require(targets.Count == 1 && stream.OfType<CombatPullEvent>().Any(p => p.Movement.TargetId == targets[0] &&
                        p.Movement.Steps.Count == trait.Magnitude), "끌어오기 수신자의 실제 걸음");
                    return Evidence(CombatChangeEvidenceKind.Effect, targets);
                case TraitActivationKind.InstantDefeat:
                    Require(targets.Count > 0 && targets.All(id => stream.OfType<CombatCharacterEvent>().Any(c => c.Final.EntityId == id &&
                        c.Kind == CombatEventKind.Died && c.Final.Health.IsDead) || stream.OfType<CombatStructureEvent>().Any(s =>
                        s.Final.EntityId == id && s.Kind == CombatEventKind.Died && s.Final.Health.IsDead)), "직접 처치 수신자의 사망 사실");
                    return Evidence(CombatChangeEvidenceKind.Effect, targets);
                case TraitActivationKind.RoarTriggered:
                    Require(stream.OfType<CombatRoarEvent>().Any(r => r.BossId == trait.SourceUnitId) && HasTraitState(stream, trait.SourceUnitId, trait.TraitKey), "포효 및 소유자의 카운터 변경");
                    return Evidence(CombatChangeEvidenceKind.Effect, new[] { trait.SourceUnitId });
                case TraitActivationKind.KillChainAttack:
                    Require(trait.ChildAttack != null, "연쇄 공격의 실제 수신 결과");
                    var recipients = ReadHits(trait.ChildAttack.Hits, stream).ToList();
                    foreach (var reaction in trait.ChildAttack.Reactions) recipients.AddRange(ReadTrait(reaction, stream).AffectedEntityIds);
                    return Evidence(CombatChangeEvidenceKind.Effect, recipients);
                case TraitActivationKind.DamageRedirected:
                    Require(trait.Interception != null, "가로채기의 실제 수신 관계");
                    // 실제 수신자의 변화는 Hits와 체력·상태 사건이 나타낸다. 보호 대상은 표시용이다.
                    return Evidence(CombatChangeEvidenceKind.Observation);
                default:
                    throw Invalid("지원하지 않는 특성 발동 " + trait.TraitActivationKind);
            }
        }

        private static bool HasTraitState(IReadOnlyList<CombatEvent> stream, EntityId id, TraitKey key) =>
            stream.OfType<CombatTraitStateEvent>().Any(t => t.Final.EntityId == id && TraitChanged(t.Expected, t.Final, key)) ||
            key == TraitKey.HuntersMark && stream.OfType<CombatHuntersMarkEvent>().Any(t => t.TargetId == id);

        private static bool TraitChanged(CharacterCombatFacts before, CharacterCombatFacts after, TraitKey key)
        {
            var a = before.Stats.TraitStatuses.ToArray().SingleOrDefault(t => t.Key == key);
            var b = after.Stats.TraitStatuses.ToArray().SingleOrDefault(t => t.Key == key);
            if (a == null ? b != null : !a.SameValues(b)) return true;
            var oldShields = before.TraitEffects.Shields.Where(g => g.Origin.DefinitionKey == (int)key).ToArray();
            var newShields = after.TraitEffects.Shields.Where(g => g.Origin.DefinitionKey == (int)key).ToArray();
            if (oldShields.Length != newShields.Length || oldShields.Zip(newShields, (x, y) => x.SameValues(y)).Any(same => !same)) return true;
            var oldStatuses = before.StatusEffects.Entries.Where(s => s.Origin.HasValue &&
                s.Origin.Value.Kind == EffectOwnerKind.Trait && s.Origin.Value.DefinitionKey == (int)key).ToArray();
            var newStatuses = after.StatusEffects.Entries.Where(s => s.Origin.HasValue &&
                s.Origin.Value.Kind == EffectOwnerKind.Trait && s.Origin.Value.DefinitionKey == (int)key).ToArray();
            if (oldStatuses.Length != newStatuses.Length || oldStatuses.Zip(newStatuses, (x, y) => x.SameValues(y)).Any(same => !same)) return true;
            var old = before.Debuffs; var next = after.Debuffs;
            return key switch
            {
                TraitKey.Cohesion => before.TraitEffects.CohesionDefenseBonus != after.TraitEffects.CohesionDefenseBonus,
                TraitKey.Disarm => old.Disarm == null ? next.Disarm != null : !old.Disarm.SameValues(next.Disarm),
                TraitKey.AnkleBreaker => old.AnkleBreaker == null ? next.AnkleBreaker != null : !old.AnkleBreaker.SameValues(next.AnkleBreaker),
                TraitKey.ArmorBreaker => old.ArmorBreak == null ? next.ArmorBreak != null : !old.ArmorBreak.SameValues(next.ArmorBreak),
                TraitKey.Intimidation => old.Intimidation == null ? next.Intimidation != null : !old.Intimidation.SameValues(next.Intimidation),
                TraitKey.HuntersMark => old.HuntersMark == null ? next.HuntersMark != null : !old.HuntersMark.SameValues(next.HuntersMark),
                _ => false
            };
        }

        private static bool HasMovement(IReadOnlyList<CombatEvent> stream, EntityId id) =>
            stream.OfType<CombatCharacterEvent>().Any(c => c.Final.EntityId == id && c.Expected.Anchor != c.Final.Anchor &&
                c.Kind is CombatEventKind.Moved or CombatEventKind.Relocated) ||
            stream.OfType<CombatStructureEvent>().Any(c => c.Final.EntityId == id && c.Expected.Anchor != c.Final.Anchor) ||
            stream.OfType<CombatPullEvent>().Any(p => p.Movement.TargetId == id);

        private static bool SamePull(AttackPullSnapshot a, AttackPullSnapshot b) => a.TargetId == b.TargetId &&
            a.StartAnchor == b.StartAnchor && a.FinalAnchor == b.FinalAnchor && a.Path.SequenceEqual(b.Path);

        private static bool HasHealthChain(IReadOnlyList<CombatEvent> stream, AttackHitResult hit)
        {
            var changes = stream.Select(f => f is CombatCharacterEvent c && c.Final.EntityId == hit.TargetId &&
                    c.Kind is CombatEventKind.HealthChanged or CombatEventKind.HealthNormalized or CombatEventKind.Died
                    ? (Valid: true, Before: c.Expected.Health.CurrentHealth, After: c.Final.Health.CurrentHealth, Normalize: c.Kind == CombatEventKind.HealthNormalized)
                    : f is CombatStructureEvent s && s.Final.EntityId == hit.TargetId
                        ? (Valid: true, Before: s.Expected.Health.CurrentHealth, After: s.Final.Health.CurrentHealth, Normalize: false)
                        : (Valid: false, Before: 0, After: 0, Normalize: false))
                .Where(c => c.Valid).ToArray();
            for (int start = 0; start < changes.Length; start++)
            {
                if (changes[start].Before != hit.TargetHealthBefore) continue;
                int health = hit.TargetHealthBefore;
                long lost = 0, recovered = 0, normalized = 0;
                for (int i = start; i < changes.Length && changes[i].Before == health; i++)
                {
                    var c = changes[i];
                    int loss = Math.Max(0, c.Before - c.After);
                    if (c.Normalize) normalized += loss; else lost += loss;
                    recovered += Math.Max(0, c.After - c.Before);
                    health = c.After;
                    if (health == hit.TargetHealthAfter && lost == hit.DamageDealt && recovered == hit.HealthRecovery && normalized == hit.HealthNormalizationLoss) return true;
                    if (lost > hit.DamageDealt || recovered > hit.HealthRecovery || normalized > hit.HealthNormalizationLoss) break;
                }
            }
            return false;
        }

        private static CombatChangeEvidence Evidence(CombatChangeEvidenceKind kind, IEnumerable<EntityId> actors = null,
            IEnumerable<CellCoord> cells = null, IEnumerable<CharacterStateChange> changes = null, IEnumerable<long> sourceIds = null) => new CombatChangeEvidence(kind,
                actors?.ToArray() ?? Array.Empty<EntityId>(), cells?.ToArray() ?? Array.Empty<CellCoord>(),
                characterChanges: changes?.ToArray(), changedSourceIds: sourceIds?.ToArray());

        private static void Require(bool valid, string detail)
        {
            if (!valid) throw Invalid(detail);
        }

        private static InvalidOperationException Invalid(string detail) => new InvalidOperationException("전투 변경 근거가 없거나 지원되지 않습니다: " + detail);
    }
}
