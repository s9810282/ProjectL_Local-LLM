namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    public enum PiercingShotStopReason
    {
        TargetBudget = 1,
        Wall = 2,
        Structure = 3,
        // 4는 옛 Range(사거리 끝)다. 관통 진행은 사거리로 끝나지 않으므로 지웠고 번호는 다시 쓰지 않는다.
        BoardEdge = 5,
    }
}
