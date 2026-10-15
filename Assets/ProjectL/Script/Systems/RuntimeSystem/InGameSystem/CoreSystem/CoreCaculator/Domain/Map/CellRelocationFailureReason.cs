namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Map
{
    public enum CellRelocationFailureReason
    {
        None = 0,
        StateInconsistent, // 실제 map 점유가 일치하지 않음
        OutOfBounds, // 범위 바깥
        DestinationOccupied // 다른 유닛이 점유하고 있음
    }
}