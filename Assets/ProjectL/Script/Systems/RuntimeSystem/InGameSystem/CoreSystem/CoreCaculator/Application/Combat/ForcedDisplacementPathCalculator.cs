using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    internal enum ForcedDisplacementStop
    {
        Completed = 1,
        Blocked = 2,
        Interrupted = 3,
    }

    /// <summary>강제 이동 한 번의 실제 경로다. 요청 거리와 실제로 들어간 칸을 구분한다.</summary>
    internal sealed class ForcedDisplacementPath
    {
        internal int RequestedDistance { get; }
        internal IReadOnlyList<CellCoord> Steps { get; }
        internal ForcedDisplacementStop Stop { get; }

        internal ForcedDisplacementPath(int requestedDistance, IReadOnlyList<CellCoord> steps, ForcedDisplacementStop stop)
        {
            RequestedDistance = requestedDistance;
            Steps = steps ?? throw new ArgumentNullException(nameof(steps));
            Stop = stop;
        }
    }

    /// <summary>
    /// 공격 넉백·타일 강제 이동이 함께 쓰는 거리 N 경로 진행이다(D6). 한 칸씩 들어가며 첫 막힌 칸 직전에서 멈춘다.
    /// 칸 검사 정책과 칸별 효과(지형·가시·오브젝트·피해)는 호출자의 기존 방식을 유지하도록 위임받는다(G8-6).
    /// 일반 Move를 N번 호출하지 않으며 AP·이동량을 쓰지 않는다.
    /// </summary>
    internal static class ForcedDisplacementPathCalculator
    {
        /// <param name="canEnter">이전 칸에서 다음 칸으로 들어갈 수 있는지. 매 칸 들어가기 직전의 현재 상태로 판정한다.</param>
        /// <param name="enter">한 칸 들어간 뒤의 효과를 처리한다. 요청 거리를 소진하는 칸인지 받으며 false면 남은 칸을 취소한다(사망·전투 종료).</param>
        internal static ForcedDisplacementPath Execute(CellCoord start, CellCoord step, int distance,
            Func<CellCoord, CellCoord, bool> canEnter, Func<CellCoord, CellCoord, bool, bool> enter)
        {
            if (distance < 1) throw new ArgumentOutOfRangeException(nameof(distance));
            if (canEnter == null) throw new ArgumentNullException(nameof(canEnter));
            if (enter == null) throw new ArgumentNullException(nameof(enter));
            ForcedDisplacementPath result = null;
            foreach (var _ in ExecuteSteps(start, step, distance, CanEnter, Enter, value => result = value)) { }
            return result;

            IEnumerable<string> CanEnter(CellCoord from, CellCoord to, Action<bool> completed)
            {
                completed(canEnter(from, to));
                yield return "ForcedPathAdmission";
            }

            IEnumerable<string> Enter(CellCoord from, CellCoord to, bool last, Action<bool> completed)
            {
                completed(enter(from, to, last));
                yield return "ForcedPathEntry";
            }
        }

        internal static IEnumerable<string> ExecuteSteps(CellCoord start, CellCoord step, int distance,
            Func<CellCoord, CellCoord, Action<bool>, IEnumerable<string>> canEnter,
            Func<CellCoord, CellCoord, bool, Action<bool>, IEnumerable<string>> enter,
            Action<ForcedDisplacementPath> completed)
        {
            if (distance < 1) throw new ArgumentOutOfRangeException(nameof(distance));
            if (canEnter == null) throw new ArgumentNullException(nameof(canEnter));
            if (enter == null) throw new ArgumentNullException(nameof(enter));
            var steps = new List<CellCoord>();
            var current = start;
            var stop = ForcedDisplacementStop.Completed;
            for (int i = 0; i < distance; i++)
            {
                var next = ForcedDisplacementGeometry.ResolveDestinationAnchor(current, step, 1);
                bool allowed = false;
                foreach (var phase in canEnter(current, next, value => allowed = value)) yield return phase;
                if (!allowed)
                {
                    stop = ForcedDisplacementStop.Blocked;
                    break;
                }
                steps.Add(next);
                // 남은 거리가 있으면 현재 칸 효과부터 처리한다. 지뢰 등이 다음 칸 점유를 바꿀 수 있으므로
                // 막힘은 효과 처리 뒤 다음 반복에서 검사하고, 요청 거리를 소진한 칸만 마지막으로 알린다.
                bool last = i == distance - 1;
                bool continued = false;
                foreach (var phase in enter(current, next, last, value => continued = value)) yield return phase;
                if (!continued)
                {
                    stop = ForcedDisplacementStop.Interrupted;
                    break;
                }
                current = next;
            }
            completed(new ForcedDisplacementPath(distance, steps, stop));
            yield return "ForcedPathComplete";
        }
    }
}
