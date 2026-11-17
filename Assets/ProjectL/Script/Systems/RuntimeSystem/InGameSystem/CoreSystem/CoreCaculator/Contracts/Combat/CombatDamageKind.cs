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
        DamageOverTime = 6,
        PlayerSkillAttack = 7,
        TraitExplosion = 8
    }

    /// <summary>대상 피해 계산·접촉 효과가 공유하는 공격 피해 계열입니다. 개별 효과의 추가 자격 조건은 각 규칙이 유지합니다.</summary>
    public static class CombatDamageKinds
    {
        public static bool IsAttack(CombatDamageKind kind) => kind is CombatDamageKind.OrdinaryAttack or
            CombatDamageKind.ExtraAttack or CombatDamageKind.ReactionAttack or CombatDamageKind.PlayerSkillAttack;
    }
}
