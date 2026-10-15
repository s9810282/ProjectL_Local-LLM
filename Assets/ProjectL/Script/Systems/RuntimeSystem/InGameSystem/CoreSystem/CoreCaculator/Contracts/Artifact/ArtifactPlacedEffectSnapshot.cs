using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact
{
    /// <summary>
    /// 조회 시점에 배치된 아티팩트의 효과 한 항목과 그 출처입니다.
    /// 효과의 발동 여부와 적용 결과는 소비 측에서 판단합니다.
    /// </summary>
    public readonly struct ArtifactPlacedEffectSnapshot
    {
        /// <summary>효과 정의를 가진 아티팩트 종류의 ID입니다.</summary>
        public int ArtifactId { get; }

        /// <summary>동일 종류의 복수 배치를 구분하는 배치 인스턴스 ID입니다.</summary>
        public int InstanceId { get; }

        /// <summary>정의에서 복사한 효과 설정입니다. 하위 설정도 값 타입으로 반환됩니다.</summary>
        public ArtifactEffectData Effect { get; }

        public ArtifactPlacedEffectSnapshot(int artifactId, int instanceId, ArtifactEffectData effect)
        {
            ArtifactId = artifactId;
            InstanceId = instanceId;
            Effect = effect;
        }
    }
}
