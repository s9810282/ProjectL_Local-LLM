using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks
{
    public abstract class StageGimmickSpec
    {
        public StageGimmickKey GimmickKey { get; }

        public StageGimmickSpec(StageGimmickKey key)
        {
            if (key == StageGimmickKey.None || Enum.IsDefined(typeof(StageGimmickKey), key) == false)
                throw new ArgumentOutOfRangeException(nameof(key));

            this.GimmickKey = key;
        }
    }
    internal static class StageGimmickMask
    {
        internal static IReadOnlyList<CellCoord> CopyNormalized(IReadOnlyList<CellCoord> cells)
        {
            var copy = StageGimmickState.CopyCells(cells, false);
            if (copy.Min(c => c.X) != 0 || copy.Min(c => c.Y) != 0)
                throw new ArgumentException("기믹 마스크의 최소 좌표는 각 축에서 0이어야 합니다.", nameof(cells));
            return copy;
        }
    }
}
