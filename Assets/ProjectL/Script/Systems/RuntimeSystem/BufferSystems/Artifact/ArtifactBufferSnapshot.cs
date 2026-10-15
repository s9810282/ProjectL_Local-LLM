using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact
{
    /// <summary>
    /// 아티팩트 보드와 목록의 읽기 전용 상태입니다.
    /// 보유 기록을 독립적으로 보관하고 계산 시 전달할 복사본을 제공합니다.
    /// </summary>
    public sealed class ArtifactBufferSnapshot
    {
        public static ArtifactBufferSnapshot Empty { get; } = new();

        public bool IsInitialized { get; }

        public int BoardWidth { get; }
        public int BoardHeight { get; }

        public IReadOnlyList<ArtifactData> PoolItems { get; }
        public IReadOnlyList<ArtifactData> PlacedItems { get; }

        private ArtifactBufferSnapshot()
        {
            IsInitialized = false;
            BoardWidth = 0;
            BoardHeight = 0;

            PoolItems = Array.AsReadOnly(Array.Empty<ArtifactData>());
            PlacedItems = Array.AsReadOnly(Array.Empty<ArtifactData>());
        }

        public ArtifactBufferSnapshot(ArtifactState source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source.BoardWidth <= 0 || source.BoardHeight <= 0)
            {
                throw new ArgumentException(
                    "초기화된 아티팩트 보드의 크기는 양수여야 합니다.",
                    nameof(source));
            }

            BoardWidth = source.BoardWidth;
            BoardHeight = source.BoardHeight;

            var instanceIds = new HashSet<int>();
            PoolItems = CopyItems(source.PoolItems, false, instanceIds);
            PlacedItems = CopyItems(source.PlacedItems, true, instanceIds);

            IsInitialized = true;
        }

        private static IReadOnlyList<ArtifactData> CopyItems(
            ReadOnlyMemory<ArtifactItemState> source, bool placed, HashSet<int> instanceIds)
        {
            var copiedItems = new ArtifactData[source.Length];
            ReadOnlySpan<ArtifactItemState> sourceItems = source.Span;

            for (int i = 0; i < sourceItems.Length; i++)
            {
                ArtifactItemState item = sourceItems[i];
                ValidateItem(item, placed, instanceIds);
                copiedItems[i] = new ArtifactData(item);
            }

            return Array.AsReadOnly(copiedItems);
        }

        private static void ValidateItem(ArtifactItemState item, bool placed, HashSet<int> instanceIds)
        {
            // 저장 형식만 검증한다. 배치 범위, 충돌과 정의 계산은 Core의 책임이다.
            if (item.ArtifactId < 0 || item.InstanceId < 0 || !instanceIds.Add(item.InstanceId))
                throw new ArgumentException("아티팩트 ID는 음수가 아니어야 하며 인스턴스 ID는 중복될 수 없습니다.", "source");
            if (item.IsPlaced != placed || !Enum.IsDefined(typeof(Rotation4), item.Rotation) ||
                (!placed && (item.Anchor != CellCoord.Zero || item.Rotation != Rotation4.R0)))
                throw new ArgumentException("아티팩트 목록 구분이나 회전 상태가 올바르지 않습니다.", "source");
            if (item.Cells.Length == 0)
                throw new ArgumentException("아티팩트에는 셀이 필요합니다.", "source");

            var cells = new HashSet<CellCoord>();
            foreach (CellCoord cell in item.Cells.Span)
                if (!cells.Add(cell))
                    throw new ArgumentException("아티팩트 셀은 중복될 수 없습니다.", "source");
        }

        public ArtifactState ToCalculationState()
        {
            if (!IsInitialized)
                throw new InvalidOperationException("초기화되지 않은 아티팩트 상태는 계산 입력으로 변환할 수 없습니다.");
            return new ArtifactState(BoardWidth, BoardHeight,
                ToCalculationItems(PoolItems), ToCalculationItems(PlacedItems));
        }

        private static ArtifactItemState[] ToCalculationItems(IReadOnlyList<ArtifactData> source)
        {
            var items = new ArtifactItemState[source.Count];
            for (int i = 0; i < items.Length; i++)
            {
                ArtifactData item = source[i];
                var cells = new CellCoord[item.Cells.Count];
                for (int j = 0; j < cells.Length; j++) cells[j] = item.Cells[j];
                items[i] = new ArtifactItemState(item.ArtifactId, item.InstanceId, item.IsPlaced,
                    item.Anchor, item.Rotation, cells);
            }
            return items;
        }
    }
}
