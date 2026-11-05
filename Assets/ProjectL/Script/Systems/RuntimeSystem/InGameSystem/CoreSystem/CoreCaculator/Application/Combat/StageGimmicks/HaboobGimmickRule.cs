using System;
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
        public StageGimmickKey Key => StageGimmickKey.Haboob;

        internal static HaboobGimmickState Prepare(CombatCommandState state, HaboobGimmickSpec spec, StageGimmickId id, ref uint random) =>
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
                    checked(end.CompletedControllerTurnCount + spec.WarningLeadControllerTurns), before.Cells);
            var progress = input.State.EnvironmentProgress;
            var state = input.State.With(gimmicks: input.State.Gimmicks.Replace(after),
                environmentProgress: new EnvironmentProgressFacts(progress.NextObjectEntryOrdinal, progress.NextSourceId, random));
            return CombatTurnCalculator.Changed(input, input.Characters.ToDictionary(c => c.EntityId), state,
                new CombatEvent[] { new CombatHaboobEvent(relocate ? HaboobGimmickStep.Relocation : HaboobGimmickStep.Warning, before, after) });
        }

        private static HaboobGimmickState Placed(HaboobGimmickSpec spec, StageGimmickId id, long count, IReadOnlyList<CellCoord> cells)
        {
            bool warning = spec.WarningLeadControllerTurns == spec.RelocationIntervalControllerTurns;
            return new HaboobGimmickState(id, warning ? HaboobGimmickPhase.RelocationWarning : HaboobGimmickPhase.Active,
                checked(count + spec.RelocationIntervalControllerTurns - (warning ? 0 : spec.WarningLeadControllerTurns)), cells);
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
            var (width, height) = AnchorLimit(state, spec);
            var candidates = new List<CellCoord>();
            int oldX = current?.Min(c => c.X) ?? -1, oldY = current?.Min(c => c.Y) ?? -1;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (x != oldX || y != oldY) candidates.Add(new CellCoord(x, y));
            // 유일한 위치면 같은 영역을 유지하되 재배치 사건과 주기는 정상적으로 게시한다.
            if (candidates.Count == 0) return current;
            var anchor = candidates[EnvironmentRandom.Choose(ref random, candidates.Count)];
            return CellsAt(spec, anchor);
        }

        // 상대 좌표 (0,0)을 놓을 수 있는 칸은 X < Width, Y < Height다(설계 HA1).
        private static (int Width, int Height) AnchorLimit(CombatCommandState state, HaboobGimmickSpec spec)
        {
            int width = state.BoardWidth - spec.FootprintOffsets.Max(c => c.X);
            int height = state.BoardHeight - spec.FootprintOffsets.Max(c => c.Y);
            if (width <= 0 || height <= 0) throw new InvalidOperationException("하부브 영역 전체가 들어가는 위치가 없습니다.");
            return (width, height);
        }

        private static IReadOnlyList<CellCoord> CellsAt(HaboobGimmickSpec spec, CellCoord anchor) =>
            spec.FootprintOffsets.Select(c => new CellCoord(anchor.X + c.X, anchor.Y + c.Y)).ToArray();
    }
}
