using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public readonly struct ArtifactCreateSnapshot
    {
        public int ArtifactID { get; }

        public CellCoord Anchor { get; }

        public CellCoord[] Cells { get; }

        public ArtifactCreateSnapshot(
            int artifactId,
            CellCoord anchor,
            CellCoord[] cells)
        {
            ArtifactID = artifactId;
            Anchor = anchor;
            Cells = cells;
        }
    }
}