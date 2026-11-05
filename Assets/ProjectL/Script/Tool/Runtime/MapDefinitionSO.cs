using System;
using UnityEngine;

namespace ProjectL.Tool.Runtime
{
    /// <summary>맵 배치 프리팹과 그 배치에서 만든 베이크 정보의 조합입니다.</summary>
    [CreateAssetMenu(fileName = "Map", menuName = "ProjectL/Map/맵 구성")]
    public sealed class MapDefinitionSO : ScriptableObject
    {
        [Header("맵 구성")]
        [SerializeField] private GameObject prefab;
        [SerializeField] private MapBakedLightingDataSO bakedLighting;
        public GameObject Prefab => prefab;
        public MapBakedLightingDataSO BakedLighting => bakedLighting;

        public void Initialize(GameObject mapPrefab, MapBakedLightingDataSO lighting)
        {
            Validate(mapPrefab, lighting);
            prefab = mapPrefab;
            bakedLighting = lighting;
        }

        public void Validate() => Validate(prefab, bakedLighting);

        private static void Validate(GameObject mapPrefab, MapBakedLightingDataSO lighting)
        {
            if (mapPrefab == null) throw new InvalidOperationException("맵 구성 SO에 배치 프리팹이 없습니다.");
            if (lighting != null) lighting.Resolve(mapPrefab.transform);
        }
    }
}
