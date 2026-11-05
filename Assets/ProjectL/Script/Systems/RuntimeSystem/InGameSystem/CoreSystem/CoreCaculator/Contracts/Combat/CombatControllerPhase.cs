namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>현재 행동 진영과 해당 진영의 턴 시작 처리 단계를 나타낸다.</summary>
    public enum CombatControllerPhase
    {
        None = 0,
        StartingPlayer = 1,
        Player = 2,
        StartingEnemy = 3,
        Enemy = 4
    }
}
