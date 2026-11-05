using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>원본·시계·중심·이동의 기록 대응만 검사한다. 대상과 착지 후보 및 난수 선택은 재실행하지 않는다.</summary>
    internal static class TornadoChangeEvidenceValidator
    {
        internal static void Validate(CombatCommandTransition transition)
        {
            var groups = transition.Events.OfType<CombatTornadoGimmickEvent>().ToArray();
            var entryEvents = transition.Events.OfType<CombatTornadoEntryEvent>().ToArray();
            var grouped = transition.Input.State.Gimmicks.Entries.OfType<TornadoGimmickState>().SingleOrDefault();
            if (groups.Length == 0 && entryEvents.Length == 0 && grouped == null &&
                !transition.Events.OfType<CombatTornadoCenterEvent>().Any() &&
                !transition.Events.OfType<CombatTornadoMemberEvent>().Any() &&
                !transition.Events.OfType<CombatCharacterEvent>().Any(c => c.TornadoContact != null)) return;
            Require(groups.Length <= 1 && grouped != null, "준비된 단일 토네이도 그룹");
            var definitions = transition.Input.Battle?.GimmickDefinitions;
            Require(definitions != null && grouped.Id.Value <= definitions.Count && definitions[grouped.Id.Value - 1] is TornadoGimmickSpec,
                "토네이도 원본 정의");
            var spec = (TornadoGimmickSpec)definitions[grouped.Id.Value - 1];
            transition.Input.State.Gimmicks.ValidateDefinitions(definitions, transition.Input.State.BoardWidth, transition.Input.State.BoardHeight);
            var characters = transition.Input.Characters.ToDictionary(c => c.EntityId);
            var centers = grouped.Members.ToDictionary(m => m.SourceId, m => m.Center);
            var steps = new Dictionary<long, int>();
            var thrown = new HashSet<(long, EntityId)>();
            var moved = new HashSet<EntityId>();
            var pulls = new Dictionary<EntityId, (CombatPullEvent Event, int NextStep)>();
            var scheduled = new Queue<TornadoContactOrigin>();
            var pendingMembers = new Queue<TornadoMemberAdvance>();
            bool active = false, terminal = false;
            CombatTornadoGimmickEvent group = null;
            for (int index = 0; index < transition.Events.Count; index++)
            {
                var fact = transition.Events[index];
                if (fact is CombatTornadoGimmickEvent change)
                {
                    Require(!terminal && !active && grouped.SameValues(change.Expected), "그룹 진행의 현재 기대값");
                    Require(transition.Input.State.Turn.StagePhase == StagePhase.Combat && transition.Input.State.Phase != CombatFlowPhase.Finished, "전투 중의 예약 진행");
                    Require(transition.Events.Take(index).OfType<CombatTurnEvent>().Any(e =>
                        e.Kind == (change.Boundary.EndedSide == Owner.Player ? CombatEventKind.PlayerTurnEnded : CombatEventKind.EnemyPhaseEnded) &&
                        e.Expected.CompletedControllerTurnCount + 1 == e.Final.CompletedControllerTurnCount &&
                        e.Final.CompletedControllerTurnCount == change.Boundary.CompletedControllerTurnCount &&
                        e.Final.CompletedControllerTurnCount == transition.FinalState.Turn.CompletedControllerTurnCount), "예약 진행 이전의 실제 진영 종료");
                    ValidateAdvance(transition, spec, change);
                    group = change;
                    grouped = change.Final;
                    active = true;
                    steps.Clear();
                    thrown.Clear();
                    foreach (var advance in change.Advances) pendingMembers.Enqueue(advance);
                    foreach (var advance in change.Advances.Where(a => a.IsTravel))
                        for (int i = 0; i < advance.Path.Count; i++)
                            scheduled.Enqueue(new TornadoContactOrigin(change.Expected.Id, advance.Expected.SourceId, advance.Path[i], i));
                }
                else if (fact is CombatTornadoEntryEvent entry)
                {
                    // 끌어오기는 기존 Pulled 경로로 이동을 게시한다. 별도 Moved 사건을 추가하여 연출을 중복시키지 않는다.
                    if (pulls.TryGetValue(entry.EntrantId, out var pullHistory))
                    {
                        int reached = -1;
                        for (int i = pullHistory.NextStep; i < pullHistory.Event.Movement.Path.Count; i++)
                            if (pullHistory.Event.Movement.Path[i] == entry.Cell) { reached = i; break; }
                        Require(reached >= pullHistory.NextStep, "한 번만 순서대로 소비하는 끌어오기 걸음");
                        var pulling = characters[entry.EntrantId];
                        characters[entry.EntrantId] = pulling.WithPlacement(pulling.Area, pulling.ReserveSlotIndex, entry.Cell,
                            pulling.Rotation, new[] { entry.Cell });
                        pulls[entry.EntrantId] = (pullHistory.Event, reached + 1);
                    }
                    Require(!terminal && scheduled.Count == 0 && pendingMembers.Count == 0 && entry.GimmickId == grouped.Id &&
                        transition.Input.State.Turn.StagePhase == StagePhase.Combat && transition.Input.State.Phase != CombatFlowPhase.Finished &&
                        characters.TryGetValue(entry.EntrantId, out var entrant) &&
                        !entrant.Health.IsDead && entrant.Area == CharacterRuntimeArea.Field && entrant.Anchor == entry.Cell &&
                        spec.Members.Any(m => Touches(entrant, centers[m.SourceId], m.SideLength)), "독립된 실제 정지 영역 진입");
                    active = true;
                    group = null;
                    steps.Clear();
                    thrown.Clear();
                }
                else if (fact is CombatTornadoCenterEvent reached)
                {
                    Require(!terminal && active && group != null && scheduled.Count > 0, "종료 전 실제 순회 중심");
                    var expected = scheduled.Dequeue();
                    Require(pendingMembers.Count > 0 && pendingMembers.Peek().Expected.SourceId == expected.SourceId &&
                        SameOrigin(expected, reached.Origin), "순서대로 도달한 대각선 중심");
                    centers[expected.SourceId] = expected.Center;
                    steps[expected.SourceId] = expected.StepIndex;
                }
                else if (fact is CombatTornadoMemberEvent completed)
                {
                    Require(active && group != null && completed.GimmickId == group.Expected.Id && pendingMembers.Count > 0,
                        "예정된 원본의 실제 완료 기록");
                    var expected = pendingMembers.Dequeue();
                    var actual = completed.Advance;
                    Require(expected.Expected.SameValues(actual.Expected) && expected.Final.SameValues(actual.Final) &&
                        expected.SideLength == actual.SideLength && expected.Path.SequenceEqual(actual.Path) &&
                        (!actual.IsTravel || steps.TryGetValue(actual.Expected.SourceId, out int step) && step == actual.Path.Count - 1),
                        "모든 중심·착지 이후의 동일한 원본 완료");
                    if (terminal) Require(pendingMembers.Count == 0 && actual.IsTravel &&
                        actual.Final.NextBoundaryCount == actual.Expected.NextBoundaryCount, "종료 이후 후속 원본·재예약 금지");
                }
                else if (fact is CombatCharacterEvent character)
                {
                    if (character.Kind == CombatEventKind.Relocated &&
                        (character.TornadoContact != null || spec.Members.Any(m => m.SourceId == character.TornadoSourceId)))
                    {
                        var origin = character.TornadoContact;
                        Require(!terminal && active && origin != null && origin.GimmickId == grouped.Id &&
                            character.TornadoSourceId == origin.SourceId && centers.ContainsKey(origin.SourceId), "재배치의 실제 토네이도 원본");
                        var member = spec.Members.Single(m => m.SourceId == origin.SourceId);
                        Require(origin.Center == centers[origin.SourceId] && origin.StepIndex ==
                            (steps.TryGetValue(origin.SourceId, out int step) ? step : -1), "재배치의 현재 중심과 실제 순번");
                        Require(thrown.Add((origin.SourceId, character.Final.EntityId)), "동일 원본의 진입 체인당 한 번 투척");
                        Require(characters.TryGetValue(character.Expected.EntityId, out var current) &&
                            current.Anchor == character.Expected.Anchor && current.Area == character.Expected.Area &&
                            current.Health.Equals(character.Expected.Health) && Touches(current, origin.Center, member.SideLength), "현재 접촉한 생존 필드 개체");
                        Require(character.Expected.OccupiedCells.Count == 1 && character.Final.Area == CharacterRuntimeArea.Field &&
                            character.Final.Anchor.HasValue && character.Final.Anchor != character.Expected.Anchor &&
                            transition.FinalState.IsInside(character.Final.Anchor.Value) &&
                            character.Expected.WithPlacement(character.Final.Area, character.Final.ReserveSlotIndex, character.Final.Anchor,
                                character.Final.Rotation, character.Final.OccupiedCells).SameValues(character.Final), "재배치의 실제 위치만 변경");
                        moved.Add(character.Final.EntityId);
                    }
                    if (character.Kind == CombatEventKind.Relocated || character.Kind == CombatEventKind.Moved ||
                        character.Kind == CombatEventKind.Placed || character.Kind == CombatEventKind.Returned)
                        pulls.Remove(character.Final.EntityId);
                    characters[character.Final.EntityId] = character.Final;
                    if (character.Final.Health.IsDead && (character.Final.OwnershipRole == CharacterOwnershipRole.Main ||
                        character.Final.EntityId == transition.Input.State.Objective.TargetEntityId))
                    {
                        ValidateTerminalMember(pendingMembers);
                        terminal = true;
                    }
                }
                else if (fact is CombatTraitStateEvent status) characters[status.Final.EntityId] = status.Final;
                else if (fact is CombatPullEvent pull)
                {
                    Require(characters.TryGetValue(pull.Movement.TargetId, out var start) && start.Anchor == pull.Movement.StartAnchor,
                        "현재 위치에서 시작한 끌어오기 경로");
                    pulls[pull.Movement.TargetId] = (pull, 0);
                }
                else if (fact is CombatSoulEvent soul && soul.Characters != null)
                {
                    foreach (var collected in soul.Characters.Characters) characters[collected.Final.EntityId] = collected.Final;
                }
                else if (fact is CombatStructureEvent structure && structure.Final.Health.IsDead &&
                    transition.Input.State.Objective.Kind == BattleObjectiveKind.EnemyHeadquartersDestroyed &&
                    structure.Final.Owner == Owner.Enemy && structure.Final.Definition.Key == EntityKey.Structure_Headquarters)
                {
                    ValidateTerminalMember(pendingMembers);
                    terminal = true;
                }
                else if (fact is CombatOutcomeEvent outcome && outcome.Outcome.IsStageEnded) terminal = true;
            }
            Require(scheduled.Count == 0, "누락 없는 실제 중심 기록");
            Require(pendingMembers.Count == 0, "누락 없는 실제 원본 완료 기록");
            foreach (var id in moved)
            {
                var published = transition.Characters.Characters.SingleOrDefault(c => c.Final.EntityId == id)?.Final ??
                    transition.Input.Characters.Single(c => c.EntityId == id);
                Require(published.Anchor == characters[id].Anchor && published.Area == characters[id].Area &&
                    published.Health.Equals(characters[id].Health), "재배치·착지 사건과 설치할 위치·체력의 대응");
            }
        }

        private static void ValidateTerminalMember(Queue<TornadoMemberAdvance> pending)
        {
            // 명령 끝의 지반 붕괴처럼 모든 원본 완료 뒤 발생한 종료는 이미 기록된 순서를 유지한다.
            if (pending.Count == 0) return;
            var current = pending.Peek();
            Require(pending.Count == 1 && current.IsTravel && current.Final.Phase == TornadoGimmickPhase.Cooldown &&
                current.Final.NextBoundaryCount == current.Expected.NextBoundaryCount, "치명적 착지의 마지막 원본과 미예약 기한");
        }

        private static void ValidateAdvance(CombatCommandTransition transition, TornadoGimmickSpec spec, CombatTornadoGimmickEvent change)
        {
            long count = change.Boundary.CompletedControllerTurnCount;
            Require(count == change.Expected.NextBoundaryCount, "그룹의 다음 예정 경계");
            var expectedMembers = change.Expected.Members.ToDictionary(m => m.SourceId);
            var due = change.Expected.Members.Where(m => m.NextBoundaryCount == count).Select(m => m.SourceId).ToArray();
            var actual = change.Advances.Select(a => a.Expected.SourceId).ToArray();
            Require(actual.Length <= due.Length && actual.SequenceEqual(due.Take(actual.Length)) &&
                (actual.Length == due.Length || transition.Outcome.IsStageEnded), "예정 원본의 정렬된 실행 접두 목록");
            for (int i = 0; i < change.Advances.Count; i++)
            {
                var advance = change.Advances[i];
                Require(expectedMembers.TryGetValue(advance.Expected.SourceId, out var before) && before.SameValues(advance.Expected), "원본 진행 기대값");
                var definition = spec.Members.Single(m => m.SourceId == before.SourceId);
                Require(advance.SideLength == definition.SideLength, "준비된 정사각형 크기");
                var final = advance.Final;
                if (!advance.IsTravel)
                {
                    Require(final.Phase == TornadoGimmickPhase.Warning && final.NextBoundaryCount == checked(count + definition.WarningLeadControllerTurns) &&
                        final.HorizontalSign == TornadoGeometry.InwardHorizontalSign(before.Center, definition.SideLength,
                            transition.FinalState.BoardWidth, before.HorizontalSign), "경고 이후 이동 기한·가로 방향");
                }
                else
                {
                    var path = TornadoGeometry.Travel(before.Center, definition.SideLength, definition.DiagonalDistance,
                        transition.FinalState.BoardWidth, transition.FinalState.BoardHeight, before.LockedDirection.Value);
                    Require(advance.Path.Count <= path.Count && advance.Path.SequenceEqual(path.Take(advance.Path.Count)), "실제 이동의 대각선 접두 경로");
                    bool stopped = final.NextBoundaryCount == before.NextBoundaryCount;
                    if (stopped)
                        Require(transition.Outcome.IsStageEnded && i == change.Advances.Count - 1 &&
                            final.Phase == TornadoGimmickPhase.Cooldown && final.HorizontalSign == before.HorizontalSign, "종료 시 새 예약 없는 마지막 원본");
                    else
                    {
                        bool warning = definition.WarningLeadControllerTurns == definition.RepeatIntervalControllerTurns;
                        Require(advance.Path.Count == path.Count && final.Phase == (warning ? TornadoGimmickPhase.Warning : TornadoGimmickPhase.Cooldown) &&
                            final.NextBoundaryCount == checked(count + definition.RepeatIntervalControllerTurns - (warning ? 0 : definition.WarningLeadControllerTurns)) &&
                            final.HorizontalSign == TornadoGeometry.InwardHorizontalSign(final.Center, definition.SideLength,
                                transition.FinalState.BoardWidth, before.HorizontalSign), "완료 이동의 다음 경고·이동 기한·가로 방향");
                    }
                }
                expectedMembers[before.SourceId] = final;
            }
            Require(new TornadoGimmickState(change.Expected.Id, expectedMembers.Values.ToArray()).SameValues(change.Final), "진행 원본과 최종 그룹의 정확한 대응");
            change.Final.ValidateDefinition(spec, transition.FinalState.BoardWidth, transition.FinalState.BoardHeight);
        }

        private static bool Touches(CharacterCombatFacts character, CellCoord center, int side) =>
            character.Area == CharacterRuntimeArea.Field && !character.Health.IsDead && character.OccupiedCells.Any(c =>
                Math.Abs((long)c.X - center.X) <= side / 2 && Math.Abs((long)c.Y - center.Y) <= side / 2);

        private static bool SameOrigin(TornadoContactOrigin a, TornadoContactOrigin b) =>
            a.GimmickId == b.GimmickId && a.SourceId == b.SourceId && a.Center == b.Center && a.StepIndex == b.StepIndex;

        private static void Require(bool condition, string detail)
        {
            if (!condition) throw new InvalidOperationException("토네이도 변경 근거가 잘못되었습니다: " + detail);
        }
    }
}
