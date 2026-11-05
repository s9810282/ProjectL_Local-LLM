using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectL.Tool.Runtime
{
    /// <summary>맵 프리팹에 저장된 라이트맵을 씬의 배열에 등록하고 각 Renderer에 연결합니다.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class MapBakedLighting : MonoBehaviour
    {
        [Serializable]
        public sealed class Atlas
        {
            public Texture2D color;
            public Texture2D direction;
            public Texture2D shadowMask;

            public LightmapData ToLightmap() => new LightmapData
                { lightmapColor = color, lightmapDir = direction, shadowMask = shadowMask };

            public bool Matches(LightmapData data) => data != null && data.lightmapColor == color &&
                data.lightmapDir == direction && data.shadowMask == shadowMask;
        }

        [Serializable]
        public sealed class Surface
        {
            public Renderer renderer;
            public int atlasIndex;
            public Vector4 scaleOffset;
        }

        [Serializable]
        public sealed class BakedLight
        {
            public Light light;
            public bool isBaked;
            public LightmapBakeType bakeType;
            public MixedLightingMode mixedMode;
            public int maskChannel;
            public int probeIndex;

            public void Apply()
            {
                if (light == null) return;
                light.bakingOutput = new LightBakingOutput
                {
                    isBaked = isBaked, lightmapBakeType = bakeType, mixedLightingMode = mixedMode,
                    occlusionMaskChannel = maskChannel, probeOcclusionLightIndex = probeIndex
                };
            }
        }

        [SerializeField] private MapBakedLightingDataSO bakedData;
        // 기존 프리팹을 열어 새 SO 형식으로 저장할 수 있도록 이전 직렬화 필드는 유지합니다.
        [SerializeField, HideInInspector] private LightmapsMode mode = LightmapsMode.CombinedDirectional;
        [SerializeField, HideInInspector] private Atlas[] atlases = Array.Empty<Atlas>();
        [SerializeField, HideInInspector] private Surface[] surfaces = Array.Empty<Surface>();
        [SerializeField, HideInInspector] private BakedLight[] lights = Array.Empty<BakedLight>();
        private int[] _registeredSlots;
        private Surface[] _appliedSurfaces;
        private static readonly List<MapBakedLighting> Active = new List<MapBakedLighting>();
        private static readonly Dictionary<int, Atlas> OwnedSlots = new Dictionary<int, Atlas>();
        private static LightmapsMode _previousMode;

        public int AtlasCount => bakedData != null ? bakedData.AtlasCount : atlases.Length;
        public int SurfaceCount => bakedData != null ? bakedData.SurfaceCount : surfaces.Length;
        public LightmapsMode Mode => bakedData != null ? bakedData.Mode : mode;
        public MapBakedLightingDataSO Data => bakedData;

        private void OnEnable()
        {
            if (!gameObject.scene.IsValid()) return;
#if UNITY_EDITOR
            // 가상 씬은 작업실이 복구 시점을 정합니다. 프리팹 에셋/프리팹 모드도 등록하지 않습니다.
            if (UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(gameObject.scene) ||
                UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject) != null) return;
#endif
            Apply();
        }

        private void OnDisable() => Release();

        public void SetData(LightmapsMode lightmapsMode, Atlas[] maps, Surface[] renderers, BakedLight[] bakedLights)
        {
            Validate(maps, renderers, bakedLights, lightmapsMode);
            Release();
            bakedData = null;
            mode = lightmapsMode;
            atlases = maps;
            surfaces = renderers;
            lights = bakedLights;
        }

        public void Bind(MapBakedLightingDataSO data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            data.Resolve(transform);
            ValidateSceneMode(data.Mode);
            Release();
            bakedData = data;
            atlases = Array.Empty<Atlas>();
            surfaces = Array.Empty<Surface>();
            lights = Array.Empty<BakedLight>();
        }

        public MapBakedLightingDataSO ExportData()
        {
            if (bakedData != null) return bakedData;
            var data = ScriptableObject.CreateInstance<MapBakedLightingDataSO>();
            try { data.Initialize(transform, mode, atlases, surfaces, lights); return data; }
            catch { DestroyImmediate(data); throw; }
        }

        /// <summary>기존 씬의 인덱스는 유지하고 같은 텍스처는 공유합니다.</summary>
        public void Apply()
        {
            if (AtlasCount == 0) return;
            MapBakedLightingDataSO.Resolved data = bakedData != null ? bakedData.Resolve(transform) :
                new MapBakedLightingDataSO.Resolved { mode = mode, atlases = atlases, surfaces = surfaces, lights = lights };
            Validate(data.atlases, data.surfaces, data.lights, data.mode);
            ValidateSceneMode(data.mode);
            LightmapData[] current = LightmapSettings.lightmaps ?? Array.Empty<LightmapData>();
            if (Active.Count == 0) _previousMode = LightmapSettings.lightmapsMode;
            List<LightmapData> table = new List<LightmapData>(current);
            int[] slots = new int[data.atlases.Length];
            for (int i = 0; i < data.atlases.Length; i++)
            {
                int slot = table.FindIndex(data.atlases[i].Matches);
                if (slot < 0)
                {
                    slot = table.FindIndex(data => data == null || data.lightmapColor == null);
                    if (slot < 0) { slot = table.Count; table.Add(data.atlases[i].ToLightmap()); }
                    else table[slot] = data.atlases[i].ToLightmap();
                    OwnedSlots[slot] = data.atlases[i];
                }
                slots[i] = slot;
            }
            int[] previous = _registeredSlots;
            _registeredSlots = slots;
            _appliedSurfaces = data.surfaces;
            if (!Active.Contains(this)) Active.Add(this);
            LightmapSettings.lightmapsMode = data.mode;
            LightmapSettings.lightmaps = table.ToArray();
            foreach (Surface surface in data.surfaces)
            {
                if (surface.renderer == null) continue;
                surface.renderer.lightmapIndex = slots[surface.atlasIndex];
                surface.renderer.lightmapScaleOffset = surface.scaleOffset;
            }
            foreach (BakedLight light in data.lights) light.Apply();
            if (previous != null) ReleaseUnusedSlots();
        }

        public static void ValidateSceneMode(LightmapsMode requestedMode)
        {
            foreach (LightmapData map in LightmapSettings.lightmaps ?? Array.Empty<LightmapData>())
                if (map != null && map.lightmapColor != null && LightmapSettings.lightmapsMode != requestedMode)
                    throw new InvalidOperationException("씬과 맵 프리팹의 Directional Mode가 다릅니다. 같은 모드로 베이크하세요.");
        }

        public void Release()
        {
            if (_registeredSlots == null) return;
            foreach (Surface surface in _appliedSurfaces)
                if (surface.renderer != null && surface.renderer.lightmapIndex == _registeredSlots[surface.atlasIndex])
                    surface.renderer.lightmapIndex = -1;
            _registeredSlots = null;
            _appliedSurfaces = null;
            Active.Remove(this);
            ReleaseUnusedSlots();
        }

        private static void ReleaseUnusedSlots()
        {
            HashSet<int> used = new HashSet<int>();
            Active.RemoveAll(item => item == null);
            foreach (MapBakedLighting item in Active)
                if (item._registeredSlots != null) foreach (int slot in item._registeredSlots) used.Add(slot);
            List<LightmapData> table = new List<LightmapData>(LightmapSettings.lightmaps ?? Array.Empty<LightmapData>());
            HashSet<int> cleared = new HashSet<int>();
            foreach (var entry in OwnedSlots)
            {
                if (used.Contains(entry.Key)) continue;
                if (entry.Key < table.Count && entry.Value.Matches(table[entry.Key]))
                {
                    table[entry.Key] = new LightmapData();
                    cleared.Add(entry.Key);
                }
            }
            while (table.Count > 0 && cleared.Contains(table.Count - 1)) table.RemoveAt(table.Count - 1);
            foreach (int slot in cleared) OwnedSlots.Remove(slot);
            LightmapSettings.lightmaps = table.ToArray();
            if (Active.Count == 0 && table.Count == 0) LightmapSettings.lightmapsMode = _previousMode;
        }

        internal static void Validate(Atlas[] maps, Surface[] renderers, BakedLight[] bakedLights, LightmapsMode lightmapsMode)
        {
            if (maps == null || renderers == null || bakedLights == null) throw new ArgumentNullException("라이트맵 연결 데이터");
            foreach (Atlas map in maps)
                if (map == null || map.color == null || (lightmapsMode == LightmapsMode.CombinedDirectional && map.direction == null))
                    throw new InvalidOperationException("라이트맵 텍스처가 누락되었습니다. 맵을 다시 베이크하세요.");
            foreach (Surface surface in renderers)
                if (surface == null || surface.atlasIndex < 0 || surface.atlasIndex >= maps.Length)
                    throw new InvalidOperationException("Renderer의 라이트맵 인덱스가 올바르지 않습니다.");
            foreach (BakedLight light in bakedLights)
                if (light == null) throw new InvalidOperationException("베이크한 Light 연결이 누락되었습니다.");
        }
    }
}
