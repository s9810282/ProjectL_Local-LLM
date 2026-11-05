using System;
using System.Collections.Generic;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>원본 계층을 바꾸지 않고 구역과 완성 자세를 수집합니다.</summary>
    internal sealed class MapRevealPartitionPlan
    {
        internal sealed class Item
        {
            internal Transform Body { get; }
            internal Vector3 Position { get; }
            internal Quaternion Rotation { get; }
            internal Vector3 Scale { get; }

            internal Item(Transform body, Vector3 position)
            {
                Body = body;
                Position = position;
                Rotation = body.localRotation;
                Scale = body.localScale;
            }
        }

        internal sealed class Area
        {
            private readonly List<Item> _items = new List<Item>();
            internal Vector2Int Index { get; }
            internal string Name => $"{MapRevealAreaLayout.AreaPrefix}X{Index.x}_Z{Index.y}";
            internal Vector3 Center { get; }
            internal IReadOnlyList<Item> Items => _items;

            internal Area(Vector2Int index, float size)
            {
                Index = index;
                Center = new Vector3((float)((index.x + 0.5d) * size), 0,
                    (float)((index.y + 0.5d) * size));
                RequireFinite(Center);
            }

            internal void Add(Item item) => _items.Add(item);
        }

        private sealed class Snapshot
        {
            private readonly Transform _body;
            private readonly Transform _parent;
            private readonly Vector3 _position;
            private readonly Quaternion _rotation;
            private readonly Vector3 _scale;
            private readonly string _name;
            private readonly bool _active;

            internal Snapshot(Transform body)
            {
                _body = body;
                _parent = body.parent;
                _position = body.localPosition;
                _rotation = body.localRotation;
                _scale = body.localScale;
                _name = body.name;
                _active = body.gameObject.activeSelf;
            }

            internal bool SameAs(Snapshot other) => _body == other._body && _parent == other._parent
                && _position.Equals(other._position) && _rotation.Equals(other._rotation)
                && _scale.Equals(other._scale) && _name == other._name && _active == other._active;
        }

        private readonly List<Area> _areas = new List<Area>();
        private readonly List<Transform> _previousAreas = new List<Transform>();
        private readonly List<Transform> _excluded = new List<Transform>();
        private readonly List<Item> _retained = new List<Item>();
        private readonly List<Snapshot> _snapshot = new List<Snapshot>();
        private readonly HashSet<string> _containerNames;
        private readonly Dictionary<Vector2Int, Area> _byIndex = new Dictionary<Vector2Int, Area>();
        private readonly Matrix4x4 _rootMatrix;

        internal Transform Root { get; }
        internal float Size { get; }
        internal string ContainerNames { get; }
        internal IReadOnlyList<Area> Areas => _areas;
        internal IReadOnlyList<Transform> PreviousAreas => _previousAreas;
        internal IReadOnlyList<Transform> Excluded => _excluded;
        internal IReadOnlyList<Item> Retained => _retained;
        internal int ItemCount { get; private set; }

        private MapRevealPartitionPlan(Transform root, float size, string containerNames)
        {
            Root = root;
            Size = size;
            ContainerNames = containerNames ?? string.Empty;
            _rootMatrix = root.localToWorldMatrix;
            _containerNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (string name in ContainerNames.Split(','))
                if (!string.IsNullOrWhiteSpace(name)) _containerNames.Add(name.Trim());
        }

        internal static MapRevealPartitionPlan Create(Transform root, float size, string containerNames)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (!IsFinite(size) || size < 0.1f)
                throw new ArgumentException("구역 크기는 0.1 이상의 유한한 수여야 합니다.", nameof(size));
            if (PrefabUtility.IsPartOfPrefabInstance(root.gameObject))
                throw new InvalidOperationException("배경 루트가 중첩 프리팹 안에 있습니다. 해당 맵 원본을 Prefab Mode에서 여세요.");
            RequireFinite(root.lossyScale);
            if (root.lossyScale.x == 0 || root.lossyScale.y == 0 || root.lossyScale.z == 0)
                throw new InvalidOperationException("배경 루트 또는 상위 오브젝트의 크기에 0을 사용할 수 없습니다.");

            var plan = new MapRevealPartitionPlan(root, size, containerNames);
            var areaNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (Transform child in root)
            {
                bool area = child.name.StartsWith(MapRevealAreaLayout.AreaPrefix, StringComparison.Ordinal);
                if (area)
                {
                    if (!areaNames.Add(child.name))
                        throw new InvalidOperationException($"기존 구역 이름이 중복됩니다: {child.name}");
                    RequireContainer(child);
                    if (ContainsCameraOrLight(child))
                        throw new InvalidOperationException($"기존 구역 안의 카메라/조명을 먼저 구역 밖으로 옮겨 주세요: {child.name}");
                    plan._previousAreas.Add(child);
                }
                plan.Collect(child, Vector3.zero, area, false);
            }
            plan._areas.Sort((a, b) => a.Index.y != b.Index.y
                ? a.Index.y.CompareTo(b.Index.y) : a.Index.x.CompareTo(b.Index.x));
            foreach (Transform body in root.GetComponentsInChildren<Transform>(true))
                plan._snapshot.Add(new Snapshot(body));
            return plan;
        }

        private void Collect(Transform body, Vector3 offset, bool previousArea, bool insideArea)
        {
            Vector3 position = offset + body.localPosition;
            RequireFinite(position);
            bool instance = PrefabUtility.IsPartOfPrefabInstance(body.gameObject);
            if (instance && !PrefabUtility.IsOutermostPrefabInstanceRoot(body.gameObject))
                throw new InvalidOperationException($"중첩 프리팹의 내부 오브젝트는 재배치할 수 없습니다: {body.name}");
            // 중첩 프리팹 루트는 이름이 분류용 부모와 같아도 내부를 풀지 않습니다.
            if (previousArea || (!instance && _containerNames.Contains(body.name)))
            {
                RequireContainer(body);
                // 이전 구역 안의 분류용 부모는 구역 밖으로 보존해 빈 구역을 안전하게 제거합니다.
                if (!previousArea && insideArea) _retained.Add(new Item(body, position));
                foreach (Transform child in body) Collect(child, position, false, previousArea);
                return;
            }
            if (ContainsCameraOrLight(body))
            {
                _excluded.Add(body);
                return;
            }
            // 시각 요소 없는 보조 오브젝트는 보존합니다. 이전 구역 안의 것은 배경 루트에 남깁니다.
            if (body.GetComponentInChildren<Renderer>(true) == null
                && body.GetComponentInChildren<Terrain>(true) == null)
            {
                if (insideArea) _retained.Add(new Item(body, position));
                return;
            }
            RequireFinite(body.localScale);
            var index = new Vector2Int(Index(position.x), Index(position.z));
            if (!_byIndex.TryGetValue(index, out Area area))
            {
                area = new Area(index, Size);
                _byIndex.Add(index, area);
                _areas.Add(area);
            }
            area.Add(new Item(body, position));
            ItemCount++;
        }

        internal bool SameAs(MapRevealPartitionPlan other)
        {
            if (Root != other.Root || Size != other.Size || ContainerNames != other.ContainerNames
                || !_rootMatrix.Equals(other._rootMatrix) || _snapshot.Count != other._snapshot.Count
                || ItemCount != other.ItemCount || _areas.Count != other._areas.Count
                || _excluded.Count != other._excluded.Count || _retained.Count != other._retained.Count) return false;
            for (int i = 0; i < _snapshot.Count; i++)
                if (!_snapshot[i].SameAs(other._snapshot[i])) return false;
            for (int i = 0; i < _areas.Count; i++)
            {
                if (_areas[i].Index != other._areas[i].Index || _areas[i].Items.Count != other._areas[i].Items.Count)
                    return false;
                for (int j = 0; j < _areas[i].Items.Count; j++)
                    if (_areas[i].Items[j].Body != other._areas[i].Items[j].Body) return false;
            }
            return true;
        }

        private int Index(float coordinate)
        {
            double value = Math.Floor((double)coordinate / Size);
            if (value < int.MinValue || value > int.MaxValue)
                throw new InvalidOperationException("좌표에 비해 구역 크기가 너무 작습니다.");
            return (int)value;
        }

        private static void RequireContainer(Transform body)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(body.gameObject) || !body.gameObject.activeSelf
                || body.GetComponents<Component>().Length != 1
                || !body.localRotation.Equals(Quaternion.identity) || !body.localScale.Equals(Vector3.one))
                throw new InvalidOperationException($"풀어낼 부모는 활성 상태이고 Transform만 있어야 하며, 회전 0/크기 1이어야 합니다: {body.name}");
        }

        private static bool ContainsCameraOrLight(Transform body) =>
            body.GetComponentInChildren<Camera>(true) != null || body.GetComponentInChildren<Light>(true) != null;

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static void RequireFinite(Vector3 value)
        {
            if (!IsFinite(value.x) || !IsFinite(value.y) || !IsFinite(value.z))
                throw new InvalidOperationException("구역과 소품 위치/크기는 유한한 수여야 합니다.");
        }
    }
}
