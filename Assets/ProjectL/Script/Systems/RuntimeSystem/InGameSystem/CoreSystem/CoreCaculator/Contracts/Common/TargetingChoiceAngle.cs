namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    /// <summary>진영의 고정 정면을 기준으로 최초 공격 지정을 제한하는 각도입니다.</summary>
    public enum TargetingChoiceAngle
    {
        Unrestricted = 0,
        Front = 1,
        FrontAndSides = 2,
        FrontAndRear = 3,
    }
}
