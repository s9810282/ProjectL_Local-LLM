namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Terrain.Item
{
    /// <summary>
    /// 지형 외형의 표시 방식을 구분합니다(타일 교체 설계 TR1). 표면 높이(<see cref="TerrainVisualData.SurfaceHeight"/>)의 뜻이 방식마다 다릅니다.
    /// </summary>
    public enum TerrainVisualMode
    {
        /// <summary>
        /// 바닥 타일을 그대로 두고 그 위에 프리팹을 놓습니다(효과·벽·고지대 블록). 프리팹이 필요하고,
        /// 표면 높이는 바탕 위로 올리는 높이(0 이상)입니다.
        /// </summary>
        OnTile = 0,

        /// <summary>
        /// 그 칸의 바닥 타일을 지형 외형으로 바꿉니다(약한 지반·낭떠러지). 프리팹이 없으면 타일만 비우고,
        /// 표면 높이는 그 칸의 새 바탕 높이(바뀐 타일의 윗면, 필드 바닥면 기준, 음수 가능)입니다.
        /// </summary>
        ReplaceTile = 1
    }
}
