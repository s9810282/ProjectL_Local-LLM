using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.LobbyInput
{
    /// <summary>로비 시설과 공통 입력을 허용합니다. 공통 액션 등록은 해당 소유자가 맡습니다.</summary>
    internal static class LobbyInputLayers
    {
        internal static readonly InputLayer Lobby = new(
            LobbyInputNames.Map,
            LobbyInputNames.CommonMap,
            LobbyInputNames.Map);

        internal static readonly InputLayer DeckPopup = new("LobbyDeckPopup", LobbyInputNames.CommonMap);
    }
}
