namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>맵 입력의 배경 드래그와 휠 이동을 카메라 표시 구현에 전달합니다.</summary>
    internal interface IStageMapCameraDrag
    {
        /// <summary>배경을 누른 화면 좌표에서 드래그를 시작합니다.</summary>
        void BeginDrag(float screenX, float screenY);

        /// <summary>드래그 중인 화면 좌표를 전달합니다.</summary>
        void MoveDrag(float screenX, float screenY);

        /// <summary>버튼 해제 또는 입력 취소로 드래그를 끝냅니다.</summary>
        void EndDrag();

        /// <summary>휠 방향으로 카메라를 이동합니다. 양수는 화면 위, 음수는 아래이며 기존 맵 범위를 지킵니다.</summary>
        void Scroll(float scrollY, float screenX, float screenY);
    }
}
