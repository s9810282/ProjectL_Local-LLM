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
        internal static bool ResolveGroupedEntry(MovementProjection projection, EntityId mover, CellCoord enteredCell,
            IReadOnlyList<IMovementTileRule> rules)
        {
            if (!ContactEligible(projection)) return false;
            var group = projection.Gimmicks.Entries.OfType<TornadoGimmickState>().SingleOrDefault();
            if (group == null) return false;
            var unit = projection.Character(mover);
            if (unit.Health.IsDead || unit.Area != CharacterRuntimeArea.Field || unit.Anchor != enteredCell) return false;
            var spec = (TornadoGimmickSpec)projection.Input.Battle.GimmickDefinitions[group.Id.Value - 1];
            var permitted = spec.Members.Where(m => !projection.TornadoObserver.HasValue || m.KnownTo.Contains(projection.TornadoObserver.Value)).ToArray();
            if (!permitted.Any(m => Touches(unit, group.Members.Single(s => s.SourceId == m.SourceId).Center, m.SideLength))) return false;
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
                    Throw(projection, group.Id, member, mover, rules);
                }
            }
            finally
            {
                if (owns) projection.TornadoResolution = null;
            }
            return resolution.Thrown.Count(pair => pair.UnitId == mover) != before;
        }

        internal static bool ContactEligible(MovementProjection projection)
        {
            var state = projection.CurrentInput().State;
            return state.Turn.StagePhase == StagePhase.Combat && state.Phase != CombatFlowPhase.Finished && !projection.Terminal;
        }

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
                Throw(projection, id, spec, recipient, rules);
            }
        }

        internal static IReadOnlyList<CellCoord> GroupedLandingCandidates(CombatCommandInput input, EntityId mover)
        {
            var unit = input.Characters.Single(c => c.EntityId == mover);
            return Cells(input.State.BoardWidth, input.State.BoardHeight).Where(cell => cell != unit.Anchor &&
                MovementRouteValidator.CanOccupy(input, cell, new[] { mover })).ToArray();
        }

        private static bool Touches(CharacterCombatFacts unit, CellCoord center, int side) =>
            !unit.Health.IsDead && unit.Area == CharacterRuntimeArea.Field &&
            unit.OccupiedCells.Any(c => Math.Abs((long)c.X - center.X) <= side / 2 && Math.Abs((long)c.Y - center.Y) <= side / 2);

        private static void Throw(MovementProjection projection, StageGimmickId id, TornadoMemberSpec spec, EntityId target,
            IReadOnlyList<IMovementTileRule> rules)
        {
            var group = (TornadoGimmickState)projection.Gimmicks.Get(id);
            var storm = group.Members.Single(m => m.SourceId == spec.SourceId);
            var unit = projection.Character(target);
            if (!Touches(unit, storm.Center, spec.SideLength) || projection.TornadoResolution.Thrown.Contains((spec.SourceId, target))) return;
            var current = projection.CurrentInput();
            if (UnitBehaviorRules.IsForcedMovementImmune(current, unit, projection.ProfileReads)) return;
            if (unit.OccupiedCells.Count != 1) throw new NotSupportedException("다중 셀 토네이도 이동에는 전체 점유 배치 지원이 필요합니다.");
            var candidates = GroupedLandingCandidates(current, target);
            if (candidates.Count == 0) return;
            var destination = candidates[projection.ChooseRandom(candidates.Count)];
            // 실제 이동이 확정된 뒤 중첩 착지보다 먼저 표시한다.
            projection.TornadoResolution.Thrown.Add((spec.SourceId, target));
            int step = projection.TornadoResolution.CenterSteps.TryGetValue(spec.SourceId, out int index) ? index : -1;
            projection.RecordForcedMove(unit, CombatMovementCalculator.MoveTo(unit, destination), spec.SourceId,
                new TornadoContactOrigin(id, spec.SourceId, storm.Center, step));
            // 과거 도착 의존은 새 이동의 도착 완료 근거가 아니다. 중첩 착지가 새로 기록한 값만 재사용한다.
            projection.ArrivalCells.Remove(target);
            if (!projection.ResolveForcedTerrainEntry(target, destination))
            {
                projection.ReconcilePresence(new[] { unit.Anchor.Value, destination });
                MovementEnvironmentCalculator.ResolveEntry(projection, destination, rules, target);
                var actual = projection.Character(target);
                if (!projection.Terminal && !actual.Health.IsDead && actual.Area == CharacterRuntimeArea.Field && actual.Anchor == destination &&
                    (!projection.ArrivalCells.TryGetValue(target, out var settled) || settled != destination))
                    projection.ResolveForcedArrival(target);
                projection.ResolveDeathBoundary();
            }
        }

        private static IEnumerable<CellCoord> Cells(int boardWidth, int boardHeight)
        {
            for (int y = 0; y < boardHeight; y++)
                for (int x = 0; x < boardWidth; x++) yield return new CellCoord(x, y);
        }
    }
}
