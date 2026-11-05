namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras
{
    /// <summary>
    /// 맵 카메라 줌 대여입니다(카메라 연출 조각 설계 K1·K3). 장면에 하나이고, 조각 실행이 빌리고 되돌립니다.
    /// 줌은 맵 프랍 안의 버추얼 카메라 전부에 같은 배율로 겁니다.
    /// </summary>
    /// <remarks>
    /// 구현은 대여 수명을 확인하고, 다음 대여를 이전 <see cref="RestoreZoom"/>이 수정하지 못하게 합니다.
    /// 맵이 빠지면 대여가 취소되고, 시작 때 받은 취소 콜백을 부릅니다.
    /// </remarks>
    internal interface ICombatCameraZoom
    {
        /// <summary>이 대여가 아직 창구의 현재 대여인지입니다. 반환·취소 뒤에는 false입니다.</summary>
        bool IsAvailable { get; }

        /// <summary>
        /// 보기 크기(플레이어 줌이 정한 렌즈값, 카메라 조작 설계 CC4)에 곱하는 배율 <paramref name="ratio"/>로 옮깁니다(작을수록 확대). 지금 배율에서 시작해 <paramref name="seconds"/> 동안
        /// 카메라 효과가 보간합니다. 0이면 바로 옮깁니다. 같은 대여의 앞 줌이 진행 중이면 그것을 덮어씁니다(효과 규칙).
        /// </summary>
        void ZoomTo(float ratio, float seconds);

        /// <summary>보기 크기로 바로 돌리고 대여를 끝냅니다. 이미 끝났거나 다른 대여로 바뀌었으면 아무 일도 하지 않습니다.</summary>
        void RestoreZoom();
    }
}
