using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item
{
    /// <summary>로비 전시 캐릭터의 로컬 표시 보정을 보관합니다.</summary>
    internal sealed class LobbyCharacterPresentationData : IData
    {
        internal Vector3 CharacterOffset { get; }
        internal float CharacterYaw { get; }
        internal float CharacterScale { get; }

        internal LobbyCharacterPresentationData(Vector3 characterOffset, float characterYaw, float characterScale)
        {
            CharacterOffset = characterOffset;
            CharacterYaw = characterYaw;
            CharacterScale = characterScale;
        }

        internal void Validate() =>
            ViewPrefabRules.RequirePresentation(CharacterOffset, CharacterYaw, CharacterScale,
                "[LobbyCharacterPresentationData] 캐릭터");
    }
}
