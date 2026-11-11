using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>이동·표시 보정 Root와 기존 모델 프리팹의 복제본·해제를 소유합니다.</summary>
    internal sealed class StageCurrentMarkerView
    {
        internal const string MovementRootName = "StageCurrentMarker";
        private Transform _root;

        /// <summary>프리팹과 표시 보정은 DB 생성 때 검사가 끝난 맵 표시 설정 값입니다(검사 후속 정리 R-f).</summary>
        internal void ShowAt(GameObject prefab, Transform nodeRoot, Vector3 offset, float yaw, float scale,
            Vector3 landingOffset)
        {
            Transform anchor = RequireAnchor(nodeRoot);
            if (_root == null)
            {
                _root = new GameObject(MovementRootName).transform;
                _root.gameObject.SetActive(false);
                try
                {
                    _root.SetParent(anchor, false);
                    Transform visual = new GameObject("StageCurrentMarkerVisual").transform;
                    visual.SetParent(_root, false);
                    visual.SetLocalPositionAndRotation(offset, Quaternion.Euler(0f, yaw, 0f));
                    visual.localScale = Vector3.one * scale;
                    GameObject instance = Object.Instantiate(prefab, visual, false);
                    // 전투와 공유하는 원본은 보존하고 복제본의 표시 정책만 적용합니다.
                    ChessPieceClone.Prepare(instance);
                }
                catch
                {
                    GameObject failed = _root.gameObject;
                    _root = null;
                    if (Application.isPlaying) Object.Destroy(failed);
                    else Object.DestroyImmediate(failed);
                    throw;
                }
            }
            else
            {
                _root.SetParent(anchor, true);
                _root.localScale = Vector3.one;
            }
            _root.localPosition = landingOffset;
            _root.gameObject.SetActive(true);
        }

        internal Transform BeginMove(Transform movementParent, Transform destination)
        {
            if (_root == null || movementParent == null)
                throw new InvalidOperationException("[StageCurrentMarkerView] 표시된 마커와 이동 부모가 필요합니다.");
            Transform targetAnchor = RequireAnchor(destination);
            ValidateHierarchy(movementParent);
            ValidateHierarchy(_root);
            ValidateHierarchy(targetAnchor);
            Matrix4x4 origin = movementParent.worldToLocalMatrix * _root.localToWorldMatrix;
            Matrix4x4 target = movementParent.worldToLocalMatrix * targetAnchor.localToWorldMatrix;
            ValidateBasis(origin);
            ValidateBasis(target);
            Vector3 sourceScale = origin.lossyScale;
            Vector3 targetScale = target.lossyScale;
            if (Vector3.Distance(sourceScale, targetScale) > 0.001f)
                throw new InvalidOperationException("[StageCurrentMarkerView] 노드 간 마커 크기가 같아야 합니다.");
            _root.SetParent(movementParent, true);
            return _root;
        }

        internal void CompleteMove(Transform destination, Vector3 landingOffset)
        {
            if (_root == null)
                throw new InvalidOperationException("[StageCurrentMarkerView] 이동 중인 마커가 없습니다.");
            _root.SetParent(RequireAnchor(destination), true);
            _root.localPosition = landingOffset;
            _root.localScale = Vector3.one;
        }

        internal void Hide()
        {
            if (_root != null) _root.gameObject.SetActive(false);
        }

        /// <summary>노드 밖으로 옮긴 Root도 직접 숨기고 해제합니다.</summary>
        internal void Detach()
        {
            Transform owned = _root;
            _root = null;
            if (owned == null) return;
            owned.gameObject.SetActive(false);
            Object.Destroy(owned.gameObject);
        }

        private static Transform RequireAnchor(Transform node)
        {
            if (node == null)
                throw new InvalidOperationException("[StageCurrentMarkerView] 마커를 배치할 노드 루트가 필요합니다.");
            return node;
        }

        private static void ValidateHierarchy(Transform target)
        {
            for (Transform current = target; current != null; current = current.parent)
            {
                Vector3 scale = current.localScale;
                if (!float.IsFinite(scale.x) || !float.IsFinite(scale.y) || !float.IsFinite(scale.z) ||
                    scale.x <= 0f || scale.y <= 0f || scale.z <= 0f)
                    throw new InvalidOperationException("[StageCurrentMarkerView] 양수 유한 크기가 필요합니다.");
            }
            ValidateBasis(target.localToWorldMatrix);
        }

        private static void ValidateBasis(Matrix4x4 matrix)
        {
            Vector3 x = matrix.GetColumn(0);
            Vector3 y = matrix.GetColumn(1);
            Vector3 z = matrix.GetColumn(2);
            if (!float.IsFinite(matrix.m03) || !float.IsFinite(matrix.m13) || !float.IsFinite(matrix.m23) ||
                x.sqrMagnitude <= 0f || y.sqrMagnitude <= 0f || z.sqrMagnitude <= 0f ||
                !float.IsFinite(x.sqrMagnitude) || !float.IsFinite(y.sqrMagnitude) || !float.IsFinite(z.sqrMagnitude) ||
                Mathf.Abs(Vector3.Dot(x.normalized, y.normalized)) > 0.001f ||
                Mathf.Abs(Vector3.Dot(x.normalized, z.normalized)) > 0.001f ||
                Mathf.Abs(Vector3.Dot(y.normalized, z.normalized)) > 0.001f)
                throw new InvalidOperationException("[StageCurrentMarkerView] shear 없는 유한 Transform이 필요합니다.");
        }
    }
}
