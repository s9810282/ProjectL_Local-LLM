namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Enum
{
    /// <summary>SO에서 선택하는 지형 종류 키입니다. 현재 전투 상태는 보관하지 않습니다.</summary>
    public enum TerrainContentKind
    {
        None = 0,
        HighGround = 1,
        Sandstorm = 2,
        Wall = 3,
        WeakGround = 4,
        Cliff = 5,
        Tornado = 6,
        PresenceModifier = 7
    }
}
