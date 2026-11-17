using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 다음 Run 메인 선택의 읽기 계약입니다. 출발 흐름이 제출 순간에 이 값을 요청에 담습니다(로비 메인 선택 설계 LM1·LM4).
    /// 선택 원본은 <see cref="LobbyMainSelection"/> 하나이고, 읽는 쪽은 값을 따로 보관하지 않습니다.
    /// </summary>
    internal interface ILobbyMainSelectionReader
    {
        /// <summary>지금 고른 다음 Run 메인 키입니다. 처음 값은 로드아웃 기본 메인입니다.</summary>
        EntityKey Selected { get; }
    }
}
