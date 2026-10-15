using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>
    /// View가 Drag 중 Highlight할 셀과 현재 배치 가능 여부를 읽는 Preview다.
    /// </summary>
    public readonly struct CombatPlacementPreview
    {
        public bool CanPlace { get; }
        public ReadOnlyMemory<CellCoord> OccupiedCells { get; }

        public CombatPlacementPreview(bool canPlace, CellCoord[] occupiedCells)
        {
            if (occupiedCells == null)
                throw new ArgumentNullException(nameof(occupiedCells));

            CellCoord[] copiedCells = new CellCoord[occupiedCells.Length];
            Array.Copy(occupiedCells, copiedCells, occupiedCells.Length);

            CanPlace = canPlace;
            OccupiedCells = copiedCells;
        }
    }
}
