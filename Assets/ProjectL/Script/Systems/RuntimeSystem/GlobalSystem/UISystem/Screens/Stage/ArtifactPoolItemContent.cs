using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>보유 목록에 그릴 값입니다. 목록·상세 문구는 전달받고, Cells는 표시할 로컬 모양입니다.</summary>
    public readonly struct ArtifactPoolItemContent
    {
        public int ArtifactId { get; }
        public int InstanceId { get; }
        public IReadOnlyList<CellCoord> Cells { get; }
        public string DisplayName { get; }
        public string Description { get; }

        public ArtifactPoolItemContent(int artifactId, int instanceId, IReadOnlyList<CellCoord> cells,
            string displayName, string description)
        {
            ArtifactId = artifactId;
            InstanceId = instanceId;
            Cells = cells ?? throw new ArgumentNullException(nameof(cells));
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
        }
    }
}
