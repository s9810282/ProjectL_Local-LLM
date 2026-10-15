using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    public sealed class AttackSelectionResolver
    {
        /// <summary>기하 지정 실패만 기존 실패 사유로 변환한다. 수치 및 정의 오류는 예외로 전파한다.</summary>
        public bool TryResolve(TargetingMethodSpec method, int range, CellCoord origin, AttackAim aim,
            out AttackCellSelection cells, out AttackFailureReason failureReason)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            if (range < 0)
                throw new ArgumentOutOfRangeException(nameof(range));

            cells = null;
            failureReason = AttackFailureReason.InvalidAttackAim;
            switch (method.ChoiceMode)
            {
                case TargetingChoiceMode.Single:
                    if (aim.Kind != AttackAimKind.Cell ||
                        aim.Cell.HasValue == false || aim.Direction != AttackDirection.None)
                        return false;
                    if (AppointShapeCalculator.ContainsBaseMask(method.Shape, range,
                        (long)aim.Cell.Value.X - origin.X, (long)aim.Cell.Value.Y - origin.Y) == false)
                    {
                        failureReason = AttackFailureReason.InvalidPrimaryTarget;
                        return false;
                    }
                    break;
                case TargetingChoiceMode.Direction:
                    if (aim.Kind != AttackAimKind.Direction || aim.Cell.HasValue)
                        return false;
                    if (AppointShapeCalculator.IsDirectionSupported(method.Shape, aim.Direction) == false)
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

            cells = AttackGeometryCalculator.CalculateResolvedCells(method, range, origin, aim);
            failureReason = AttackFailureReason.None;
            return true;
        }

    }
}
