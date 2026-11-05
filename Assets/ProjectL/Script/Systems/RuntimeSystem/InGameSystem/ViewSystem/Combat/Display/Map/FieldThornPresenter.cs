using System;
using System.Collections.Generic;
using ProjectL.Framework.Utility;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>전달받은 실제 가시밭 칸의 표시·재사용·반환을 소유합니다. 생성 규칙·피해·남은 턴은 계산하지 않습니다.</summary>
    internal sealed class FieldThornPresenter
    {
        private sealed class Instance
        {
            internal readonly GameObject Object;

            internal Instance(GameObject gameObject)
            {
                Object = gameObject;
            }
        }

        private readonly GameObject _prefab;
        private readonly Transform _root;
        private readonly Vector3 _prefabScale;
        private readonly Quaternion _prefabRotation;
        private readonly Dictionary<CellCoord, Instance> _active = new();
        private readonly List<Instance> _instances = new();
        private readonly HashSet<CellCoord> _wanted = new();
        private readonly List<CellCoord> _removal = new();
        private readonly Queue<GameObject> _pendingDestruction = new();
        // 가시 프리팹 루트 BoxCollider 모양 캐시입니다(접촉 몸, 이동 접촉 설계 M2). 프리팹이 하나라 한 번 읽고 Clear마다 다시 읽습니다.
        private readonly CombatBodyShapeCache _shapes = new("[FieldThornPresenter]");
        private CustomPool<Instance> _pool;
        private GameObject _inactiveRoot;
        private bool _released;
        private bool _clearing;
        private bool _syncing;
        private bool _destroying;
        private int _visibilityDepth;
        private long _cancellationVersion;

        /// <summary>원본 프리팹·전달 루트는 보존하고, 이 표시가 만든 보관 자식과 복제본만 소유합니다.</summary>
        internal FieldThornPresenter(GameObject prefab, Transform root)
        {
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            _root = root != null ? root : throw new ArgumentNullException(nameof(root));
            _prefabScale = prefab.transform.localScale;
            _prefabRotation = prefab.transform.localRotation;
            _inactiveRoot = new GameObject("FieldThorns_Inactive");
            SetActiveSafely(_inactiveRoot, false);
            _inactiveRoot.transform.SetParent(root, false);
            _pool = new CustomPool<Instance>(new PoolContext<Instance>(CreateInstance, 0));
        }

        internal int ActiveCount => _active.Count;

        /// <summary>Install·결과 완료 Refresh에서 호출합니다. 목록에 있는 칸만 표시하고 사라진 칸만 반환합니다.</summary>
        internal void Sync(IReadOnlyList<ThornFieldCellSnapshot> fields, FieldGridMapper mapper)
        {
            if (fields == null)
                throw new ArgumentNullException(nameof(fields));
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));
            EnsureCanSync();
            _syncing = true;
            long version = _cancellationVersion;
            try
            {
                // 전달 계약 오류는 기존 표시를 바꾸기 전에 보고합니다.
                for (int i = 0; i < fields.Count; i++)
                {
                    CellCoord cell = fields[i].Cell;
                    if (!mapper.IsInside(cell))
                        throw new InvalidOperationException($"[FieldThornPresenter] 가시밭 칸이 보드 밖입니다: {cell}.");
                    if (!_wanted.Add(cell))
                        throw new InvalidOperationException($"[FieldThornPresenter] 가시밭 칸이 중복됩니다: {cell}.");
                }

                foreach (CellCoord cell in _active.Keys)
                    if (!_wanted.Contains(cell))
                        _removal.Add(cell);

                try
                {
                    for (int i = 0; i < _removal.Count; i++)
                    {
                        if (!CanContinue(version))
                            return;
                        Remove(_removal[i]);
                    }

                    for (int i = 0; i < fields.Count; i++)
                    {
                        if (!CanContinue(version))
                            return;
                        CellCoord cell = fields[i].Cell;
                        Instance instance = Acquire(cell);
                        if (!Apply(cell, instance, mapper, version))
                            return;
                    }
                }
                catch (Exception exception)
                {
                    try
                    {
                        Clear();
                    }
                    catch (Exception cleanup)
                    {
                        throw new InvalidOperationException("[FieldThornPresenter] 표시와 정리 중 실패했습니다.",
                            new AggregateException(exception, cleanup));
                    }
                    throw;
                }
            }
            finally
            {
                _wanted.Clear();
                _removal.Clear();
                _syncing = false;
            }
        }

        /// <summary>실제 ThornConsumed 사건의 칸 표시만 회수합니다. 다음 Refresh는 전달된 현재 목록을 그대로 표시합니다.</summary>
        internal void Consume(CellCoord cell)
        {
            if (_released || !_active.ContainsKey(cell))
                return;
            _cancellationVersion++;
            Remove(cell);
        }

        /// <summary>
        /// 그 칸 가시의 접촉 몸(필드 로컬 XZ)입니다(이동 접촉 설계 M2). 모양은 가시 프리팹 루트 BoxCollider, 배치는 <see cref="Apply"/>와 같은
        /// <see cref="Placement"/>(칸 중심, 필드 R0 × 프리팹 기본 회전)와 표시 루트 아래 프리팹 크기라 인스턴스 유무와 관계없이 칸만으로 정해집니다.
        /// 콜라이더가 없으면 칸 중심의 점 몸입니다. 표시 루트는 균일 크기를 전제합니다.
        /// </summary>
        internal CombatBody BodyOf(CellCoord cell, FieldGridMapper mapper)
        {
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            Placement(cell, mapper, out Vector3 worldCenter, out Quaternion rotation);
            if (_released || !_shapes.TryGet(_prefab, "Thorn", out Vector3 center, out Vector3 size))
                return CombatBody.Point(CombatBody.Flatten(mapper.WorldToLocal(worldCenter)));
            // 인스턴스 localScale = _prefabScale(Apply)이므로 월드 크기 = 표시 루트 크기 × 프리팹 크기입니다.
            Vector3 scale = Vector3.Scale(_root.lossyScale, _prefabScale);
            return CombatBodyPlacement.FromWorldBox(worldCenter + rotation * Vector3.Scale(center, scale),
                rotation * new Vector3(size.x * scale.x * .5f, 0f, 0f),
                rotation * new Vector3(0f, 0f, size.z * scale.z * .5f), mapper);
        }

        // 가시 인스턴스의 월드 배치입니다. 표시(Apply)와 접촉 몸(BodyOf)이 함께 쓰는 한 곳입니다.
        // 가시는 칸 표면(바닥 타일 + 지형) 위에 놓습니다(타일 높이 설계 TH5). 접촉 몸은 가로만 쓰므로 높이와 무관합니다.
        private void Placement(CellCoord cell, FieldGridMapper mapper, out Vector3 position, out Quaternion rotation)
        {
            position = mapper.CellSurfaceToWorld(cell);
            rotation = mapper.RotationToWorld(Rotation4.R0) * _prefabRotation;
        }

        internal bool TryGet(CellCoord cell, out GameObject instance)
        {
            if (_active.TryGetValue(cell, out Instance entry) && entry.Object != null)
            {
                instance = entry.Object;
                return true;
            }

            instance = null;
            return false;
        }

        /// <summary>표시만 비우고 풀은 유지합니다. 재설치는 현재 목록의 Sync로 복원합니다.</summary>
        internal void Clear()
        {
            if (_released || _clearing)
                return;
            _clearing = true;
            _cancellationVersion++;
            _shapes.Clear();
            var instances = new Instance[_active.Count];
            _active.Values.CopyTo(instances, 0);
            _active.Clear();
            List<Exception> errors = null;
            try
            {
                for (int i = 0; i < instances.Length; i++)
                {
                    try
                    {
                        Return(instances[i]);
                    }
                    catch (Exception exception)
                    {
                        errors ??= new List<Exception>();
                        errors.Add(exception);
                    }
                }
            }
            finally
            {
                _clearing = false;
            }

            ThrowErrors(errors, "가시밭 반환 중 실패했습니다.");
        }

        /// <summary>활성·보관 복제본과 전용 보관 자식을 파괴합니다. 가시성 콜백 안의 파괴는 SetActive 반환 직후 수행합니다.</summary>
        internal void Release()
        {
            if (_released)
                return;
            _released = true;
            _cancellationVersion++;
            _active.Clear();
            Instance[] instances = _instances.ToArray();
            _instances.Clear();
            _pool = null;
            GameObject inactiveRoot = _inactiveRoot;
            _inactiveRoot = null;
            List<Exception> errors = null;
            for (int i = 0; i < instances.Length; i++)
            {
                try
                {
                    DestroyOwned(instances[i].Object);
                }
                catch (Exception exception)
                {
                    errors ??= new List<Exception>();
                    errors.Add(exception);
                }
            }

            try
            {
                DestroyOwned(inactiveRoot);
            }
            catch (Exception exception)
            {
                errors ??= new List<Exception>();
                errors.Add(exception);
            }

            ThrowErrors(errors, "가시밭 최종 해제 중 실패했습니다.");
        }

        private Instance Acquire(CellCoord cell)
        {
            if (_active.TryGetValue(cell, out Instance instance))
            {
                if (instance.Object != null)
                    return instance;
                _active.Remove(cell);
                _instances.Remove(instance);
            }

            do
            {
                instance = _pool.Get();
                if (instance.Object == null)
                    _instances.Remove(instance);
            }
            while (instance.Object == null);
            _active.Add(cell, instance);
            return instance;
        }

        private bool Apply(CellCoord cell, Instance instance, FieldGridMapper mapper, long version)
        {
            if (instance.Object == null)
                throw new InvalidOperationException("[FieldThornPresenter] 가시밭 표시가 외부에서 파괴됐습니다.");
            Transform transform = instance.Object.transform;
            if (transform.parent != _root)
            {
                transform.SetParent(_root, false);
                if (!CanContinue(version))
                    return false;
            }

            Placement(cell, mapper, out Vector3 position, out Quaternion rotation);
            if (transform.position != position)
                transform.position = position;
            if (transform.rotation != rotation)
                transform.rotation = rotation;
            if (transform.localScale != _prefabScale)
                transform.localScale = _prefabScale;
            if (!instance.Object.activeSelf)
                SetActiveSafely(instance.Object, true);
            if (CanContinue(version) && instance.Object == null)
                throw new InvalidOperationException("[FieldThornPresenter] 표시 시작 중 가시밭이 파괴됐습니다.");
            return CanContinue(version);
        }

        private Instance CreateInstance()
        {
            GameObject gameObject = UnityEngine.Object.Instantiate(_prefab, _inactiveRoot.transform, false);
            var instance = new Instance(gameObject);
            _instances.Add(instance);
            try
            {
                if (gameObject == null)
                    throw new InvalidOperationException("[FieldThornPresenter] 가시밭 프리팹 복제본이 없습니다.");
                // 원본 잔류 프리팹은 표시 전용입니다. 필드 포인터 집기를 가로채지 않습니다.
                foreach (Transform child in gameObject.GetComponentsInChildren<Transform>(true))
                    child.gameObject.layer = FieldRenderLayers.BothRendering;
                foreach (Collider collider in gameObject.GetComponentsInChildren<Collider>(true))
                    collider.enabled = false;
                SetActiveSafely(gameObject, false);
                return instance;
            }
            catch (Exception exception)
            {
                var errors = new List<Exception> { exception };
                TryDestroy(instance, errors);
                ThrowErrors(errors, "가시밭 생성 중 실패했습니다.");
                throw;
            }
        }

        private void Remove(CellCoord cell)
        {
            if (!_active.TryGetValue(cell, out Instance instance))
                return;
            _active.Remove(cell);
            Return(instance);
        }

        private void Return(Instance instance)
        {
            if (_released)
                return;
            List<Exception> errors = null;
            try
            {
                if (instance.Object != null)
                {
                    SetActiveSafely(instance.Object, false);
                    if (_released)
                        return;
                    if (instance.Object != null)
                    {
                        Transform transform = instance.Object.transform;
                        transform.SetParent(_inactiveRoot.transform, false);
                        if (_released)
                            return;
                        transform.SetLocalPositionAndRotation(Vector3.zero, _prefabRotation);
                        transform.localScale = _prefabScale;
                        _pool.Return(instance);
                        return;
                    }
                }
            }
            catch (Exception exception)
            {
                errors = new List<Exception> { exception };
            }

            errors ??= new List<Exception>();
            TryDestroy(instance, errors);
            ThrowErrors(errors, "가시밭 회수 중 실패했습니다.");
        }

        private bool CanContinue(long version) => !_released && _cancellationVersion == version;

        private void EnsureCanSync()
        {
            if (_released || _clearing || _syncing)
                throw new InvalidOperationException("[FieldThornPresenter] 정리·해제 중이거나 중첩된 Sync입니다.");
            if (_prefab == null || _root == null || _inactiveRoot == null)
                throw new InvalidOperationException("[FieldThornPresenter] 프리팹 또는 표시 루트가 파괴됐습니다.");
        }

        private void TryDestroy(Instance instance, List<Exception> errors)
        {
            try
            {
                DestroyOwned(instance.Object);
                _instances.Remove(instance);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        private void DestroyOwned(GameObject gameObject)
        {
            if (gameObject == null)
                return;
            _pendingDestruction.Enqueue(gameObject);
            DrainPendingDestruction();
        }

        private void SetActiveSafely(GameObject gameObject, bool active)
        {
            _visibilityDepth++;
            Exception visibilityError = null;
            try
            {
                gameObject.SetActive(active);
            }
            catch (Exception exception)
            {
                visibilityError = exception;
            }
            finally
            {
                _visibilityDepth--;
            }

            try
            {
                DrainPendingDestruction();
            }
            catch (Exception exception)
            {
                if (visibilityError != null)
                    throw new InvalidOperationException("[FieldThornPresenter] 가시성 변경과 파괴 중 실패했습니다.",
                        new AggregateException(visibilityError, exception));
                throw;
            }

            if (visibilityError != null)
                throw new InvalidOperationException("[FieldThornPresenter] 가시성 변경 중 실패했습니다.", visibilityError);
        }

        private void DrainPendingDestruction()
        {
            if (_visibilityDepth > 0 || _destroying || _pendingDestruction.Count == 0)
                return;
            _destroying = true;
            List<Exception> errors = null;
            try
            {
                while (_pendingDestruction.Count > 0)
                {
                    GameObject gameObject = _pendingDestruction.Dequeue();
                    try
                    {
                        if (gameObject == null)
                            continue;
                        SetActiveSafely(gameObject, false);
                        if (gameObject == null)
                            continue;
                        if (Application.isPlaying)
                            UnityEngine.Object.Destroy(gameObject);
                        else
                            UnityEngine.Object.DestroyImmediate(gameObject);
                    }
                    catch (Exception exception)
                    {
                        errors ??= new List<Exception>();
                        errors.Add(exception);
                    }
                }
            }
            finally
            {
                _destroying = false;
            }

            ThrowErrors(errors, "가시밭 파괴 중 실패했습니다.");
        }

        private static void ThrowErrors(List<Exception> errors, string message)
        {
            if (errors != null && errors.Count > 0)
                throw new InvalidOperationException("[FieldThornPresenter] " + message, new AggregateException(errors));
        }
    }
}
