using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>한 순회 또는 독립 진입의 중첩 착지 동안만 유지되는 접촉 장부다.</summary>
    internal sealed class TornadoContactResolution
    {
        internal HashSet<(long SourceId, EntityId UnitId)> Thrown { get; } = new();
        internal Dictionary<long, int> CenterSteps { get; } = new();
    }

    /// <summary>칸 진입과 완료된 진영 턴 경계에서 실행되는 명령 내부 지형 계산이다.</summary>
    public static class TornadoCalculator
    {
        /// <summary>현재 공개 영역에 예정된 도착 위치가 닿는지 난수 없이 검사한다.</summary>
        internal static IEnumerable<bool> AssessmentEntryContacts(CombatCommandInput input, CharacterCombatFacts unit,
            IEnumerable<CellCoord> anchors)
        {
            if (input.Battle == null || input.State.Turn.StagePhase != StagePhase.Combat ||
                input.State.Phase == CombatFlowPhase.Finished) yield break;
            foreach (var entry in input.State.Gimmicks.Entries)
            {
                yield return false;
                if (entry is not TornadoGimmickState group || UnitBehaviorRules.IsForcedMovementImmune(input, unit)) continue;
                var spec = (TornadoGimmickSpec)input.Battle.GimmickDefinitions[group.Id.Value - 1];
                foreach (var member in spec.Members)
                {
                    yield return false;
                    if (!member.KnownTo.Contains(unit.Owner)) continue;
                    var center = group.Members.Single(state => state.SourceId == member.SourceId).Center;
                    foreach (var anchor in anchors)
                        yield return Touches(unit, center, member.SideLength, anchor - unit.Anchor.Value);
                }
            }
        }

        internal static bool ResolveGroupedEntry(MovementProjection projection, EntityId mover, CellCoord enteredCell,
            IReadOnlyList<IMovementTileRule> rules)
        {
            bool thrown = false;
            foreach (var _ in ResolveGroupedEntrySteps(projection, mover, enteredCell, rules, value => thrown = value)) { }
            return thrown;
        }

        internal static IEnumerable<string> ResolveGroupedEntrySteps(MovementProjection projection, EntityId mover,
            CellCoord enteredCell, IReadOnlyList<IMovementTileRule> rules, Action<bool> completed)
        {
            using (var observation = projection.BeginCurrentInput())
            {
                while (!observation.IsComplete)
                {
                    observation.Step();
                    yield return "TornadoContactObservation";
                }
                if (!ContactEligible(observation.Result.State, projection)) yield break;
            }
            var group = projection.Gimmicks.Entries.OfType<TornadoGimmickState>().SingleOrDefault();
            if (group == null) yield break;
            var unit = projection.Character(mover);
            if (unit.Health.IsDead || unit.Area != CharacterRuntimeArea.Field || unit.Anchor != enteredCell) yield break;
            var spec = (TornadoGimmickSpec)projection.Input.Battle.GimmickDefinitions[group.Id.Value - 1];
            var permitted = spec.Members.Where(m => !projection.TornadoObserver.HasValue || m.KnownTo.Contains(projection.TornadoObserver.Value)).ToArray();
            if (!permitted.Any(m => Touches(unit, group.Members.Single(s => s.SourceId == m.SourceId).Center, m.SideLength))) yield break;
            bool owns = projection.TornadoResolution == null;
            if (owns)
            {
                projection.TornadoResolution = new TornadoContactResolution();
                projection.Events.Add(new CombatTornadoEntryEvent(group.Id, mover, enteredCell));
            }
            var resolution = projection.TornadoResolution;
            int before = resolution.Thrown.Count(pair => pair.UnitId == mover);
            try
            {
                foreach (var member in permitted)
                {
                    unit = projection.Character(mover);
                    if (projection.Terminal || unit.Health.IsDead || unit.Area != CharacterRuntimeArea.Field || unit.Anchor != enteredCell) break;
                    foreach (var phase in ThrowSteps(projection, group.Id, member, mover, rules)) yield return phase;
                    yield return "TornadoContactMember";
                }
            }
            finally
            {
                if (owns) projection.TornadoResolution = null;
            }
            completed(resolution.Thrown.Count(pair => pair.UnitId == mover) != before);
            yield return "TornadoContactComplete";
        }

        internal static bool ContactEligible(MovementProjection projection)
        {
            return ContactEligible(projection.CurrentInput().State, projection);
        }

        private static bool ContactEligible(CombatCommandState state, MovementProjection projection) =>
            state.Turn.StagePhase == StagePhase.Combat && state.Phase != CombatFlowPhase.Finished && !projection.Terminal;

        internal static void ResolveSweepCenter(MovementProjection projection, StageGimmickId id, TornadoMemberSpec spec,
            CellCoord center, int step, IReadOnlyList<IMovementTileRule> rules)
        {
            if (!ContactEligible(projection)) return;
            projection.TornadoResolution.CenterSteps[spec.SourceId] = step;
            projection.Events.Add(new CombatTornadoCenterEvent(new TornadoContactOrigin(id, spec.SourceId, center, step)));
            var recipients = projection.CurrentInput().Characters.Where(c => Touches(c, center, spec.SideLength))
                .OrderBy(c => c.EntityId).Select(c => c.EntityId).ToArray();
            foreach (var recipient in recipients)
            {
                if (projection.Terminal) break;
                foreach (var _ in ThrowSteps(projection, id, spec, recipient, rules)) { }
            }
        }

        internal static IReadOnlyList<CellCoord> GroupedLandingCandidates(CombatCommandInput input, EntityId mover)
        {
            var candidates = new List<CellCoord>();
            foreach (var _ in LandingSteps(input, mover, candidates)) { }
            return candidates.ToArray();
        }

        private static IEnumerable<string> LandingSteps(CombatCommandInput input, EntityId mover, List<CellCoord> candidates,
            IReadOnlyCollection<CellCoord> reservations = null)
        {
            var unit = input.Characters.Single(c => c.EntityId == mover);
            var ignored = new[] { mover };
            foreach (var cell in Cells(input.State.BoardWidth, input.State.BoardHeight))
            {
                if (cell != unit.Anchor && (reservations == null || !reservations.Contains(cell)) &&
                    MovementRouteValidator.CanOccupy(input, cell, ignored)) candidates.Add(cell);
                yield return "TornadoLandingCell";
            }
        }

        private static bool Touches(CharacterCombatFacts unit, CellCoord center, int side, CellCoord offset = default) =>
            !unit.Health.IsDead && unit.Area == CharacterRuntimeArea.Field &&
            unit.OccupiedCells.Any(c => Math.Abs((long)c.X + offset.X - center.X) <= side / 2 &&
                Math.Abs((long)c.Y + offset.Y - center.Y) <= side / 2);

        private static IEnumerable<string> ThrowSteps(MovementProjection projection, StageGimmickId id, TornadoMemberSpec spec, EntityId target,
            IReadOnlyList<IMovementTileRule> rules)
        {
            var group = (TornadoGimmickState)projection.Gimmicks.Get(id);
            var storm = group.Members.Single(m => m.SourceId == spec.SourceId);
            var unit = projection.Character(target);
            if (!Touches(unit, storm.Center, spec.SideLength) || projection.TornadoResolution.Thrown.Contains((spec.SourceId, target))) yield break;
            using var observation = projection.BeginCurrentInput();
            while (!observation.IsComplete)
            {
                observation.Step();
                yield return "TornadoThrowObservation";
            }
            var current = observation.Result;
            if (UnitBehaviorRules.IsForcedMovementImmune(current, unit, projection.ProfileReads)) yield break;
            if (unit.OccupiedCells.Count != 1) throw new NotSupportedException("다중 셀 토네이도 이동에는 전체 점유 배치 지원이 필요합니다.");
            var candidates = new List<CellCoord>();
            foreach (var phase in LandingSteps(current, target, candidates, projection.GimmickReservations)) yield return phase;
            if (candidates.Count == 0) yield break;
            var destination = candidates[projection.ChooseRandom(candidates.Count)];
            // 실제 이동이 확정된 뒤 중첩 착지보다 먼저 표시한다.
            projection.TornadoResolution.Thrown.Add((spec.SourceId, target));
            int step = projection.TornadoResolution.CenterSteps.TryGetValue(spec.SourceId, out int index) ? index : -1;
            projection.RecordForcedMove(unit, CombatMovementCalculator.MoveTo(unit, destination), spec.SourceId,
                new TornadoContactOrigin(id, spec.SourceId, storm.Center, step));
            // 과거 도착 의존은 새 이동의 도착 완료 근거가 아니다. 중첩 착지가 새로 기록한 값만 재사용한다.
            projection.ArrivalCells.Remove(target);
            yield return "TornadoRelocation";
            if (!projection.ResolveForcedTerrainEntry(target, destination))
            {
                foreach (var phase in projection.ReconcilePresenceSteps(new[] { unit.Anchor.Value, destination })) yield return phase;
                foreach (var phase in MovementEnvironmentCalculator.ResolveEntrySteps(projection, destination, rules, target)) yield return phase;
                var actual = projection.Character(target);
                if (!projection.Terminal && !actual.Health.IsDead && actual.Area == CharacterRuntimeArea.Field && actual.Anchor == destination &&
                    (!projection.ArrivalCells.TryGetValue(target, out var settled) || settled != destination))
                    foreach (var phase in projection.ResolveForcedArrivalSteps(target, _ => { })) yield return phase;
                projection.ResolveDeathBoundary();
                yield return "TornadoDeaths";
            }
        }

        private static IEnumerable<CellCoord> Cells(int boardWidth, int boardHeight)
        {
            for (int y = 0; y < boardHeight; y++)
                for (int x = 0; x < boardWidth; x++) yield return new CellCoord(x, y);
        }
    }
}
