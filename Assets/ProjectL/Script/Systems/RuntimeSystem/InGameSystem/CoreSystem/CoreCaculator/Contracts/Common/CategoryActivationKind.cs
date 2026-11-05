namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    /// <summary>
    /// 이번 행동에서 Category 규칙이 실제로 발동한 사실을 분류합니다.
    /// TraitActivationKind와 숫자를 공유하지 않으며 서로 변환하지 않습니다.
    /// </summary>
    public enum CategoryActivationKind
    {
        None = 0,
        // 폐기된 환급(10)과 아군 보호 경감(21)의 사건 번호는 재사용하지 않습니다.
        DefenseFacingChanged = 20, // 폐기된 방향 방어 이벤트 번호 예약
    }
}
