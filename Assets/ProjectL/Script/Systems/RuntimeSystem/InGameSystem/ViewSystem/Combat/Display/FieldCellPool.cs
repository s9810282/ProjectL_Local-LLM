using System;
using System.Collections.Generic;
using ProjectL.Framework.Utility;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// 셀 프리팹 하나의 풀입니다. 셀은 파괴하지 않고 돌려받아 다시 씁니다(옛 FieldCellPool 이식, FW-014).
    /// 첫 인스턴스에서 셀 크기(X·Z)를 한 번 잽니다.
    /// </summary>
    internal sealed class FieldCellPool
    {
        private readonly Transform _cellsRoot;
        private readonly GameObject _cellPrefab;
        private readonly IColorSystemService _colors;
        private readonly CustomPool<FieldCellHandle> _pool;
        private readonly List<FieldCellHandle> _active = new();

        private bool _hasFootprint;
        private float _footprintWidth;
        private float _footprintLength;

        internal FieldCellPool(Transform cellsRoot, GameObject cellPrefab, IColorSystemService colors)
        {
            _cellsRoot = cellsRoot != null ? cellsRoot : throw new ArgumentNullException(nameof(cellsRoot));
            _cellPrefab = cellPrefab != null ? cellPrefab : throw new ArgumentNullException(nameof(cellPrefab));
            _colors = colors ?? throw new ArgumentNullException(nameof(colors));
            _pool = new CustomPool<FieldCellHandle>(
                new PoolContext<FieldCellHandle>(CreateHandle, 0, null, new CellPoolLifecycle()));
        }

        internal GameObject Prefab => _cellPrefab;

        internal float FootprintWidth => RequireFootprint()._footprintWidth;

        internal float FootprintLength => RequireFootprint()._footprintLength;

        /// <summary>지금 빌린 것을 모두 돌려준 뒤 count개를 새로 빌립니다.</summary>
        internal void Rent(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            ReturnAll();
            if (_active.Capacity < count)
                _active.Capacity = count;
            for (int i = 0; i < count; i++)
                _active.Add(_pool.Get());
        }

        internal FieldCellHandle GetActive(int index)
        {
            return _active[index];
        }

        internal void ReturnAll()
        {
            for (int i = 0; i < _active.Count; i++)
                _pool.Return(_active[i]);
            _active.Clear();
        }

        /// <summary>풀 크기를 재기 위해 빌리지 않고 셀 하나를 만들어 둡니다.</summary>
        internal void EnsureMeasured()
        {
            if (_hasFootprint)
                return;

            _pool.Return(_pool.Get());
        }

        private FieldCellPool RequireFootprint()
        {
            if (!_hasFootprint)
                throw new InvalidOperationException("셀 크기를 아직 재지 않았습니다.");
            return this;
        }

        private FieldCellHandle CreateHandle()
        {
            var anchor = new GameObject("FieldCell");
            anchor.transform.SetParent(_cellsRoot, false);
            GameObject visual = UnityEngine.Object.Instantiate(_cellPrefab, anchor.transform, false);
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);

            if (!_hasFootprint)
            {
                Measure(anchor.transform, renderers);
                _hasFootprint = true;
            }

            var handle = new FieldCellHandle(anchor, renderers, _colors);
            handle.ResetForPool();
            return handle;
        }

        private void Measure(Transform anchor, Renderer[] renderers)
        {
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                Bounds bounds = renderer.localBounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var local = new Vector3(
                        bounds.center.x + ((corner & 1) == 0 ? -bounds.extents.x : bounds.extents.x),
                        bounds.center.y + ((corner & 2) == 0 ? -bounds.extents.y : bounds.extents.y),
                        bounds.center.z + ((corner & 4) == 0 ? -bounds.extents.z : bounds.extents.z));
                    Vector3 point = anchor.InverseTransformPoint(renderer.transform.TransformPoint(local));
                    min = Vector3.Min(min, point);
                    max = Vector3.Max(max, point);
                }
            }

            _footprintWidth = max.x - min.x;
            _footprintLength = max.z - min.z;
        }

        private sealed class CellPoolLifecycle : IPoolLifecycle<FieldCellHandle>
        {
            public void OnGet(FieldCellHandle item)
            {
            }

            public void OnBeforeReturn(FieldCellHandle item)
            {
                item.ResetForPool();
            }

            public void OnReturn(FieldCellHandle item)
            {
            }
        }
    }
}
