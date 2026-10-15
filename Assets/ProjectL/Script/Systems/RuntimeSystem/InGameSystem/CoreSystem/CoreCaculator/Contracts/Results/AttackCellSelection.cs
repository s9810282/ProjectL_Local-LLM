using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>맵 및 대상 정책을 적용하기 전의 지정 셀과 추가 셀이다.</summary>
    public sealed class AttackCellSelection
    {
        private readonly CellCoord[] _designatedCells;
        private readonly CellCoord[] _additionalCells;

        public CellCoord? PrimaryCell { get; }
        public ReadOnlyMemory<CellCoord> DesignatedCells => _designatedCells;
        public ReadOnlyMemory<CellCoord> AdditionalCells => _additionalCells;

        public AttackCellSelection(CellCoord? primaryCell, CellCoord[] designatedCells, CellCoord[] additionalCells)
        {
            if (designatedCells == null)
                throw new ArgumentNullException(nameof(designatedCells));
            if (additionalCells == null)
                throw new ArgumentNullException(nameof(additionalCells));
            if (primaryCell.HasValue)
            {
                if (designatedCells.Length != 1 || designatedCells[0] != primaryCell.Value)
                    throw new ArgumentException("주 셀은 유일한 지정 셀이어야 합니다.", nameof(designatedCells));
                for (int i = 0; i < additionalCells.Length; i++)
                    if (additionalCells[i] == primaryCell.Value)
                        throw new ArgumentException("추가 셀에 주 셀을 포함할 수 없습니다.", nameof(additionalCells));
            }
            else if (additionalCells.Length != 0)
                throw new ArgumentException("추가 셀은 주 셀이 있을 때만 지정할 수 있습니다.", nameof(additionalCells));

            PrimaryCell = primaryCell;
            _designatedCells = (CellCoord[])designatedCells.Clone();
            _additionalCells = (CellCoord[])additionalCells.Clone();
        }
    }
}
