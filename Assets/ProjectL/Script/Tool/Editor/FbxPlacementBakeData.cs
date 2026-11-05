using System;
using System.Collections.Generic;
using System.Text;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectL.Tool.Editor
{
    /// <summary>베이크 대상, UV 검사, 복제 씬의 결과를 원본 맵 Renderer에 연결합니다.</summary>
    public static class FbxPlacementBakeData
    {
        public static bool IsTarget(MeshRenderer renderer) => renderer != null && renderer.enabled &&
            renderer.gameObject.activeInHierarchy && renderer.receiveGI == ReceiveGI.Lightmaps &&
            (GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) & StaticEditorFlags.ContributeGI) != 0;

        public static void SetTargets(GameObject root, bool enabled)
        {
            var targets = new List<GameObject>();
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                targets.Add(renderer.gameObject);
            FbxPlacementStaticSettings.SetFlag(targets, StaticEditorFlags.ContributeGI, enabled);
        }

        public static List<MeshRenderer> GetTargets(GameObject root)
        {
            var result = new List<MeshRenderer>();
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>())
                if (IsTarget(renderer)) result.Add(renderer);
            return result;
        }

        public static List<string> MissingUvModels(GameObject root)
        {
            var paths = new List<string>();
            foreach (MeshRenderer renderer in GetTargets(root))
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                Mesh mesh = filter != null ? filter.sharedMesh : null;
                if (mesh == null || mesh.HasVertexAttribute(VertexAttribute.TexCoord1)) continue;
                string path = AssetDatabase.GetAssetPath(mesh);
                if (!paths.Contains(path)) paths.Add(path);
            }
            return paths;
        }

        public static void GenerateMissingUvs(GameObject root)
        {
            List<string> paths = MissingUvModels(root);
            foreach (string path in paths)
                if (AssetImporter.GetAtPath(path) is not ModelImporter)
                    throw new InvalidOperationException("UV2가 없는 메시의 원본 모델을 찾을 수 없습니다: " + path);
            foreach (string path in paths)
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.generateSecondaryUV = true;
                importer.SaveAndReimport();
            }
        }

        public static void Validate(GameObject root)
        {
            if (!FbxPlacementWorkspace.IsWorkspaceRoot(root)) throw new ArgumentException("가상 씬의 맵이 필요합니다.", nameof(root));
            List<MeshRenderer> targets = GetTargets(root);
            if (targets.Count == 0) throw new InvalidOperationException("베이크 대상이 없습니다. '전체 메시 대상 설정'을 누르세요.");
            foreach (MeshRenderer renderer in targets)
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                    throw new InvalidOperationException("베이크 대상의 메시가 없습니다: " + renderer.name);
            }
            if (MissingUvModels(root).Count > 0)
                throw new InvalidOperationException("UV2가 없는 모델이 있습니다. '누락 UV2 자동 생성'을 누른 뒤 베이크하세요.");
            bool hasLight = false;
            foreach (Light light in root.GetComponentsInChildren<Light>())
                hasLight |= light.enabled && light.lightmapBakeType != LightmapBakeType.Realtime;
            if (!hasLight) throw new InvalidOperationException("Baked 또는 Mixed 라이트가 필요합니다. 라이트의 Mode를 변경하세요.");
        }

        public static string Fingerprint(GameObject root)
        {
            StringBuilder text = new StringBuilder();
            foreach (Transform target in root.GetComponentsInChildren<Transform>(true))
            {
                text.Append(EditorJsonUtility.ToJson(target));
                text.Append(target.gameObject.activeSelf).Append((int)GameObjectUtility.GetStaticEditorFlags(target.gameObject));
                text.Append(target.name);
                foreach (MeshRenderer renderer in target.GetComponents<MeshRenderer>())
                {
                    text.Append(renderer.enabled).Append(renderer.receiveGI).Append(renderer.scaleInLightmap);
                    text.Append(renderer.shadowCastingMode).Append(renderer.receiveShadows);
                    foreach (Material material in renderer.sharedMaterials) text.Append(material != null ? material.GetInstanceID() : 0);
                }
                MeshFilter filter = target.GetComponent<MeshFilter>();
                Mesh mesh = filter != null ? filter.sharedMesh : null;
                text.Append(mesh != null ? mesh.GetInstanceID() : 0);
                foreach (Light light in target.GetComponents<Light>())
                    text.Append(light.enabled).Append(light.type).Append(light.lightmapBakeType).Append(light.color)
                        .Append(light.intensity).Append(light.range).Append(light.spotAngle).Append(light.innerSpotAngle)
                        .Append(light.areaSize).Append(light.shadows).Append(light.shadowStrength).Append(light.cullingMask);
            }
            return Hash128.Compute(text.ToString()).ToString();
        }

        public sealed class Result
        {
            public LightmapsMode mode;
            public MapBakedLighting.Atlas[] atlases;
            public MapBakedLighting.Surface[] surfaces;
            public MapBakedLighting.BakedLight[] lights;

            public MapBakedLighting Apply(GameObject root)
            {
                MapBakedLighting binding = root.GetComponent<MapBakedLighting>();
                if (binding == null) binding = root.AddComponent<MapBakedLighting>();
                binding.SetData(mode, atlases, surfaces, lights);
                binding.Apply();
                EditorUtility.SetDirty(binding);
                return binding;
            }

            public MapBakedLightingDataSO CreateAsset(GameObject root, string path)
            {
                var data = ScriptableObject.CreateInstance<MapBakedLightingDataSO>();
                try
                {
                    data.Initialize(root.transform, mode, atlases, surfaces, lights);
                    AssetDatabase.CreateAsset(data, path);
                    return data;
                }
                catch { UnityEngine.Object.DestroyImmediate(data); throw; }
            }
        }

        public static Result Capture(GameObject source, GameObject bakedRoot, LightmapData[] maps, LightmapsMode mode)
        {
            if (maps == null || maps.Length == 0) throw new InvalidOperationException("Unity가 라이트맵을 생성하지 못했습니다. Console을 확인하세요.");
            var result = new Result { mode = mode };
            var atlases = new List<MapBakedLighting.Atlas>();
            var surfaces = new List<MapBakedLighting.Surface>();
            var indices = new Dictionary<int, int>();
            MeshRenderer[] originals = source.GetComponentsInChildren<MeshRenderer>(true);
            MeshRenderer[] baked = bakedRoot.GetComponentsInChildren<MeshRenderer>(true);
            if (originals.Length != baked.Length) throw new InvalidOperationException("베이크 중 맵의 메시 계층이 변경되었습니다.");
            for (int i = 0; i < baked.Length; i++)
            {
                if (!IsTarget(baked[i])) continue;
                int index = baked[i].lightmapIndex;
                if (index < 0 || index >= maps.Length || maps[index]?.lightmapColor == null)
                    throw new InvalidOperationException("라이트맵을 받지 못한 오브젝트: " + baked[i].name);
                if (!indices.TryGetValue(index, out int localIndex))
                {
                    localIndex = atlases.Count;
                    indices.Add(index, localIndex);
                    atlases.Add(new MapBakedLighting.Atlas
                        { color = maps[index].lightmapColor, direction = maps[index].lightmapDir, shadowMask = maps[index].shadowMask });
                }
                surfaces.Add(new MapBakedLighting.Surface
                    { renderer = originals[i], atlasIndex = localIndex, scaleOffset = baked[i].lightmapScaleOffset });
            }
            if (surfaces.Count == 0) throw new InvalidOperationException("베이크된 메시가 없습니다.");
            Light[] originalLights = source.GetComponentsInChildren<Light>(true);
            Light[] bakedLights = bakedRoot.GetComponentsInChildren<Light>(true);
            if (originalLights.Length != bakedLights.Length) throw new InvalidOperationException("베이크 중 라이트 계층이 변경되었습니다.");
            var lights = new MapBakedLighting.BakedLight[originalLights.Length];
            for (int i = 0; i < lights.Length; i++)
            {
                LightBakingOutput output = bakedLights[i].bakingOutput;
                lights[i] = new MapBakedLighting.BakedLight
                {
                    light = originalLights[i], isBaked = output.isBaked, bakeType = output.lightmapBakeType,
                    mixedMode = output.mixedLightingMode, maskChannel = output.occlusionMaskChannel,
                    probeIndex = output.probeOcclusionLightIndex
                };
            }
            result.atlases = atlases.ToArray();
            result.surfaces = surfaces.ToArray();
            result.lights = lights;
            return result;
        }
    }
}
