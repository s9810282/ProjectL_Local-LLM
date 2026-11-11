namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    public enum TraitKey
    {
        None = 0,
        Focus = 10,
        MoveAttackDiscount = 20,
        PiercingFalloff = 30,
        ThornField = 40,
        Knockback = 50,
        DistanceDamageScale = 60,
        KillChainAttack = 70,
        Necromancy = 80, // 보스 사령술. 전투 중 소환 출처의 능력 키로도 쓴다.
        Roar = 90, // 보스 포효. 밤 피격 누적에 따른 반응 공격.
        Regeneration = 100, // 캐릭터 턴 종료 시 체력을 회복하는 특성.
        Intimidation = 110, // 피격 대상의 다음 공격과 우선도를 낮추는 특성.
        // 폐기된 특성의 키와 직렬화 번호는 호환성 검증을 위해 보존하며 재사용하지 않는다.
        DoubleShot = 160,
        LuckyBullet = 170,
        ChainExplosion = 180,
        HuntersMark = 190,
        Pull = 200,
        ImpactBolt = 210,
        Vanguard = 220,
        CleanSweep = 230,
        SoloAction = 240,
        CallOfBlood = 250,
        MoraleBoost = 260,
        Disarm = 270,
        AnkleBreaker = 280,
        Cohesion = 290,
        Sacrifice = 300,
        Taunt = 310,
        Entrenchment = 320,
        SteadfastWill = 330,
        GroundCompaction = 340,
        DustScreen = 350,
        Leap = 360,
        Acceleration = 370,
        StationaryFire = 380,
        ArmorBreaker = 390,
        FinishingShot = 400,
        TrackingShot = 410,
        Hunter = 420,
        FocusedBombardment = 430,
        PositionSwap = 440,
        ReconSquad = 450,
        Rampage = 460,
        LavaCannon = 470,
        Bleeding = 480,
        Reconstruction = 490, // 적 전용. 치명 피해를 한 번 버티고 다음 자기 턴 뒤에 다시 일어난다.
        SelfDestruct = 500, // 적 전용. 피해로 죽으면 주변 8칸에 고정 피해를 준다.
        Fear = 510, // 적 전용. 피해로 죽으면 주변 8칸 캐릭터의 이동·공격을 막는다.
        CrashImpact = 520, // 작렬. 넉백 변형: 날아가다 막혀 멈추면 날아간 대상(과 부딪힌 유닛)이 최대 체력 비율 피해를 입는다.
        // 폐기된 특성의 직렬화 번호 120은 재사용하지 않습니다.
        // 아래 세 값도 실행하지 않는 폐기 특성의 예약 번호다.
        DoorBreaching = 130,
        ContemptForWeak = 140,
        PrecisionShot = 150,
    }
}
