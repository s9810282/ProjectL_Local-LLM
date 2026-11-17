using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>캐릭터 프리팹의 접지 높이와 Renderer 머티리얼을 에디터에서 확인합니다.</summary>
    internal static class CharacterAppearanceSetup
    {
        private const string PrefabRoot = "Assets/ProjectL/Prefabs/Characters/New";
        private const string MaterialRoot = "Assets/ProjectL/Script/Render/Shader/View/CustomShader/BaseShader";
        private const string ReportPath = "Temp/character-appearance-report.json";

        [Serializable]
        private sealed class MaterialSlot
        {
            public string prefab;
            public string renderer;
            public int slot;
            public string material;
            public string materialPath;
            public string shader;
            public string[] textures;
        }

        [Serializable]
        private sealed class HeightInfo
        {
            public string prefab;
            public float animatorY;
            public float minimumY;
        }

        [Serializable]
        private sealed class AppearanceReport
        {
            public List<MaterialSlot> slots = new List<MaterialSlot>();
            public List<MaterialSlot> available = new List<MaterialSlot>();
            public List<HeightInfo> heights = new List<HeightInfo>();
        }

        [Serializable]
        private sealed class HeightChange
        {
            public string prefab;
            public float beforeAnimatorY;
            public float afterAnimatorY;
            public float beforeMinimumY;
            public float afterMinimumY;
        }

        [Serializable]
        private sealed class ApplyResult
        {
            public int materialSlotsReplaced;
            public List<string> changedPrefabs = new List<string>();
            public List<HeightChange> heights = new List<HeightChange>();
        }

        [MenuItem("ProjectL/Animation/New 캐릭터 접지·BaseShader 머티리얼 적용")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("[CharacterAppearanceSetup] 에셋 작업은 편집 모드에서 실행합니다.");
            var available = new List<Material>();
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { MaterialRoot }))
                available.Add(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)));
            var result = new ApplyResult();
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                bool usesOpenStage = stage != null && stage.assetPath == path;
                GameObject root = usesOpenStage ? stage.prefabContentsRoot : PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var replacements = new Dictionary<Renderer, Material[]>();
                    int changedSlots = 0;
                    foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                    {
                        if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                        Material[] slots = renderer.sharedMaterials;
                        bool changedRenderer = false;
                        for (int i = 0; i < slots.Length; i++)
                        {
                            Material replacement = ResolveMaterial(slots[i], available, path, renderer.name, i);
                            if (replacement == slots[i]) continue;
                            slots[i] = replacement;
                            changedRenderer = true;
                            changedSlots++;
                        }
                        if (changedRenderer) replacements.Add(renderer, slots);
                    }

                    bool fixHeight = path.EndsWith("/EnemyShabow/EnemyShabow.prefab", StringComparison.Ordinal)
                        || path.EndsWith("/Boomber/Boomber.prefab", StringComparison.Ordinal);
                    if (changedSlots == 0 && !fixHeight) continue;
                    string backup = "Temp/character-appearance-backups/" + path.Substring(PrefabRoot.Length + 1);
                    Directory.CreateDirectory(Path.GetDirectoryName(backup));
                    if (!File.Exists(backup)) File.Copy(path, backup);
                    if (!File.Exists(backup + ".meta")) File.Copy(path + ".meta", backup + ".meta");

                    foreach (KeyValuePair<Renderer, Material[]> replacement in replacements)
                        replacement.Key.sharedMaterials = replacement.Value;
                    if (fixHeight)
                    {
                        Animator animator = root.GetComponentInChildren<Animator>(true);
                        if (animator == null) throw new InvalidOperationException($"[CharacterAppearanceSetup] Animator가 없습니다: {path}");
                        var height = new HeightChange
                        {
                            prefab = path,
                            beforeAnimatorY = animator.transform.localPosition.y,
                            beforeMinimumY = MeasureGround(root, animator),
                        };
                        Transform parent = animator.transform.parent;
                        Vector3 axis = root.transform.InverseTransformVector(parent != null ? parent.TransformVector(Vector3.up) : Vector3.up);
                        if (Mathf.Abs(axis.y) < 0.0001f)
                            throw new InvalidOperationException($"[CharacterAppearanceSetup] 부모 Y축으로 접지 높이를 보정할 수 없습니다: {path}");
                        Vector3 position = animator.transform.localPosition;
                        position.y -= height.beforeMinimumY / axis.y;
                        animator.transform.localPosition = position;
                        height.afterAnimatorY = position.y;
                        height.afterMinimumY = MeasureGround(root, animator);
                        if (Mathf.Abs(height.afterMinimumY) > 0.001f)
                            throw new InvalidOperationException($"[CharacterAppearanceSetup] 보정 뒤 접지 높이가 맞지 않습니다: {path}, {height.afterMinimumY}");
                        result.heights.Add(height);
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                    if (!success) throw new InvalidOperationException($"[CharacterAppearanceSetup] 저장 실패: {path}");
                    result.changedPrefabs.Add(path);
                    result.materialSlotsReplaced += changedSlots;
                }
                finally
                {
                    if (!usesOpenStage) PrefabUtility.UnloadPrefabContents(root);
                }
            }
            File.WriteAllText("Temp/character-appearance-applied.json", JsonUtility.ToJson(result, true));
            Inspect();
            Debug.Log($"[CharacterAppearanceSetup] 적용 완료: 프리팹 {result.changedPrefabs.Count}개, 머티리얼 {result.materialSlotsReplaced}슬롯, 접지 {result.heights.Count}개.");
        }

        private static Material ResolveMaterial(Material source, List<Material> available, string prefab, string renderer, int slot)
        {
            if (source == null)
                throw new InvalidOperationException($"[CharacterAppearanceSetup] 머티리얼이 없습니다: {prefab}, {renderer}, 슬롯 {slot}");
            if (AssetDatabase.GetAssetPath(source).StartsWith(MaterialRoot + "/", StringComparison.Ordinal)) return source;
            Texture texture = BaseTexture(source);
            if (texture == null)
                throw new InvalidOperationException($"[CharacterAppearanceSetup] 대응할 기본 텍스처가 없습니다: {prefab}, {renderer}, {source.name}");
            Material match = null;
            foreach (Material candidate in available)
            {
                if (BaseTexture(candidate) != texture) continue;
                if (match != null)
                    throw new InvalidOperationException($"[CharacterAppearanceSetup] 같은 텍스처의 머티리얼이 여러 개입니다: {source.name}, {match.name}, {candidate.name}");
                match = candidate;
            }
            if (match == null)
                throw new InvalidOperationException($"[CharacterAppearanceSetup] BaseShader에 대응 머티리얼이 없습니다: {prefab}, {source.name}, {AssetDatabase.GetAssetPath(texture)}");
            return match;
        }

        private static Texture BaseTexture(Material material)
        {
            if (material == null) return null;
            foreach (string property in new[] { "_BaseMap", "_MainTex" })
                if (material.HasProperty(property) && material.GetTexture(property) != null)
                    return material.GetTexture(property);
            return null;
        }

        [MenuItem("ProjectL/Animation/New 캐릭터 높이·머티리얼 점검")]
        public static void Inspect()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("[CharacterAppearanceSetup] 에셋 작업은 편집 모드에서 실행합니다.");
            var report = new AppearanceReport();
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { MaterialRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                report.available.Add(Describe(null, null, 0, AssetDatabase.LoadAssetAtPath<Material>(path)));
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                    {
                        if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                        Material[] materials = renderer.sharedMaterials;
                        for (int i = 0; i < materials.Length; i++)
                            report.slots.Add(Describe(path, AnimationUtility.CalculateTransformPath(renderer.transform, root.transform), i, materials[i]));
                    }
                    if (path.EndsWith("/EnemyShabow/EnemyShabow.prefab", StringComparison.Ordinal)
                        || path.EndsWith("/Boomber/Boomber.prefab", StringComparison.Ordinal))
                    {
                        Animator animator = root.GetComponentInChildren<Animator>(true);
                        if (animator == null) throw new InvalidOperationException($"[CharacterAppearanceSetup] Animator가 없습니다: {path}");
                        report.heights.Add(new HeightInfo { prefab = path, animatorY = animator.transform.localPosition.y, minimumY = MeasureGround(root, animator) });
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            Debug.Log($"[CharacterAppearanceSetup] 높이·머티리얼 점검 완료: {ReportPath}");
        }

        private static MaterialSlot Describe(string prefab, string renderer, int slot, Material material)
        {
            var textures = new List<string>();
            if (material != null)
                foreach (string property in material.GetTexturePropertyNames())
                {
                    Texture texture = material.GetTexture(property);
                    if (texture != null) textures.Add(property + "=" + AssetDatabase.GetAssetPath(texture));
                }
            return new MaterialSlot
            {
                prefab = prefab,
                renderer = renderer,
                slot = slot,
                material = material != null ? material.name : null,
                materialPath = material != null ? AssetDatabase.GetAssetPath(material) : null,
                shader = material != null && material.shader != null ? material.shader.name : null,
                textures = textures.ToArray(),
            };
        }

        private static float MeasureGround(GameObject root, Animator animator)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            var positions = new Vector3[transforms.Length];
            var rotations = new Quaternion[transforms.Length];
            var scales = new Vector3[transforms.Length];
            for (int i = 0; i < transforms.Length; i++)
            {
                positions[i] = transforms[i].localPosition;
                rotations[i] = transforms[i].localRotation;
                scales[i] = transforms[i].localScale;
            }
            var mesh = new Mesh();
            try
            {
                if (animator.runtimeAnimatorController != null)
                    foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                        clip.SampleAnimation(animator.gameObject, 0f);
                float minimum = float.PositiveInfinity;
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    Mesh geometry;
                    if (renderer is SkinnedMeshRenderer skinned)
                    {
                        mesh.Clear();
                        skinned.BakeMesh(mesh);
                        geometry = mesh;
                    }
                    else if (renderer is MeshRenderer && renderer.TryGetComponent(out MeshFilter filter))
                        geometry = filter.sharedMesh;
                    else continue;
                    if (geometry == null) continue;
                    foreach (Vector3 vertex in geometry.vertices)
                        minimum = Mathf.Min(minimum, root.transform.InverseTransformPoint(renderer.transform.TransformPoint(vertex)).y);
                }
                if (!float.IsFinite(minimum)) throw new InvalidOperationException($"[CharacterAppearanceSetup] 측정할 메시가 없습니다: {root.name}");
                return minimum;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
                for (int i = 0; i < transforms.Length; i++)
                {
                    transforms[i].localPosition = positions[i];
                    transforms[i].localRotation = rotations[i];
                    transforms[i].localScale = scales[i];
                }
            }
        }
    }
}
