namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister
{
    public enum JobRootType
    {
        Lobby = 0,
        Stage = 1,
        Artifact = 2,
        Combat = 3
    }

    public enum JobType
    {
        None = 0,
        Start = 1,
        NewRun = 2,
        ProcessCommand = 3,
        ProcessPreview = 4,
        AdvanceCombat = 5,
        /// <summary>Plan이 지금 사실을 읽어 다음 작업을 정한다. 할 일이 없으면 Job을 만들지 않는다.</summary>
        Next = 6,
        /// <summary>구간을 나간 뒤의 사후 확인이다.</summary>
        ExitCheck = 7,
    }
}
