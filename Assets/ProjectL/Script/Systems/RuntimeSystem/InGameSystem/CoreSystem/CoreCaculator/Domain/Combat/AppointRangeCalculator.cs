using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Map;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    public sealed class AppointRangeCalculator
    {
        /// <summary>단일 명령의 값만으로 지정 가능 셀을 계산한다. 맵 경계 정책은 호출자가 적용한다.</summary>
        public CellCoord[] CalculateRangeCells(TargetingMethodSpec method, int range, CellCoord origin)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            return AppointShapeCalculator.Translate(AppointShapeCalculator.CreateBaseMask(method.Shape, range), origin);
        }
    }
}
