namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    public enum TraitActivationKind
    {
        None = 0,
        DamageModified = 140,
        DefenseBypassed = 150,
        StatusApplied = 160,
        StatusConsumed = 170,
        StatusRemoved = 180,
        StatusExpired = 190,
        StatusReset = 200,
        ShieldGranted = 210,
        ShieldAbsorbed = 220,
        ShieldRemoved = 230,
        ShieldReset = 240,
        MovementChanged = 250,
        TrailCreated = 260,
        TrailChanged = 270,
        TrailRemoved = 280,
        KnockbackApplied = 290,
        KnockbackPrevented = 300,
        PullApplied = 310,
        TauntSelected = 320,
        RangeChanged = 330,
        DefenseChanged = 340,
        InstantDefeat = 350,
        RoarTriggered = 360,
        ReconstructionStarted = 370, // 치명 피해를 재구성으로 버팀. 출처·대상은 재구성한 유닛, Magnitude는 잃은 체력
        ReconstructionEnded = 380, // 재구성 중 상태가 풀려 부활함. 출처·대상은 부활한 유닛
        SelfDestructExploded = 390, // 자폭 폭발. 출처는 죽은 유닛, 대상은 실제로 바뀐 개체, 칸은 폭발 칸, 선택 칸은 사망 칸, Magnitude는 폭발 피해
        CorpseDisplaced = 400, // 넉백으로 밀린 사망 특성 보유자의 시체 경로. 출처는 죽은 유닛, 칸은 시작 → 끝, Magnitude는 칸 수. 위치 사건은 없음
        // 가시 폭탄 폭발 연출 줄의 열쇠다(가시 폭탄 Phase 4 D3). Core는 이 발동 사실을 내지 않고 판독기도 거절한다.
        // 폭발 사실은 CombatThornBombDetonatedEvent 하나이고, View가 그 사건에서 이 열쇠로 TraitVisual 발동 줄을 찾는다.
        ThornBombDetonated = 420,
        CrashImpactCollided = 410, // 작렬로 날아가다 막혀 부딪힘. 출처는 작렬 보유자, 대상은 날아간 유닛(+ 부딪힌 유닛), 칸은 막은 칸, 선택 칸은 날아간 유닛의 멈춘 칸, Magnitude는 방어 전 기준 피해. 부딪힘 피해 사건보다 앞에 온다
        HunterTargetSelected = 130, // 행동 종료 또는 참여 때 잠긴 대상의 변경입니다.
        ArmorBroken = 120, // 타격 뒤 실제 수신자에게 방어 잔존 비율을 추가했습니다.
        HealthRecovered = 100, // 처치로 실제 회복한 체력을 기록합니다.
        AttackGranted = 90, // 출처가 있는 공격 기여분을 부여했습니다.
        FollowUpAttack = 80, // 효과 원본과 대상은 발동 사실에 기록
        FocusGainScheduled = 10, // 포커스 기능 다음 턴에 적용되도록 예약
        FocusStackApplied = 11, // 포커스 중첩이 실제로 적용됨
        FocusReset = 12, // 중첩 리셋
        AttackDiscountArmed = 20, // 다음 공격 할인이 준비됨
        AttackCostReduced = 21, // 실제 공격에서 비용 감소가 적용됨
        AttackDiscountExpired = 22, // 사용하지 못한 할인이 턴 종료로 사라짐
        KillChainAttack = 70, // 처치로 생긴 추가 공격 1회. 대상은 TargetId, Magnitude는 보유 방어 뒤 최종 피해
        DamageRedirected = 110, // 접촉 대상을 유지하면서 숫자 피해를 희생 소유자에게 전달했습니다.
    }
}