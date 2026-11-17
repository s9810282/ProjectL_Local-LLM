using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public sealed class LavaChainGimmickState : StageGimmickState
    {
        private readonly IReadOnlyList<CellCoord> _blockingCells;

        public CellCoord HeadCell { get; }
        public bool IsWarning { get; }
        public CellCoord? Heading { get; }

        /// <summary>발동 사이에 남는 머리 칸 하나다. 확장 중인 사슬 칸은 해석 중 임시 예약이며 여기에 없다.</summary>
        public override IReadOnlyList<CellCoord> BlockingCells => _blockingCells;

        public LavaChainGimmickState(StageGimmickId id, CellCoord headCell, long nextBoundaryCount,
            bool isWarning = false, CellCoord? heading = null) : base(id, StageGimmickKey.LavaChain, nextBoundaryCount)
        {
            if (headCell.X < 0 || headCell.Y < 0) throw new ArgumentOutOfRangeException(nameof(headCell));
            if (!isWarning && heading.HasValue || heading is CellCoord step &&
                (step == CellCoord.Zero || step.X < -1 || step.X > 1 || step.Y < -1 || step.Y > 1))
                throw new ArgumentException("사슬 진행 방향은 경고 중의 유효한 8방향이어야 합니다.", nameof(heading));
            HeadCell = headCell;
            IsWarning = isWarning;
            Heading = heading;
            _blockingCells = Array.AsReadOnly(new[] { headCell });
        }

        internal void ValidateBoard(int width, int height)
        {
            if (HeadCell.X >= width || HeadCell.Y >= height)
                throw new InvalidOperationException("사슬 머리가 보드 밖입니다.");
        }

        public override bool SameValues(StageGimmickState other) => other is LavaChainGimmickState state &&
            Id == state.Id && HeadCell == state.HeadCell && IsWarning == state.IsWarning && Heading == state.Heading &&
            NextBoundaryCount == state.NextBoundaryCount;
    }
}
