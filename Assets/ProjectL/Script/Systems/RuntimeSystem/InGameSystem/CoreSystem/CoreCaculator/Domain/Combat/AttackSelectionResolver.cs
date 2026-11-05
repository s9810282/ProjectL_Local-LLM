using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    public sealed class AttackSelectionResolver
    {
        /// <summary>각도 제한이 없는 기존 호출을 유지합니다. 제한이 있으면 진영을 명시해야 합니다.</summary>
        public bool TryResolve(TargetingMethodSpec method, int range, CellCoord origin, AttackAim aim,
            out AttackCellSelection cells, out AttackFailureReason failureReason)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            if (method.ChoiceAngle != TargetingChoiceAngle.Unrestricted)
                throw new ArgumentException("각도 제한이 있는 지정 방식에는 진영이 필요합니다.", nameof(method));
            return TryResolve(method, range, origin, Owner.Player, aim, out cells, out failureReason);
        }

        /// <summary>기하 지정 실패만 기존 실패 사유로 변환한다. 수치 및 정의 오류는 예외로 전파한다.</summary>
        public bool TryResolve(TargetingMethodSpec method, int range, CellCoord origin, Owner owner, AttackAim aim,
            out AttackCellSelection cells, out AttackFailureReason failureReason)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            if (range < 0)
                throw new ArgumentOutOfRangeException(nameof(range));
            TargetingAngleCalculator.Validate(method.ChoiceAngle, owner);

            cells = null;
            failureReason = AttackFailureReason.InvalidAttackAim;
            switch (method.ChoiceMode)
            {
                case TargetingChoiceMode.Single:
                    if (aim.Kind != AttackAimKind.Cell ||
                        aim.Cell.HasValue == false || aim.Direction != AttackDirection.None)
                        return false;
                    if (AppointShapeCalculator.ContainsBaseMask(method.Shape, range,
                        (long)aim.Cell.Value.X - origin.X, (long)aim.Cell.Value.Y - origin.Y) == false ||
                        !TargetingAngleCalculator.Contains(method.ChoiceAngle, owner, origin, aim.Cell.Value))
                    {
                        failureReason = AttackFailureReason.InvalidPrimaryTarget;
                        return false;
                    }
                    break;
                case TargetingChoiceMode.Direction:
                    if (aim.Kind != AttackAimKind.Direction || aim.Cell.HasValue)
                        return false;
                    if (AppointShapeCalculator.IsDirectionSupported(method.Shape, aim.Direction) == false ||
                        !TargetingAngleCalculator.Contains(method.ChoiceAngle, owner, CellCoord.Zero,
                            AppointShapeCalculator.GetDirectionStep(aim.Direction)))
                    {
                        failureReason = AttackFailureReason.InvalidAttackDirection;
                        return false;
                    }
                    break;
                case TargetingChoiceMode.All:
                    if (aim.Kind != AttackAimKind.All ||
                        aim.Cell.HasValue || aim.Direction != AttackDirection.None)
                        return false;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(method));
            }

            cells = CalculateResolvedCells(method, range, origin, owner, aim);
            failureReason = AttackFailureReason.None;
            return true;
        }

        /// <summary>검증을 마친 지정 의도를 칸 집합으로 바꾼다. 칸 선택은 주 칸과 스플래시, 방향·전체는 모양의 칸을 쓴다.</summary>
        private static AttackCellSelection CalculateResolvedCells(
            TargetingMethodSpec method, int range, CellCoord origin, Owner owner, AttackAim aim)
        {
            if (method.ChoiceMode == TargetingChoiceMode.Single)
            {
                CellCoord primary = aim.Cell.Value;
                CellCoord[] additional = AppointShapeCalculator.Translate(
                    AppointShapeCalculator.CreateSplashOffsets(method.Splash), primary);
                return new AttackCellSelection(primary, new[] { primary }, additional);
            }

            CellCoord[] offsets = method.ChoiceMode == TargetingChoiceMode.All
                ? AppointShapeCalculator.CreateBaseMask(method.Shape, range)
                : AppointShapeCalculator.CreateDirectionMask(method.Shape, range, aim.Direction);
            return new AttackCellSelection(null, AppointShapeCalculator.Translate(
                TargetingAngleCalculator.FilterOffsets(method.ChoiceAngle, owner, offsets), origin), Array.Empty<CellCoord>());
        }

    }
}
