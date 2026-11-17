namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 출발 처리가 입력을 붙잡고 있는지의 좁은 읽기 계약입니다. 원본 판정은 <see cref="LobbyDepartureFlow"/> 하나이고,
    /// 메인 선택이 잠김을 답할 때 읽습니다(로비 메인 선택 설계 LM2).
    /// </summary>
    internal interface ILobbyDepartureHold
    {
        /// <summary>처리 중인 요청이 있거나, 이 연결의 요청이 거절·취소로 끝나지 않았으면 참입니다.</summary>
        bool IsHolding { get; }
    }
}
