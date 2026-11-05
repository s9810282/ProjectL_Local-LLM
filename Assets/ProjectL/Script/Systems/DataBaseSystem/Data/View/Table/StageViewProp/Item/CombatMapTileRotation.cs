namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageViewProp.Item
{
    /// <summary>바닥 타일의 회전 입력입니다. Random은 타일을 만들 때 허용 각 중 하나로 확정합니다.</summary>
    internal enum CombatMapTileRotation : byte
    {
        R0 = 0,
        R90 = 1,
        R180 = 2,
        R270 = 3,
        Random = 4,
    }
}
