using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Entry;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Completion;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.SelectOutlineSystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>전달받은 데이터로 맵 표시와 해제 순서를 조율합니다. Unity 메인 스레드에서 사용합니다.</summary>
    internal sealed class StageMapViewDisplay
    {
        private readonly IColorSystemService _colors;
        private readonly StageNodeOutlineView _outline;
        private readonly StageMarkerPlacement _placement;
        private readonly Vector3[] _buildingCorners = new Vector3[8];

        internal StageMapViewDisplay(IColorSystemService colors, StageMarkerPlacement placement)
        {
            _colors = colors ?? throw new ArgumentNullException(nameof(colors));
            _placement = placement ?? throw new ArgumentNullException(nameof(placement));
        }

        internal StageMapViewDisplay(IColorSystemService colors, ISelectOutlineSystemService outlines,
            StageMarkerPlacement placement) : this(colors, placement)
        {
            _outline = new StageNodeOutlineView(outlines);
        }

        internal bool HasVisibleBuilding(int nodeId) => _nodes.GetVisibleBuilding(nodeId) != null;

        internal void FillVisibleNodeIds(List<int> nodeIds) => _nodes.FillVisibleNodeIds(nodeIds);

        internal void CollectLinkDrawTargets(List<IStageLinkDrawTarget> targets) => _links.CollectDrawTargets(targets);

        internal void TickHover(int nodeId, HashSet<int> availableNodes, StageNodeHoverStyleData style, float deltaTime)
        {
            if (_outline == null)
                throw new InvalidOperationException("[StageMapViewDisplay] 호버에는 SelectOutline 서비스가 필요합니다.");
            GameObject visual = _movingToNodeId == 0 ? _nodes.GetVisibleBuilding(nodeId) : null;
            _outline.Tick(_nodes, availableNodes, visual != null ? nodeId : 0, _movingToNodeId == 0, style, deltaTime);
            _links.TickHover(CurrentNodeId, visual != null && availableNodes.Contains(nodeId) ? nodeId : 0, style, deltaTime);
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
        private Transform _mapRoot;
        private StageMarkerLanding _currentLanding = StageMarkerLanding.Center;
        private StageMarkerLanding _movingLanding;
        private Vector3 _movingDestination;
        private Vector3 _movingLandingOffset;
        private long _generation;
        private bool _hasCompletionBuilding;
        private StageNodeClearKey _completionKey;
        private long _completionVersion;
        private bool _completionBuildingShown;

        internal int CurrentNodeId => _nodes.CurrentNodeId;
        internal StageMarkerLanding CurrentLanding => _currentLanding;

        internal void HoldCompletionBuilding(StageNodeClearKey key, long preparationVersion)
        {
            _completionKey = key;
            _completionVersion = preparationVersion;
            _hasCompletionBuilding = true;
            _completionBuildingShown = true;
            _nodes.SetCompletionBuilding(key.NodeId, true);
        }

        private bool OwnsCompletionBuilding(StageNodeClearKey key, long version) =>
            _hasCompletionBuilding && _completionKey == key && _completionVersion == version;

        internal StageNodeClearTarget GetCompletionTarget(StageNodeClearKey key, long version)
        {
            if (!OwnsCompletionBuilding(key, version) || _mapRoot == null)
                throw new InvalidOperationException("[StageMapViewDisplay] 준비된 파괴 대상의 표시 문맥이 없습니다.");
            StageMarkerPlacement.ValidateMapRoot(_mapRoot);
            Pose pose = _nodes.GetBreakPose(key.NodeId, out EffectType effect);
            return new StageNodeClearTarget(key, effect, pose, _mapRoot, version);
        }

        internal void HideCompletionBuilding(StageNodeClearKey key, long version)
        {
            if (!OwnsCompletionBuilding(key, version))
                throw new InvalidOperationException("[StageMapViewDisplay] 파괴 대상의 표시 소유권이 다릅니다.");
            long generation = _generation;
            _outline?.Clear();
            if (_generation != generation || !OwnsCompletionBuilding(key, version)) return;
            _completionBuildingShown = false;
            _nodes.SetCompletionBuilding(key.NodeId, false);
        }

        internal void ReleaseCompletionBuilding(StageNodeClearKey key, long version)
        {
            if (!OwnsCompletionBuilding(key, version)) return;
            _hasCompletionBuilding = false;
            _nodes.SetCompletionBuilding(0, false);
        }

        /// <summary>작은 점프의 표시만 정리합니다. Core 진입 결과나 이동 완료를 게시하지 않습니다.</summary>
        internal void CancelSettleMove(int nodeId)
        {
            if (_movingToNodeId != nodeId || _movingLanding != StageMarkerLanding.Center) return;
            _movingToNodeId = 0;
            PlaceCurrentMarker(nodeId, StageMarkerLanding.Center);
        }

        internal Transform BeginMarkerMove(int targetNodeId, Transform movementParent, StageMarkerLanding landing)
        {
            if (_movingToNodeId != 0 || CurrentNodeId == 0 || !_nodes.TryGetNode(targetNodeId, out Transform target))
                throw new InvalidOperationException("[StageMapViewDisplay] 현재 마커와 표시된 단일 목적지가 필요합니다.");
            int originNodeId = targetNodeId != CurrentNodeId ? CurrentNodeId : 0;
            Vector3 destination = CalculateMarkerDestination(targetNodeId, landing, originNodeId);
            Vector3 landingOffset = target.InverseTransformPoint(_mapRoot.TransformPoint(destination));
            Transform root = _marker.BeginMove(movementParent, target);
            _movingLanding = landing;
            _movingDestination = destination;
            _movingLandingOffset = landingOffset;
            _movingToNodeId = targetNodeId;
            return root;
        }

        internal void CompleteMarkerMove(int targetNodeId)
        {
            if (_movingToNodeId != targetNodeId || !_nodes.TryGetNode(targetNodeId, out Transform target))
                throw new InvalidOperationException("[StageMapViewDisplay] 이동 목적지가 일치하지 않습니다.");
            long generation = _generation;
            _marker.CompleteMove(target, _movingLandingOffset);
            _currentLanding = _movingLanding;
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
            Func<StageProgressData, StageNodeVisualData> findVisual, Transform parent, int currentNodeId,
            List<StageNodeDropTarget> dropTargets = null)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            if (mapData == null) throw new ArgumentNullException(nameof(mapData));
            if (findVisual == null) throw new ArgumentNullException(nameof(findVisual));
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            StageMarkerPlacement.ValidateMapRoot(parent);
            if (!float.IsFinite(mapData.NodeScale) || Mathf.Abs(mapData.NodeScale - 1f) > 0.001f)
                throw new InvalidOperationException("[StageMapViewDisplay] 건물 앞 이동은 NodeScale 1만 지원합니다.");

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
                try
                {
                    _nodes.CollectDropTargets(mapData, parent, dropTargets);
                    _nodes.Refresh(nodes);
                    if (_generation != movingGeneration) RestoreDropTargets(dropTargets);
                    else _nodes.FinalizeDropTargets(dropTargets);
                }
                catch
                {
                    RestoreDropTargets(dropTargets);
                    throw;
                }
                return;
            }

            long showGeneration = _generation;
            bool dropPrepared = false;
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
                        _nodes.CollectDropTargets(mapData, parent, dropTargets);
                        _nodes.Refresh(nodes);
                        if (_generation != refreshGeneration) return;
                        SetCurrentNode(currentNodeId);
                        if (_generation != refreshGeneration) return;
                        _nodes.FinalizeDropTargets(dropTargets);
                        dropPrepared = true;
                        return;
                    }
                }

                long generation = _generation + 1;
                Clear(true);
                if (_generation != generation) return;
                showGeneration = generation;
                _mapRoot = parent;
                if (_hasCompletionBuilding)
                    _nodes.SetCompletionBuilding(_completionKey.NodeId, _completionBuildingShown);
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
                }, parent, dropTargets);
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
                _nodes.FinalizeDropTargets(dropTargets);
                dropPrepared = true;
            }
            catch
            {
                // 옛 조회/활성화 콜백의 실패가 새 표시까지 지우지 않습니다.
                if (_generation == showGeneration) Clear();
                throw;
            }
            finally
            {
                if (!dropPrepared) RestoreDropTargets(dropTargets);
            }
        }

        private static void RestoreDropTargets(List<StageNodeDropTarget> targets)
        {
            if (targets == null) return;
            foreach (StageNodeDropTarget target in targets) target.Restore();
            targets.Clear();
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
            PlaceCurrentMarker(nodeId, nodeId == 0 ? StageMarkerLanding.Center : GetRestingLanding(nodeId));
        }

        internal StageMarkerLanding GetRestingLanding(int nodeId) =>
            _hasCompletionBuilding && _completionKey.NodeId == nodeId && _completionBuildingShown
                ? StageMarkerLanding.Front : _nodes.GetRestingLanding(nodeId);

        /// <summary>준비·취소 복원 전용입니다. 같은 노드라도 지정한 자리에 다시 놓습니다.</summary>
        internal void PlaceCurrentMarker(int nodeId, StageMarkerLanding landing)
        {
            if (_movingToNodeId != 0)
                throw new InvalidOperationException("[StageMapViewDisplay] 이동을 정리한 뒤 마커를 배치해야 합니다.");
            long generation = _generation;
            if (nodeId == 0) _marker.Hide();
            else
            {
                Vector3 destination = GetMarkerDestination(nodeId, landing);
                if (!_nodes.TryGetNode(nodeId, out Transform nodeRoot))
                    throw new InvalidOperationException($"[StageMapViewDisplay] 현재 노드가 없습니다: {nodeId}");
                Vector3 offset = nodeRoot.InverseTransformPoint(_mapRoot.TransformPoint(destination));
                _marker.ShowAt(_markerPrefab, nodeRoot, _markerOffset, _markerYaw, _markerScale, offset);
            }
            if (_generation != generation) return;
            _currentLanding = landing;
            _nodes.SetCurrentNode(nodeId);
        }

        /// <summary>맵 로컬 바닥점입니다. 이동 중인 목적지는 시작 때 계산한 값을 유지합니다.</summary>
        internal Vector3 GetMarkerDestination(int nodeId, StageMarkerLanding landing)
        {
            if (_movingToNodeId != 0 && _movingToNodeId == nodeId && _movingLanding == landing) return _movingDestination;
            return CalculateMarkerDestination(nodeId, landing, 0);
        }

        private Vector3 CalculateMarkerDestination(int nodeId, StageMarkerLanding landing, int originNodeId)
        {
            if (landing != StageMarkerLanding.Front && landing != StageMarkerLanding.Center)
                throw new ArgumentOutOfRangeException(nameof(landing));
            if (_mapRoot == null || !_nodes.TryGetNode(nodeId, out Transform nodeRoot))
                throw new InvalidOperationException($"[StageMapViewDisplay] 마커 목적지 노드가 없습니다: {nodeId}");
            if (landing == StageMarkerLanding.Center) return _mapRoot.InverseTransformPoint(nodeRoot.position);
            int origin = _nodes.GetEntryOriginNodeId(nodeId, originNodeId);
            if (!_nodes.TryGetNode(origin, out Transform originRoot))
                throw new InvalidOperationException($"[StageMapViewDisplay] 진입선 출발 노드가 없습니다: {origin}");
            _nodes.FillBuildingCorners(nodeId, _mapRoot, _buildingCorners);
            return _placement.GetFront(_mapRoot, nodeRoot.position, originRoot.position, _buildingCorners);
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
        internal void Clear() => Clear(false);

        private void Clear(bool preserveCompletion)
        {
            ++_generation;
            // Outline Scope가 노드의 자식이므로 원본 노드를 파괴하기 전에 공용 풀로 반환합니다.
            _outline?.Clear();
            StageCurrentMarkerView marker = _marker;
            StageLinkView links = _links;
            StageNodeView nodes = _nodes;
            _marker = new StageCurrentMarkerView();
            _links = new StageLinkView();
            _nodes = new StageNodeView();
            _movingToNodeId = 0;
            _mapRoot = null;
            _currentLanding = StageMarkerLanding.Center;
            _movingLanding = StageMarkerLanding.Center;
            _movingDestination = default;
            _movingLandingOffset = default;
            _markerPrefab = null;
            _markerOffset = default;
            _markerYaw = 0f;
            _markerScale = 0f;
            _identity = null;
            if (!preserveCompletion) _hasCompletionBuilding = false;
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
            // 맵 표시 설정은 DB가 만든 불변 Data이고 System이 한 번 받아 연결마다 같은 참조를 넘깁니다.
            // 칸을 하나씩 베껴 비교하지 않고 참조로 비교합니다. 설정이 바뀌면 DB를 다시 만들어 참조가 바뀝니다(평가 N-16).
            private readonly StageMapViewData _data;
            private readonly Dictionary<int, NodeIdentity> _nodes = new();

            internal DisplayIdentity(int mapKey, IReadOnlyList<StageProgressData> nodes, StageMapViewData data,
                Transform parent)
            {
                _mapKey = mapKey;
                _parent = parent;
                _parentRotation = parent.rotation;
                _parentScale = parent.lossyScale;
                _data = data;

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
                    !ReferenceEquals(_data, other._data) || _nodes.Count != other._nodes.Count)
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
            private EffectType _breakEffect;

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
                _breakEffect = visual?.BreakEffect ?? EffectType.Empty;
            }

            internal bool Matches(NodeIdentity other, bool compareVisuals)
            {
                if (_stageKey != other._stageKey || _stageType != other._stageType || _mapRole != other._mapRole ||
                    _depth != other._depth || _widthIndex != other._widthIndex || _targets.Length != other._targets.Length)
                    return false;
                if (compareVisuals && (_visualPrefab != other._visualPrefab || !_rotationOffset.Equals(other._rotationOffset) ||
                    _breakEffect != other._breakEffect)) return false;
                for (int index = 0; index < _targets.Length; index++)
                    if (_targets[index] != other._targets[index]) return false;
                return true;
            }
        }
    }
}
