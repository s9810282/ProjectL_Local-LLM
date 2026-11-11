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
    internal static class TornadoGimmickPreparationCalculator
    {
        internal static TornadoGimmickState Prepare(TornadoGimmickSpec spec, StageGimmickId id, int width, int height,
            ref uint random, long completedControllerTurnCount = 0, string stageContext = "stage")
        {
            if (spec == null) return null;
            if (completedControllerTurnCount < 0) throw new ArgumentOutOfRangeException(nameof(completedControllerTurnCount));
            // 입력을 모두 먼저 검사하여 잘못된 후속 원본이 난수 커서를 진행시키지 않게 한다.
            foreach (var member in spec.Members)
            {
                int radius = member.SideLength / 2;
                if (width < member.SideLength || height < member.SideLength ||
                    member.InitialCenter.HasValue && !TornadoGeometry.Fits(member.InitialCenter.Value, member.SideLength, width, height))
                    throw new InvalidOperationException($"{stageContext}: 토네이도 {member.SourceId}의 정사각형이 {width}×{height} 판에 들어가지 않습니다.");
                if (member.InitialCenter.HasValue && member.InitialCenter.Value.X != radius &&
                    member.InitialCenter.Value.X != width - 1 - radius && !member.InitialHorizontalSign.HasValue)
                    throw new InvalidOperationException($"{stageContext}: 토네이도 {member.SourceId}의 내부 중심에는 최초 가로 방향이 필요합니다.");
            }

            uint cursor = random;
            var members = new List<TornadoMemberState>();
            foreach (var member in spec.Members)
            {
                int radius = member.SideLength / 2;
                int right = width - 1 - radius;
                CellCoord center;
                int sign;
                if (member.InitialCenter.HasValue)
                {
                    center = member.InitialCenter.Value;
                    sign = radius == right ? member.InitialHorizontalSign ?? (EnvironmentRandom.Choose(ref cursor, 2) == 0 ? 1 : -1) :
                        center.X == radius ? 1 : center.X == right ? -1 : member.InitialHorizontalSign.Value;
                }
                else
                {
                    bool left = radius == right && member.InitialHorizontalSign.HasValue ? member.InitialHorizontalSign == 1 :
                        EnvironmentRandom.Choose(ref cursor, 2) == 0;
                    sign = left ? 1 : -1;
                    center = new CellCoord(left ? radius : right, radius + EnvironmentRandom.Choose(ref cursor, height - member.SideLength + 1));
                }
                long warningAt = checked(completedControllerTurnCount + member.FirstActivationAfterControllerTurns - member.WarningLeadControllerTurns);
                var state = new TornadoMemberState(member.SourceId, center, sign, TornadoGimmickPhase.Cooldown,
                    Math.Max(1, warningAt), null);
                if (warningAt == completedControllerTurnCount)
                    state = LockWarning(member, state, width, height,
                        checked(completedControllerTurnCount + member.FirstActivationAfterControllerTurns), ref cursor);
                members.Add(state);
            }
            var result = new TornadoGimmickState(id, members);
            random = cursor;
            return result;
        }

        internal static TornadoMemberState Warn(TornadoMemberSpec spec, TornadoMemberState state,
            int width, int height, ref uint random)
        {
            Validate(spec, state, width, height, TornadoGimmickPhase.Cooldown);
            return LockWarning(spec, state, width, height, TravelAfterWarning(spec, state.NextBoundaryCount), ref random);
        }

        internal static (TornadoMemberState State, IReadOnlyList<CellCoord> Path) CompleteTravel(TornadoMemberSpec spec,
            TornadoMemberState state, int width, int height, ref uint random)
        {
            Validate(spec, state, width, height, TornadoGimmickPhase.Warning);
            var path = TornadoGeometry.Travel(state.Center, spec.SideLength, spec.DiagonalDistance, width, height, state.LockedDirection.Value);
            var center = path[path.Count - 1];
            int sign = TornadoGeometry.InwardHorizontalSign(center, spec.SideLength, width, state.HorizontalSign);
            long nextWarning = WarningAfterTravel(spec, state.NextBoundaryCount);
            var next = new TornadoMemberState(state.SourceId, center, sign, TornadoGimmickPhase.Cooldown,
                nextWarning, null);
            if (spec.WarningLeadControllerTurns == spec.RepeatIntervalControllerTurns)
                next = LockWarning(spec, next, width, height, TravelAfterWarning(spec, nextWarning), ref random);
            return (next, path);
        }

        internal static long TravelAfterWarning(TornadoMemberSpec spec, long warningBoundary) =>
            checked(warningBoundary + spec.WarningLeadControllerTurns);

        internal static long WarningAfterTravel(TornadoMemberSpec spec, long travelBoundary) =>
            checked(travelBoundary + spec.RepeatIntervalControllerTurns) - spec.WarningLeadControllerTurns;

        private static TornadoMemberState LockWarning(TornadoMemberSpec spec, TornadoMemberState state,
            int width, int height, long travelAt, ref uint random)
        {
            int sign = TornadoGeometry.InwardHorizontalSign(state.Center, spec.SideLength, width, state.HorizontalSign);
            var directions = new[] { new CellCoord(sign, -1), new CellCoord(sign, 1) }
                .Where(d => TornadoGeometry.Fits(state.Center + d, spec.SideLength, width, height)).ToArray();
            var direction = directions.Length == 0 ? CellCoord.Zero : directions[EnvironmentRandom.Choose(ref random, directions.Length)];
            return new TornadoMemberState(state.SourceId, state.Center, sign, TornadoGimmickPhase.Warning, travelAt, direction);
        }

        private static void Validate(TornadoMemberSpec spec, TornadoMemberState state, int width, int height, TornadoGimmickPhase phase)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (spec.SourceId != state.SourceId || state.Phase != phase || !TornadoGeometry.Fits(state.Center, spec.SideLength, width, height))
                throw new InvalidOperationException("토네이도 원본·단계·영역이 계산 입력과 일치하지 않습니다.");
        }
    }
}
