using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 생성된 방 복제본에서 시설을 모읍니다. 시설 셋과 클릭 영역 구성은 DB 생성 때 원본 프리팹에서 검사했고
    /// (<see cref="LobbyVisualPrefabRules.ValidateRoom"/>), 복제본은 같은 구성이라 다시 검사하지 않습니다(TP-R13).
    /// </summary>
    internal static class LobbyRoomBinding
    {
        internal static IReadOnlyList<LobbyFacilityView> Create(GameObject instance)
        {
            if (instance == null || !instance.scene.IsValid())
                throw new InvalidOperationException("[Lobby] 시설 연결에는 생성된 방 인스턴스가 필요합니다.");
            return Array.AsReadOnly(instance.GetComponentsInChildren<LobbyFacilityView>(true));
        }
    }
}
