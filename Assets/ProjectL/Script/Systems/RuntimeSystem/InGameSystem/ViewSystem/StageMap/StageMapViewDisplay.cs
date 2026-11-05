using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>전달받은 데이터로 맵 표시와 해제 순서를 조율합니다. Unity 메인 스레드에서 사용합니다.</summary>
    internal sealed class StageMapViewDisplay
    {
        private readonly IColorSystemService _colors;

        internal StageMapViewDisplay(IColorSystemService colors)
        {
            _colors = colors ?? throw new ArgumentNullException(nameof(colors));
        }

        private StageNodeView _nodes = new();
        private StageLinkView _links = new();
        private StageCurrentMarkerView _marker = new();
        private GameObject _markerPrefab;
        private Vector3 _markerOffset;
        private float _markerYaw;
        private float _markerScale;
        private DisplayIdentity _identity;
        private int _movingToNodeId;
        private long _generation;

        internal int CurrentNodeId => _nodes.CurrentNodeId;

        internal Transform BeginMarkerMove(int targetNodeId, Transform movementParent)
        {
            if (_movingToNodeId != 0 || CurrentNodeId == 0 || !_nodes.TryGetNode(targetNodeId, out Transform target))
                throw new InvalidOperationException("[StageMapViewDisplay] 현재 마커와 표시된 단일 목적지가 필요합니다.");
            Transform root = _marker.BeginMove(movementParent, target);
            _movingToNodeId = targetNodeId;
            return root;
        }

        internal void CompleteMarkerMove(int targetNodeId)
        {
            if (_movingToNodeId != targetNodeId || !_nodes.TryGetNode(targetNodeId, out Transform target))
                throw new InvalidOperationException("[StageMapViewDisplay] 이동 목적지가 일치하지 않습니다.");
            long generation = _generation;
            _marker.CompleteMove(target);
            _nodes.SetCurrentNode(targetNodeId);
            if (_generation != generation) return;
            _movingToNodeId = 0;
        }

        /// <summary>같은 표시를 갱신하거나 교체합니다. 외형 조회 함수는 기존 Table에 연결하며 null은 외형 미지정입니다.</summary>
        internal void Show(IReadOnlyList<StageProgressData> nodes, StageMapViewData mapData,
            Func<StageProgressData, StageNodeVisualData> findVisual, Transform parent)
        {
            Show(nodes, mapData, findVisual, parent, 0);
        }

        /// <summary>맵 키를 지정하지 않는 표시 호출입니다. 같은 표시의 완료 상태와 현재 위치를 다시 적용합니다.</summary>
        internal void Show(IReadOnlyList<StageProgressData> nodes, StageMapViewData mapData,
            Func<StageProgressData, StageNodeVisualData> findVisual, Transform parent, int currentNodeId)
        {
            Show(0, nodes, mapData, findVisual, parent, currentNodeId);
        }

        /// <summary>동일 맵·구조·표시 구성이면 인스턴스를 보존하며, 다른 표시에서는 소유 자원을 교체합니다.</summary>
        internal void Show(int mapKey, IReadOnlyList<StageProgressData> nodes, StageMapViewData mapData,
            Func<StageProgressData, StageNodeVisualData> findVisual, Transform parent, int currentNodeId)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            if (mapData == null) throw new ArgumentNullException(nameof(mapData));
            if (findVisual == null) throw new ArgumentNullException(nameof(findVisual));
            if (parent == null) throw new ArgumentNullException(nameof(parent));

            if (_movingToNodeId != 0)
            {
                // 수락된 표시의 구조 교체는 호출자가 이동/lease를 먼저 무효화해야 합니다.
                // 아래 일반 표시 실패의 Clear보다 먼저 진단해 소유 순서를 보존합니다.
                long movingGeneration = _generation;
                var movingIdentity = new DisplayIdentity(mapKey, nodes, mapData, parent);
                foreach (StageProgressData node in nodes)
                {
                    if (!node.IsStart) movingIdentity.SetVisual(node.NodeId, findVisual(node));
                    if (_generation != movingGeneration) return;
                }
                if (_identity == null || !_identity.Matches(movingIdentity, true))
                    throw new InvalidOperationException("[StageMapViewDisplay] 이동 중 맵 구조를 교체할 수 없습니다.");
                if (currentNodeId != CurrentNodeId && currentNodeId != _movingToNodeId)
                    throw new InvalidOperationException("[StageMapViewDisplay] 이동 문맥과 현재 노드가 다릅니다.");
                _nodes.Refresh(nodes);
                return;
            }

            long showGeneration = _generation;
            try
            {
                var identity = new DisplayIdentity(mapKey, nodes, mapData, parent);
                var visuals = new Dictionary<int, StageNodeVisualData>();
                if (_identity != null && _identity.Matches(identity, false))
                {
                    foreach (StageProgressData node in nodes)
                    {
                        if (node.IsStart) continue;
                        StageNodeVisualData visual = findVisual(node);
                        if (_generation != showGeneration) return;
                        visuals.Add(node.NodeId, visual);
                        identity.SetVisual(node.NodeId, visual);
                    }
                    if (_identity.Matches(identity, true))
                    {
                        long refreshGeneration = _generation;
                        _nodes.Refresh(nodes);
                        if (_generation != refreshGeneration) return;
                        SetCurrentNode(currentNodeId);
                        return;
                    }
                }

                long generation = _generation + 1;
                Clear();
                if (_generation != generation) return;
                showGeneration = generation;
                _nodes.Show(nodes, mapData, node =>
                {
                    // 첫 표시·구조 교체는 기존 노드 생성 순서에서 외형을 조회합니다.
                    if (!visuals.TryGetValue(node.NodeId, out StageNodeVisualData visual))
                    {
                        visual = findVisual(node);
                        visuals.Add(node.NodeId, visual);
                        identity.SetVisual(node.NodeId, visual);
                    }
                    return visual;
                }, parent);
                if (_generation != generation) return;
                _links.Show(nodes, mapData, _nodes, parent, _colors);
                if (_generation != generation) return;
                _markerPrefab = mapData.CurrentMarkerPrefab;
                _markerOffset = mapData.MarkerOffset;
                _markerYaw = mapData.MarkerYaw;
                _markerScale = mapData.MarkerScale;
                SetCurrentNode(currentNodeId);
                if (_generation != generation) return;
                _identity = identity;
            }
            catch
            {
                // 옛 조회/활성화 콜백의 실패가 새 표시까지 지우지 않습니다.
                if (_generation == showGeneration) Clear();
                throw;
            }
        }

        /// <summary>노드를 다시 만들지 않고 현재 마커와 이전·현재 노드 외형만 갱신합니다.</summary>
        internal void SetCurrentNode(int nodeId)
        {
            if (_movingToNodeId != 0)
            {
                if (nodeId == CurrentNodeId || nodeId == _movingToNodeId) return;
                throw new InvalidOperationException("[StageMapViewDisplay] 이동 중 다른 현재 노드를 적용할 수 없습니다.");
            }
            if (nodeId == _nodes.CurrentNodeId) return;
            long generation = _generation;

            if (nodeId == 0)
            {
                _marker.Hide();
            }
            else
            {
                if (!_nodes.TryGetNode(nodeId, out Transform nodeRoot))
                    throw new InvalidOperationException($"[StageMapViewDisplay] 표시 중인 현재 노드가 없습니다: {nodeId}");
                _marker.ShowAt(_markerPrefab, nodeRoot, _markerOffset, _markerYaw, _markerScale);
            }
            if (_generation != generation) return;
            _nodes.SetCurrentNode(nodeId);
        }

        /// <summary>표시 중인 노드의 Transform을 빌려줍니다. Clear 이후에는 보관해 사용하지 않습니다.</summary>
        internal bool TryGetNode(int nodeId, out Transform nodeRoot)
        {
            return _nodes.TryGetNode(nodeId, out nodeRoot);
        }

        /// <summary>표시 중인 노드의 건물에서 가장 가까운 클릭 대상을 찾습니다.</summary>
        internal bool TryPickNode(Ray ray, float pickDistance, out int nodeId)
        {
            return _nodes.TryPickNode(ray, pickDistance, out nodeId);
        }

        /// <summary>생성한 연결선과 노드만 정리합니다. 씬 이탈 때 호출해야 합니다.</summary>
        internal void Clear()
        {
            ++_generation;
            StageCurrentMarkerView marker = _marker;
            StageLinkView links = _links;
            StageNodeView nodes = _nodes;
            _marker = new StageCurrentMarkerView();
            _links = new StageLinkView();
            _nodes = new StageNodeView();
            _movingToNodeId = 0;
            _markerPrefab = null;
            _markerOffset = default;
            _markerYaw = 0f;
            _markerScale = 0f;
            _identity = null;
            // 이동 중 노드 밖에 있는 마커부터 해제합니다.
            marker.Detach();
            links.Clear();
            nodes.Clear();
        }

        /// <summary>마지막으로 표시한 값의 사본입니다. Buffer 목록과 완료 상태의 원본을 소유하지 않습니다.</summary>
        private sealed class DisplayIdentity
        {
            private readonly int _mapKey;
            private readonly Transform _parent;
            private readonly Quaternion _parentRotation;
            private readonly Vector3 _parentScale;
            private readonly GameObject _connectionPrefab;
            private readonly GameObject _markerPrefab;
            private readonly Vector3 _markerOffset;
            private readonly float _markerYaw;
            private readonly float _markerScale;
            private readonly float _widthSpacing;
            private readonly float _depthSpacing;
            private readonly float _nodeScale;
            private readonly float _buildingHeight;
            private readonly float _lineHeight;
            private readonly float _lineWidth;
            private readonly Color _lineColor;
            private readonly Dictionary<int, NodeIdentity> _nodes = new();

            internal DisplayIdentity(int mapKey, IReadOnlyList<StageProgressData> nodes, StageMapViewData data,
                Transform parent)
            {
                _mapKey = mapKey;
                _parent = parent;
                _parentRotation = parent.rotation;
                _parentScale = parent.lossyScale;
                _connectionPrefab = data.ConnectionPrefab;
                _markerPrefab = data.CurrentMarkerPrefab;
                _markerOffset = data.MarkerOffset;
                _markerYaw = data.MarkerYaw;
                _markerScale = data.MarkerScale;
                _widthSpacing = data.WidthSpacing;
                _depthSpacing = data.DepthSpacing;
                _nodeScale = data.NodeScale;
                _buildingHeight = data.BuildingHeight;
                _lineHeight = data.LineHeight;
                _lineWidth = data.LineWidth;
                _lineColor = data.LineColor;

                foreach (StageProgressData node in nodes)
                {
                    if (node.NodeId <= 0 || _nodes.ContainsKey(node.NodeId))
                        throw new InvalidOperationException($"[StageMapViewDisplay] 노드 ID가 유효하지 않거나 중복됩니다: {node.NodeId}");
                    _nodes.Add(node.NodeId, new NodeIdentity(node, data.StartVisualPrefab));
                }
                foreach (NodeIdentity node in _nodes.Values) node.ValidateTargets(_nodes);
            }

            internal void SetVisual(int nodeId, StageNodeVisualData visual)
            {
                _nodes[nodeId].SetVisual(visual);
            }

            internal bool Matches(DisplayIdentity other, bool compareVisuals)
            {
                if (_mapKey != other._mapKey || _parent != other._parent ||
                    !_parentRotation.Equals(other._parentRotation) || !_parentScale.Equals(other._parentScale) ||
                    _connectionPrefab != other._connectionPrefab || _markerPrefab != other._markerPrefab ||
                    !_markerOffset.Equals(other._markerOffset) || _markerYaw != other._markerYaw || _markerScale != other._markerScale ||
                    _widthSpacing != other._widthSpacing || _depthSpacing != other._depthSpacing ||
                    _nodeScale != other._nodeScale || _buildingHeight != other._buildingHeight ||
                    _lineHeight != other._lineHeight || _lineWidth != other._lineWidth ||
                    !_lineColor.Equals(other._lineColor) || _nodes.Count != other._nodes.Count)
                    return false;

                foreach (KeyValuePair<int, NodeIdentity> node in _nodes)
                {
                    if (!other._nodes.TryGetValue(node.Key, out NodeIdentity otherNode) ||
                        !node.Value.Matches(otherNode, compareVisuals))
                        return false;
                }
                return true;
            }
        }

        /// <summary>노드 ID별 구조와 조회한 외형 참조를 값으로 비교합니다.</summary>
        private sealed class NodeIdentity
        {
            private readonly int _nodeId;
            private readonly int _stageKey;
            private readonly StageType _stageType;
            private readonly StageMapRole _mapRole;
            private readonly int _depth;
            private readonly int _widthIndex;
            private readonly int[] _targets;
            private GameObject _visualPrefab;
            private Vector3 _rotationOffset;

            internal NodeIdentity(StageProgressData node, GameObject startVisual)
            {
                _nodeId = node.NodeId;
                _stageKey = node.StageKey;
                _stageType = node.StageType;
                _mapRole = node.MapRole;
                _depth = node.Depth;
                _widthIndex = node.WidthIndex;
                _targets = new int[node.NextNodeIds.Count];
                for (int index = 0; index < _targets.Length; index++) _targets[index] = node.NextNodeIds[index];
                Array.Sort(_targets);
                if (_stageKey == 0) _visualPrefab = startVisual;
            }

            internal void ValidateTargets(Dictionary<int, NodeIdentity> nodes)
            {
                for (int index = 0; index < _targets.Length; index++)
                {
                    int target = _targets[index];
                    if (target <= 0 || target == _nodeId || !nodes.ContainsKey(target) ||
                        (index > 0 && target == _targets[index - 1]))
                        throw new InvalidOperationException($"[StageMapViewDisplay] 연결 대상이 유효하지 않습니다: {_nodeId} → {target}");
                }
            }

            internal void SetVisual(StageNodeVisualData visual)
            {
                _visualPrefab = visual?.VisualPrefab;
                _rotationOffset = visual?.RotationOffset ?? Vector3.zero;
            }

            internal bool Matches(NodeIdentity other, bool compareVisuals)
            {
                if (_stageKey != other._stageKey || _stageType != other._stageType || _mapRole != other._mapRole ||
                    _depth != other._depth || _widthIndex != other._widthIndex || _targets.Length != other._targets.Length)
                    return false;
                if (compareVisuals && (_visualPrefab != other._visualPrefab || !_rotationOffset.Equals(other._rotationOffset))) return false;
                for (int index = 0; index < _targets.Length; index++)
                    if (_targets[index] != other._targets[index]) return false;
                return true;
            }
        }
    }
}
