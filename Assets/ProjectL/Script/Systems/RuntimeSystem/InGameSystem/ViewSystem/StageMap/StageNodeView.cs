using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>맵 노드 자리와 건물 인스턴스의 배치, 표시, 클릭 조회와 수명을 관리하는 일반 C# 클래스입니다.</summary>
    internal sealed class StageNodeView
    {
        private readonly Dictionary<int, Transform> _roots = new();
        private readonly Dictionary<int, NodeAppearance> _appearances = new();
        private IReadOnlyList<StageProgressData> _sourceNodes;
        private int _currentNodeId;
        private long _appearanceVersion;

        internal int CurrentNodeId => _currentNodeId;

        internal void Show(IReadOnlyList<StageProgressData> nodes, StageMapViewData mapData,
            Func<StageProgressData, StageNodeVisualData> findVisual, Transform parent)
        {
            long version = ++_appearanceVersion;
            _sourceNodes = nodes;
            Vector3[] positions = StageMapNodeLayout.CalculateLocalPositions(nodes, mapData.WidthSpacing, mapData.DepthSpacing);
            for (int i = 0; i < nodes.Count; i++)
            {
                StageProgressData node = nodes[i];
                if (node.NodeId <= 0 || _roots.ContainsKey(node.NodeId))
                    throw new InvalidOperationException($"[StageNodeView] Invalid or duplicate node: {node.NodeId}");

                Transform root = new GameObject($"StageNode_{node.NodeId}").transform;
                root.SetParent(parent, false);
                _roots.Add(node.NodeId, root);
                root.localPosition = positions[i];
                root.localScale = Vector3.one * mapData.NodeScale;

                StageNodeVisualData visualData = !node.IsStart ? findVisual(node) : null;
                if (_appearanceVersion != version) return;
                GameObject prefab = node.IsStart ? mapData.StartVisualPrefab : visualData?.VisualPrefab;
                // 건물을 눌러 노드를 고르므로, 건물이 없으면 갈 수 없는 노드가 됩니다. 시작 노드는 누를 일이 없습니다.
                if (prefab == null && !node.IsStart)
                    throw new InvalidOperationException($"[StageNodeView] 노드에 건물이 필요합니다: {node.NodeId}");

                // 건물 인스턴스는 현재 위치에서 떠날 때 다시 표시할 수 있도록 보관합니다.
                GameObject visual = null;
                BoxCollider hit = null;
                if (prefab != null)
                {
                    visual = Object.Instantiate(prefab, root, false);
                    if (_appearanceVersion != version) return;
                    // 노드 기준 회전을 프리팹 고유 회전 앞에 합성하고, 클릭 영역도 최종 외형으로 계산합니다.
                    if (visualData != null)
                        visual.transform.localRotation = Quaternion.Euler(visualData.RotationOffset) * visual.transform.localRotation;
                    hit = PlaceBuilding(root, visual, mapData.BuildingHeight);
                }
                var appearance = new NodeAppearance(i, visual, hit);
                _appearances.Add(node.NodeId, appearance);
                if (!ApplyAppearance(appearance, false, version)) return;
            }
        }

        /// <summary>현재 노드의 건물을 숨기고, 떠난 노드는 Buffer의 완료 사실로 복원합니다.</summary>
        internal void SetCurrentNode(int nodeId)
        {
            if (nodeId < 0 || (nodeId != 0 && !_appearances.ContainsKey(nodeId)))
                throw new InvalidOperationException($"[StageNodeView] 표시 중인 현재 노드가 없습니다: {nodeId}");

            long version = ++_appearanceVersion;
            if (_currentNodeId != 0 && !ApplyAppearance(_appearances[_currentNodeId], false, version)) return;
            _currentNodeId = nodeId;
            if (nodeId != 0)
                ApplyAppearance(_appearances[nodeId], true, version);
        }

        /// <summary>같은 표시의 ID별 목록 인덱스를 갱신하고 현재 위치를 포함한 모든 완료 표시를 다시 적용합니다.</summary>
        internal void Refresh(IReadOnlyList<StageProgressData> nodes)
        {
            long version = ++_appearanceVersion;
            _sourceNodes = nodes;
            for (int index = 0; index < nodes.Count; index++)
            {
                int nodeId = nodes[index].NodeId;
                NodeAppearance previous = _appearances[nodeId];
                var appearance = new NodeAppearance(index, previous.Visual, previous.Hit);
                _appearances[nodeId] = appearance;
                if (!ApplyAppearance(appearance, nodeId == _currentNodeId, version)) return;
            }
        }

        private bool ApplyAppearance(NodeAppearance appearance, bool isCurrent, long version)
        {
            if (_appearanceVersion != version) return false;
            if (appearance.Visual == null) return true;
            // 완료 상태를 별도로 저장하지 않고 Show에서 받은 Buffer 목록의 최신 값을 읽습니다.
            StageProgressData node = _sourceNodes[appearance.SourceIndex];
            // 이동 목적지는 착지 후 현재 노드로 바뀔 때 숨깁니다.
            bool isCleared = !node.IsStart && node.IsClear;
            bool buildingShown = !isCurrent && !isCleared;
            if (appearance.Visual.activeSelf != buildingShown) appearance.Visual.SetActive(buildingShown);
            if (_appearanceVersion != version) return false;
            // 건물이 숨은 동안에는 누를 곳이 없습니다. 그 자리의 누름은 배경 끌기가 됩니다.
            appearance.Hit.enabled = buildingShown;
            return true;
        }

        internal bool TryGetNode(int nodeId, out Transform nodeRoot)
        {
            if (_roots.TryGetValue(nodeId, out nodeRoot) && nodeRoot != null)
                return true;
            nodeRoot = null;
            return false;
        }

        /// <summary>보이는 건물을 감싼 클릭 상자만 검사합니다. 겹치면 가까운 상자가 잡힙니다.</summary>
        internal bool TryPickNode(Ray ray, float pickDistance, out int nodeId)
        {
            nodeId = 0;
            float nearestDistance = pickDistance;
            foreach (KeyValuePair<int, NodeAppearance> target in _appearances)
            {
                BoxCollider hit = target.Value.Hit;
                if (hit == null || !hit.enabled || !hit.gameObject.activeInHierarchy)
                    continue;

                if (!hit.Raycast(ray, out RaycastHit result, nearestDistance))
                    continue;

                nearestDistance = result.distance;
                nodeId = target.Key;
            }
            return nodeId != 0;
        }

        internal void Clear()
        {
            ++_appearanceVersion;
            // 외부 OnDisable이 새 표시를 연결해도 이전 객체만 정리합니다.
            Transform[] owned = new Transform[_roots.Count];
            _roots.Values.CopyTo(owned, 0);
            _roots.Clear();
            _appearances.Clear();
            _sourceNodes = null;
            _currentNodeId = 0;
            foreach (Transform root in owned)
            {
                if (root == null) continue;
                // Destroy가 지연되는 프레임에도 이전 노드가 보이거나 피킹되지 않게 합니다.
                root.gameObject.SetActive(false);
                Object.Destroy(root.gameObject);
            }
        }

        /// <summary>
        /// 켜진 건물 메시 전체를 노드 좌표계에서 재서, 피벗이 달라도 밑면이 같은 높이에 오게 놓고
        /// 그 건물을 감싸는 클릭 상자를 노드 자리에 붙입니다.
        /// </summary>
        private static BoxCollider PlaceBuilding(Transform root, GameObject visual, float bottomHeight)
        {
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;
            foreach (MeshRenderer renderer in visual.GetComponentsInChildren<MeshRenderer>())
            {
                if (!renderer.enabled) continue;
                Bounds bounds = renderer.localBounds;
                for (int index = 0; index < 8; index++)
                {
                    Vector3 corner = new((index & 1) == 0 ? bounds.min.x : bounds.max.x,
                        (index & 2) == 0 ? bounds.min.y : bounds.max.y, (index & 4) == 0 ? bounds.min.z : bounds.max.z);
                    Vector3 point = root.InverseTransformPoint(renderer.transform.TransformPoint(corner));
                    min = Vector3.Min(min, point);
                    max = Vector3.Max(max, point);
                }
            }
            if (!float.IsFinite(max.x) || !float.IsFinite(max.y) || !float.IsFinite(max.z) ||
                !float.IsFinite(min.x) || !float.IsFinite(min.y) || !float.IsFinite(min.z))
                throw new InvalidOperationException($"[StageNodeView] 건물에 유효한 활성 MeshRenderer가 필요합니다: {visual.name}");

            Vector3 lift = Vector3.up * (bottomHeight - min.y);
            visual.transform.localPosition += lift;

            BoxCollider hit = root.gameObject.AddComponent<BoxCollider>();
            hit.center = (min + max) * 0.5f + lift;
            hit.size = max - min;
            return hit;
        }

        private readonly struct NodeAppearance
        {
            internal readonly int SourceIndex;
            internal readonly GameObject Visual;
            internal readonly BoxCollider Hit;

            internal NodeAppearance(int sourceIndex, GameObject visual, BoxCollider hit)
            {
                SourceIndex = sourceIndex;
                Visual = visual;
                Hit = hit;
            }
        }
    }
}
