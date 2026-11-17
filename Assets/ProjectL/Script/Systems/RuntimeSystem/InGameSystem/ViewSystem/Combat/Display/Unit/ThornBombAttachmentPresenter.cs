using System;
using System.Collections.Generic;
using ProjectL.Framework.Utility;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>폭탄 ID별 부착물을 유닛 표시의 대여 수명에 맞춰 관리합니다.</summary>
    internal sealed class ThornBombAttachmentPresenter : IDisposable
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly Func<EntityId, Func<(bool Bound, Vector3? Body)>> _track;
        private readonly CustomPool<GameObject> _pool;
        private readonly Dictionary<long, Attachment> _active = new();
        private readonly List<long> _stale = new();
        private GameObject _root;
        private bool _disposed;

        internal ThornBombAttachmentPresenter(GameObject prefab, Transform parent,
            Func<EntityId, Func<(bool Bound, Vector3? Body)>> track)
        {
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            _parent = parent != null ? parent : throw new ArgumentNullException(nameof(parent));
            _track = track ?? throw new ArgumentNullException(nameof(track));
            _pool = new CustomPool<GameObject>(new PoolContext<GameObject>(Create, 0));
        }

        internal void Sync(ThornBombStateSet state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            _stale.Clear();
            foreach (long id in _active.Keys)
                _stale.Add(id);
            foreach (var bomb in state.Bombs)
            {
                _stale.Remove(bomb.BombId);
                if (_active.TryGetValue(bomb.BombId, out var item) && !item.Track().Bound)
                    Remove(bomb.BombId);
                Attach(bomb);
            }
            foreach (long id in _stale)
                Remove(id);
            _stale.Clear();
        }

        internal void Attach(ThornBombSnapshot bomb)
        {
            if (bomb == null) throw new ArgumentNullException(nameof(bomb));
            if (_disposed) throw new ObjectDisposedException(nameof(ThornBombAttachmentPresenter));
            if (_active.ContainsKey(bomb.BombId)) return;

            var track = _track(bomb.TargetId);
            if (track == null || !track().Bound) return;
            var item = new Attachment(_pool.Get(), track);
            _active.Add(bomb.BombId, item);
            Position(item);
        }

        internal void Remove(long bombId)
        {
            if (!_active.Remove(bombId, out var item)) return;
            item.Model.SetActive(false);
            _pool.Return(item.Model);
        }

        internal void Tick()
        {
            foreach (var item in _active.Values)
                Position(item);
        }

        internal void Clear()
        {
            foreach (var item in _active.Values)
            {
                item.Model.SetActive(false);
                _pool.Return(item.Model);
            }
            _active.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Clear();
            if (_root == null) return;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(_root);
            else
                UnityEngine.Object.DestroyImmediate(_root);
        }

        private GameObject Create()
        {
            if (_root == null)
            {
                _root = new GameObject("ThornBombAttachments");
                _root.transform.SetParent(_parent, false);
            }
            var model = UnityEngine.Object.Instantiate(_prefab, _root.transform, false);
            model.SetActive(false);
            return model;
        }

        private void Position(Attachment item)
        {
            var (bound, body) = item.Track();
            bool visible = bound && body.HasValue;
            if (visible) item.Model.transform.position = body.Value + _parent.up * _prefab.transform.localScale.y;
            if (item.Model.activeSelf != visible) item.Model.SetActive(visible);
        }

        private readonly struct Attachment
        {
            internal readonly GameObject Model;
            internal readonly Func<(bool Bound, Vector3? Body)> Track;

            internal Attachment(GameObject model, Func<(bool Bound, Vector3? Body)> track)
            {
                Model = model;
                Track = track;
            }
        }
    }
}
