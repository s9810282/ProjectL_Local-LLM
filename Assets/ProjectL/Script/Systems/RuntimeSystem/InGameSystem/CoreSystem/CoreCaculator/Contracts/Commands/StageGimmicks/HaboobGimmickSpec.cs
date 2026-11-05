using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks
{
    public sealed class HaboobGimmickSpec : StageGimmickSpec
    {
        public int RelocationIntervalControllerTurns { get; }
        public int WarningLeadControllerTurns { get; }
        public IReadOnlyList<CellCoord> FootprintOffsets { get; }
        /// <summary>전투 준비 때 상대 좌표 (0,0)을 놓을 칸입니다. null이면 무작위이고, 재배치는 늘 무작위입니다.</summary>
        public CellCoord? InitialAnchor { get; }

        public HaboobGimmickSpec(int relocationIntervalControllerTurns = 2, int warningLeadControllerTurns = 1,
            IReadOnlyList<CellCoord> footprintOffsets = null, CellCoord? initialAnchor = null) : base(StageGimmickKey.Haboob)
        {
            if (relocationIntervalControllerTurns < 1) throw new ArgumentOutOfRangeException(nameof(relocationIntervalControllerTurns));
            if (warningLeadControllerTurns < 1 || warningLeadControllerTurns > relocationIntervalControllerTurns)
                throw new ArgumentOutOfRangeException(nameof(warningLeadControllerTurns));
            // 판 크기에 따른 검사는 전투 준비가 맡는다(설계 HA5).
            if (initialAnchor.HasValue && (initialAnchor.Value.X < 0 || initialAnchor.Value.Y < 0))
                throw new ArgumentOutOfRangeException(nameof(initialAnchor));
            RelocationIntervalControllerTurns = relocationIntervalControllerTurns;
            WarningLeadControllerTurns = warningLeadControllerTurns;
            InitialAnchor = initialAnchor;
            FootprintOffsets = StageGimmickMask.CopyNormalized(footprintOffsets ?? new[] {
                new CellCoord(1, 0), new CellCoord(2, 0),
                new CellCoord(0, 1), new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(3, 1),
                new CellCoord(0, 2), new CellCoord(1, 2), new CellCoord(2, 2), new CellCoord(3, 2),
                new CellCoord(1, 3), new CellCoord(2, 3) });
        }
    }
}
