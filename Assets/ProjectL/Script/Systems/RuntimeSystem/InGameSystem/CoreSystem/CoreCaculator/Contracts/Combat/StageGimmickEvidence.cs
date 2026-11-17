namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>
    /// 환경 난수를 쓴 사건이 사용 직전·직후 값을 선언한다. 값이 없으면(null) 그 사건은 난수를 쓰지 않았다.
    /// 검증은 이 값으로 같은 전이 안의 환경 난수 흐름을 잇는다.
    /// </summary>
    public interface IEnvironmentRandomEvidence
    {
        uint? ExpectedRandomState { get; }
        uint? FinalRandomState { get; }
    }

    /// <summary>사건 직후의 기믹 상태를 선언한다. 검증용 공간 투영은 이 값으로 기믹 점유를 사건 순서대로 따라간다.</summary>
    public interface IStageGimmickStateChange
    {
        StageGimmickState FinalGimmick { get; }
    }
}
