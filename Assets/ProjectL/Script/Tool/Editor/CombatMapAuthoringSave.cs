using System;
using System.Linq;
using Cinemachine;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Tool.Editor
{
    /// <summary>보정 SO와 맵 프리팹을 서로 독립적으로 저장합니다. 씬 표시용 override는 프리팹에 적용하지 않습니다.</summary>
    internal static class CombatMapAuthoringSave
    {
        internal static void SaveOffsets(CombatMapAuthoringRoot root, GameObject source, string mode)
        {
            RequireSource(root, source);
            CombatStageSoAssets.WriteStagePreviewOffsets(root.ViewStage, root.DraftPositionOffset,
                root.DraftRotationOffset, root.DraftScaleOffset, root.DraftTileOffset, root.DraftAnchorTile,
                mode == "PrefabWithTiles");
            AssetDatabase.SaveAssetIfDirty(root.ViewStage);
        }

        /// <returns>적용한 프리팹 속성 override 수와 구조 변경 수의 합입니다.</returns>
        internal static int SavePrefab(CombatMapAuthoringRoot root, GameObject source, CombatStageSoView view,
            FbxCombatStageContext context)
        {
            RequireSource(root, source);
            GameObject map = root.PreviewMap;
            if (PrefabUtility.GetOutermostPrefabInstanceRoot(map) != map)
                throw new InvalidOperationException("맵 프리뷰가 최상위 프리팹 인스턴스가 아닙니다.");
            string path = AssetDatabase.GetAssetPath(source);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("배경 원본의 프리팹 경로가 없습니다.");

            PropertyModification[] original = PrefabUtility.GetPropertyModifications(map) ?? Array.Empty<PropertyModification>();
            Object rootGameObject = PrefabUtility.GetCorrespondingObjectFromSource(map);
            Object rootTransform = PrefabUtility.GetCorrespondingObjectFromSource(map.transform);
            PropertyModification[] authored = original.Where(mod => mod != null && mod.target != null
                && mod.target != rootGameObject && mod.target != rootTransform && !IsDisplayCamera(mod.target)).ToArray();
            int structural = PrefabUtility.GetAddedGameObjects(map).Count + PrefabUtility.GetRemovedGameObjects(map).Count
                + PrefabUtility.GetAddedComponents(map).Count + PrefabUtility.GetRemovedComponents(map).Count;
            int count = authored.Length + structural;
            if (count == 0) return 0;

            bool applied = false;
            try
            {
                PrefabUtility.SetPropertyModifications(map, authored);
                PrefabUtility.ApplyPrefabInstance(map, InteractionMode.UserAction);
                AssetDatabase.SaveAssetIfDirty(source);
                applied = true;
                return count;
            }
            finally
            {
                if (!applied && map != null)
                    PrefabUtility.SetPropertyModifications(map, original);
                if (map != null)
                {
                    CombatMapAuthoringPlacement.Reposition(root.PreviewContainer.transform, map.transform,
                        root.transform, source, view, context, root.DraftPositionOffset, root.DraftRotationOffset,
                        root.DraftScaleOffset, root.DraftTileOffset, root.DraftAnchorTile);
                    SuppressMapCameras(map);
                    EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
                }
            }
        }

        private static void RequireSource(CombatMapAuthoringRoot root, GameObject source)
        {
            if (root == null || root.ViewStage == null || root.LoadedViewStage != root.ViewStage
                || root.PreviewContainer == null || root.PreviewMap == null || source == null
                || !root.PreviewMap.transform.IsChildOf(root.PreviewContainer.transform)
                || PrefabUtility.GetCorrespondingObjectFromSource(root.PreviewMap) != source)
                throw new InvalidOperationException("선택한 ViewStage와 씬 프리뷰의 프리팹 출처가 다릅니다. 다시 불러오세요.");
            var current = CombatStageSoAssets.ReadStageViewPropSource(root.ViewStage);
            if (current.Prefab != source)
                throw new InvalidOperationException("운영 ViewStage가 참조하는 프리팹이 바뀌었습니다. 다시 불러오세요.");
        }

        private static bool IsDisplayCamera(Object target) => target is Camera
            || target is CinemachineVirtualCameraBase || target is CinemachineImpulseListener
            || target is CinemachineExtension;

        private static void SuppressMapCameras(GameObject map)
        {
            foreach (Camera camera in map.GetComponentsInChildren<Camera>(true))
            {
                camera.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
            }
            foreach (CinemachineVirtualCameraBase camera in map.GetComponentsInChildren<CinemachineVirtualCameraBase>(true))
            {
                camera.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
            }
            foreach (CinemachineImpulseListener listener in map.GetComponentsInChildren<CinemachineImpulseListener>(true))
            {
                listener.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(listener);
            }
        }
    }
}
