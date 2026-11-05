namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    /// <summary>
    /// Category가 소유한 내부 규칙을 식별하는 안정적인 Key입니다.
    /// Character Trait의 TraitKey와 별개이며 서로 섞어 쓰지 않습니다.
    /// </summary>
    public enum CategoryRuleKey
    {
        None = 0,
        // 폐기된 이동 환급(1)과 아군 보호(4)의 직렬화 번호는 재사용하지 않습니다.
        DirectionalArmor = 2, // 폐기된 방향 방어의 직렬화 번호 예약
        SafeBombardment = 3, // Cannon: 근접 직접 조준 제한
    }
}
