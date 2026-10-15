using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>맵 노드 인스턴스의 배치, 외형, 조회와 수명을 관리하는 일반 C# 클래스입니다.</summary>
    internal sealed class StageNodeView
    {
        private readonly Dictionary<int, Transform> _roots = new();
        private readonly Dictionary<Transform, int> _nodeIdsByRoot = new();

        internal void Show(IReadOnlyList<StageProgressData> nodes, StageMapViewData mapData,
            Func<StageProgressData, GameObject> findVisualPrefab, Transform parent)
        {
            if (mapData.NodePrefab == null)
                throw new InvalidOperationException("[StageNodeView] Node prefab is required.");

            foreach (StageProgressData node in nodes)
            {
                if (node.NodeId <= 0 || _roots.ContainsKey(node.NodeId))
                    throw new InvalidOperationException($"[StageNodeView] Invalid or duplicate node: {node.NodeId}");

                GameObject instance = Object.Instantiate(mapData.NodePrefab, parent, false);
                Transform root = instance.transform;
                _roots.Add(node.NodeId, root);
                _nodeIdsByRoot.Add(root, node.NodeId);
                instance.name = $"StageNode_{node.NodeId}";
                root.localPosition = new Vector3(node.WidthIndex * mapData.WidthSpacing, 0f,
                    node.Depth * mapData.DepthSpacing);
                root.localScale *= mapData.NodeScale;

                // 완료 여부는 Core의 값만 사용하며 선택·호버 색과 독립적으로 표시합니다.
                Transform clearedIndicator = root.Find(StageMapViewData.ClearedIndicatorName);
                if (clearedIndicator == null)
                    throw new InvalidOperationException("[StageNodeView] Node prefab requires ClearedIndicator.");
                clearedIndicator.gameObject.SetActive(node.StageKey != 0 && node.IsClear);

                GameObject prefab = mapData.StartVisualPrefab;
                if (node.StageKey != 0)
                {
                    prefab = findVisualPrefab(node);
                }

                // 외형 미지정 시 기본 노드만 유지합니다. SO 원본을 변경하지 않습니다.
                if (prefab != null)
                {
                    Transform visualRoot = root.Find(StageMapViewData.VisualRootName);
                    Object.Instantiate(prefab, visualRoot != null ? visualRoot : root, false);
                }
            }
        }

        internal bool TryGetNode(int nodeId, out Transform nodeRoot)
        {
            if (_roots.TryGetValue(nodeId, out nodeRoot) && nodeRoot != null)
                return true;
            nodeRoot = null;
            return false;
        }

        /// <summary>
        /// 노드 안의 Transform(맞은 Collider 등)에서 부모를 따라 올라가 노드 ID를 찾습니다.
        /// 외형 프리팹에 Collider가 있어도 같은 노드로 판별합니다.
        /// </summary>
        internal bool TryGetNodeId(Transform hit, out int nodeId)
        {
            for (Transform current = hit; current != null; current = current.parent)
            {
                if (_nodeIdsByRoot.TryGetValue(current, out nodeId))
                    return true;
            }

            nodeId = 0;
            return false;
        }

        internal void Clear()
        {
            foreach (Transform root in _roots.Values)
            {
                if (root == null) continue;
                // Destroy가 지연되는 프레임에도 이전 노드가 보이거나 피킹되지 않게 합니다.
                root.gameObject.SetActive(false);
                Object.Destroy(root.gameObject);
            }
            _roots.Clear();
            _nodeIdsByRoot.Clear();
        }
    }
}
