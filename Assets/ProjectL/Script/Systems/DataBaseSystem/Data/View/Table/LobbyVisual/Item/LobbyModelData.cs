using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item
{
    /// <summary>로비 방·바닥 프리팹 참조를 보관하는 읽기 전용 스냅샷입니다. 캐릭터는 유닛 외형 표를 사용합니다.</summary>
    internal sealed class LobbyModelData : IData
    {
        internal GameObject RoomPrefab { get; }
        internal GameObject FloorPrefab { get; }

        internal LobbyModelData(GameObject roomPrefab, GameObject floorPrefab)
        {
            RoomPrefab = roomPrefab;
            FloorPrefab = floorPrefab;
        }

        /// <summary>
        /// DB 생성 때 한 번 부릅니다. 방의 시설 셋까지 여기서 검사하고, View는 방·바닥 프리팹을 다시 순회하지 않습니다(TP-R13).
        /// </summary>
        internal void Validate()
        {
            LobbyVisualPrefabRules.ValidateRoom(RoomPrefab, nameof(RoomPrefab));
            LobbyVisualPrefabRules.Validate(FloorPrefab, nameof(FloorPrefab));
        }
    }
}
