using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using System;
using System.Collections.Generic;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact
{
    /// <summary>
    /// Core에서 복사하여 View에 전달하는 개별 아티팩트 상태입니다.
    /// </summary>

    [Serializable]
    public sealed class ArtifactData
    {
        public int ArtifactId { get; }
        public int InstanceId { get; }
        public bool IsPlaced { get; }

        public CellCoord Anchor { get; }
        public Rotation4 Rotation { get; }

        public IReadOnlyList<CellCoord> Cells { get; }

        public ArtifactData(ArtifactItemState source)
        {
            ArtifactId = source.ArtifactId;
            InstanceId = source.InstanceId;
            IsPlaced = source.IsPlaced;
            Anchor = source.Anchor;
            Rotation = source.Rotation;

            // Core가 가진 배열과 분리된 복사본을 보관합니다.
            CellCoord[] copiedCells = source.Cells.ToArray();
            Cells = Array.AsReadOnly(copiedCells);
        }
    }
}
