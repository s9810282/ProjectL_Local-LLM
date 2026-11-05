using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>이 연결에서 생성한 방·바닥의 생성·정리만 담당합니다.</summary>
    internal sealed class LobbyVisualSet : IDisposable
    {
        private List<GameObject> _instances = new(2);

        internal IReadOnlyList<LobbyFacilityView> Facilities { get; private set; }

        private LobbyVisualSet() { }

        internal static LobbyVisualSet Create(Transform worldRoot, LobbyModelData data)
        {
            if (worldRoot == null) throw new ArgumentNullException(nameof(worldRoot));
            if (data == null) throw new ArgumentNullException(nameof(data));
            // 모델 프리팹(방의 시설 셋 포함)은 DB 생성 때 검사가 끝났습니다(TP-R13).
            var owner = new LobbyVisualSet();
            try
            {
                GameObject room = owner.CreateOne(data.RoomPrefab, worldRoot);
                owner.Facilities = LobbyRoomBinding.Create(room);
                owner.CreateOne(data.FloorPrefab, worldRoot);
                return owner;
            }
            catch
            {
                owner.Dispose();
                throw;
            }
        }

        // Instantiate(prefab, parent, false)가 프리팹의 로컬 자세·배율을 그대로 둡니다. 시험용 생성 입구는 걷었습니다(로비 리팩토링 2차 설계 N-71).
        private GameObject CreateOne(GameObject prefab, Transform parent)
        {
            GameObject instance = UnityEngine.Object.Instantiate(prefab, parent, false);
            _instances.Add(instance);
            return instance;
        }

        public void Dispose()
        {
            List<GameObject> instances = _instances;
            _instances = null;
            Facilities = Array.Empty<LobbyFacilityView>();
            if (instances == null) return;
            for (int i = instances.Count - 1; i >= 0; i--)
            {
                GameObject instance = instances[i];
                if (instance == null) continue;
                instance.SetActive(false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(instance);
                else UnityEngine.Object.DestroyImmediate(instance);
            }
        }

    }
}
