namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>한 계산 호출의 효과 시점이다. Buffer의 턴 상태나 연출 대기를 대신하지 않는다.</summary>
    public enum CombatResolutionWindow
    {
        Normal,
        BeforeControllerTurn
    }
}
