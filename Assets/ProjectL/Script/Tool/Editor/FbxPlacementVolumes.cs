using System;
using System.Collections.Generic;
using System.IO;
using Cinemachine.PostFX;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ProjectL.Tool.Editor
{
    /// <summary>맵 볼륨과 작업용 프로필 복사본을 생성하고 저장합니다.</summary>
    public static class FbxPlacementVolumes
    {
        public static VolumeProfile CopyProfile(VolumeProfile source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var copy = ScriptableObject.CreateInstance<VolumeProfile>();
            copy.name = source.name;
            foreach (VolumeComponent component in source.components)
            {
                if (component == null) continue;
                VolumeComponent item = Object.Instantiate(component);
                item.hideFlags = HideFlags.None;
                copy.components.Add(item);
            }
            return copy;
        }

        public static Volume Add(GameObject root, Transform layer = null)
        {
            if (!FbxPlacementWorkspace.IsWorkspaceRoot(root)) throw new ArgumentException("가상 씬의 맵이 필요합니다.", nameof(root));
            Transform parent = FbxPlacementLayers.Resolve(root, layer);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "맵 무드";
            profile.Add<ColorAdjustments>();
            profile.Add<Bloom>();
            profile.Add<Tonemapping>();
            profile.Add<Vignette>();
            profile.Add<WhiteBalance>();
            GameObject instance = null;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            try
            {
                instance = EditorUtility.CreateGameObjectWithHideFlags("Global Volume", HideFlags.HideAndDontSave);
                SceneManager.MoveGameObjectToScene(instance, root.scene);
                instance.hideFlags = HideFlags.None;
                instance.transform.SetParent(parent, false);
                Volume volume = instance.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.weight = 1;
                volume.sharedProfile = profile;
                Undo.RegisterCreatedObjectUndo(instance, "맵 볼륨 추가");
                Undo.CollapseUndoOperations(group);
                Undo.IncrementCurrentGroup();
                return volume;
            }
            catch
            {
                if (instance != null) Object.DestroyImmediate(instance);
                DestroyProfile(profile);
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }

        /// <summary>일반 씬에 있는 후처리 Volume을 작업실 전용 복사본으로 가져옵니다.</summary>
        public static Volume Import(GameObject root, Volume source, Transform layer = null)
        {
            if (!FbxPlacementWorkspace.IsWorkspaceRoot(root)) throw new ArgumentException("가상 씬의 맵이 필요합니다.", nameof(root));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source.sharedProfile == null) throw new InvalidOperationException("가져올 Volume에 Profile이 없습니다.");
            if (source.gameObject.scene == root.scene)
                throw new ArgumentException("작업실 안의 Volume은 다시 가져올 수 없습니다.", nameof(source));

            Transform parent = FbxPlacementLayers.Resolve(root, layer);
            VolumeProfile profile = CopyProfile(source.sharedProfile);
            GameObject instance = null;
            try
            {
                instance = EditorUtility.CreateGameObjectWithHideFlags(source.name, HideFlags.HideAndDontSave);
                SceneManager.MoveGameObjectToScene(instance, root.scene);
                instance.hideFlags = HideFlags.None;
                instance.layer = source.gameObject.layer;
                instance.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                instance.transform.localScale = source.transform.lossyScale;
                instance.transform.SetParent(parent, true);

                Volume volume = instance.AddComponent<Volume>();
                volume.isGlobal = source.isGlobal;
                volume.priority = source.priority;
                volume.blendDistance = source.blendDistance;
                volume.weight = source.weight;
                volume.sharedProfile = profile;
                volume.enabled = source.enabled;
                if (!source.isGlobal) CopyCollider(source, instance);
                instance.SetActive(source.gameObject.activeSelf);
                Undo.RegisterCreatedObjectUndo(instance, "기존 Volume 가져오기");
                Undo.IncrementCurrentGroup();
                return volume;
            }
            catch
            {
                if (instance != null) Object.DestroyImmediate(instance);
                DestroyProfile(profile);
                throw;
            }
        }

        public static void DestroyProfile(VolumeProfile profile)
        {
            if (profile == null || EditorUtility.IsPersistent(profile)) return;
            foreach (VolumeComponent component in profile.components)
                if (component != null && !EditorUtility.IsPersistent(component)) Object.DestroyImmediate(component);
            Object.DestroyImmediate(profile);
        }

        private static void CopyCollider(Volume source, GameObject destination)
        {
            Collider collider = source.GetComponent<Collider>();
            if (collider == null)
                throw new InvalidOperationException("로컬 Volume에는 Collider가 필요합니다.");

            Component copy = destination.AddComponent(collider.GetType());
            EditorUtility.CopySerialized(collider, copy);
        }

        /// <summary>복제한 저장용 맵의 임시 프로필만 에셋화합니다. 편집 중인 원본은 유지합니다.</summary>
        public static void SaveProfiles(GameObject copy, string prefabPath)
        {
            var saved = new Dictionary<VolumeProfile, VolumeProfile>();
            foreach (Volume volume in copy.GetComponentsInChildren<Volume>(true))
                volume.sharedProfile = SaveProfile(volume.sharedProfile, saved, prefabPath);
            foreach (CinemachineVolumeSettings settings in copy.GetComponentsInChildren<CinemachineVolumeSettings>(true))
                settings.m_Profile = SaveProfile(settings.m_Profile, saved, prefabPath);
        }

        private static VolumeProfile SaveProfile(VolumeProfile profile, Dictionary<VolumeProfile, VolumeProfile> saved,
            string prefabPath)
        {
            if (profile == null || EditorUtility.IsPersistent(profile)) return profile;
            if (!saved.TryGetValue(profile, out VolumeProfile asset))
            {
                string parent = Path.GetDirectoryName(prefabPath)?.Replace('\\', '/');
                string name = Path.GetFileNameWithoutExtension(prefabPath) + "_Volumes";
                string folder = parent + "/" + name;
                if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(parent, name);
                string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/Mood.asset");
                asset = CopyProfile(profile);
                AssetDatabase.CreateAsset(asset, path);
                foreach (VolumeComponent component in asset.components) AssetDatabase.AddObjectToAsset(component, asset);
                EditorUtility.SetDirty(asset);
                saved.Add(profile, asset);
            }
            return asset;
        }
    }
}
