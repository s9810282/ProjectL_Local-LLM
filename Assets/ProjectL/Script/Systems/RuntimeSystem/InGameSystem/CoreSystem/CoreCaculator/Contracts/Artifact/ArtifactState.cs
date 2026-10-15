using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact
{
    public readonly struct ArtifactItemState
    {
        private readonly CellCoord[] _cells;

        public int ArtifactId { get; }
        public int InstanceId { get; }
        public bool IsPlaced { get; }
        public CellCoord Anchor { get; }
        public Rotation4 Rotation { get; }
        public ReadOnlyMemory<CellCoord> Cells => _cells ?? Array.Empty<CellCoord>();

        public ArtifactItemState(
            int artifactId,
            int instanceId,
            bool isPlaced,
            CellCoord anchor,
            Rotation4 rotation,
            CellCoord[] cells)
        {
            ArtifactId = artifactId;
            InstanceId = instanceId;
            IsPlaced = isPlaced;
            Anchor = anchor;
            Rotation = rotation;
            _cells = cells ?? Array.Empty<CellCoord>();
        }
    }

    public sealed class ArtifactState
    {
        private readonly ArtifactItemState[] _poolItems;
        private readonly ArtifactItemState[] _placedItems;

        public int BoardWidth { get; }
        public int BoardHeight { get; }
        public ReadOnlyMemory<ArtifactItemState> PoolItems => _poolItems;
        public ReadOnlyMemory<ArtifactItemState> PlacedItems => _placedItems;

        public ArtifactState(
            int boardWidth,
            int boardHeight,
            ArtifactItemState[] poolItems,
            ArtifactItemState[] placedItems)
        {
            BoardWidth = boardWidth;
            BoardHeight = boardHeight;
            _poolItems = poolItems ?? Array.Empty<ArtifactItemState>();
            _placedItems = placedItems ?? Array.Empty<ArtifactItemState>();
        }

        /// <summary>입력의 항목 배열과 각 셀 배열을 복사하여 독립된 전달 상태를 만듭니다.</summary>
        public static ArtifactState DetachedCopyOf(ArtifactState source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new ArtifactState(source.BoardWidth, source.BoardHeight,
                CopyItems(source.PoolItems), CopyItems(source.PlacedItems));
        }

        private static ArtifactItemState[] CopyItems(ReadOnlyMemory<ArtifactItemState> source)
        {
            var result = new ArtifactItemState[source.Length];
            ReadOnlySpan<ArtifactItemState> items = source.Span;
            for (int i = 0; i < items.Length; i++)
            {
                ArtifactItemState item = items[i];
                result[i] = new ArtifactItemState(item.ArtifactId, item.InstanceId, item.IsPlaced,
                    item.Anchor, item.Rotation, item.Cells.ToArray());
            }
            return result;
        }
    }
}
