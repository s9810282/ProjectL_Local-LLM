using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>로비에서 선택한 다음 Run 메인을 판 위 모델에 반영하는 좁은 표시 계약입니다.</summary>
    internal interface ILobbyMainModelDisplay
    {
        void ShowMain(EntityKey key);
    }
}
