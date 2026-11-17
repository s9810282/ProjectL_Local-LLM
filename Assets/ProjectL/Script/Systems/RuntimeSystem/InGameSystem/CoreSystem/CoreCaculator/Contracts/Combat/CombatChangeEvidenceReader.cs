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
        /// <summary>게시할 전이의 체력 실행을 먼저 검증하고 비체력 변경 근거를 함께 읽습니다.</summary>
        public static IReadOnlyList<CombatChangeEvidence> ReadAll(CombatCommandTransition transition)
        {
            if (transition == null) throw new ArgumentNullException(nameof(transition));
            CombatHealthEvidenceValidator.Validate(transition);
            ObstacleDurabilityEvidenceValidator.Validate(transition);
            PillarTerrainEvidenceValidator.Validate(transition);
            PlatformMotionEvidenceValidator.Validate(transition);
            ValidateThornBombChanges(transition);
            return Array.AsReadOnly(transition.Events.Select((fact, index) => Read(fact, transition.Events, index, true)).ToArray());
        }

        /// <summary>독립 사건 표본의 형식 검사입니다. 실행·선언 연결이 없으므로 게시 검증에 사용할 수 없습니다.</summary>
        public static IReadOnlyList<CombatChangeEvidence> ReadEventShapes(IReadOnlyList<CombatEvent> facts)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            if (facts.Any(f => f == null)) throw Invalid("null 사건");
            return Array.AsReadOnly(facts.Select((fact, index) => Read(fact, facts, index, false)).ToArray());
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

        private static CombatChangeEvidence Read(CombatEvent fact, IReadOnlyList<CombatEvent> stream, int index, bool healthValidated)
        {
            switch (fact)
            {
                case CombatCharacterEvent c when c.Kind is CombatEventKind.Moved or CombatEventKind.Placed or CombatEventKind.Returned or
                    CombatEventKind.HealthChanged or CombatEventKind.HealthNormalized or CombatEventKind.Died or CombatEventKind.Relocated or
                    CombatEventKind.Reconstructed:
                    var kind = c.Kind == CombatEventKind.Returned ? CombatChangeEvidenceKind.Return : CombatChangeEvidenceKind.Effect;
                    if (c.Kind == CombatEventKind.Moved && c.MovementCause?.Agency != CombatMovementAgency.Forced &&
                        c.Expected.WithPlacement(c.Final.Area, c.Final.ReserveSlotIndex, c.Final.Anchor, c.Final.Rotation,
                            c.Final.OccupiedCells).SameValues(c.Final)) kind = CombatChangeEvidenceKind.OrdinaryMove;
                    return Evidence(kind, new[] { c.Final.EntityId }, changes: new[] { new CharacterStateChange(c.Expected, c.Final) });
                case CombatStructureEvent s when s.Kind is CombatEventKind.HealthChanged or CombatEventKind.Died:
                    return Evidence(CombatChangeEvidenceKind.Effect, new[] { s.Final.EntityId });
                case CombatAttackEvent a when a.Kind == CombatEventKind.Attacked:
                    return ReadAttack(a.Attack, stream, CombatChangeEvidenceKind.Action, healthValidated);
                case CombatPlayerSkillAttackEvent skill when skill.Kind == CombatEventKind.PlayerSkillAttacked:
                    return ReadPlayerSkillAttack(skill, stream, index, healthValidated);
                case CombatRoarEvent r when r.Kind == CombatEventKind.Roared:
                    return r.Attack.HasValue ? ReadAttack(r.Attack.Value, stream, CombatChangeEvidenceKind.Effect, healthValidated) :
                        Evidence(CombatChangeEvidenceKind.Effect, new[] { r.BossId });
                case CombatEffectsEvent e when e.Kind == CombatEventKind.EffectsActivated:
                    if (e.Categories.Count != 0) throw Invalid("지원하지 않는 Category 발동");
                    if (e.Traits.Count == 0) throw Invalid("비어 있는 효과 발동");
                    var traits = e.Traits.Select(t => ReadTrait(t, stream, healthValidated)).ToArray();
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
                case CombatSweepEvent s when s.Kind == CombatEventKind.Swept:
                    return Evidence(CombatChangeEvidenceKind.Observation);
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
                case CombatThornBombAttachedEvent attached:
                    return Evidence(CombatChangeEvidenceKind.Effect, new[] { attached.Bomb.TargetId });
                case CombatThornBombDetonatedEvent detonated:
                    return Evidence(CombatChangeEvidenceKind.Effect, new[] { detonated.Bomb.TargetId });
                case CombatThornBombDiscardedEvent discarded:
                    return Evidence(CombatChangeEvidenceKind.Effect, new[] { discarded.Bomb.TargetId });
                case CombatSoulEvent s when s.Kind is CombatEventKind.SoulsCollected or CombatEventKind.SoulFieldSettled:
                    if (s.Kind == CombatEventKind.SoulFieldSettled) return ReadChanges(s.Field);
                    Require(!s.CollectorId.IsEmpty && s.Characters != null && s.ConsumedIds.Count > 0, "영혼 수집자와 실제 소비 사실");
                    try { _ = new SoulActionChanges(s.Characters, s.Field, s.ConsumedIds); }
                    catch (ArgumentException exception) { throw new InvalidOperationException("영혼 사건의 범위가 불완전합니다.", exception); }
                    return Evidence(CombatChangeEvidenceKind.Effect, s.Characters.Characters.Where(c => !c.Expected.SameValues(c.Final))
                        .Select(c => c.Final.EntityId).Append(s.CollectorId), ReadChanges(s.Field).ChangedSourceCells);
                case CombatArtifactEvent a when a.Kind == CombatEventKind.ArtifactActivated:
                    return ReadArtifact(a, stream, healthValidated);
                case CombatObstacleReactionEvent o when o.Kind == CombatEventKind.ObstacleReacted:
                    if (!healthValidated)
                        Require(stream.OfType<CombatObstacleDurabilityEvent>().Any(change =>
                            change.ObstacleId == o.ObstacleId && change.Cause != ObstacleDestructionCause.Fall &&
                            change.Final.IsDestroyed == (o.Reaction == ObstacleReactionKind.Destroyed)),
                            "장애물 반응의 실제 상태 변경");
                    return Evidence(CombatChangeEvidenceKind.Effect, new[] { o.ObstacleId });
                case CombatObstacleDurabilityEvent durability:
                    return Evidence(CombatChangeEvidenceKind.Effect, new[] { durability.ObstacleId },
                        durability.Final.IsDestroyed ? durability.Expected.OccupiedCells : Array.Empty<CellCoord>());
                case CombatPillarFallEvent fall:
                    return Evidence(CombatChangeEvidenceKind.Effect,
                        fall.CrushedUnitIds.Concat(fall.DestroyedObstacleIds).Append(fall.PillarId), fall.ImpactCells);
                case CombatPillarTerrainEvent terrain:
                    return Source(terrain.Expected, terrain.Final);
                case CombatPlatformBoundaryEvent:
                    return Evidence(CombatChangeEvidenceKind.Lifecycle);
                case CombatPlatformMotionEvent motion:
                    return Evidence(CombatChangeEvidenceKind.Effect, cells:
                        motion.Steps.SelectMany(s => s.Expected.OccupiedCells.Concat(s.Final.OccupiedCells)));
                case CombatPlatformStepEvent step:
                    return Evidence(CombatChangeEvidenceKind.Effect, step.Passengers.Select(p => p.EntityId),
                        step.Expected.OccupiedCells.Concat(step.Final.OccupiedCells), sourceIds: new[] { step.PlatformSourceId });
                case CombatEnvironmentRandomEvent:
                    return Evidence(CombatChangeEvidenceKind.Observation);
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
                case CombatMeteorEvent meteor:
                    return Evidence(CombatChangeEvidenceKind.Effect, cells: meteor.Step switch
                    {
                        MeteorGimmickStep.Warning => meteor.Final.Warnings.SelectMany(site => site.Cells),
                        MeteorGimmickStep.Expired => meteor.Expected.Flames.Where(f =>
                            f.ExpiresAtBoundaryCount <= meteor.Boundary.CompletedControllerTurnCount).SelectMany(f => f.Footprint.Cells).Distinct(),
                        _ => Array.Empty<CellCoord>()
                    });
                case CombatLavaChainEvent chain:
                    return Evidence(CombatChangeEvidenceKind.Effect, Array.Empty<EntityId>(), chain.HeadPath);
                case CombatLavaChainContactEvent contact:
                    return Evidence(CombatChangeEvidenceKind.Observation, Array.Empty<EntityId>(), Array.Empty<CellCoord>());
                case CombatMeteorImpactEvent impact:
                    return Evidence(CombatChangeEvidenceKind.Effect, impact.RecipientIds, impact.Footprint.Cells);
                case CombatSummonEvent s when s.Kind == CombatEventKind.Summoned:
                    return Evidence(CombatChangeEvidenceKind.Effect, new[] { s.Created.EntityId });
                case CombatReinforcementEvent r when r.Kind == CombatEventKind.Reinforced:
                    return Evidence(CombatChangeEvidenceKind.Effect, new[] { r.Created.EntityId });
                case CombatItemEvent i when i.Kind is CombatEventKind.ItemActivated or CombatEventKind.ItemEquipmentChanged or CombatEventKind.ItemDiscarded:
                    return ReadItem(i, stream, healthValidated);
                case CombatExperienceEvent e when e.Kind is CombatEventKind.ExperienceEarned or CombatEventKind.ExperienceApplied:
                    return Evidence(CombatChangeEvidenceKind.Effect, new[] { e.RecipientId });
                case CombatTurnEvent t when t.Kind is CombatEventKind.CombatEntered or CombatEventKind.ControllerTurnStarted or
                    CombatEventKind.PlayerTurnEnded or CombatEventKind.EnemyPhaseEnded:
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

        /// <summary>충돌 전부터 있던 다른 출처 속박이 그대로 남은 경우입니다(긴 쪽 규칙으로 낙석 속박이 끼지 않음).</summary>
        private static bool HasLongerBind(CharacterCombatFacts before, CharacterCombatFacts after)
        {
            var prior = before.StatusEffects.Entries.OfType<ActionRestrictionStatus>().SingleOrDefault(s => s.Key == StatusEffectKey.Bind);
            return prior != null && after.StatusEffects.Entries.OfType<ActionRestrictionStatus>()
                .Any(s => s.Key == StatusEffectKey.Bind && s.Origin == prior.Origin && s.RemainingAffectedSideTurnEnds == prior.RemainingAffectedSideTurnEnds);
        }

        private static void ValidateRockfallRecipients(CombatRockfallEvent rock, IReadOnlyList<CombatEvent> stream, int next)
        {
            // 충돌 바로 뒤에 수신자 순서대로 체력 변화(있을 때)와 생존자의 속박이 기록된다.
            // 치명을 재구성으로 버틴 수신자는 체력 변화 대신 재구성 사건(최대 체력으로 남음)이 오고, 생존자처럼 속박을 받는다.
            var cells = rock.Expected.WarnedCells.ToHashSet();
            foreach (var id in rock.RecipientIds)
            {
                CharacterCombatFacts hurt = null;
                if (next < stream.Count && stream[next] is CombatCharacterEvent hp && hp.Final.EntityId == id)
                {
                    Require(hp.Expected.Area == CharacterRuntimeArea.Field && hp.Expected.OccupiedCells.Any(cells.Contains) &&
                        (hp.Kind == CombatEventKind.Reconstructed ||
                            hp.Expected.Health.CurrentHealth > hp.Final.Health.CurrentHealth &&
                            hp.Kind == (hp.Final.Health.IsDead ? CombatEventKind.Died : CombatEventKind.HealthChanged)), "낙석 충돌 직후의 체력 변화");
                    next++;
                    hurt = hp.Final;
                    if (hurt.Health.IsDead) continue;
                }
                // 같은 키 제한은 남은 횟수가 긴 쪽이 남으므로, 다른 출처의 더 긴 속박이 이미 있으면 낙석 속박은 끼지 않는다.
                Require(next < stream.Count && stream[next] is CombatTraitStateEvent grant && grant.Final.EntityId == id &&
                    grant.Expected.Area == CharacterRuntimeArea.Field && grant.Expected.OccupiedCells.Any(cells.Contains) &&
                    (hurt == null || hurt.SameValues(grant.Expected)) && !grant.Final.Health.IsDead &&
                    (HasRockfallBind(grant.Final, rock.Expected) || HasLongerBind(grant.Expected, grant.Final)), "낙석 생존자의 충돌 직후 속박");
                next++;
            }
        }

        /// <summary>부착·소모 순서로 활성 상태와 다음 폭탄 식별자를 재구성해 게시 제안과 대조한다.</summary>
        private static void ValidateThornBombChanges(CombatCommandTransition transition)
        {
            var before = transition.Input.State.ThornBombs;
            var final = transition.FinalState.ThornBombs;
            Dictionary<EntityId, ThornBombSnapshot> active = null;
            long nextId = before.NextBombId;
            foreach (var fact in transition.Events)
            {
                switch (fact)
                {
                    case CombatThornBombAttachedEvent attached:
                        Require(transition.Accepted, "거절된 명령의 가시 폭탄 부착");
                        active ??= before.Bombs.ToDictionary(b => b.TargetId);
                        var bomb = attached.Bomb;
                        Require(bomb.BombId == nextId && active.TryAdd(bomb.TargetId, bomb), "가시 폭탄 발급 순번 또는 중복 부착");
                        nextId = checked(nextId + 1);
                        break;
                    case CombatThornBombDetonatedEvent detonated:
                        Require(transition.Accepted, "거절된 명령의 가시 폭탄 기폭");
                        active ??= before.Bombs.ToDictionary(b => b.TargetId);
                        Require(active.TryGetValue(detonated.Bomb.TargetId, out var current) && current.SameValues(detonated.Bomb),
                            "기폭으로 소모한 활성 가시 폭탄");
                        // 합성 전이는 독립 계산의 공격 그룹을 합치므로 그룹 번호는 대조하지 않는다(Phase 2 기록).
                        Require(transition.Input.State.IsInside(detonated.Center), "가시 폭탄 폭심지");
                        active.Remove(detonated.Bomb.TargetId);
                        break;
                    case CombatThornBombDiscardedEvent discarded:
                        Require(transition.Accepted, "거절된 명령의 가시 폭탄 소멸");
                        active ??= before.Bombs.ToDictionary(b => b.TargetId);
                        Require(active.TryGetValue(discarded.Bomb.TargetId, out var lost) && lost.SameValues(discarded.Bomb),
                            "소멸한 활성 가시 폭탄");
                        // 터지지 않고 사라지는 경우는 낙사뿐이다(설계 R15).
                        Require(transition.Events.OfType<CombatFallEvent>().Any(f => f.EntityId == discarded.Bomb.TargetId),
                            "가시 폭탄 소멸 대상의 낙사 사실");
                        active.Remove(discarded.Bomb.TargetId);
                        break;
                }
            }

            if (transition.Outcome.IsStageEnded)
            {
                Require(final.Bombs.Count == 0 && final.NextBombId == nextId, "전투 종료 후 남은 폭탄 또는 발급 순번");
                return;
            }
            Require(active == null ? before.SameValues(final) :
                nextId == final.NextBombId && active.Count == final.Bombs.Count &&
                final.Bombs.All(b => active.TryGetValue(b.TargetId, out var current) && current.SameValues(b)),
                "가시 폭탄 사건과 최종 활성 상태 또는 다음 식별자");
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
                else if (fact is CombatLavaChainEvent chain)
                {
                    Require(state.Get(chain.Expected.Id).SameValues(chain.Expected), "사슬의 현재 기대값");
                    seen.Add(chain.Expected.Id);
                    state = state.Replace(chain.Final);
                }
                else if (fact is CombatMeteorEvent meteor)
                {
                    Require(state.Get(meteor.Expected.Id).SameValues(meteor.Expected), "운석의 현재 기대값");
                    seen.Add(meteor.Expected.Id);
                    state = state.Replace(meteor.Final);
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

        private static CombatChangeEvidence ReadAttack(AttackResult attack, IReadOnlyList<CombatEvent> stream, CombatChangeEvidenceKind kind, bool healthValidated)
        {
            Require(attack.Succeeded && !attack.AttackerId.IsEmpty, "수락된 공격 사실");
            var actors = new List<EntityId> { attack.AttackerId };
            actors.AddRange(ReadHits(attack.Hits.ToArray(), stream, healthValidated));
            foreach (var trait in attack.TraitActivations.ToArray()) actors.AddRange(ReadTrait(trait, stream, healthValidated).AffectedEntityIds);
            Require(attack.CategoryActivations.Length == 0, "지원하지 않는 Category 발동");
            if (attack.Pull != null)
                Require(stream.OfType<CombatPullEvent>().Any(p => SamePull(p.Movement, attack.Pull)), "공격의 끌어오기 사실");
            foreach (var knockback in attack.Knockbacks.Where(k => k.Path.Count > 0))
                Require(HasMovement(stream, knockback.TargetId), "공격의 넉백 이동 사실");
            return Evidence(kind, actors);
        }

        private static IEnumerable<EntityId> ReadHits(IReadOnlyList<AttackHitResult> hits, IReadOnlyList<CombatEvent> stream, bool healthValidated)
        {
            foreach (var hit in hits)
            {
                Require(!hit.TargetId.IsEmpty, "공격의 실제 수신자");
                if (hit.DamageDealt == 0 && hit.HealthRecovery == 0 && hit.HealthNormalizationLoss == 0) continue;
                if (!healthValidated) Require(HasHealthChain(stream, hit), "공격 수신자의 체력·회복·상한 정규화 사실 " + hit.TargetId);
                yield return hit.TargetId;
            }
        }

        private static CombatChangeEvidence ReadPlayerSkillAttack(CombatPlayerSkillAttackEvent occurrence,
            IReadOnlyList<CombatEvent> stream, int index, bool healthValidated)
        {
            var attack = occurrence.Attack;
            var actors = new List<EntityId> { attack.ActorId };
            actors.AddRange(ReadHits(attack.Hits, stream, healthValidated));
            foreach (var trait in attack.TraitActivations) actors.AddRange(ReadTrait(trait, stream, healthValidated).AffectedEntityIds);
            Require(attack.CategoryActivations.Count == 0, "지원하지 않는 Category 발동");
            var effects = stream.Skip(index + 1).TakeWhile(e => e is not CombatPlayerSkillAttackEvent).ToArray();
            foreach (var knockback in attack.Knockbacks)
            {
                var from = knockback.StartAnchor;
                int cursor = 0;
                foreach (var to in knockback.Path)
                {
                    bool Matches(CombatEvent e) => knockback.TargetKind == AttackKnockbackTargetKind.Character
                        ? e is CombatCharacterEvent c && c.Kind == CombatEventKind.Moved && c.Final.EntityId == knockback.TargetId &&
                            c.MovementCause?.Agency == CombatMovementAgency.Forced && c.Expected.Anchor == from && c.Final.Anchor == to
                        : e is CombatStructureEvent s && s.Final.EntityId == knockback.TargetId &&
                            s.MovementCause?.Agency == CombatMovementAgency.Forced && s.Expected.Anchor == from && s.Final.Anchor == to;
                    while (cursor < effects.Length && !Matches(effects[cursor])) cursor++;
                    Require(cursor < effects.Length, "스킬 넉백 경로와 실제 강제 이동 사건");
                    cursor++;
                    from = to;
                }
            }
            return Evidence(CombatChangeEvidenceKind.Action, actors);
        }

        private static CombatChangeEvidence ReadArtifact(CombatArtifactEvent artifact, IReadOnlyList<CombatEvent> stream, bool healthValidated)
        {
            Require(artifact.Activations.Count > 0 && artifact.Activations.All(a => a != null), "Artifact 발동 값");
            if (artifact.Purpose == CombatArtifactPurpose.AttackPowerContribution)
                return Evidence(CombatChangeEvidenceKind.Observation);
            Require(artifact.Purpose is CombatArtifactPurpose.Healing or CombatArtifactPurpose.Reflection, "Artifact 발동 목적");
            foreach (var activation in artifact.Activations.Where(a => !healthValidated && a.AppliedAmount > 0))
            {
                long amount = ReadHealthChanges(stream.OfType<CombatCharacterEvent>(), activation.TargetId)
                    .Sum(c => (long)(artifact.Purpose == CombatArtifactPurpose.Healing ? c.Recovery : c.Damage));
                Require(amount >= activation.AppliedAmount, "Artifact 실제 수신자의 체력 사실");
            }
            return Evidence(CombatChangeEvidenceKind.Effect, artifact.Activations.Where(a => a.AppliedAmount > 0).Select(a => a.TargetId));
        }

        private static CombatChangeEvidence ReadItem(CombatItemEvent item, IReadOnlyList<CombatEvent> stream, bool healthValidated)
        {
            if (item.Kind != CombatEventKind.ItemActivated) return Evidence(CombatChangeEvidenceKind.Effect, new[] { item.HolderId });
            Require(item.ActivationKind != ItemActivationKind.Unspecified, "아이템 발동 효과 종류");
            if (item.ActivationKind == ItemActivationKind.DamageMitigation)
                Require(item.Stage == ItemEventStage.BeforeDamage && item.TargetId == item.HolderId, "피해 감소의 소지자·단계");
            if (item.ActivationKind == ItemActivationKind.Healing)
            {
                Require(item.Stage == ItemEventStage.AfterDamage && item.TargetId == item.HolderId, "회복의 소지자·단계");
                if (!healthValidated)
                {
                    long recovered = ReadHealthChanges(stream.OfType<CombatCharacterEvent>(), item.HolderId).Sum(c => (long)c.Recovery);
                    Require(recovered >= item.AppliedMagnitude, "아이템 회복의 실제 체력 사실");
                }
            }
            // 계산 수치가 0이어도 새 임시 효과 슬롯은 실제 수신자의 상태 변경이다. 중복 소모는 소지자만 바뀐다.
            return Evidence(CombatChangeEvidenceKind.Effect, item.ActivationKind == ItemActivationKind.TemporaryEffect && item.Reason == ItemEventReason.Activated
                ? new[] { item.HolderId, item.TargetId.Value } : new[] { item.HolderId });
        }

        private static CombatChangeEvidence ReadTrait(TraitActivationSnapshot trait, IReadOnlyList<CombatEvent> stream, bool healthValidated)
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
                    Require(targets.Count > 0 && (healthValidated || targets.All(id => ReadHealthChanges(stream.OfType<CombatCharacterEvent>(), id)
                        .Any(c => c.Recovery > 0))), "특성 회복 수신자의 체력 사실");
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
                    var paths = attacks.SelectMany(a => a.Knockbacks).Concat(stream.OfType<CombatPlayerSkillAttackEvent>()
                        .Where(e => trait.ResultOrigin == null || e.Attack.AttackGroup == trait.ResultOrigin.AttackGroup)
                        .SelectMany(e => e.Attack.Knockbacks)).Where(k => targets.Contains(k.TargetId)).ToArray();
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
                    // 재구성 보유자는 즉사를 버텨 사망 대신 재구성 사건이 남는다.
                    Require(targets.Count > 0 && targets.All(id => stream.OfType<CombatCharacterEvent>().Any(c => c.Final.EntityId == id &&
                        (c.Kind == CombatEventKind.Died && c.Final.Health.IsDead || c.Kind == CombatEventKind.Reconstructed)) ||
                        stream.OfType<CombatStructureEvent>().Any(s =>
                        s.Final.EntityId == id && s.Kind == CombatEventKind.Died && s.Final.Health.IsDead)), "직접 처치 수신자의 사망 사실");
                    return Evidence(CombatChangeEvidenceKind.Effect, targets);
                case TraitActivationKind.RoarTriggered:
                    Require(stream.OfType<CombatRoarEvent>().Any(r => r.BossId == trait.SourceUnitId) && HasTraitState(stream, trait.SourceUnitId, trait.TraitKey), "포효 및 소유자의 카운터 변경");
                    return Evidence(CombatChangeEvidenceKind.Effect, new[] { trait.SourceUnitId });
                case TraitActivationKind.KillChainAttack:
                    Require(trait.ChildAttack != null, "연쇄 공격의 실제 수신 결과");
                    var recipients = ReadHits(trait.ChildAttack.Hits, stream, healthValidated).ToList();
                    foreach (var reaction in trait.ChildAttack.Reactions) recipients.AddRange(ReadTrait(reaction, stream, healthValidated).AffectedEntityIds);
                    return Evidence(CombatChangeEvidenceKind.Effect, recipients);
                case TraitActivationKind.SelfDestructExploded:
                    // 폭발 대상은 실제로 바뀐 개체만 싣는다. 체력·보호막(특성 상태)·구조물 사건 중 하나가 남아 있어야 한다.
                    Require(targets.All(id => stream.OfType<CombatCharacterEvent>().Any(c => c.Final.EntityId == id) ||
                        stream.OfType<CombatTraitStateEvent>().Any(t => t.Final.EntityId == id) ||
                        stream.OfType<CombatStructureEvent>().Any(s => s.Final.EntityId == id) ||
                        stream.OfType<CombatObstacleDurabilityEvent>().Any(s => s.ObstacleId == id)), "자폭 수신자의 실제 변화");
                    return Evidence(targets.Count == 0 ? CombatChangeEvidenceKind.Observation : CombatChangeEvidenceKind.Effect, targets);
                case TraitActivationKind.ReconstructionStarted:
                    Require(targets.Count == 1 && stream.OfType<CombatCharacterEvent>().Any(c => c.Final.EntityId == targets[0] &&
                        c.Kind == CombatEventKind.Reconstructed), "재구성 수신자의 재구성 사실");
                    return Evidence(CombatChangeEvidenceKind.Effect, targets);
                case TraitActivationKind.ReconstructionEnded:
                    Require(targets.Count == 1 && stream.OfType<CombatTraitStateEvent>().Any(t => t.Final.EntityId == targets[0] &&
                        t.Expected.StatusEffects.Entries.Any(s => s is ReconstructionStatus) &&
                        !t.Final.StatusEffects.Entries.Any(s => s is ReconstructionStatus)), "부활 수신자의 재구성 상태 해제");
                    return Evidence(CombatChangeEvidenceKind.Effect, targets);
                case TraitActivationKind.CorpseDisplaced:
                    // 시체 밀기는 위치 사건 없이 경로만 남긴다. 출처 유닛의 사망 사실과 두 칸 이상의 경로가 필요하다.
                    Require(trait.TargetCells.Count >= 2 && stream.OfType<CombatCharacterEvent>().Any(c => c.Final.EntityId == trait.SourceUnitId &&
                        c.Kind == CombatEventKind.Died && c.Final.Health.IsDead), "시체 밀기의 사망 사실과 경로");
                    return Evidence(CombatChangeEvidenceKind.Observation);
                case TraitActivationKind.CrashImpactCollided:
                    // 부딪힘은 막은 칸 하나와 받는 쪽(날아간 유닛, 캐릭터에 부딪혔으면 그 유닛)을 싣는다.
                    // 피해가 0이거나(비율 0·전투 종료) 보호막·보유 방어가 모두 막으면 체력 변화가 없을 수 있다.
                    Require(trait.TargetCells.Count == 1 && targets.Count >= 1 && targets.Count <= 2, "부딪힘 칸과 받는 쪽");
                    var crashed = targets.Where(id => stream.OfType<CombatCharacterEvent>().Any(c => c.Final.EntityId == id) ||
                        stream.OfType<CombatTraitStateEvent>().Any(t => t.Final.EntityId == id) ||
                        stream.OfType<CombatHeldEnvironmentEvent>().Any(h => h.CharacterId == id)).ToArray();
                    return Evidence(crashed.Length == 0 ? CombatChangeEvidenceKind.Observation : CombatChangeEvidenceKind.Effect, crashed);
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
            var changes = ReadHealthChanges(stream, hit.TargetId).ToArray();
            for (int start = 0; start < changes.Length; start++)
            {
                if (changes[start].Before != hit.TargetHealthBefore) continue;
                int health = hit.TargetHealthBefore;
                long lost = 0, recovered = 0, normalized = 0;
                for (int i = start; i < changes.Length && changes[i].Before == health; i++)
                {
                    var c = changes[i];
                    lost += c.Damage;
                    recovered += c.Recovery;
                    normalized += c.NormalizationLoss;
                    health = c.After;
                    if (health == hit.TargetHealthAfter && lost == hit.DamageDealt && recovered == hit.HealthRecovery && normalized == hit.HealthNormalizationLoss) return true;
                    if (lost > hit.DamageDealt || recovered > hit.HealthRecovery || normalized > hit.HealthNormalizationLoss) break;
                }
            }
            return false;
        }

        /// <summary>체력 사건의 의미는 여기에서만 해석하고, 각 검증은 필요한 합계나 연속성을 확인합니다.</summary>
        private static IEnumerable<HealthChange> ReadHealthChanges(IEnumerable<CombatEvent> stream, EntityId targetId)
        {
            foreach (var fact in stream)
            {
                if (fact is CombatCharacterEvent character && character.Final.EntityId == targetId)
                {
                    int before = character.Expected.Health.CurrentHealth;
                    int after = character.Final.Health.CurrentHealth;
                    if (character.Kind == CombatEventKind.Reconstructed)
                    {
                        // 최종 체력 차이와 실제 피해·회복은 다릅니다. 기존 재구성 사건은 손실 뒤 회복한 두 단계입니다.
                        yield return new HealthChange(before, 0);
                        yield return new HealthChange(0, after);
                    }
                    else if (character.Kind is CombatEventKind.HealthChanged or CombatEventKind.HealthNormalized or CombatEventKind.Died)
                        yield return new HealthChange(before, after, character.Kind == CombatEventKind.HealthNormalized);
                }
                else if (fact is CombatStructureEvent structure && structure.Final.EntityId == targetId)
                    yield return new HealthChange(structure.Expected.Health.CurrentHealth, structure.Final.Health.CurrentHealth);
            }
        }

        /// <summary>기존 사건에서 읽은 한 체력 변화입니다. 전투 상태나 새로운 피해 계산을 소유하지 않습니다.</summary>
        private readonly struct HealthChange
        {
            public int Before { get; }
            public int After { get; }
            public int Damage { get; }
            public int Recovery { get; }
            public int NormalizationLoss { get; }

            public HealthChange(int before, int after, bool normalize = false)
            {
                Before = before;
                After = after;
                int loss = Math.Max(0, before - after);
                Damage = normalize ? 0 : loss;
                Recovery = Math.Max(0, after - before);
                NormalizationLoss = normalize ? loss : 0;
            }
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
