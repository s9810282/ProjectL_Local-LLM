namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageViewProp.Item
{
    /// <summary>
    /// 전투 맵 배경의 구멍을 채우는 바닥 타일 종류입니다. 지금 값은 임시 두 종류이고,
    /// 종류별 외형(프리팹·윗면 높이)은 타일 세트(<see cref="CombatMapTileSetSO"/>)가 정합니다.
    /// 0은 쓰지 않아, 값을 채우지 않은 배치(0으로 채워진 새 배열)는 검사에서 거절됩니다.
    /// </summary>
    internal enum CombatMapTileKind
    {
        Tile1,
        Tile2,
        Tile3,
        Tile4,
        BrokenTile,
        Boundary,
        BoundaryConer,
        RandomTile,
    }
}
