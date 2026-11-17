using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum AttackSweepStop
    {
        Completed = 1,
        Blocked,
        Immune,
        Interrupted,
        NotProcessed
    }

    /// <summary>한 대상의 쓸기 경로와 이동 단계가 끝났을 때의 위치입니다.</summary>
    public sealed class AttackSweepTargetSnapshot
    {
        public EntityId TargetId { get; }
        public CellCoord StartAnchor { get; }
        public CellCoord IntendedEndpoint { get; }
        public IReadOnlyList<CellCoord> EnteredCells { get; }
        public CellCoord? FinalAnchor { get; }
        public AttackSweepStop Stop { get; }

        public AttackSweepTargetSnapshot(EntityId targetId, CellCoord startAnchor, CellCoord intendedEndpoint,
            IReadOnlyList<CellCoord> enteredCells, CellCoord? finalAnchor, AttackSweepStop stop)
        {
            if (targetId.IsEmpty) throw new ArgumentOutOfRangeException(nameof(targetId));
            if (!Enum.IsDefined(typeof(AttackSweepStop), stop)) throw new ArgumentOutOfRangeException(nameof(stop));
            var path = (enteredCells ?? throw new ArgumentNullException(nameof(enteredCells))).ToArray();
            long dx = (long)intendedEndpoint.X - startAnchor.X;
            long dy = (long)intendedEndpoint.Y - startAnchor.Y;
            if ((dx == 0) == (dy == 0))
                throw new ArgumentException("쓸기 목적지는 시작 칸과 같은 줄에 있어야 합니다.", nameof(intendedEndpoint));
            if (path.Length > Math.Abs(dx) + Math.Abs(dy))
                throw new ArgumentException("쓸기 경로는 목적지를 넘을 수 없습니다.", nameof(enteredCells));

            for (int i = 0; i < path.Length; i++)
            {
                long distance = i + 1L;
                if (path[i].X != startAnchor.X + Math.Sign(dx) * distance ||
                    path[i].Y != startAnchor.Y + Math.Sign(dy) * distance)
                    throw new ArgumentException("쓸기 경로는 목적지 쪽으로 한 칸씩 이어져야 합니다.", nameof(enteredCells));
            }

            var last = path.Length == 0 ? startAnchor : path[path.Length - 1];
            if (stop == AttackSweepStop.Completed && last != intendedEndpoint ||
                stop == AttackSweepStop.Blocked && last == intendedEndpoint ||
                (stop == AttackSweepStop.Immune || stop == AttackSweepStop.NotProcessed) && path.Length != 0 ||
                stop == AttackSweepStop.Immune && finalAnchor != startAnchor)
                throw new ArgumentException("쓸기 종료 사유와 기록된 이동이 일치하지 않습니다.", nameof(stop));

            TargetId = targetId;
            StartAnchor = startAnchor;
            IntendedEndpoint = intendedEndpoint;
            EnteredCells = Array.AsReadOnly(path);
            FinalAnchor = finalAnchor;
            Stop = stop;
        }
    }
}
