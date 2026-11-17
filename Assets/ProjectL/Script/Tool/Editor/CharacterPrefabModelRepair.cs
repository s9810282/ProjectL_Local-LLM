using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>승인된 프리팹의 Animator 아래 모델 자식만 교체하고 루트·전투 컴포넌트를 보존합니다.</summary>
    internal static class CharacterPrefabModelRepair
    {
        internal static void ReplaceVisualChildren(string prefabPath, string sourcePath)
        {
            if (prefabPath != "Assets/ProjectL/Prefabs/Characters/New/Enemy/EnemyShabow/EnemyShabow.prefab"
                && prefabPath != "Assets/ProjectL/Prefabs/Characters/New/Playerable/Boomber/Boomber.prefab")
                throw new InvalidOperationException($"[CharacterPrefabModelRepair] 모델 교체가 승인되지 않은 프리팹입니다: {prefabPath}");

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (model == null) throw new InvalidOperationException($"[CharacterPrefabModelRepair] FBX가 없습니다: {sourcePath}");
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            bool usesOpenStage = stage != null && stage.assetPath == prefabPath;
            GameObject root = usesOpenStage ? stage.prefabContentsRoot : PrefabUtility.LoadPrefabContents(prefabPath);
            GameObject replacement = null;
            try
            {
                Animator[] animators = root.GetComponentsInChildren<Animator>(true);
                if (animators.Length != 1)
                    throw new InvalidOperationException($"[CharacterPrefabModelRepair] Animator가 하나여야 합니다: {prefabPath}");
                Animator animator = animators[0];
                var oldChildren = new List<GameObject>();
                foreach (Transform child in animator.transform)
                {
                    if (child.GetComponentsInChildren<MonoBehaviour>(true).Length > 0)
                        throw new InvalidOperationException($"[CharacterPrefabModelRepair] 모델 자식에 보존해야 할 스크립트가 있습니다: {prefabPath}, {child.name}");
                    oldChildren.Add(child.gameObject);
                }
                ValidateReferences(root, oldChildren, prefabPath);

                replacement = (GameObject)PrefabUtility.InstantiatePrefab(model, root.scene);
                PrefabUtility.UnpackPrefabInstance(replacement, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                Transform sourceRoot = replacement.transform;
                if (sourceRoot.localPosition.sqrMagnitude > 0.000001f
                    || Quaternion.Angle(sourceRoot.localRotation, Quaternion.identity) > 0.001f
                    || (sourceRoot.localScale - Vector3.one).sqrMagnitude > 0.000001f)
                    throw new InvalidOperationException($"[CharacterPrefabModelRepair] FBX 루트 변환을 별도로 이식해야 합니다: {sourcePath}");
                foreach (Component component in replacement.GetComponents<Component>())
                    if (!(component is Transform) && !(component is Animator))
                        throw new InvalidOperationException($"[CharacterPrefabModelRepair] FBX 루트에 별도 이식이 필요한 컴포넌트가 있습니다: {sourcePath}");

                foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(sourcePath))
                {
                    if (!(asset is AnimationClip clip) || clip.name.StartsWith("__preview__", StringComparison.Ordinal)) continue;
                    foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
                        if (binding.path.Length > 0 && sourceRoot.Find(binding.path) == null)
                            throw new InvalidOperationException($"[CharacterPrefabModelRepair] 원본 FBX에서도 클립 경로를 찾지 못했습니다: {clip.name}, {binding.path}");
                }

                string backupFolder = "Temp/character-model-backups/" + root.name;
                Directory.CreateDirectory(backupFolder);
                string backup = backupFolder + "/" + Path.GetFileName(prefabPath);
                if (!File.Exists(backup)) File.Copy(prefabPath, backup);
                if (!File.Exists(backup + ".meta")) File.Copy(prefabPath + ".meta", backup + ".meta");

                foreach (SkinnedMeshRenderer renderer in replacement.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (renderer.rootBone == sourceRoot) renderer.rootBone = animator.transform;
                    Transform[] bones = renderer.bones;
                    for (int i = 0; i < bones.Length; i++)
                        if (bones[i] == sourceRoot) bones[i] = animator.transform;
                    renderer.bones = bones;
                }
                foreach (Transform child in replacement.GetComponentsInChildren<Transform>(true))
                    child.gameObject.layer = animator.gameObject.layer;
                foreach (GameObject child in oldChildren) UnityEngine.Object.DestroyImmediate(child);
                while (sourceRoot.childCount > 0) sourceRoot.GetChild(0).SetParent(animator.transform, false);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool success);
                if (!success) throw new InvalidOperationException($"[CharacterPrefabModelRepair] 프리팹 저장 실패: {prefabPath}");
            }
            finally
            {
                if (replacement != null) UnityEngine.Object.DestroyImmediate(replacement);
                if (!usesOpenStage) PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ValidateReferences(GameObject root, List<GameObject> oldChildren, string prefab)
        {
            foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null)
                    throw new InvalidOperationException($"[CharacterPrefabModelRepair] Missing Script가 있습니다: {prefab}");
                var serialized = new SerializedObject(component);
                SerializedProperty property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    UnityEngine.Object value = property.objectReferenceValue;
                    Transform target = value is GameObject gameObject ? gameObject.transform : (value as Component)?.transform;
                    if (target == null) continue;
                    foreach (GameObject child in oldChildren)
                        if (target == child.transform || target.IsChildOf(child.transform))
                            throw new InvalidOperationException($"[CharacterPrefabModelRepair] 모델 자식 참조를 별도로 이식해야 합니다: {prefab}, {component.GetType().Name}.{property.propertyPath}");
                }
            }
        }
    }
}
