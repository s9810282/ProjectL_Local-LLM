using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.ArtifactView
{
    /// <summary>최신 배치·이동 프리뷰 의도입니다. 배치 가능 여부는 Core가 판단합니다.</summary>
    public readonly struct ArtifactViewPreviewData
    {
        private readonly ArtifactViewCommandData _data;
        public ArtifactViewCommandKind Kind => _data.Kind;
        public int ArtifactId => _data.ArtifactId;
        public int InstanceId => _data.InstanceId;
        public CellCoord Cell => _data.Cell;
        public Rotation4 Rotation => _data.Rotation;

        private ArtifactViewPreviewData(ArtifactViewCommandData data)
        {
            _data = data;
        }
        public static ArtifactViewPreviewData Place(int artifactId, CellCoord cell, Rotation4 rotation) =>
            new(ArtifactViewCommandData.Place(artifactId, cell, rotation));
        public static ArtifactViewPreviewData Move(int instanceId, CellCoord cell, Rotation4 rotation) =>
            new(ArtifactViewCommandData.Move(instanceId, cell, rotation));

        internal void Validate()
        {
            if (Kind != ArtifactViewCommandKind.Place && Kind != ArtifactViewCommandKind.Move)
                throw new ArgumentException("배치 또는 이동 프리뷰가 필요합니다.");
            _data.Validate();
        }

        internal bool Matches(ArtifactViewPreviewData other) => _data.Matches(other._data);
    }

    /// <summary>명령과 같은 Buffer 발급 번호를 쓰는 최신 프리뷰 슬롯입니다.</summary>
    public readonly struct ArtifactViewPreviewRequest
    {
        public int Number { get; }
        public ArtifactViewPreviewData Data { get; }
        internal ArtifactViewPreviewRequest(int number, ArtifactViewPreviewData data)
        {
            if (number <= 0) throw new ArgumentOutOfRangeException(nameof(number));
            // 데이터는 Buffer의 SetPreview가 넣기 전에 검사합니다(로비 리팩토링 2차 설계 N-20).
            Number = number;
            Data = data;
        }
    }
}
