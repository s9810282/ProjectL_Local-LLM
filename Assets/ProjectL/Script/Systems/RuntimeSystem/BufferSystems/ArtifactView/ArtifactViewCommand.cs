using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.ArtifactView
{
    /// <summary>사용자의 선택만 담습니다. 소유 여부와 배치 가능 여부는 Core가 판단합니다.</summary>
    public readonly struct ArtifactViewCommandData
    {
        public ArtifactViewCommandKind Kind { get; }
        /// <summary>Place에서 사용하는 종류 ID입니다. 실제 보유 항목 선택은 기존 Core API가 담당합니다.</summary>
        public int ArtifactId { get; }
        /// <summary>Move와 Return에서 사용하는 Core 발급 개체 ID입니다.</summary>
        public int InstanceId { get; }
        /// <summary>Place와 Move의 목표 논리 칸입니다. Return에서는 사용하지 않습니다.</summary>
        public CellCoord Cell { get; }
        /// <summary>Place와 Move의 최종 회전입니다. Return에서는 사용하지 않습니다.</summary>
        public Rotation4 Rotation { get; }

        private ArtifactViewCommandData(ArtifactViewCommandKind kind, int artifactId,
            int instanceId, CellCoord cell, Rotation4 rotation)
        {
            Kind = kind;
            ArtifactId = artifactId;
            InstanceId = instanceId;
            Cell = cell;
            Rotation = rotation;
        }

        public static ArtifactViewCommandData Place(int artifactId, CellCoord cell, Rotation4 rotation)
        {
            RequireId(artifactId, nameof(artifactId));
            RequireRotation(rotation);
            return new ArtifactViewCommandData(ArtifactViewCommandKind.Place, artifactId, 0, cell, rotation);
        }

        public static ArtifactViewCommandData Move(int instanceId, CellCoord cell, Rotation4 rotation)
        {
            RequireId(instanceId, nameof(instanceId));
            RequireRotation(rotation);
            return new ArtifactViewCommandData(ArtifactViewCommandKind.Move, 0, instanceId, cell, rotation);
        }

        public static ArtifactViewCommandData Return(int instanceId)
        {
            RequireId(instanceId, nameof(instanceId));
            return new ArtifactViewCommandData(ArtifactViewCommandKind.Return, 0, instanceId, default, default);
        }

        internal void Validate()
        {
            switch (Kind)
            {
                case ArtifactViewCommandKind.Place:
                    RequireId(ArtifactId, nameof(ArtifactId));
                    RequireRotation(Rotation);
                    break;
                case ArtifactViewCommandKind.Move:
                    RequireId(InstanceId, nameof(InstanceId));
                    RequireRotation(Rotation);
                    break;
                case ArtifactViewCommandKind.Return:
                    RequireId(InstanceId, nameof(InstanceId));
                    break;
                default:
                    throw new ArgumentException("유효한 아티팩트 요청 종류가 필요합니다.");
            }
        }

        private static void RequireId(int id, string name)
        {
            if (id <= 0)
                throw new ArgumentOutOfRangeException(name, "식별자는 양수여야 합니다.");
        }

        private static void RequireRotation(Rotation4 rotation)
        {
            if (!Enum.IsDefined(typeof(Rotation4), rotation))
                throw new ArgumentOutOfRangeException(nameof(rotation));
        }
    }

    /// <summary>
    /// Buffer 인스턴스 안에서 재사용하지 않는 요청 번호와 데이터입니다.
    /// 다른 입력 Buffer의 번호와는 별개이므로 결과 대응 시 Buffer 종류도 구분해야 합니다.
    /// </summary>
    public readonly struct ArtifactViewCommand
    {
        public int Number { get; }
        public ArtifactViewCommandData Data { get; }

        internal ArtifactViewCommand(int number, ArtifactViewCommandData data)
        {
            if (number <= 0)
                throw new ArgumentOutOfRangeException(nameof(number));

            data.Validate();
            Number = number;
            Data = data;
        }
    }
}
