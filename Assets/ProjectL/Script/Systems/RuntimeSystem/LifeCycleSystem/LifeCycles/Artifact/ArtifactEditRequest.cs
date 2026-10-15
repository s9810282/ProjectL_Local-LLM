using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact
{
    /// <summary>아티팩트 요청의 실행 단계입니다. 외부는 읽기만 하고 Job과 스케줄러만 바꿉니다.</summary>
    public enum ArtifactRequestStatus
    {
        Queued,
        Running,
        Completed,
        Cancelled,
        Faulted
    }

    public enum ArtifactEditKind
    {
        Place,
        Move,
        Return
    }

    public enum ArtifactPreviewKind
    {
        Place,
        Move
    }

    /// <summary>
    /// 배치·이동·반환 요청 하나입니다. 대상(종류·ID·칸·회전)은 생성 후 바뀌지 않고, 한 호출의 결과만 보관합니다.
    /// </summary>
    /// <remarks>
    /// 정의 ID와 인스턴스 ID는 0도 유효합니다(인계 §3). 성공 여부는 Core 호출 직후 게시된 결과로만 판단합니다(인계 §5).
    /// 전투 진입 요청(<c>CombatEntryRequest</c>)과 같은 구조입니다(아티팩트 팝업 계획 Q1·D8b).
    /// </remarks>
    public sealed class ArtifactEditRequest
    {
        public ArtifactEditKind Kind { get; }
        /// <summary>배치할 정의 ID입니다. 배치 요청에서만 의미가 있습니다.</summary>
        public int ArtifactId { get; }
        /// <summary>이동·반환할 배치 인스턴스 ID입니다. 이동·반환 요청에서만 의미가 있습니다.</summary>
        public int InstanceId { get; }
        public CellCoord Anchor { get; }
        public Rotation4 Rotation { get; }

        public ArtifactRequestStatus Status { get; private set; } = ArtifactRequestStatus.Queued;

        /// <summary>Completed일 때만 값이 있습니다. 정상 거절도 Completed이며 실패 사유를 가집니다.</summary>
        public ArtifactActionResult? Result { get; private set; }

        private ArtifactEditRequest(ArtifactEditKind kind, int artifactId, int instanceId,
            CellCoord anchor, Rotation4 rotation)
        {
            if (artifactId < 0) throw new ArgumentOutOfRangeException(nameof(artifactId));
            if (instanceId < 0) throw new ArgumentOutOfRangeException(nameof(instanceId));
            if (!System.Enum.IsDefined(typeof(Rotation4), rotation))
                throw new ArgumentOutOfRangeException(nameof(rotation));

            Kind = kind;
            ArtifactId = artifactId;
            InstanceId = instanceId;
            Anchor = anchor;
            Rotation = rotation;
        }

        public static ArtifactEditRequest Place(int artifactId, CellCoord anchor, Rotation4 rotation) =>
            new(ArtifactEditKind.Place, artifactId, 0, anchor, rotation);

        public static ArtifactEditRequest Move(int instanceId, CellCoord anchor, Rotation4 rotation) =>
            new(ArtifactEditKind.Move, 0, instanceId, anchor, rotation);

        public static ArtifactEditRequest Return(int instanceId) =>
            new(ArtifactEditKind.Return, 0, instanceId, default, Rotation4.R0);

        internal bool IsPending => Status == ArtifactRequestStatus.Queued || Status == ArtifactRequestStatus.Running;

        /// <summary>실행 전 취소입니다. 이미 실행을 시작했거나 끝났으면 false입니다.</summary>
        internal bool Cancel()
        {
            if (Status != ArtifactRequestStatus.Queued)
                return false;

            Status = ArtifactRequestStatus.Cancelled;
            return true;
        }

        /// <summary>실행을 시작합니다. 취소된 요청이면 false입니다.</summary>
        internal bool TryStart()
        {
            if (Status != ArtifactRequestStatus.Queued)
                return false;

            Status = ArtifactRequestStatus.Running;
            return true;
        }

        internal void Complete(ArtifactActionResult result)
        {
            if (Status != ArtifactRequestStatus.Running)
                throw new InvalidOperationException("[ArtifactEditRequest] Only a running request can complete.");

            Result = result;
            Status = ArtifactRequestStatus.Completed;
        }

        internal void Fail()
        {
            if (Status == ArtifactRequestStatus.Running)
                Status = ArtifactRequestStatus.Faulted;
        }
    }

    /// <summary>
    /// 공간 프리뷰 요청 하나입니다. 대상은 생성 후 바뀌지 않습니다. 재고·편집 권한은 보장하지 않습니다(인계 §6).
    /// </summary>
    public sealed class ArtifactPreviewRequest
    {
        public ArtifactPreviewKind Kind { get; }
        public int ArtifactId { get; }
        public int InstanceId { get; }
        public CellCoord Anchor { get; }
        public Rotation4 Rotation { get; }

        public ArtifactRequestStatus Status { get; private set; } = ArtifactRequestStatus.Queued;

        /// <summary>Completed일 때만 값이 있습니다.</summary>
        public ArtifactPlacementPreview Result { get; private set; }

        private ArtifactPreviewRequest(ArtifactPreviewKind kind, int artifactId, int instanceId,
            CellCoord anchor, Rotation4 rotation)
        {
            if (artifactId < 0) throw new ArgumentOutOfRangeException(nameof(artifactId));
            if (instanceId < 0) throw new ArgumentOutOfRangeException(nameof(instanceId));
            if (!System.Enum.IsDefined(typeof(Rotation4), rotation))
                throw new ArgumentOutOfRangeException(nameof(rotation));

            Kind = kind;
            ArtifactId = artifactId;
            InstanceId = instanceId;
            Anchor = anchor;
            Rotation = rotation;
        }

        public static ArtifactPreviewRequest Place(int artifactId, CellCoord anchor, Rotation4 rotation) =>
            new(ArtifactPreviewKind.Place, artifactId, 0, anchor, rotation);

        public static ArtifactPreviewRequest Move(int instanceId, CellCoord anchor, Rotation4 rotation) =>
            new(ArtifactPreviewKind.Move, 0, instanceId, anchor, rotation);

        internal bool Cancel()
        {
            if (Status != ArtifactRequestStatus.Queued)
                return false;

            Status = ArtifactRequestStatus.Cancelled;
            return true;
        }

        internal bool TryStart()
        {
            if (Status != ArtifactRequestStatus.Queued)
                return false;

            Status = ArtifactRequestStatus.Running;
            return true;
        }

        internal void Complete(ArtifactPlacementPreview result)
        {
            if (Status != ArtifactRequestStatus.Running)
                throw new InvalidOperationException("[ArtifactPreviewRequest] Only a running request can complete.");

            Result = result ?? throw new ArgumentNullException(nameof(result));
            Status = ArtifactRequestStatus.Completed;
        }

        internal void Fail()
        {
            if (Status == ArtifactRequestStatus.Running)
                Status = ArtifactRequestStatus.Faulted;
        }
    }
}
