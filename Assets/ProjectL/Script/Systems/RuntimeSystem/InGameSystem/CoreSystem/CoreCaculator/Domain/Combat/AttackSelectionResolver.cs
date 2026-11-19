using System;
using System.Collections.Generic;
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
            (cells, failureReason) = ResolveDirect(method, range, origin, owner, aim);
            return failureReason == AttackFailureReason.None;
        }

        internal static IEnumerable<string> ResolveSteps(TargetingMethodSpec method, int range, CellCoord origin, Owner owner,
            AttackAim aim, Action<AttackCellSelection, AttackFailureReason> completed)
        {
            var failure = ValidateAim(method, range, origin, owner, aim);
            if (failure != AttackFailureReason.None)
            {
                completed(null, failure);
                yield return "SelectionInvalidAim";
                yield break;
            }
            yield return "SelectionAimValidated";

            if (method.ChoiceMode == TargetingChoiceMode.Single)
            {
                CellCoord primary = aim.Cell.Value;
                var additional = AppointShapeCalculator.Translate(AppointShapeCalculator.CreateSplashOffsets(method.Splash), primary);
                completed(new AttackCellSelection(primary, new[] { primary }, additional), AttackFailureReason.None);
                yield return "SelectionMaskComplete";
                yield break;
            }
            var cells = new List<CellCoord>();
            var direction = method.ChoiceMode == TargetingChoiceMode.All ? (AttackDirection?)null : aim.Direction;
            foreach (var offset in AppointShapeCalculator.MaskSteps(method.Shape, range, direction, method.Width))
            {
                if (offset.HasValue && TargetingAngleCalculator.Contains(method.ChoiceAngle, owner, CellCoord.Zero, offset.Value))
                    cells.Add(new CellCoord(checked(origin.X + offset.Value.X), checked(origin.Y + offset.Value.Y)));
                yield return "SelectionMaskItem";
            }
            completed(new AttackCellSelection(null, cells.ToArray(), Array.Empty<CellCoord>()), AttackFailureReason.None);
            yield return "SelectionMaskComplete";
        }

        internal static (AttackCellSelection, AttackFailureReason) ResolveDirect(TargetingMethodSpec method, int range, CellCoord origin, Owner owner,
            AttackAim aim)
        {
            var failure = ValidateAim(method, range, origin, owner, aim);
            if (failure != AttackFailureReason.None)
            {
                return (null, failure);
                { }
                { }
            }
            { }

            if (method.ChoiceMode == TargetingChoiceMode.Single)
            {
                CellCoord primary = aim.Cell.Value;
                var additional = AppointShapeCalculator.Translate(AppointShapeCalculator.CreateSplashOffsets(method.Splash), primary);
                return (new AttackCellSelection(primary, new[] { primary }, additional), AttackFailureReason.None);
                { }
                { }
            }
            var cells = new List<CellCoord>();
            var direction = method.ChoiceMode == TargetingChoiceMode.All ? (AttackDirection?)null : aim.Direction;
            foreach (var offset in AppointShapeCalculator.MaskSteps(method.Shape, range, direction, method.Width))
            {
                if (offset.HasValue && TargetingAngleCalculator.Contains(method.ChoiceAngle, owner, CellCoord.Zero, offset.Value))
                    cells.Add(new CellCoord(checked(origin.X + offset.Value.X), checked(origin.Y + offset.Value.Y)));
                { }
            }
            return (new AttackCellSelection(null, cells.ToArray(), Array.Empty<CellCoord>()), AttackFailureReason.None);
            { }
        }

        internal static AttackFailureReason ValidateAim(TargetingMethodSpec method, int range, CellCoord origin, Owner owner, AttackAim aim)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            if (range < 0)
                throw new ArgumentOutOfRangeException(nameof(range));
            TargetingAngleCalculator.Validate(method.ChoiceAngle, owner);

            switch (method.ChoiceMode)
            {
                case TargetingChoiceMode.Single:
                    if (aim.Kind != AttackAimKind.Cell ||
                        aim.Cell.HasValue == false || aim.Direction != AttackDirection.None)
                        return AttackFailureReason.InvalidAttackAim;
                    if (!AppointShapeCalculator.ContainsBaseMask(method.Shape, range,
                        (long)aim.Cell.Value.X - origin.X, (long)aim.Cell.Value.Y - origin.Y, method.Width) ||
                        !TargetingAngleCalculator.Contains(method.ChoiceAngle, owner, origin, aim.Cell.Value))
                        return AttackFailureReason.InvalidPrimaryTarget;
                    break;
                case TargetingChoiceMode.Direction:
                    if (aim.Kind != AttackAimKind.Direction || aim.Cell.HasValue)
                        return AttackFailureReason.InvalidAttackAim;
                    if (!AppointShapeCalculator.IsDirectionSupported(method.Shape, aim.Direction) ||
                        !TargetingAngleCalculator.Contains(method.ChoiceAngle, owner, CellCoord.Zero,
                            AppointShapeCalculator.GetDirectionStep(aim.Direction)))
                        return AttackFailureReason.InvalidAttackDirection;
                    break;
                case TargetingChoiceMode.All:
                    if (aim.Kind != AttackAimKind.All || aim.Cell.HasValue || aim.Direction != AttackDirection.None)
                        return AttackFailureReason.InvalidAttackAim;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(method));
            }
            return AttackFailureReason.None;
        }

    }
}
