namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageOfferText.Item
{
    /// <summary>글 묶음을 쓰는 곳입니다. 스테이지는 키 목록으로 찾고 증강은 하나뿐입니다.</summary>
    internal enum StageOfferTextTarget
    {
        /// <summary>팝업 화면이 있는 스테이지(상점·훈련소·회복)의 화면 글입니다. 키 목록으로 찾습니다.</summary>
        Stage = 0,
        TraitGrowth = 1,
        /// <summary>
        /// 화면 없이 이름·설명만 쓰는 줄입니다(건물 호버, 스테이지 맵 설계 §5.7 3판).
        /// 키 목록이 있으면 그 키 전용, 비어 있으면 그 줄의 종류·역할 공통입니다.
        /// </summary>
        Info = 2
    }
}
