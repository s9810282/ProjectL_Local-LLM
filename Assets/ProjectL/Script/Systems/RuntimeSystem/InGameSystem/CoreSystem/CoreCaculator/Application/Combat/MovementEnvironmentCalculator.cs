using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>한 이동 명령 안의 진입 계산을 조정한다. 규칙에 장부·사망·도착·게시 권한을 주지 않는다.</summary>
    internal static class MovementEnvironmentCalculator
    {
        /// <summary>
        /// 공격의 강제 이동(넉백·당기기)이 닿을 수 있는 칸(<paramref name="reach"/>)의 공개 도착 원본만 검사한다(적 AI 평가).
        /// 경로 검사와 같은 규칙이다: 지뢰는 폭발 칸의 개체를 노출로 기록하고, 그 밖의 도착 콜백은 대상을 한정할 수 없어 상한을 남긴다.
        /// 밀려난 대상이 토네이도에 닿으면 착지 칸을 모르므로 판 전체의 도착 원본을 본다. 닿을 수 없는 칸의 원본은 이 공격과 무관하다.
        /// 강제 이동 자체(충돌·낙사·가시)는 정밀 미리보기가 실제 공격 계산으로 센다.
        /// </summary>
        internal static IEnumerable<CombatAssessmentGap> ForcedEntryAssessmentCoverageSteps(CombatCommandInput input,
            IReadOnlyList<CharacterCombatFacts> movers, ISet<CellCoord> reach, IReadOnlyList<IMovementTileRule> rules,
            ISet<EntityId> exposed, Action markUnbounded)
        {
            bool randomLanding = false;
            foreach (var mover in movers)
            {
                foreach (bool contact in TornadoCalculator.AssessmentEntryContacts(input, mover, reach))
                {
                    if (contact) { randomLanding = true; exposed.Add(mover.EntityId); }
                    yield return contact ? CombatAssessmentGap.RandomOutcome | CombatAssessmentGap.UnknownPosition : CombatAssessmentGap.None;
                    if (contact) break;
                }
                if (randomLanding) break;
            }
            var blastCells = new HashSet<CellCoord>();
            bool knownMineRule = rules.Any(rule => rule is MineMovementTileRule);
            foreach (var tile in input.State.Environment.Tiles)
            {
                yield return CombatAssessmentGap.None;
                if (!randomLanding && !reach.Contains(tile.Cell)) continue;
                var gap = TileAssessmentGap(tile);
                if (gap == CombatAssessmentGap.None) continue;
                if (knownMineRule && tile is MineSourceFact mine) AddMineBlast(input, mine, blastCells);
                else markUnbounded();
                yield return gap;
            }
            foreach (var gap in BlastExposureSteps(input, blastCells, exposed)) yield return gap;
        }

        /// <summary>경로에서 닿는 공개 도착 원본과 영향을 받을 수 있는 개체를 기록한다.</summary>
        internal static IEnumerable<CombatAssessmentGap> RouteAssessmentCoverageSteps(CombatCommandInput input,
            CharacterCombatFacts actor, IReadOnlyList<IMovementTileRule> rules, ISet<EntityId> exposed, Action markUnbounded)
        {
            var path = input.Path;
            var entries = new HashSet<CellCoord>();
            for (int i = 1; i < path.Cells.Count; i++)
            {
                entries.Add(path.Cells[i]);
                yield return CombatAssessmentGap.None;
            }
            var passenger = MovementRouteValidator.SwapPassenger(input, path.Endpoint);
            var exchangeEntries = passenger == null ? Array.Empty<CellCoord>() : new[] { path.Origin };
            bool randomLanding = false;
            foreach (bool contact in TornadoCalculator.AssessmentEntryContacts(input, actor, entries))
            {
                if (contact) { randomLanding = true; exposed.Add(actor.EntityId); }
                yield return contact ? CombatAssessmentGap.RandomOutcome | CombatAssessmentGap.UnknownPosition : CombatAssessmentGap.None;
                if (contact) break;
            }
            if (passenger != null)
            {
                foreach (bool contact in TornadoCalculator.AssessmentEntryContacts(input, passenger, exchangeEntries))
                {
                    if (contact) { randomLanding = true; exposed.Add(passenger.EntityId); }
                    yield return contact ? CombatAssessmentGap.RandomOutcome | CombatAssessmentGap.UnknownPosition : CombatAssessmentGap.None;
                    if (contact) break;
                }
            }

            var blastCells = new HashSet<CellCoord>();
            bool knownMineRule = rules.Any(rule => rule is MineMovementTileRule);
            bool? leapActive = null;
            List<MineSourceFact> leapCrossings = null;
            bool unresolvedArrival = false;
            foreach (var tile in input.State.Environment.Tiles)
            {
                yield return CombatAssessmentGap.None;
                if (!randomLanding && !entries.Contains(tile.Cell) && !exchangeEntries.Contains(tile.Cell)) continue;
                var gap = TileAssessmentGap(tile);
                if (gap == CombatAssessmentGap.None) continue;
                if (knownMineRule && tile is MineSourceFact mine)
                {
                    if (!randomLanding && !exchangeEntries.Contains(mine.Cell))
                    {
                        leapActive ??= CombatMovementCalculator.IsLeapActive(input, actor);
                        if (LeapTraitRule.SkipsMineEntry(leapActive.Value, mine.Cell == path.Endpoint))
                        {
                            (leapCrossings ??= new List<MineSourceFact>()).Add(mine);
                            continue;
                        }
                    }
                    AddMineBlast(input, mine, blastCells);
                }
                else
                {
                    // 임의의 도착 콜백은 대상이나 후속 이동을 제한할 근거가 없다.
                    markUnbounded();
                    unresolvedArrival = true;
                }
                yield return gap;
            }
            // 다른 진입 효과가 강제 이동을 일으키면 앞서 도약으로 건넌 지뢰에도 닿을 수 있다.
            if (unresolvedArrival && leapCrossings != null)
            {
                foreach (var mine in leapCrossings)
                {
                    AddMineBlast(input, mine, blastCells);
                    yield return TileAssessmentGap(mine);
                }
            }
            if (blastCells.Count == 0) yield break;
            if (entries.Overlaps(blastCells)) exposed.Add(actor.EntityId);
            if (passenger != null && blastCells.Contains(path.Origin)) exposed.Add(passenger.EntityId);
            foreach (var gap in BlastExposureSteps(input, blastCells, exposed)) yield return gap;
        }

        private static void AddMineBlast(CombatCommandInput input, MineSourceFact mine, ISet<CellCoord> blastCells)
        {
            foreach (var cell in MineMovementTileRule.Cells(mine.Cell, input.State.BoardWidth, input.State.BoardHeight))
                blastCells.Add(cell);
        }

        // 지뢰 폭발 칸 안의 살아 있는 유닛·구조물을 노출로 기록한다(경로·강제 이동 검사가 함께 쓴다).
        private static IEnumerable<CombatAssessmentGap> BlastExposureSteps(CombatCommandInput input, ISet<CellCoord> blastCells,
            ISet<EntityId> exposed)
        {
            if (blastCells.Count == 0) yield break;
            foreach (var unit in input.Characters)
            {
                if (unit.Area == CharacterRuntimeArea.Field && !unit.Health.IsDead && unit.OccupiedCells.Any(blastCells.Contains))
                    exposed.Add(unit.EntityId);
                yield return CombatAssessmentGap.None;
            }
            foreach (var structure in input.State.Structures)
            {
                if (!structure.Health.IsDead && structure.OccupiedCells.Any(blastCells.Contains))
                    exposed.Add(structure.EntityId);
                yield return CombatAssessmentGap.None;
            }
        }

        private static CombatAssessmentGap TileAssessmentGap(MovementTileFact tile)
        {
            if (!tile.Active || tile is StaticTerrainFact || tile is DestinationTileModifierFact)
                return CombatAssessmentGap.None;
            return tile is TornadoSourceFact
                ? CombatAssessmentGap.RandomOutcome | CombatAssessmentGap.UnknownPosition
                : CombatAssessmentGap.UnsupportedEffect;
        }

        internal static void ResolveObjectsAt(MovementProjection projection, EntityId arriver, CellCoord cell,
            IReadOnlyList<IMovementTileRule> rules, bool skipMines = false)
        {
            foreach (var _ in ResolveObjectsAtSteps(projection, arriver, cell, rules, skipMines)) { }
        }

        internal static IEnumerable<string> ResolveObjectsAtSteps(MovementProjection projection, EntityId arriver, CellCoord cell,
            IReadOnlyList<IMovementTileRule> rules, bool skipMines = false)
        {
            var sources = ObjectArrivalCalculator.OrderedAt(projection.Environment, projection.Bindings, cell);
            foreach (var source in sources)
            {
                var current = projection.Character(arriver);
                if (current.Health.IsDead || current.Area != CharacterRuntimeArea.Field || current.Anchor != cell || projection.Terminal) yield break;
                yield return "MovementObjectCandidate";
                var tile = (ObjectSourceFact)projection.Source(source.Id);
                if (!tile.Active || skipMines && tile.IsMine) continue;
                var rule = rules.SingleOrDefault(r => r.Key == tile.RuleKey) ??
                    throw new NotSupportedException("Object의 진입 규칙이 연결되지 않았습니다: " + tile.RuleKey);
                using var observation = projection.BeginCurrentInput();
                while (!observation.IsComplete)
                {
                    observation.Step();
                    yield return "MovementObjectObservation";
                }
                var targets = rule.TargetIds(tile, arriver, observation.Result)
                    ?? throw new InvalidOperationException("Object 후보 대상은 null일 수 없습니다.");
                var effects = rule.Evaluate(projection.StepInput(cell, MovementEffectPhase.Hazard, targets, arriver), tile) ??
                    throw new InvalidOperationException("Object 효과는 null일 수 없습니다.");
                if (effects.Damage.Select(r => r.TargetId).Concat(effects.Healing.Select(r => r.TargetId))
                    .Concat(effects.DebuffChanges.Select(r => r.TargetId)).Concat(effects.MovementMethods.Select(r => r.TargetId)).Concat(effects.HeldChanges.Select(r => r.TargetId))
                    .Any(id => !targets.Contains(id)))
                    throw new InvalidOperationException("Object 효과가 선언한 후보 대상 밖을 변경할 수 없습니다.");
                if (effects.ForcedMovement.Count != 0)
                    throw new NotSupportedException("Object 자체의 강제 이동 요청은 아직 지원하지 않습니다.");
                yield return "MovementObjectEffects";
                // 원본 변경이 없으면 규칙이 발동하지 않기로 판단한 것이다. 다른 효과 없이 원본을 남긴다.
                if (effects.ExpectedSource == null)
                {
                    if (effects.Damage.Count != 0 || effects.Healing.Count != 0 || effects.MovementMethods.Count != 0 ||
                        effects.HeldChanges.Count != 0 || effects.DebuffChanges.Count != 0)
                        throw new InvalidOperationException("발동하지 않은 Object는 다른 효과를 낼 수 없습니다.");
                    continue;
                }
                if (effects.FinalSource.Active)
                    throw new InvalidOperationException("발동한 Object는 선택된 원본만 비활성화해야 합니다.");
                projection.Events.Add(new CombatObjectActivationEvent(tile, arriver));
                foreach (var held in effects.HeldChanges) projection.ChangeHeld(held);
                projection.ChangeSource(tile, effects);
                var origin = effects.Damage.Count == 0 ? null : new CombatDamageOrigin(cell, null, projection.NextAttackGroup());
                foreach (var damage in effects.Damage)
                    foreach (var phase in projection.ChangeHealthSteps(damage, false, origin)) yield return phase;
                foreach (var debuff in effects.DebuffChanges) projection.ChangeDebuffs(debuff);
                foreach (var method in effects.MovementMethods) projection.ChangeMethod(method);
                projection.ResolveDeathBoundary();
                yield return "MovementObjectDeaths";
                if (projection.Character(arriver).Health.IsDead || projection.Terminal) yield break;
                foreach (var heal in effects.Healing)
                    foreach (var phase in projection.ChangeHealthSteps(heal, true)) yield return phase;
            }
        }

        internal static void RequireRules(IReadOnlyList<IMovementTileRule> rules)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (rules.Any(r => r == null || string.IsNullOrWhiteSpace(r.Key)) ||
                rules.Select(r => r.Key).Distinct(StringComparer.Ordinal).Count() != rules.Count)
                throw new ArgumentException("규칙 키는 유효하며 중복될 수 없습니다.", nameof(rules));
        }

        internal static void ResolveEntry(MovementProjection projection, CellCoord enteredCell, IReadOnlyList<IMovementTileRule> rules,
            EntityId? mover = null, bool consumeThorn = true, bool skipMines = false)
        {
            foreach (var _ in ResolveEntrySteps(projection, enteredCell, rules, mover, consumeThorn, skipMines)) { }
        }

        internal static IEnumerable<string> ResolveEntrySteps(MovementProjection projection, CellCoord enteredCell,
            IReadOnlyList<IMovementTileRule> rules, EntityId? mover = null, bool consumeThorn = true, bool skipMines = false)
        {
            var arriver = mover ?? projection.Input.ActorId;
            bool Interrupted() => projection.Character(arriver).Health.IsDead || projection.Terminal ||
                projection.Character(arriver).Area != CharacterRuntimeArea.Field || projection.Character(arriver).Anchor != enteredCell;
            if (Interrupted()) yield break;
            if (consumeThorn)
                foreach (var phase in ResolveThornContactSteps(projection, arriver, enteredCell, rules)) yield return phase;
            if (Interrupted()) yield break;
            bool thrown = false;
            foreach (var phase in TornadoCalculator.ResolveGroupedEntrySteps(projection, arriver, enteredCell, rules, value => thrown = value))
                yield return phase;
            if (thrown) yield break;
            foreach (var phase in new[] { MovementEffectPhase.Hazard, MovementEffectPhase.Recovery })
            {
                if (Interrupted()) yield break;
                if (phase == MovementEffectPhase.Recovery)
                {
                    foreach (var child in ResolveObjectsAtSteps(projection, arriver, enteredCell, rules, skipMines)) yield return child;
                    if (Interrupted()) yield break;
                }
                var ids = projection.Environment.Tiles.Where(t => t.Active && t.Cell == enteredCell &&
                        t.TriggersArrivalRule && (!skipMines || !t.IsMine) &&
                        !projection.Bindings.ObjectSourceIds.Contains(t.Id))
                    .OrderBy(t => t.Priority).ThenBy(t => t.Id).Select(t => t.Id).ToArray();
                foreach (var id in ids)
                {
                    if (Interrupted()) yield break;
                    yield return "MovementTileCandidate";
                    var tile = projection.Source(id);
                    if (!tile.Active) continue;
                    var rule = rules.SingleOrDefault(r => r.Key == tile.RuleKey) ??
                        throw new NotSupportedException("활성 이동 원본의 작성 규칙이 연결되지 않았습니다: " + tile.RuleKey);
                    using var observation = projection.BeginCurrentInput();
                    while (!observation.IsComplete)
                    {
                        observation.Step();
                        yield return "MovementTileObservation";
                    }
                    var targetIds = rule.TargetIds(tile, arriver, observation.Result) ??
                        throw new InvalidOperationException("규칙 후보 대상은 null일 수 없습니다.");
                    var effects = rule.Evaluate(projection.StepInput(enteredCell, phase, targetIds, arriver), tile) ??
                        throw new InvalidOperationException("진입 규칙은 null 결과를 반환할 수 없습니다.");
                    if (effects.Damage.Select(r => r.TargetId).Concat(effects.Healing.Select(r => r.TargetId))
                        .Concat(effects.ForcedMovement.Select(r => r.TargetId)).Concat(effects.DebuffChanges.Select(r => r.TargetId)).Concat(effects.MovementMethods.Select(r => r.TargetId))
                        .Any(id => !targetIds.Contains(id)))
                        throw new InvalidOperationException("규칙 결과가 선언한 후보 대상 밖을 변경할 수 없습니다.");
                    if (phase == MovementEffectPhase.Hazard && effects.Healing.Count != 0 ||
                        phase == MovementEffectPhase.Recovery && (effects.Damage.Count != 0 || effects.ForcedMovement.Count != 0))
                        throw new InvalidOperationException("위험 및 생존 후 회복 단계의 효과를 섞을 수 없습니다.");
                    if (effects.HeldChanges.Count != 0)
                        throw new NotSupportedException("보유 효과 변경은 진입 순서를 가진 Object 규칙만 지원합니다.");
                    projection.ChangeSource(tile, effects);
                    var origin = effects.Damage.Count == 0 ? null : new CombatDamageOrigin(tile.Cell, null, projection.NextAttackGroup());
                    yield return "MovementTileEffects";
                    foreach (var damage in effects.Damage)
                        foreach (var child in projection.ChangeHealthSteps(damage, false, origin)) yield return child;
                    foreach (var debuff in effects.DebuffChanges) projection.ChangeDebuffs(debuff);
                    foreach (var method in effects.MovementMethods) projection.ChangeMethod(method);
                    projection.ResolveDeathBoundary();
                    yield return "MovementTileDeaths";
                    if (Interrupted()) yield break;
                    foreach (var forced in effects.ForcedMovement)
                    {
                        foreach (var child in ResolveForcedMovementSteps(projection, forced, rules)) yield return child;
                        if (Interrupted()) yield break;
                    }
                    foreach (var heal in effects.Healing)
                        foreach (var child in projection.ChangeHealthSteps(heal, true)) yield return child;
                }
            }
        }

        /// <summary>이동 원인과 무관하게 현재 대상과 턴 소유자로 가시 접촉을 적용한다.</summary>
        private static IEnumerable<string> ResolveThornContactSteps(MovementProjection projection, EntityId mover, CellCoord cell,
            IReadOnlyList<IMovementTileRule> rules)
        {
            if (!projection.HasThorn(cell)) yield break;
            var effects = RequiredThorn(rules).EvaluateThorn(
                projection.StepInput(cell, MovementEffectPhase.Hazard, mover: mover), projection.Thorn(cell));
            projection.ConsumeThorn(cell);
            var origin = new CombatDamageOrigin(cell, null, projection.NextAttackGroup());
            yield return "MovementThornEffects";
            foreach (var damage in effects.Damage)
                foreach (var phase in projection.ChangeHealthSteps(damage, false, origin)) yield return phase;
            foreach (var debuff in effects.DebuffChanges) projection.ChangeDebuffs(debuff);
            projection.ResolveDeathBoundary();
            yield return "MovementThornDeaths";
        }

        private static ThornMovementTileRule RequiredThorn(IReadOnlyList<IMovementTileRule> rules) =>
            rules.OfType<ThornMovementTileRule>().SingleOrDefault() ??
            throw new NotSupportedException("가시밭 진입 규칙이 구성되지 않았습니다.");

        private static IEnumerable<string> ResolveForcedMovementSteps(MovementProjection projection, MovementForcedMoveRequest request,
            IReadOnlyList<IMovementTileRule> rules)
        {
            if (request.Distance == 0) yield break;
            var before = projection.Character(request.TargetId);
            if (before.Health.IsDead || before.Area != CharacterRuntimeArea.Field) yield break;
            if (before.OccupiedCells.Count != 1) throw new NotSupportedException("다중 셀 강제 이동은 연결되지 않았습니다.");
            using var observation = projection.BeginCurrentInput();
            while (!observation.IsComplete)
            {
                observation.Step();
                yield return "TileForcedObservation";
            }
            var input = observation.Result;
            // 강제 이동 면역은 원인과 관계없이 이동만 막는다(G9).
            if (UnitBehaviorRules.IsForcedMovementImmune(input, before, projection.ProfileReads)) yield break;
            foreach (var phase in ResolveForcedStepsSteps(projection, input, request.TargetId,
                ImpactPatternCalculator.GetForwardStep(request.Direction), request.Distance,
                request.Cause == MovementDisplacementCause.Knockback, null, rules, null))
                yield return phase;
        }

        /// <summary>
        /// 이동 투영 위 한 칸 캐릭터를 <paramref name="step"/>(8방향 한 칸) 쪽으로 최대 <paramref name="distance"/>칸 강제 이동합니다.
        /// 칸마다 낙사·약한 지반·가시·토네이도·오브젝트를 즉시 정산하고, 끊기지 않았으면 마지막 칸에서 강제 도착을 처리합니다.
        /// 면역 판정과 여러 칸 유닛 처리는 호출자가 합니다. 타일 효과 강제 이동과 재구성 부활 넉백이 이 입구 하나를 씁니다.
        /// </summary>
        /// <param name="knockbackTerrain">참이면 넉백 지형 규칙(높은 지대 등)으로 진입을 검사합니다(G8-6).</param>
        /// <param name="movementCause">이동 사건에 남길 원인입니다. 비우면 원인 특성 없는 강제 이동입니다.</param>
        internal static IEnumerable<string> ResolveForcedStepsSteps(MovementProjection projection, CombatCommandInput input,
            EntityId targetId, CellCoord step, int distance, bool knockbackTerrain, CombatMovementCause movementCause,
            IReadOnlyList<IMovementTileRule> rules, Action<ForcedDisplacementPath> finished)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            if (input == null) throw new ArgumentNullException(nameof(input));
            var before = projection.Character(targetId);
            if (before.OccupiedCells.Count != 1) throw new NotSupportedException("다중 셀 강제 이동은 연결되지 않았습니다.");
            var profile = CombatCommandCalculator.RequiredProfile(input, before, projection.ProfileReads);
            var footprint = new TileFootprint(profile.Definition.FootprintWidth, profile.Definition.FootprintHeight,
                profile.Definition.FootprintLocalCells.ToArray());
            var rotation = before.Rotation.Value;
            bool departed = false;
            // 거리 N을 한 칸씩 진행한다. 칸 검사는 기존 조합(넉백 원인일 때만 높은 지대 규칙)을 그대로 쓴다(G8-6).
            ForcedDisplacementPath path = null;
            foreach (var phase in ForcedDisplacementPathCalculator.ExecuteSteps(before.Anchor.Value,
                step, distance, CanEnter, Enter, value => path = value))
                yield return phase;
            // 도중 사망·전투 종료로 끊긴 경우가 아니면 마지막으로 들어간 칸에서 강제 도착을 처리한다.
            if (path.Steps.Count != 0 && path.Stop != ForcedDisplacementStop.Interrupted)
                foreach (var phase in projection.ResolveForcedArrivalSteps(before.EntityId, _ => { })) yield return phase;
            finished?.Invoke(path);

            IEnumerable<string> CanEnter(CellCoord from, CellCoord to, Action<bool> completed)
            {
                using var stepInput = projection.BeginCurrentInput();
                while (!stepInput.IsComplete)
                {
                    stepInput.Step();
                    yield return "TileForcedAdmissionObservation";
                }
                var current = stepInput.Result;
                if (knockbackTerrain)
                {
                    projection.TerrainQueryCells.Add(from);
                    projection.TerrainQueryCells.Add(to);
                    if (!EnvironmentTerrainRules.CanKnockback(current.State.Environment, current.EnvironmentBindings, from, to)) yield break;
                }
                completed(current.State.IsInside(to) &&
                    !EnvironmentTerrainRules.IsWallOccupied(current.State.Environment, current.EnvironmentBindings, to) &&
                    !current.Characters.Any(c => c.EntityId != before.EntityId && !c.Health.IsDead && c.OccupiedCells.Contains(to)) &&
                    !current.State.Structures.Any(s => !s.Health.IsDead && s.OccupiedCells.Contains(to)));
                yield return "TileForcedAdmission";
            }

            IEnumerable<string> Enter(CellCoord from, CellCoord to, bool last, Action<bool> completed)
            {
                var prior = projection.Character(before.EntityId);
                var moving = prior;
                if (!departed)
                {
                    // 위치 보정은 출발할 때 한 번만 해제한다.
                    moving = PositionTileModifierCalculator.Replace(prior, PositionTileModifier.None);
                    PositionTileModifierCalculator.RecordNormalization(projection.Events, prior, moving);
                    departed = true;
                }
                var after = CombatMovementCalculator.MoveTo(moving, to);
                projection.RecordForcedMove(prior, after, movementCause: movementCause);
                if (projection.ResolveForcedTerrainEntry(before.EntityId, to)) yield break;
                yield return "TileForcedEntry";
                foreach (var cell in MovementPathCalculator.ResolvePathCells(from, to, footprint, rotation))
                {
                    foreach (var phase in ResolveThornContactSteps(projection, before.EntityId, cell, rules)) yield return phase;
                    if (projection.Terminal || projection.Character(before.EntityId).Health.IsDead) yield break;
                }
                // 지나가는 칸과 마지막 칸 모두 오브젝트를 즉시 처리한다(G8-2 B). 타일 강제 이동은 원래 즉시 처리 경로다.
                if (!projection.Terminal)
                {
                    bool thrown = false;
                    foreach (var phase in TornadoCalculator.ResolveGroupedEntrySteps(projection, before.EntityId, to, rules, value => thrown = value))
                        yield return phase;
                    if (thrown || projection.Terminal || projection.Character(before.EntityId).Health.IsDead ||
                        projection.Character(before.EntityId).Anchor != to) yield break;
                }
                foreach (var phase in ResolveObjectsAtSteps(projection, before.EntityId, to, rules)) yield return phase;
                completed(last || !projection.Character(before.EntityId).Health.IsDead && !projection.Terminal);
                yield return "TileForcedArrival";
            }
        }
    }
}
