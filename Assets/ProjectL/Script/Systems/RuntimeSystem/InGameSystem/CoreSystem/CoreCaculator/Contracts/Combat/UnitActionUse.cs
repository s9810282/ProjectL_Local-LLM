namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>유닛의 자기 진영 턴 내 일반 행동 사용 상태다. 생존·위치·행동 제한은 별도로 검사한다.</summary>
    public enum UnitActionUse
    {
        Deferred = 0,
        Available = 1,
        Moved = 2,
        Attacked = 3
    }
}
