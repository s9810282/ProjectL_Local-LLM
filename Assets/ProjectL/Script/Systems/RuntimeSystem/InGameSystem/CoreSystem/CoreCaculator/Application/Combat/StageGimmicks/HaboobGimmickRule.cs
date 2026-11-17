using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks
{
    internal sealed class HaboobGimmickRule : IStageGimmickRule
    {
        public IEnumerator<HazardCellFact?> EnumerateForecast(CombatCommandInput input, StageGimmickSpec definition,
            StageGimmickId id, long lastBoundaryCount)
        {
            var spec = definition as HaboobGimmickSpec ?? throw new InvalidOperationException("하부브 설정 형식이 다릅니다.");
            var state = (HaboobGimmickState)input.State.Gimmicks.Get(id);
            long boundary = input.State.Turn.CompletedControllerTurnCount;
            long next = state.NextBoundaryCount;
            if (next <= boundary) throw new InvalidOperationException("하부브의 예정 경계가 현재 종료 누계보다 뒤여야 합니다.");
            var phase = state.Phase;
            int minX = int.MaxValue, minY = int.MaxValue;
            foreach (var cell in state.Cells)
            {
                minX = Math.Min(minX, cell.X);
                minY = Math.Min(minY, cell.Y);
                yield return Region(cell);
            }
            bool? canRelocate = null;
            while (next <= lastBoundaryCount)
            {
                boundary = next;
                if (phase == HaboobGimmickPhase.RelocationWarning)
                {
                    if (!canRelocate.HasValue)
                    {
                        int maxX = 0, maxY = 0;
                        foreach (var offset in spec.FootprintOffsets)
                        {
                            maxX = Math.Max(maxX, offset.X);
                            maxY = Math.Max(maxY, offset.Y);
                            yield return null;
                        }
                        canRelocate = HasAlternativeAnchor(AnchorLimit(input.State, maxX, maxY), new CellCoord(minX, minY));
                    }
                    if (canRelocate.Value)
                    {
                        yield return new HazardCellFact(id, Key, boundary, null, gaps: CombatAssessmentGap.FutureWarning);
                        yield break;
                    }
                    (phase, next) = AfterPlacement(spec, boundary);
                }
                else
                {
                    phase = HaboobGimmickPhase.RelocationWarning;
                    next = WarningEndsAt(spec, boundary);
                }
                foreach (var cell in state.Cells) yield return Region(cell);
            }

            HazardCellFact Region(CellCoord cell) => new HazardCellFact(id, Key, boundary, cell,
                haboobRegion: state.Cells, haboobPhase: phase, haboobNextBoundaryCount: next);
        }

        public StageGimmickKey Key => StageGimmickKey.Haboob;

        public bool RunsAt(StageGimmickBoundaryPhase phase) => phase == StageGimmickBoundaryPhase.Scheduled;

        public StageGimmickState Initialize(CombatCommandState initialState, StageGimmickSpec definition,
            StageGimmickId id, ref uint randomState)
        {
            var spec = definition as HaboobGimmickSpec ?? throw new InvalidOperationException("하부브 설정 형식이 다릅니다.");
            return Prepare(initialState, spec, id, ref randomState);
        }

        private static HaboobGimmickState Prepare(CombatCommandState state, HaboobGimmickSpec spec, StageGimmickId id, ref uint random) =>
            Placed(spec, id, state.Turn.CompletedControllerTurnCount, spec.InitialAnchor.HasValue ?
                FixedCells(state, spec, spec.InitialAnchor.Value) : SelectCells(state, spec, null, ref random));

        public CombatCommandTransition TryAdvance(CombatCommandInput input, StageGimmickSpec definition,
            StageGimmickId id, StageGimmickBoundary? boundary)
        {
            var spec = definition as HaboobGimmickSpec ?? throw new InvalidOperationException("하부브 설정 형식이 다릅니다.");
            var end = boundary ?? throw new InvalidOperationException("하부브에는 완료 진영 경계가 필요합니다.");
            var before = (HaboobGimmickState)input.State.Gimmicks.Get(id);
            if (end.CompletedControllerTurnCount < before.NextBoundaryCount) return null;
            if (end.CompletedControllerTurnCount != before.NextBoundaryCount)
                throw new InvalidOperationException("하부브의 예정 경계를 건너뛸 수 없습니다.");
            uint random = input.State.EnvironmentProgress.RandomState;
            bool relocate = before.Phase == HaboobGimmickPhase.RelocationWarning;
            var after = relocate ? Placed(spec, id, end.CompletedControllerTurnCount, SelectCells(input.State, spec, before.Cells, ref random)) :
                new HaboobGimmickState(id, HaboobGimmickPhase.RelocationWarning,
                    WarningEndsAt(spec, end.CompletedControllerTurnCount), before.Cells);
            var progress = input.State.EnvironmentProgress;
            var state = input.State.With(gimmicks: input.State.Gimmicks.Replace(after),
                environmentProgress: new EnvironmentProgressFacts(progress.NextObjectEntryOrdinal, progress.NextSourceId, random));
            return CombatTurnCalculator.Changed(input, input.Characters.ToDictionary(c => c.EntityId), state,
                new CombatEvent[] { new CombatHaboobEvent(relocate ? HaboobGimmickStep.Relocation : HaboobGimmickStep.Warning, before, after) });
        }

        private static HaboobGimmickState Placed(HaboobGimmickSpec spec, StageGimmickId id, long count, IReadOnlyList<CellCoord> cells)
        {
            var (phase, next) = AfterPlacement(spec, count);
            return new HaboobGimmickState(id, phase, next, cells);
        }

        private static long WarningEndsAt(HaboobGimmickSpec spec, long count) => checked(count + spec.WarningLeadControllerTurns);

        private static (HaboobGimmickPhase Phase, long Next) AfterPlacement(HaboobGimmickSpec spec, long count)
        {
            bool warning = spec.WarningLeadControllerTurns == spec.RelocationIntervalControllerTurns;
            return (warning ? HaboobGimmickPhase.RelocationWarning : HaboobGimmickPhase.Active,
                checked(count + spec.RelocationIntervalControllerTurns - (warning ? 0 : spec.WarningLeadControllerTurns)));
        }

        // 지정 시작 위치는 난수를 쓰지 않는다(설계 HA3). 들어가는 범위는 무작위 후보와 같은 식이다.
        private static IReadOnlyList<CellCoord> FixedCells(CombatCommandState state, HaboobGimmickSpec spec, CellCoord anchor)
        {
            var (width, height) = AnchorLimit(state, spec);
            if (anchor.X >= width || anchor.Y >= height)
                throw new InvalidOperationException(
                    $"하부브 시작 위치 ({anchor.X}, {anchor.Y})에서는 영역 전체가 {state.BoardWidth}×{state.BoardHeight} 판에 들어가지 않습니다.");
            return CellsAt(spec, anchor);
        }

        private static IReadOnlyList<CellCoord> SelectCells(CombatCommandState state, HaboobGimmickSpec spec,
            IReadOnlyList<CellCoord> current, ref uint random)
        {
            var limits = AnchorLimit(state, spec);
            var (width, height) = limits;
            var old = current == null ? (CellCoord?)null : new CellCoord(current.Min(c => c.X), current.Min(c => c.Y));
            // 유일한 위치면 같은 영역을 유지하되 재배치 사건과 주기는 정상적으로 게시한다.
            if (!HasAlternativeAnchor(limits, old)) return current;
            var candidates = new List<CellCoord>();
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var cell = new CellCoord(x, y);
                if (cell != old) candidates.Add(cell);
            }
            var anchor = candidates[EnvironmentRandom.Choose(ref random, candidates.Count)];
            return CellsAt(spec, anchor);
        }

        // 상대 좌표 (0,0)을 놓을 수 있는 칸은 X < Width, Y < Height다(설계 HA1).
        private static (int Width, int Height) AnchorLimit(CombatCommandState state, HaboobGimmickSpec spec) =>
            AnchorLimit(state, spec.FootprintOffsets.Max(c => c.X), spec.FootprintOffsets.Max(c => c.Y));

        private static (int Width, int Height) AnchorLimit(CombatCommandState state, int maxX, int maxY)
        {
            int width = state.BoardWidth - maxX;
            int height = state.BoardHeight - maxY;
            if (width <= 0 || height <= 0) throw new InvalidOperationException("하부브 영역 전체가 들어가는 위치가 없습니다.");
            return (width, height);
        }

        private static bool HasAlternativeAnchor((int Width, int Height) limits, CellCoord? old) =>
            limits.Width > 1 || limits.Height > 1 || old != CellCoord.Zero;

        private static IReadOnlyList<CellCoord> CellsAt(HaboobGimmickSpec spec, CellCoord anchor) =>
            spec.FootprintOffsets.Select(c => new CellCoord(anchor.X + c.X, anchor.Y + c.Y)).ToArray();
    }
}
