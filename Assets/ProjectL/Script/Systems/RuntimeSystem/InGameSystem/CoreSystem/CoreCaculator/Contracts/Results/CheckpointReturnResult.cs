using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>체크포인트 복귀로 실제 환급한 자원이다. 같은 칸에서의 장부 환급도 허용한다.</summary>
    public sealed class CheckpointReturnResult
    {
        public CellCoord From { get; }
        public CellCoord To { get; }
        public int ActionCostRefunded { get; }
        public int DistanceRefunded { get; }

        public CheckpointReturnResult(CellCoord from, CellCoord to, int actionCostRefunded, int distanceRefunded)
        {
            if (actionCostRefunded < 0) throw new ArgumentOutOfRangeException(nameof(actionCostRefunded));
            if (distanceRefunded < 0) throw new ArgumentOutOfRangeException(nameof(distanceRefunded));
            From = from;
            To = to;
            ActionCostRefunded = actionCostRefunded;
            DistanceRefunded = distanceRefunded;
        }
    }
}
