using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Entry;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>노드 사이 연결선 인스턴스의 생성과 정리를 담당하는 일반 C# 클래스입니다.</summary>
    internal sealed class StageLinkView
    {
        private readonly List<GameObject> _instances = new();
        private readonly List<IColorHandle> _colorHandles = new();
        private readonly List<LinkHighlight> _highlights = new();
        private Color _baseColor;
        private float _baseWidth;

        internal void Show(IReadOnlyList<StageProgressData> nodes, StageMapViewData mapData,
            StageNodeView nodeView, Transform parent, IColorSystemService colors)
        {
            if (colors == null) throw new ArgumentNullException(nameof(colors));
            if (mapData.ConnectionPrefab == null)
                throw new InvalidOperationException("[StageLinkView] Connection prefab is required.");

            _baseColor = mapData.LineColor;
            _baseWidth = mapData.LineWidth;
            foreach (StageProgressData node in nodes)
            {
                if (!nodeView.TryGetNode(node.NodeId, out Transform source))
                    throw new InvalidOperationException($"[StageLinkView] Missing source: {node.NodeId}");

                foreach (int nextId in node.NextNodeIds)
                {
                    if (!nodeView.TryGetNode(nextId, out Transform target))
                        throw new InvalidOperationException($"[StageLinkView] Missing target: {nextId}");

                    Vector3 start = parent.InverseTransformPoint(source.position);
                    Vector3 end = parent.InverseTransformPoint(target.position);
                    Vector3 direction = end - start;
                    float length = direction.magnitude;
                    if (!float.IsFinite(length) || length <= 0.0001f)
                        throw new InvalidOperationException($"[StageLinkView] 연결선 길이가 유효하지 않습니다: {node.NodeId} → {nextId}");

                    GameObject instance = Object.Instantiate(mapData.ConnectionPrefab, parent, false);
                    _instances.Add(instance);
                    instance.name = $"StageLink_{node.NodeId}_{nextId}";
                    MeshRenderer renderer = instance.GetComponent<MeshRenderer>();
                    if (renderer == null)
                        throw new InvalidOperationException("[StageLinkView] 연결선 루트에 MeshRenderer가 필요합니다.");

                    // XY Quad의 세로축을 연결 방향으로 눕힙니다. Root의 회전·배율·이동을 그대로 따릅니다.
                    Transform link = instance.transform;
                    link.localPosition = (start + end) * 0.5f + Vector3.up * mapData.LineHeight;
                    link.localRotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
                    link.localScale = new Vector3(mapData.LineWidth, length, 1f);

                    IColorHandle color = colors.CreateColorHandle(renderer)
                        ?? throw new InvalidOperationException("[StageLinkView] 연결선 색상 핸들을 만들 수 없습니다.");
                    _colorHandles.Add(color);
                    color.SetOverrideData(0, MaterialBaseTypeKey.MainColor, mapData.LineColor);
                    _highlights.Add(new LinkHighlight(node.NodeId, nextId, link, renderer, color, _baseColor, _baseWidth));
                }
            }
        }

        /// <summary>호버 가능한 단일 연결만 강조합니다. 그 외 선은 원래 색상·폭으로 되돌립니다.</summary>
        internal void TickHover(int fromNodeId, int toNodeId, StageNodeHoverStyleData style, float deltaTime)
        {
            foreach (LinkHighlight link in _highlights)
            {
                bool selected = link.From == fromNodeId && link.To == toNodeId;
                link.Transition.SetTarget(selected ? style.ConnectionColor : _baseColor,
                    selected ? style.ConnectionWidth : _baseWidth, style.TransitionDuration);
                if (!link.Transition.Tick(deltaTime)) continue;
                link.Color.SetOverrideData(0, MaterialBaseTypeKey.MainColor, link.Transition.Color);
                Vector3 scale = link.Transform.localScale;
                scale.x = link.Transition.Width;
                link.Transform.localScale = scale;
            }
        }

        internal void CollectDrawTargets(List<IStageLinkDrawTarget> targets)
        {
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            foreach (LinkHighlight link in _highlights) targets.Add(link.Borrow());
        }

        internal void Clear()
        {
            List<Exception> errors = null;
            LinkHighlight[] links = _highlights.ToArray();
            _highlights.Clear();
            foreach (LinkHighlight link in links) CleanupErrors.Try(link.Close, ref errors);
            // Renderer가 살아 있는 동안 반환해야 ColorSystem의 등록도 해제됩니다(FW-011).
            foreach (IColorHandle color in _colorHandles) CleanupErrors.Try(color.Dispose, ref errors);
            _colorHandles.Clear();
            GameObject[] instances = _instances.ToArray();
            _instances.Clear();
            foreach (GameObject instance in instances)
            {
                if (instance == null) continue;
                CleanupErrors.Try(() => instance.SetActive(false), ref errors);
                CleanupErrors.Try(() => Object.Destroy(instance), ref errors);
            }
            CleanupErrors.ThrowIfAny(errors, "[StageLinkView] 연결선 자원 정리 실패.");
        }

        private sealed class LinkHighlight
        {
            internal readonly int From;
            internal readonly int To;
            internal readonly Transform Transform;
            internal readonly IColorHandle Color;
            internal readonly StageHighlightTransition Transition;
            private readonly MeshFilter _filter;
            private readonly MeshRenderer _renderer;
            private readonly Mesh _original;
            private readonly bool _enabled;
            private Mesh _partial;
            private Vector3[] _vertices;
            private Vector2[] _uv;
            private Vector3[] _originalVertices;
            private Vector2[] _originalUv;
            private readonly int[] _starts = new int[2];
            private readonly int[] _ends = new int[2];
            private long _generation;
            private bool _closed;

            internal LinkHighlight(int from, int to, Transform transform, MeshRenderer renderer, IColorHandle color,
                Color baseColor, float baseWidth)
            {
                From = from;
                To = to;
                Transform = transform;
                Color = color;
                Transition = new StageHighlightTransition(baseColor, baseWidth);
                _renderer = renderer;
                _filter = transform.GetComponent<MeshFilter>();
                if (_filter == null || _filter.sharedMesh == null)
                    throw Invalid("원본 MeshFilter가 없습니다");
                _original = _filter.sharedMesh;
                _enabled = renderer.enabled;
            }

            internal IStageLinkDrawTarget Borrow()
            {
                ++_generation;
                Restore();
                return new DrawTarget(this, _generation);
            }

            internal void Close()
            {
                _closed = true;
                ++_generation;
                Restore();
            }

            private void Prepare()
            {
                Restore();
                if (!_original.isReadable || _original.vertexCount != 4 || _original.subMeshCount != 1)
                    throw Invalid("읽기 가능한 4정점 Quad가 필요합니다");
                _originalVertices = _original.vertices;
                _originalUv = _original.uv;
                if (_originalUv.Length != 4) throw Invalid("정점별 UV가 필요합니다");
                for (int side = 0; side < 2; side++)
                {
                    _starts[side] = FindCorner(side == 0 ? -0.5f : 0.5f, -0.5f);
                    _ends[side] = FindCorner(side == 0 ? -0.5f : 0.5f, 0.5f);
                    Vector2 a = _originalUv[_starts[side]];
                    Vector2 b = _originalUv[_ends[side]];
                    if (!float.IsFinite(a.x) || !float.IsFinite(a.y) || !float.IsFinite(b.x) || !float.IsFinite(b.y) ||
                        b.x <= a.x || Mathf.Abs(a.y - b.y) > 0.0001f)
                        throw Invalid("길이 방향 UV.x 대응이 올바르지 않습니다");
                }
                if (Mathf.Abs(_originalUv[_starts[0]].x - _originalUv[_starts[1]].x) > 0.0001f ||
                    Mathf.Abs(_originalUv[_ends[0]].x - _originalUv[_ends[1]].x) > 0.0001f)
                    throw Invalid("양쪽 길이 방향 UV가 일치해야 합니다");
                ValidateTriangles();
                _vertices = (Vector3[])_originalVertices.Clone();
                _uv = (Vector2[])_originalUv.Clone();
                _partial = Object.Instantiate(_original);
                _partial.name = $"StageLinkReveal_{From}_{To}";
                _filter.sharedMesh = _partial;
                SetProgress(0f);
            }

            private int FindCorner(float x, float y)
            {
                int found = -1;
                for (int i = 0; i < _originalVertices.Length; i++)
                {
                    Vector3 value = _originalVertices[i];
                    if (!float.IsFinite(value.x) || !float.IsFinite(value.y) || !float.IsFinite(value.z))
                        throw Invalid("정점 좌표가 유한하지 않습니다");
                    if ((value - new Vector3(x, y, 0f)).sqrMagnitude > 0.000001f) continue;
                    if (found >= 0) throw Invalid("Quad 모서리 정점이 중복됩니다");
                    found = i;
                }
                if (found < 0) throw Invalid("중심 원점의 1×1 XY Quad가 필요합니다");
                return found;
            }

            private void ValidateTriangles()
            {
                int[] indices = _original.triangles;
                if (indices.Length != 6) throw Invalid("두 삼각형으로 된 Quad가 필요합니다");
                var common = new List<int>(2);
                for (int i = 0; i < 6; i++)
                {
                    if (indices[i] < 0 || indices[i] >= 4) throw Invalid("Quad 정점 인덱스가 잘못됐습니다");
                    int triangleStart = i < 3 ? 0 : 3;
                    for (int j = triangleStart; j < i; j++)
                        if (indices[i] == indices[j]) throw Invalid("퇴화한 삼각형입니다");
                }
                for (int i = 0; i < 3; i++)
                    for (int j = 3; j < 6; j++)
                        if (indices[i] == indices[j]) common.Add(indices[i]);
                if (common.Count != 2) throw Invalid("Quad 대각선을 공유해야 합니다");
                Vector3 diagonal = _originalVertices[common[0]] - _originalVertices[common[1]];
                if (Mathf.Abs(Mathf.Abs(diagonal.x) - 1f) > 0.0001f || Mathf.Abs(Mathf.Abs(diagonal.y) - 1f) > 0.0001f)
                    throw Invalid("삼각형의 공유 변이 대각선이어야 합니다");
            }

            private void SetProgress(float progress)
            {
                if (!float.IsFinite(progress)) throw Invalid("노출 진행률이 유한하지 않습니다");
                float p = Mathf.Clamp01(progress);
                if (p >= 1f)
                {
                    Restore();
                    return;
                }
                if (_partial == null || _filter == null || _renderer == null)
                    throw Invalid("준비된 연결선 표시가 없습니다");
                for (int side = 0; side < 2; side++)
                {
                    int start = _starts[side];
                    int end = _ends[side];
                    _vertices[end] = Vector3.LerpUnclamped(_originalVertices[start], _originalVertices[end], p);
                    _uv[end] = Vector2.LerpUnclamped(_originalUv[start], _originalUv[end], p);
                }
                _partial.vertices = _vertices;
                _partial.uv = _uv;
                _partial.bounds = _original.bounds;
                _renderer.enabled = p > 0f && _enabled;
            }

            private void Restore()
            {
                Mesh owned = _partial;
                _partial = null;
                List<Exception> errors = null;
                CleanupErrors.Try(() => { if (_filter != null) _filter.sharedMesh = _original; }, ref errors);
                CleanupErrors.Try(() => { if (owned != null) Object.Destroy(owned); }, ref errors);
                CleanupErrors.Try(() => { if (_renderer != null) _renderer.enabled = _enabled; }, ref errors);
                _vertices = null;
                _uv = null;
                _originalVertices = null;
                _originalUv = null;
                CleanupErrors.ThrowIfAny(errors, "[StageLinkView] 원본 연결선 복원 실패.");
            }

            private InvalidOperationException Invalid(string reason)
                => new($"[StageLinkView] {From} → {To}: {reason}.");

            private sealed class DrawTarget : IStageLinkDrawTarget
            {
                private readonly LinkHighlight _owner;
                private readonly long _generation;
                internal DrawTarget(LinkHighlight owner, long generation)
                {
                    _owner = owner;
                    _generation = generation;
                }
                public int FromNodeId => _owner.From;
                public int ToNodeId => _owner.To;
                private bool IsCurrent => !_owner._closed && _generation == _owner._generation;
                public void Prepare()
                {
                    if (IsCurrent) _owner.Prepare();
                }
                public void SetProgress(float progress)
                {
                    if (IsCurrent) _owner.SetProgress(progress);
                }
                public void Restore()
                {
                    if (IsCurrent) _owner.Restore();
                }
            }
        }
    }
}
