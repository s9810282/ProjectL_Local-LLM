using System;
using System.IO;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Tool.Editor
{
    /// <summary>배치 프리팹과 맵 구성 SO를 저장하고 베이크 정보를 외부 SO로 보관합니다.</summary>
    public static class FbxPlacementMapAssets
    {
        public static string DefinitionPath(string prefabPath) =>
            Path.ChangeExtension(prefabPath, null).Replace('\\', '/') + "_Map.asset";

        public static MapDefinitionSO Save(GameObject root, string prefabPath)
        {
            if (!FbxPlacementWorkspace.IsWorkspaceRoot(root)) throw new ArgumentException("가상 씬의 맵이 필요합니다.", nameof(root));
            string mapPath = DefinitionPath(prefabPath);
            MapDefinitionSO map = AssetDatabase.LoadAssetAtPath<MapDefinitionSO>(mapPath);
            if (map == null && AssetDatabase.LoadMainAssetAtPath(mapPath) != null)
                throw new InvalidOperationException("맵 SO 경로에 다른 종류의 에셋이 있습니다: " + mapPath);
            MapBakedLighting binding = root.GetComponent<MapBakedLighting>();
            MapBakedLightingDataSO lighting = null;
            try
            {
                if (binding != null && binding.AtlasCount > 0)
                {
                    lighting = binding.ExportData();
                    lighting.Resolve(root.transform);
                }
                // 부모를 지정해 처음부터 가상 씬에서 복제합니다. 활성 일반 씬에는 생성하지 않습니다.
                GameObject copy = Object.Instantiate(root, root.transform, false);
                copy.transform.SetParent(null, false);
                copy.name = root.name;
                GameObject prefab;
                try
                {
                    MapBakedLighting copiedBinding = copy.GetComponent<MapBakedLighting>();
                    if (copiedBinding != null) Object.DestroyImmediate(copiedBinding);
                    FbxPlacementVolumes.SaveProfiles(copy, prefabPath);
                    prefab = FbxPlacementWorkspace.SavePrefab(copy, prefabPath);
                }
                finally { Object.DestroyImmediate(copy); }
                if (lighting != null && !EditorUtility.IsPersistent(lighting))
                {
                    string path = AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(prefabPath, null).Replace('\\', '/') + "_Lighting.asset");
                    AssetDatabase.CreateAsset(lighting, path);
                }
                if (map == null)
                {
                    map = ScriptableObject.CreateInstance<MapDefinitionSO>();
                    map.Initialize(prefab, lighting);
                    AssetDatabase.CreateAsset(map, mapPath);
                }
                else
                {
                    map.Initialize(prefab, lighting);
                    EditorUtility.SetDirty(map);
                }
                if (lighting != null) binding.Bind(lighting);
                if (binding != null) binding.Apply();
                AssetDatabase.SaveAssets();
                return map;
            }
            finally
            {
                if (lighting != null && !EditorUtility.IsPersistent(lighting)) Object.DestroyImmediate(lighting);
            }
        }

        public static GameObject Open(MapDefinitionSO map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            map.Validate();
            GameObject root = FbxPlacementWorkspace.OpenPrefab(map.Prefab);
            try { MapAssembler.Apply(root, map.BakedLighting); return root; }
            catch { FbxPlacementWorkspace.Close(root); throw; }
        }
    }
}
