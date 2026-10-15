namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    /// <summary>
    /// Category가 소유한 내부 규칙을 식별하는 안정적인 Key입니다.
    /// Character Trait의 TraitKey와 별개이며 서로 섞어 쓰지 않습니다.
    /// </summary>
    public enum CategoryRuleKey
    {
        None = 0,
        ManeuverRefund = 1, // Rider: 이동 비용 환급
        DirectionalArmor = 2, // Heavy: 전방 피해 경감
        SafeBombardment = 3, // Cannon: 근접 직접 조준 제한
    }
}
