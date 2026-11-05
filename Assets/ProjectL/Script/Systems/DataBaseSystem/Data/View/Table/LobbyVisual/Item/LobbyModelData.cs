using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item
{
    /// <summary>로비 정적 모델과 전시할 캐릭터 프리팹 참조를 보관하는 읽기 전용 스냅샷입니다.</summary>
    internal sealed class LobbyModelData : IData
    {
        internal GameObject RoomPrefab { get; }
        internal GameObject FloorPrefab { get; }
        internal GameObject CharacterPrefab { get; }

        internal LobbyModelData(GameObject roomPrefab, GameObject floorPrefab, GameObject characterPrefab)
        {
            RoomPrefab = roomPrefab;
            FloorPrefab = floorPrefab;
            CharacterPrefab = characterPrefab;
        }

        /// <summary>
        /// DB 생성 때 한 번 부릅니다. 방의 시설 셋까지 여기서 검사하고, View는 방·바닥 프리팹을 다시 순회하지 않습니다(TP-R13).
        /// 캐릭터 프리팹 확인은 View 생성 때도 한 번 더 부릅니다(R-b 결정, 값만 보는 가벼운 검사).
        /// </summary>
        internal void Validate()
        {
            ValidateCharacterPrefab(CharacterPrefab);
            LobbyVisualPrefabRules.ValidateRoom(RoomPrefab, nameof(RoomPrefab));
            LobbyVisualPrefabRules.Validate(FloorPrefab, nameof(FloorPrefab));
        }

        /// <summary>캐릭터는 애니메이션 프리팹을 허용하며 복제본의 루트 보정은 View가 적용합니다.</summary>
        private static void ValidateCharacterPrefab(GameObject prefab)
        {
            const string owner = "[LobbyModelData] " + nameof(CharacterPrefab);
            ViewPrefabRules.RequirePrefabAsset(prefab, owner);
            ViewPrefabRules.RequireActiveRoot(prefab, owner);
        }
    }
}
