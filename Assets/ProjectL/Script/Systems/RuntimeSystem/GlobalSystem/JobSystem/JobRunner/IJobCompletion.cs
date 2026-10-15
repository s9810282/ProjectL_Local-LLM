namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner
{
    /// <summary>Job 실행 서비스가 현재 작업을 완료했는지 알립니다.</summary>
    public interface IJobCompletion
    {
        bool IsCompleted { get; }
    }
}
