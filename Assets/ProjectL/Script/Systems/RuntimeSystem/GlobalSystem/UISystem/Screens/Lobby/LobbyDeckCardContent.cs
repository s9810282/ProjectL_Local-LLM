using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Lobby
{
    /// <summary>로비 덱 카드에 필요한 표현 값입니다.</summary>
    public sealed class LobbyDeckCardContent
    {
        public LobbyDeckCardContent(EntityKey key, string displayName, Sprite icon)
        {
            Key = key;
            DisplayName = displayName ?? string.Empty;
            Icon = icon;
        }

        public EntityKey Key { get; }
        public string DisplayName { get; }
        public Sprite Icon { get; }
    }
}
