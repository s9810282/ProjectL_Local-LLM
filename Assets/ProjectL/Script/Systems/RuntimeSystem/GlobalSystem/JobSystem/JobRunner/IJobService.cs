namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner
{
    /// <summary>
    /// 각 Job이 직접 구현하는 실행 계약입니다.
    /// JobRunner는 이 인터페이스만 받아 우선순위 순서로 실행합니다.
    /// </summary>
    public interface IJobService : IJobCompletion
    {
        int Priority { get; }
        void Execute();
    }
}
