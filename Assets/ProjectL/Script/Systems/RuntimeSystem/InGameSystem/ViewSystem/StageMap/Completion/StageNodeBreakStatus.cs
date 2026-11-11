namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Completion
{
    /// <summary>현재 효과의 자연 종료와 취소된 세대를 구분합니다.</summary>
    internal enum StageNodeBreakStatus
    {
        Playing,
        Completed,
        Cancelled
    }
}
