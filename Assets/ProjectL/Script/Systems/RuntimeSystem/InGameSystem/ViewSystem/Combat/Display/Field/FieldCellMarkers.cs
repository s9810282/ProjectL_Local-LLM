using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// 칸마다 프리팹 표지를 눕혀 놓습니다(낙석 View 계획 RP-18). 받은 칸만 그리고 그 칸이 왜 표시되는지는 모릅니다.
    /// 높이는 칸 강조 채우기·테두리와 같은 칸 표시 높이이고, 색은 프리팹 그대로입니다.
    /// </summary>
    /// <remarks>인스턴스는 가장 많이 쓴 수만큼 남겨 다시 쓰고, 해제할 때만 없앱니다. 다시 놓는 때는 <see cref="Show"/>뿐이며 매 프레임 하는 일은 없습니다.</remarks>
    internal sealed class FieldCellMarkers
    {
        private readonly Transform _parent;
        private readonly GameObject _prefab;
        private readonly float _size;
        private readonly float _lift;
        private readonly int _sortingOrder;
        private readonly List<GameObject> _markers = new();
        private Transform _root;

        /// <param name="parent">필드 칸 루트입니다. 크기는 이 루트의 칸 크기 기준입니다.</param>
        /// <param name="size">표지 크기입니다(칸 대비 비율).</param>
        /// <param name="lift">칸 표시 높이에서 띄우는 거리입니다.</param>
        /// <param name="sortingOrder">정렬 순서입니다. 값의 원본은 <see cref="FieldSortingOrders"/>입니다.</param>
        internal FieldCellMarkers(Transform parent, GameObject prefab, float size, float lift, int sortingOrder)
        {
            _parent = parent != null ? parent : throw new ArgumentNullException(nameof(parent));
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            _size = size;
            _lift = lift;
            _sortingOrder = sortingOrder;
        }

        /// <summary>칸마다 표지 하나를 놓고 켭니다. 남는 인스턴스는 끕니다. 빈 목록이면 모두 끈 것과 같습니다.</summary>
        internal void Show(IReadOnlyList<CellCoord> cells, FieldGridMapper mapper)
        {
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            while (_markers.Count < cells.Count)
                _markers.Add(CreateMarker());

            Transform prefab = _prefab.transform;
            Quaternion rotation = mapper.RotationToWorld(Rotation4.R0) * prefab.localRotation;
            Vector3 scale = Vector3.Scale(prefab.localScale,
                new Vector3(mapper.TotalWidth / mapper.ColumnCount * _size, 1f, mapper.TotalLength / mapper.RowCount * _size));
            for (int i = 0; i < cells.Count; i++)
            {
                Transform marker = _markers[i].transform;
                marker.SetPositionAndRotation(mapper.CellOverlayToWorld(cells[i]) + (mapper.WorldUp * _lift), rotation);
                marker.localScale = scale;
                _markers[i].SetActive(true);
            }

            for (int i = cells.Count; i < _markers.Count; i++)
                _markers[i].SetActive(false);
        }

        /// <summary>표지를 모두 끕니다. 여러 번 불려도 안전합니다.</summary>
        internal void Clear()
        {
            for (int i = 0; i < _markers.Count; i++)
                _markers[i].SetActive(false);
        }

        /// <summary>인스턴스를 모두 없앱니다. 여러 번 불려도 안전합니다.</summary>
        internal void Release()
        {
            _markers.Clear();
            if (_root == null)
                return;
            GameObject root = _root.gameObject;
            _root = null;
            root.SetActive(false);
            if (Application.isPlaying) Object.Destroy(root);
            else Object.DestroyImmediate(root);
        }

        private GameObject CreateMarker()
        {
            if (_root == null)
            {
                _root = new GameObject("FieldCellMarkers").transform;
                _root.SetParent(_parent, false);
            }

            GameObject marker = Object.Instantiate(_prefab, _root, false);
            marker.name = $"FieldCellMarker_{_markers.Count}";
            marker.SetActive(false);
            foreach (Renderer renderer in marker.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sortingOrder = _sortingOrder;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            return marker;
        }
    }
}
