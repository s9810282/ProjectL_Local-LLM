using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>
    /// 직접 조준 단계에서 특정 Cell이 거부된 사실과 그 사유입니다.
    /// 이 거부는 직접 조준 후보에만 적용되며, 적법한 Aim의 간접 광역/관통 피해를 막지 않습니다.
    /// </summary>
    public readonly struct AppointCellRejection
    {
        public CellCoord Cell { get; }
        public CategoryRuleKey Source { get; }

        public AppointCellRejection(CellCoord cell, CategoryRuleKey source)
        {
            if (source == CategoryRuleKey.None ||
                Enum.IsDefined(typeof(CategoryRuleKey), source) == false)
            {
                throw new ArgumentOutOfRangeException(nameof(source));
            }

            Cell = cell;
            Source = source;
        }
    }
}
