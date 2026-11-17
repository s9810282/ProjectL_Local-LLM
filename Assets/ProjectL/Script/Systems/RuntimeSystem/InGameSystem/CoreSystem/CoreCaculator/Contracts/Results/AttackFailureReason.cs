namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum AttackFailureReason
    {
        None = 0,
        WrongPhase, // 잘못된 페이즈
        EntityNotFound, // 공격자 또는 대상이 존재하지 않음
        NotUnit,// 공격자가 유닛이 아님
        NotInCombat, // Combat 위치에 없음
        Dead, // 공격자 또는 대상이 사망 상태임
        NotSupportAppointMethod = 7, // 삭제한 활성 유닛 오류의 번호 6은 재사용하지 않는다.
        InvalidPrimaryTarget, // 주요 대상이 공격자의 공격 범위 내에 없음
        NonHostile, // 공격 대상이 적대적이지 않음
        OutOfBounds, // 사거리 밖
        ActionCostInsufficient, // 행동력 부족
        MapOccupancyMismatch, // 공격자 또는 대상의 맵 점유 상태가 예상과 다름
        StageEnded, // 스테이지 종료
        InvalidAttackDirection,
        AmbiguousAttackDirection,
        InvalidAttackAim,
        AppointCellRejected, // 인접 제외 또는 벽 점유 규칙이 선택 셀을 거부함
        NoEligibleTargets = 19, // 삭제한 이동 정산 사망 오류의 번호 18은 재사용하지 않는다.
        UnresolvedEmptyAll, // 빈 전체 지정 정책은 아직 미정
        DeferredMultiCellUnit, // 다칸 유닛은 기획 검토 전까지 보류
        BlockedByWall, // 피격 자격을 갖춘 접촉 또는 허용된 빈 중심으로 향하는 경로가 벽에 막힘
        UnsupportedAttackDelivery = 23,
        BasicAttackDisabled = 24, // 정의상 기본 공격을 하지 않는 유닛
        WrongController = 30,
        ActionAlreadyUsed,
        ActionDeferred,
        StaleTurn,
        PullPathBlocked = 40, // 끌어오기 전체 경로가 막혀 비용 없이 거절됨
        PullImmune = 41, // 명시적 강제 이동 면역
        PullNoMovement = 42, // 아군에게 실제 이동이 없는 지정
        Disarmed = 50,
        HaboobDesignationBlocked = 51,
        Reconstructing = 52, // 적 전용 재구성 중(이동·공격 불가)
        Stunned = 53,
    }
}
