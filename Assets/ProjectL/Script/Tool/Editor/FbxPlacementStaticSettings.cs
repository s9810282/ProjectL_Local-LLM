using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectL.Tool.Editor
{
    /// <summary>선택 범위의 정적 플래그와 베이크 수신 설정을 함께 변경합니다.</summary>
    public static class FbxPlacementStaticSettings
    {
#pragma warning disable CS0618 // Unity가 유지하는 이전 내비게이션 플래그도 편집할 수 있게 합니다.
        public const StaticEditorFlags AllFlags = StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic |
            StaticEditorFlags.NavigationStatic | StaticEditorFlags.OffMeshLinkGeneration;
#pragma warning restore CS0618

        public static List<GameObject> GetTargets(IEnumerable<Transform> selection, bool includeChildren)
        {
            var targets = new List<GameObject>();
            var seen = new HashSet<GameObject>();
            foreach (Transform selected in selection)
            {
                if (selected == null) continue;
                if (includeChildren)
                {
                    foreach (Transform target in selected.GetComponentsInChildren<Transform>(true))
                        if (seen.Add(target.gameObject)) targets.Add(target.gameObject);
                }
                else if (seen.Add(selected.gameObject)) targets.Add(selected.gameObject);
            }
            return targets;
        }

        public static void SetFlag(IReadOnlyList<GameObject> targets, StaticEditorFlags flag, bool enabled)
        {
            Apply(targets, flag, enabled, false);
        }

        public static void SetAll(IReadOnlyList<GameObject> targets, bool enabled)
        {
            Apply(targets, AllFlags, enabled, true);
        }

        private static void Apply(IReadOnlyList<GameObject> targets, StaticEditorFlags flag, bool enabled, bool replace)
        {
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (flag == 0 || (flag & ~AllFlags) != 0) throw new ArgumentException("지원하지 않는 정적 설정입니다.", nameof(flag));
            foreach (GameObject target in targets)
                if (target == null || EditorUtility.IsPersistent(target) || !target.scene.IsValid() || !target.scene.isLoaded)
                    throw new ArgumentException("씬에 배치된 오브젝트를 선택하세요.", nameof(targets));
            const string undoName = "맵 정적 설정 변경";
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            foreach (GameObject target in targets)
            {
                StaticEditorFlags flags = replace ? (enabled ? AllFlags : 0) : GameObjectUtility.GetStaticEditorFlags(target);
                if (!replace) flags = enabled ? flags | flag : flags & ~flag;
                // 프리팹 생성 시 라이트맵 ST를 연결하므로 베이크 대상의 정적 배칭은 제외합니다.
                if ((flags & StaticEditorFlags.ContributeGI) != 0) flags &= ~StaticEditorFlags.BatchingStatic;
                Undo.RecordObject(target, undoName);
                GameObjectUtility.SetStaticEditorFlags(target, flags);
                if ((flag & StaticEditorFlags.ContributeGI) != 0)
                {
                    MeshRenderer renderer = target.GetComponent<MeshRenderer>();
                    if (renderer == null) continue;
                    Undo.RecordObject(renderer, undoName);
                    renderer.receiveGI = (flags & StaticEditorFlags.ContributeGI) != 0 ? ReceiveGI.Lightmaps : ReceiveGI.LightProbes;
                }
            }
            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(group);
            Undo.IncrementCurrentGroup();
        }
    }
}
