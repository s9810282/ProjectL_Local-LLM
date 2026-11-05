using ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>정상 도착한 시설의 실행 요청을 받는 View 내부 경계입니다.</summary>
    internal interface ILobbyFacilityActions
    {
        /// <summary>지금 실행할 수 있으면 실행하고 true를 돌려줍니다. 판정과 실행이 한 입구라 판정을 두 번 하지 않습니다(로비 리팩토링 2차 설계 N-24).</summary>
        bool TryExecute(LobbyFacilityKind kind);
    }
}
