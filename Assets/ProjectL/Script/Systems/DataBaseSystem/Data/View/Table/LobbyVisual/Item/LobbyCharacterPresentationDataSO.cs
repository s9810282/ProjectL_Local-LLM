using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item
{
    /// <summary>로비 전시 캐릭터의 로컬 표시 보정을 입력합니다.</summary>
    [CreateAssetMenu(fileName = "NewLobbyCharacterPresentationData", menuName = "ProjectL/Database/View/LobbyCharacterPresentationData")]
    internal sealed class LobbyCharacterPresentationDataSO : DatabaseItem
    {
        [SerializeField] private Vector3 characterOffset = new(0f, -0.08218316f, 0f);
        [SerializeField] private float characterYaw;
        [SerializeField] private float characterScale = 2.146858f;

        public override IData GetData() => new LobbyCharacterPresentationData(characterOffset, characterYaw, characterScale);
    }
}
