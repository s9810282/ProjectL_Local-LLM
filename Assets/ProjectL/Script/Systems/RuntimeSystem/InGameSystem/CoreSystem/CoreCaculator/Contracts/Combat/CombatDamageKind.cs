namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>
    /// 캐릭터에게 적용되는 피해 1회의 원인 분류다. 피해 경로가 적용 경계에 넘기며 아이템 단계가 이 값으로 자격을 판단한다.
    /// 0은 비워 둔다. default 값이 정의되지 않은 값이 되어 계약 생성자 검사에서 바로 거부된다.
    /// </summary>
    public enum CombatDamageKind
    {
        OrdinaryAttack = 1,
        ExtraAttack = 2,
        ReactionAttack = 3,
        ReflectedDamage = 4,
        EnvironmentDamage = 5,
        DamageOverTime = 6
    }
}
