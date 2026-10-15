using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact
{
    /// <summary>상태를 소유하지 않고 아티팩트의 회전과 공간 배치 가능 여부만 계산합니다.</summary>
    public static class ArtifactPlacementRules
    {
        public static ArtifactShapeSnapshot GetShape(ArtifactDefinition definition, Rotation4 rotation)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            var localCells = new CellCoord[definition.Cells.Count];
            for (int i = 0; i < localCells.Length; i++) localCells[i] = definition.Cells[i];
            TileFootprint footprint = TileFootprint.CreateFromCells(localCells);

            var rotatedCells = new CellCoord[footprint.CellCount];
            for (int i = 0; i < rotatedCells.Length; i++)
                rotatedCells[i] = footprint.GetRotatedLocalCell(i, rotation);

            bool swapsDimensions = rotation == Rotation4.R90 || rotation == Rotation4.R270;
            return new ArtifactShapeSnapshot(
                swapsDimensions ? footprint.Height : footprint.Width,
                swapsDimensions ? footprint.Width : footprint.Height,
                rotatedCells);
        }

        public static ArtifactActionFailureReason CheckPlacement(
            ArtifactState state, ArtifactShapeSnapshot shape, CellCoord anchor, int? ignoredInstanceId)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (shape == null) throw new ArgumentNullException(nameof(shape));

            var candidateCells = new CellCoord[shape.Cells.Count];
            // 좌표를 넓힌 뒤 검증하여 정수 오버플로와 셀 순서에 따른 실패 우선순위 변화를 방지합니다.
            for (int i = 0; i < candidateCells.Length; i++)
            {
                CellCoord local = shape.Cells[i];
                long x = (long)anchor.X + local.X;
                long y = (long)anchor.Y + local.Y;
                if (x < 0 || x >= state.BoardWidth || y < 0 || y >= state.BoardHeight)
                    return ArtifactActionFailureReason.OutOfBounds;
                candidateCells[i] = new CellCoord((int)x, (int)y);
            }

            // 점유 조회는 이번 계산에만 존재하며, 배치 셀은 이미 절대 좌표입니다.
            var occupiedCells = new HashSet<CellCoord>();
            ReadOnlySpan<ArtifactItemState> placedItems = state.PlacedItems.Span;
            for (int i = 0; i < placedItems.Length; i++)
            {
                ArtifactItemState item = placedItems[i];
                if (ignoredInstanceId.HasValue && item.InstanceId == ignoredInstanceId.Value) continue;

                ReadOnlySpan<CellCoord> cells = item.Cells.Span;
                for (int j = 0; j < cells.Length; j++) occupiedCells.Add(cells[j]);
            }

            for (int i = 0; i < candidateCells.Length; i++)
                if (occupiedCells.Contains(candidateCells[i]))
                    return ArtifactActionFailureReason.CellOccupied;

            return ArtifactActionFailureReason.None;
        }
    }
}
