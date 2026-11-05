using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Tool.Runtime
{
    /// <summary>인스턴스 참조 없이 라이트맵 텍스처와 프리팹 내부 연결 위치를 보관합니다.</summary>
    [CreateAssetMenu(fileName = "MapLighting", menuName = "ProjectL/Map/베이크 정보")]
    public sealed class MapBakedLightingDataSO : ScriptableObject
    {
        [Serializable]
        private sealed class Node
        {
            public int[] indices;
            public string nodeName;
            public int componentIndex;

            public static Node Capture<T>(Transform root, T component) where T : Component
            {
                if (component == null) throw new InvalidOperationException("베이크 연결 대상이 누락되었습니다.");
                var indices = new List<int>();
                Transform target = component.transform;
                while (target != root)
                {
                    if (target.parent == null) throw new InvalidOperationException("베이크 연결 대상은 이 맵의 자식이어야 합니다.");
                    indices.Add(target.GetSiblingIndex());
                    target = target.parent;
                }
                indices.Reverse();
                int index = Array.IndexOf(component.GetComponents<T>(), component);
                return new Node { indices = indices.ToArray(), nodeName = component.name, componentIndex = index };
            }

            public T Resolve<T>(Transform root) where T : Component
            {
                Transform target = root;
                foreach (int index in indices)
                {
                    if (index < 0 || index >= target.childCount) throw Changed();
                    target = target.GetChild(index);
                }
                // Instantiate가 루트 이름에 붙이는 (Clone)은 연결 위치에 영향을 주지 않습니다.
                if (indices.Length > 0 && target.name != nodeName) throw Changed();
                T[] components = target.GetComponents<T>();
                if (componentIndex < 0 || componentIndex >= components.Length) throw Changed();
                return components[componentIndex];
            }

            private InvalidOperationException Changed() =>
                new InvalidOperationException("맵의 베이크 연결 대상이 바뀌었습니다. 다시 베이크하세요: " + nodeName);
        }

        [Serializable]
        private sealed class SurfaceRecord
        {
            public Node node;
            public Mesh mesh;
            public int atlasIndex;
            public Vector4 scaleOffset;
        }

        [Serializable]
        private sealed class LightRecord
        {
            public Node node;
            public bool isBaked;
            public LightmapBakeType bakeType;
            public MixedLightingMode mixedMode;
            public int maskChannel;
            public int probeIndex;
        }

        [SerializeField] private LightmapsMode mode = LightmapsMode.CombinedDirectional;
        [SerializeField] private MapBakedLighting.Atlas[] atlases = Array.Empty<MapBakedLighting.Atlas>();
        [SerializeField] private SurfaceRecord[] surfaces = Array.Empty<SurfaceRecord>();
        [SerializeField] private LightRecord[] lights = Array.Empty<LightRecord>();
        public int AtlasCount => atlases.Length;
        public int SurfaceCount => surfaces.Length;
        public LightmapsMode Mode => mode;

        public sealed class Resolved
        {
            public LightmapsMode mode;
            public MapBakedLighting.Atlas[] atlases;
            public MapBakedLighting.Surface[] surfaces;
            public MapBakedLighting.BakedLight[] lights;
        }

        public void Initialize(Transform root, LightmapsMode lightmapsMode, MapBakedLighting.Atlas[] maps,
            MapBakedLighting.Surface[] rendererBindings, MapBakedLighting.BakedLight[] lightBindings)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            MapBakedLighting.Validate(maps, rendererBindings, lightBindings, lightmapsMode);
            var newSurfaces = new SurfaceRecord[rendererBindings.Length];
            for (int i = 0; i < newSurfaces.Length; i++)
            {
                MapBakedLighting.Surface binding = rendererBindings[i];
                MeshFilter filter = binding.renderer != null ? binding.renderer.GetComponent<MeshFilter>() : null;
                newSurfaces[i] = new SurfaceRecord
                {
                    node = Node.Capture(root, binding.renderer), mesh = filter != null ? filter.sharedMesh : null,
                    atlasIndex = binding.atlasIndex, scaleOffset = binding.scaleOffset
                };
            }
            var newLights = new LightRecord[lightBindings.Length];
            for (int i = 0; i < newLights.Length; i++)
            {
                MapBakedLighting.BakedLight binding = lightBindings[i];
                newLights[i] = new LightRecord
                {
                    node = Node.Capture(root, binding.light), isBaked = binding.isBaked, bakeType = binding.bakeType,
                    mixedMode = binding.mixedMode, maskChannel = binding.maskChannel, probeIndex = binding.probeIndex
                };
            }
            mode = lightmapsMode;
            atlases = (MapBakedLighting.Atlas[])maps.Clone();
            surfaces = newSurfaces;
            lights = newLights;
        }

        public Resolved Resolve(Transform root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            var resolvedSurfaces = new MapBakedLighting.Surface[surfaces.Length];
            for (int i = 0; i < surfaces.Length; i++)
            {
                SurfaceRecord record = surfaces[i];
                Renderer renderer = record.node.Resolve<Renderer>(root);
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (record.mesh != null && (filter == null || filter.sharedMesh != record.mesh))
                    throw new InvalidOperationException("맵의 메시가 베이크 결과와 다릅니다: " + renderer.name);
                resolvedSurfaces[i] = new MapBakedLighting.Surface
                    { renderer = renderer, atlasIndex = record.atlasIndex, scaleOffset = record.scaleOffset };
            }
            var resolvedLights = new MapBakedLighting.BakedLight[lights.Length];
            for (int i = 0; i < lights.Length; i++)
            {
                LightRecord record = lights[i];
                resolvedLights[i] = new MapBakedLighting.BakedLight
                {
                    light = record.node.Resolve<Light>(root), isBaked = record.isBaked, bakeType = record.bakeType,
                    mixedMode = record.mixedMode, maskChannel = record.maskChannel, probeIndex = record.probeIndex
                };
            }
            MapBakedLighting.Validate(atlases, resolvedSurfaces, resolvedLights, mode);
            return new Resolved { mode = mode, atlases = atlases, surfaces = resolvedSurfaces, lights = resolvedLights };
        }
    }
}
