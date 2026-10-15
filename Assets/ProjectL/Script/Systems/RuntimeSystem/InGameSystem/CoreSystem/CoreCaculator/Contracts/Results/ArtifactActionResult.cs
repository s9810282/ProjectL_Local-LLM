using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>
    /// 아티팩트 행동의 성공 여부, 실패 사유, 성공한 행동의 대상 ID를 전달합니다.
    /// 유효한 결과는 Success 또는 Failure로 생성합니다.
    /// </summary>
    public readonly struct ArtifactActionResult
    {
        /// <summary>요청한 행동이 성공했는지 나타냅니다.</summary>
        public bool Succeeded { get; }

        /// <summary>실패한 이유입니다. 성공 시에는 None입니다.</summary>
        public ArtifactActionFailureReason FailureReason { get; }

        /// <summary>성공한 행동 대상의 정의 ID입니다. 실패하거나 보드를 확장한 경우 null입니다.</summary>
        public int? ArtifactId { get; }

        /// <summary>성공한 행동 대상의 인스턴스 ID입니다. 실패하거나 보드를 확장한 경우 null입니다.</summary>
        public int? InstanceId { get; }

        private ArtifactActionResult(
            bool succeeded,
            ArtifactActionFailureReason failureReason,
            int? artifactId,
            int? instanceId)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            ArtifactId = artifactId;
            InstanceId = instanceId;
        }

        /// <summary>보드 확장처럼 개별 아티팩트 대상이 없는 행동의 성공 결과를 생성합니다.</summary>
        public static ArtifactActionResult Success()
        {
            return new ArtifactActionResult(true, ArtifactActionFailureReason.None, null, null);
        }

        /// <summary>배치, 이동, 반환에 성공한 행동의 대상 ID를 담은 결과를 생성합니다.</summary>
        public static ArtifactActionResult Success(int artifactId, int instanceId)
        {
            if (artifactId < 0) throw new ArgumentOutOfRangeException(nameof(artifactId));
            if (instanceId < 0) throw new ArgumentOutOfRangeException(nameof(instanceId));

            return new ArtifactActionResult(true, ArtifactActionFailureReason.None, artifactId, instanceId);
        }

        /// <summary>대상 ID 없이 실패 사유를 담은 결과를 생성합니다.</summary>
        public static ArtifactActionResult Failure(ArtifactActionFailureReason failureReason)
        {
            if (failureReason == ArtifactActionFailureReason.None ||
                Enum.IsDefined(typeof(ArtifactActionFailureReason), failureReason) == false)
                throw new ArgumentOutOfRangeException(nameof(failureReason));

            return new ArtifactActionResult(false, failureReason, null, null);
        }
    }
}
