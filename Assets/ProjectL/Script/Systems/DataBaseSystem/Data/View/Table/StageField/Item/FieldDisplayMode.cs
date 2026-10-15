namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item
{
    /// <summary>전투 필드 칸을 그리는 방식입니다.</summary>
    internal enum FieldDisplayMode
    {
        /// <summary>칸마다 셀 프리팹을 깔고, 강조를 셀 색으로 보여 줍니다.</summary>
        CellPrefab = 0,

        /// <summary>셀을 만들지 않고, 행동 중에만 격자 데칼과 칸 아웃라인을 그립니다.</summary>
        GridOutline = 1
    }
}
