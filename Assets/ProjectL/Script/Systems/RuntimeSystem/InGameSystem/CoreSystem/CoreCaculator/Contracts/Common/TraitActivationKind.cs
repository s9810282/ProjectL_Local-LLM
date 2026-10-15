namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    public enum TraitActivationKind
    {
        None = 0,
        FocusGainScheduled = 10, // 포커스 기능 다음 턴에 적용되도록 예약
        FocusStackApplied = 11, // 포커스 중첩이 실제로 적용됨
        FocusReset = 12, // 중첩 리셋
        AttackDiscountArmed = 20, // 다음 공격 할인이 준비됨
        AttackCostReduced = 21, // 실제 공격에서 비용 감소가 적용됨
        AttackDiscountExpired = 22, // 사용하지 못한 할인이 턴 종료로 사라짐
        KillChainAttack = 70, // 처치로 생긴 추가 공격 1회. 대상은 TargetId, Magnitude는 보유 방어 뒤 최종 피해
    }
}