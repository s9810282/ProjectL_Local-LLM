using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Entry;
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
        private int _completionNodeId;
        private bool _completionBuildingShown;

        internal int CurrentNodeId => _currentNodeId;

        internal void Show(IReadOnlyList<StageProgressData> nodes, StageMapViewData mapData,
            Func<StageProgressData, StageNodeVisualData> findVisual, Transform parent,
            List<StageNodeDropTarget> dropTargets = null)
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
                    if (!node.IsStart && node.NodeId != _completionNodeId && dropTargets != null)
                        CollectDropTarget(node.NodeId, visual.transform, root, parent, mapData.BuildingHeight, dropTargets);
                }
                var appearance = new NodeAppearance(i, visual, hit, visualData?.BreakEffect ?? EffectType.Empty);
                _appearances.Add(node.NodeId, appearance);
                if (!ApplyAppearance(appearance, false, version)) return;
            }
        }

        /// <summary>현재 자리를 바꾸고 Buffer의 완료 사실로 건물 표시를 갱신합니다.</summary>
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
                var appearance = new NodeAppearance(index, previous.Visual, previous.Hit, previous.BreakEffect);
                _appearances[nodeId] = appearance;
                if (!ApplyAppearance(appearance, nodeId == _currentNodeId, version)) return;
            }
        }

        /// <summary>복원된 재사용 외형을 수집합니다. 시작 장식은 제외합니다.</summary>
        internal void CollectDropTargets(StageMapViewData mapData, Transform parent, List<StageNodeDropTarget> targets)
        {
            if (targets == null) return;
            foreach (KeyValuePair<int, NodeAppearance> pair in _appearances)
            {
                NodeAppearance appearance = pair.Value;
                if (_sourceNodes[appearance.SourceIndex].IsStart || pair.Key == _completionNodeId || appearance.Visual == null) continue;
                CollectDropTarget(pair.Key, appearance.Visual.transform, _roots[pair.Key], parent,
                    mapData.BuildingHeight, targets);
            }
        }

        /// <summary>현재/완료 정책을 적용한 뒤 실제 표시 대상만 남깁니다.</summary>
        internal void FinalizeDropTargets(List<StageNodeDropTarget> targets)
        {
            if (targets == null) return;
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                StageNodeDropTarget target = targets[i];
                GameObject visible = GetVisibleBuilding(target.NodeId);
                if (visible != null && visible.transform == target.Visual) continue;
                target.Restore();
                targets.RemoveAt(i);
            }
        }

        private static void CollectDropTarget(int nodeId, Transform visual, Transform root, Transform parent,
            float buildingHeight, List<StageNodeDropTarget> targets)
        {
            var target = new StageNodeDropTarget(nodeId, visual,
                new Pose(root.TransformPoint(0f, buildingHeight, 0f), parent.rotation));
            targets.Add(target);
            target.Hide();
        }

        private bool ApplyAppearance(NodeAppearance appearance, bool isCurrent, long version)
        {
            if (_appearanceVersion != version) return false;
            if (appearance.Visual == null) return true;
            // 완료 상태를 별도로 저장하지 않고 Show에서 받은 Buffer 목록의 최신 값을 읽습니다.
            StageProgressData node = _sourceNodes[appearance.SourceIndex];
            // 실제 노드는 클리어 전까지 현재 자리에서도 유지합니다. 시작 장식의 기존 숨김 정책은 보존합니다.
            bool isCleared = !node.IsStart && node.IsClear;
            bool buildingShown = node.IsStart ? !isCurrent : !isCleared;
            if (!node.IsStart && node.NodeId == _completionNodeId) buildingShown = _completionBuildingShown;
            if (appearance.Visual.activeSelf != buildingShown) appearance.Visual.SetActive(buildingShown);
            if (_appearanceVersion != version) return false;
            // 건물이 숨은 동안에는 누를 곳이 없습니다. 그 자리의 누름은 배경 끌기가 됩니다.
            // 시작 노드는 건물이 보여도 집지 않습니다(돌아갈 수 없는 노드, 평가 C-27). "시작 노드는 집지 않는다"의 원본은 여기 하나입니다.
            appearance.Hit.enabled = buildingShown && !node.IsStart;
            return true;
        }

        /// <summary>완료 연출이 소유한 임시 표시만 적용합니다. 클리어 사실은 계속 Buffer에서 읽습니다.</summary>
        internal void SetCompletionBuilding(int nodeId, bool shown)
        {
            int previous = _completionNodeId;
            _completionNodeId = nodeId;
            _completionBuildingShown = shown;
            long version = ++_appearanceVersion;
            if (_appearances.TryGetValue(previous, out NodeAppearance old) &&
                !ApplyAppearance(old, previous == _currentNodeId, version)) return;
            if (nodeId != previous && _appearances.TryGetValue(nodeId, out NodeAppearance current))
                ApplyAppearance(current, nodeId == _currentNodeId, version);
        }

        internal Pose GetBreakPose(int nodeId, out EffectType effect)
        {
            if (!_appearances.TryGetValue(nodeId, out NodeAppearance appearance) || appearance.Visual == null ||
                _sourceNodes[appearance.SourceIndex].IsStart || appearance.BreakEffect == EffectType.Empty)
                throw new InvalidOperationException($"[StageNodeView] 파괴할 실제 건물과 효과가 필요합니다: {nodeId}");
            effect = appearance.BreakEffect;
            Transform building = appearance.Visual.transform;
            return new Pose(building.position, building.rotation);
        }

        internal bool TryGetNode(int nodeId, out Transform nodeRoot)
        {
            if (_roots.TryGetValue(nodeId, out nodeRoot) && nodeRoot != null)
                return true;
            nodeRoot = null;
            return false;
        }

        /// <summary>새 표시 준비에 사용할 자리입니다. 진행 사실은 현재 Buffer 목록에서만 읽습니다.</summary>
        internal StageMarkerLanding GetRestingLanding(int nodeId)
        {
            if (!_appearances.TryGetValue(nodeId, out NodeAppearance appearance))
                throw new InvalidOperationException($"[StageNodeView] 표시된 노드가 없습니다: {nodeId}");
            StageProgressData node = _sourceNodes[appearance.SourceIndex];
            return node.IsStart || node.IsClear ? StageMarkerLanding.Center : StageMarkerLanding.Front;
        }

        /// <summary>이동은 지정한 출발 노드, 복원(0)은 직전 깊이의 유일한 클리어/시작 연결 노드를 사용합니다.</summary>
        internal int GetEntryOriginNodeId(int nodeId, int originNodeId)
        {
            if (!_appearances.TryGetValue(nodeId, out NodeAppearance appearance))
                throw new InvalidOperationException($"[StageNodeView] 진입선을 찾을 목적지 노드가 없습니다: {nodeId}");
            StageProgressData target = _sourceNodes[appearance.SourceIndex];
            int found = 0;
            for (int index = 0; index < _sourceNodes.Count; index++)
            {
                StageProgressData origin = _sourceNodes[index];
                if (origin.Depth != target.Depth - 1) continue;
                if (originNodeId != 0 && origin.NodeId != originNodeId) continue;
                if (originNodeId == 0 && !origin.IsStart && !origin.IsClear) continue;
                foreach (int next in origin.NextNodeIds)
                {
                    if (next != nodeId) continue;
                    if (found != 0)
                        throw new InvalidOperationException($"[StageNodeView] 노드 {nodeId}의 진입선을 하나로 복원할 수 없습니다: {found}, {origin.NodeId}");
                    found = origin.NodeId;
                }
            }
            if (found == 0)
                throw new InvalidOperationException($"[StageNodeView] 노드 {nodeId}의 진입선이 없습니다(지정 출발: {originNodeId}).");
            return found;
        }

        /// <summary>이미 계산한 클릭 상자의 모서리를 맵 로컬로 변환합니다. Renderer를 다시 검색하지 않습니다.</summary>
        internal void FillBuildingCorners(int nodeId, Transform mapRoot, Vector3[] corners)
        {
            if (mapRoot == null) throw new ArgumentNullException(nameof(mapRoot));
            if (corners == null || corners.Length != 8) throw new ArgumentException("모서리 8개가 필요합니다.", nameof(corners));
            if (!_appearances.TryGetValue(nodeId, out NodeAppearance appearance) || appearance.Hit == null ||
                appearance.Visual == null || _sourceNodes[appearance.SourceIndex].IsStart)
                throw new InvalidOperationException($"[StageNodeView] 앞쪽 계산에 실제 건물과 클릭 상자가 필요합니다: {nodeId}");
            BoxCollider hit = appearance.Hit;
            Vector3 min = hit.center - hit.size * 0.5f;
            Vector3 max = hit.center + hit.size * 0.5f;
            for (int index = 0; index < corners.Length; index++)
            {
                Vector3 corner = new((index & 1) == 0 ? min.x : max.x,
                    (index & 2) == 0 ? min.y : max.y, (index & 4) == 0 ? min.z : max.z);
                corners[index] = mapRoot.InverseTransformPoint(hit.transform.TransformPoint(corner));
            }
        }

        /// <summary>현재 표시 중인 건물만 빌려줍니다. 시작·현재 위치·클리어 상태를 다시 계산하지 않습니다.</summary>
        internal GameObject GetVisibleBuilding(int nodeId)
        {
            if (!_appearances.TryGetValue(nodeId, out NodeAppearance appearance) ||
                appearance.Hit == null || !appearance.Hit.enabled || appearance.Visual == null ||
                !appearance.Visual.activeInHierarchy) return null;
            return appearance.Visual;
        }

        /// <summary>재사용 목록에 현재 보이는 건물 ID를 담습니다. 매 프레임 새 목록이나 열거자를 만들지 않습니다.</summary>
        internal void FillVisibleNodeIds(List<int> nodeIds)
        {
            nodeIds.Clear();
            foreach (int nodeId in _appearances.Keys)
                if (GetVisibleBuilding(nodeId) != null) nodeIds.Add(nodeId);
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
            internal readonly EffectType BreakEffect;

            internal NodeAppearance(int sourceIndex, GameObject visual, BoxCollider hit, EffectType breakEffect)
            {
                SourceIndex = sourceIndex;
                Visual = visual;
                Hit = hit;
                BreakEffect = breakEffect;
            }
        }
    }
}
