namespace ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum
{
    public enum EffectType
    {
        Empty,
        TestFire,
        Boom = 2,
        GunFire = 3,
        Hit = 4,
        Cure = 5,

        Stage1Boss1,

        Execution,
        DefensiceShield,
        ForcedMovement,
        Knock,
        Sacrifice,
        Targeting,

        Buff,
        DeBuff,

        FallingRocks,
        Smoke,
        ArrowBoom,

        // 작은 크기 복사본입니다(작렬 View 계획 §4.5, 프리팹 루트 0.5배·파티클 크기 방식 Hierarchy). 에셋이 정수로 저장하므로 끝에만 더합니다.
        BoomSmall = 18,
        FallingRocksSmall = 19,

        // 자폭 이펙트입니다(자폭 깜빡임 설계 SD8). 에셋은 번호로 저장되므로 새 값은 맨 끝에 번호를 적어 붙입니다(중간에 끼우면 뒤 값을 쓰는 모든 큐·표 항목이 바뀜).
        SelfDestruction = 20,

        // 스테이지 노드 파괴 효과. 기존 직렬화 키는 변경하지 않는다.
        StageNodeBreakCombat = 21,
        StageNodeBreakBoss = 22,
        StageNodeBreakMainCharacter = 23,
        StageNodeBreakMainSkill = 24,
        StageNodeBreakRecruitment = 25,
        StageNodeBreakRandomEvent = 26,
        StageNodeBreakRecovery = 27,
        StageNodeBreakSupportingCharacter = 28

    }

}
