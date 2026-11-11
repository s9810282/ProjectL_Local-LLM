namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    /// <summary>
    /// 특성을 가질 수 있는 진영 제한의 원본입니다. 특성 규칙 본문은 진영과 무관하게 짜고,
    /// 플레이어 쪽 진입은 DB 유닛 검사·성장 카탈로그 검사·유닛 자격 검사가 이 함수로 거절합니다.
    /// 제한을 풀 때는 이 함수와 그 세 검사만 고칩니다.
    /// </summary>
    public static class TraitOwnerPolicy
    {
        public static bool IsEnemyOnly(TraitKey key) =>
            key == TraitKey.Reconstruction || key == TraitKey.SelfDestruct || key == TraitKey.Fear;
    }
}
