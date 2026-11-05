using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Tool.Runtime
{
    /// <summary>조회한 맵 구성으로 인스턴스를 생성하고 베이크 정보를 연결합니다.</summary>
    public static class MapAssembler
    {
        public static GameObject Instantiate(MapDefinitionSO map, Transform parent = null)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            map.Validate();
            if (map.BakedLighting != null) MapBakedLighting.ValidateSceneMode(map.BakedLighting.Mode);
            GameObject instance = Object.Instantiate(map.Prefab, parent, false);
            try
            {
                Apply(instance, map.BakedLighting);
                return instance;
            }
            catch
            {
                if (Application.isPlaying) Object.Destroy(instance);
                else Object.DestroyImmediate(instance);
                throw;
            }
        }

        public static void Apply(GameObject instance, MapBakedLightingDataSO lighting)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (lighting == null) return;
            // 경로를 먼저 검증해 잘못된 조합이 오브젝트 일부에만 적용되는 것을 막습니다.
            lighting.Resolve(instance.transform);
            MapBakedLighting binding = instance.GetComponent<MapBakedLighting>();
            if (binding == null) binding = instance.AddComponent<MapBakedLighting>();
            binding.Bind(lighting);
            binding.Apply();
        }
    }
}
