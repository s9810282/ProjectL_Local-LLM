namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item
{
    /// <summary>GridOutline 모드에서 칸 테두리와 칸 채우기 판을 묶는 방식입니다(겹친 강조 2단계 설계 5판 RC14).</summary>
    internal enum FieldCellOutlineStyle
    {
        /// <summary>맞닿은 칸을 한 덩어리로 묶어 바깥 둘레만 그립니다. 겹친 칸도 같은 층끼리 덩어리로 이어집니다.</summary>
        Merged = 0,

        /// <summary>
        /// 칸마다 네 변을 모두 그립니다. 칸 크기(cellOutlineScale)가 1이고 모서리가 직각이면 맞닿은 변은 한 번만 그리고,
        /// 아니면 칸마다 따로 그립니다. 겹친 칸의 판·테두리도 칸마다 따로입니다.
        /// </summary>
        EachCell = 1
    }
}
