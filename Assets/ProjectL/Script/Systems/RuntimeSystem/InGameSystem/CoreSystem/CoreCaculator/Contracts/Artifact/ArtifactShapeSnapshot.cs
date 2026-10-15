using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact
{
    /// <summary>Core가 회전을 적용한 로컬 셀과 외곽 크기입니다. View는 화면 좌표로만 변환합니다.</summary>
    public sealed class ArtifactShapeSnapshot
    {
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<CellCoord> Cells { get; }

        public ArtifactShapeSnapshot(int width, int height, IReadOnlyList<CellCoord> cells)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            var copy = new CellCoord[cells.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = cells[i];
            Width = width;
            Height = height;
            Cells = Array.AsReadOnly(copy);
        }
    }
}
