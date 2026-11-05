namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    public enum AttackTargetRole
    {
        None = 0,
        Primary = 1,
        Additional = 2,
        Designated = 3,
        Effect = 4, // 지정 대상 외에 특성의 전달 경로로 피격된 대상
    }
}
