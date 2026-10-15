namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    /// <summary>
    /// 이번 행동에서 Category 규칙이 실제로 발동한 사실을 분류합니다.
    /// TraitActivationKind와 숫자를 공유하지 않으며 서로 변환하지 않습니다.
    /// </summary>
    public enum CategoryActivationKind
    {
        None = 0,
        RefundApplied = 10, // 실제 양수 환급이 적용됨
        DefenseFacingChanged = 20, // 방어 Facing이 갱신됨
        IncomingDamageReduced = 21, // 피격 피해가 실제로 경감됨
    }
}
