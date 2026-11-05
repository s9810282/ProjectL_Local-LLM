using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    public sealed class AppointRangeCalculator
    {
        /// <summary>각도 제한이 없는 기존 호출을 유지합니다. 제한이 있으면 진영을 명시해야 합니다.</summary>
        public CellCoord[] CalculateRangeCells(TargetingMethodSpec method, int range, CellCoord origin)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            if (method.ChoiceAngle != TargetingChoiceAngle.Unrestricted)
                throw new ArgumentException("각도 제한이 있는 지정 방식에는 진영이 필요합니다.", nameof(method));
            return CalculateRangeCells(method, range, origin, Owner.Player);
        }

        /// <summary>진영의 고정 정면으로 기존 지정 범위를 제한합니다. 맵 경계 정책은 호출자가 적용합니다.</summary>
        public CellCoord[] CalculateRangeCells(TargetingMethodSpec method, int range, CellCoord origin, Owner owner)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            TargetingAngleCalculator.Validate(method.ChoiceAngle, owner);
            var offsets = AppointShapeCalculator.CreateBaseMask(method.Shape, range);
            return AppointShapeCalculator.Translate(
                TargetingAngleCalculator.FilterOffsets(method.ChoiceAngle, owner, offsets), origin);
        }
    }
}
