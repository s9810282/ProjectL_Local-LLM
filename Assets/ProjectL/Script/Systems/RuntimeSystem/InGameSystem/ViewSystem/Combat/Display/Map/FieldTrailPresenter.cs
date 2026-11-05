using System;
using System.Collections.Generic;
using ProjectL.Framework.Utility;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 특성 흔적 지형(땅 다지기·먼지 연막, <see cref="TraitTrailFact"/>)의 외형·칸 배치·풀 수명입니다(특성 시스템 View 설계 TS7, 계획 Te.2).
    /// 원본 ID마다 인스턴스 하나이고, 외형은 흔적의 특성 키(<see cref="TraitTrailFact.Key"/>)로 특성 외형 표에서 찾습니다. Buffer는 읽지 않고 전달받은 사실만 표시합니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 사건 시점(<see cref="Show"/>): 이동 원본 사건의 결과가 흔적이면 사실 처리기가 부릅니다. 켜져 있고 플레이어에게 보이면 놓고(새 원본이면 만들고, 같은 원본이면 옮김),
    /// 아니면 지웁니다. 결과 끝(<see cref="Render"/>): 환경 원본 목록의 흔적으로 다시 맞춥니다. Core는 지운 흔적도 목록에 꺼진 채로 남깁니다(계획 §3.3).
    /// </para>
    /// <para>
    /// 바닥 지형은 건드리지 않고 그 위에 겹칩니다. 칸 하나에 흔적이 여럿이면 원본 ID 차례로 조금씩 위에 놓아 겹쳐 깜박이지 않게 합니다(계획 TP-10).
    /// 남은 턴은 Core가 줄이고 View는 세지 않습니다. 남은 턴 글자는 범위 밖이며, Rider 연막 호버 설명은 <c>CombatEnvironmentText</c>가 처리합니다.
    /// </para>
    /// </remarks>
    internal sealed class FieldTrailPresenter
    {
        // 같은 칸에 겹친 흔적끼리 띄우는 높이입니다(월드 단위). 칸 테두리 높이 규칙(테두리 칸 수 × 0.002)과 같은 크기입니다.
        private const float StackStep = 0.002f;

        private sealed class Instance
        {
            internal readonly GameObject GameObject;
            internal readonly TraitKey Key;
            internal readonly Quaternion BaseRotation;
            internal readonly Vector3 Scale;
            internal readonly Vector3 Offset;
            internal TraitTrailFact Fact;

            internal Instance(GameObject gameObject, TraitKey key, GameObject prefab, Vector3 offset)
            {
                GameObject = gameObject;
                Key = key;
                BaseRotation = prefab.transform.localRotation;
                Scale = prefab.transform.localScale;
                Offset = offset;
            }
        }

        private readonly Transform _parent;
        private readonly ITraitVisualTableQuery _visuals;
        private readonly Dictionary<TraitKey, CustomPool<Instance>> _pools = new();
        private readonly Dictionary<long, Instance> _active = new();
        private readonly HashSet<long> _wanted = new();
        private readonly List<long> _removal = new();
        private readonly HashSet<TraitKey> _warnedMissing = new();
        private Transform _root;
        private bool _isReleased;

        internal FieldTrailPresenter(Transform parent, ITraitVisualTableQuery visuals)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            _parent = parent;
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
        }

        /// <summary>
        /// 사건 시점에 흔적 원본 하나를 맞춥니다(생성·바뀜·끔). 켜져 있고 플레이어에게 보이면(<see cref="CombatEnvironmentFacts.IsShown"/>) 그 칸에 놓고, 아니면 지웁니다.
        /// 결과 끝 <see cref="Render"/>가 최종 사실로 다시 맞춥니다.
        /// </summary>
        internal void Show(TraitTrailFact trail, FieldGridMapper mapper)
        {
            ThrowIfReleased();
            if (trail == null) throw new ArgumentNullException(nameof(trail));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            if (CombatEnvironmentFacts.IsShown(trail) && TryPlace(trail, mapper))
                return;
            Remove(trail.Id);
        }

        /// <summary>결과 끝에 환경 원본 목록의 흔적으로 표시를 다시 맞춥니다. 목록에 없거나 꺼진 원본의 인스턴스는 풀로 돌려보냅니다.</summary>
        internal void Render(MovementEnvironmentFacts environment, FieldGridMapper mapper)
        {
            ThrowIfReleased();
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            _wanted.Clear();
            for (int i = 0; i < environment.Tiles.Count; i++)
            {
                if (environment.Tiles[i] is TraitTrailFact trail && CombatEnvironmentFacts.IsShown(trail) && TryPlace(trail, mapper))
                    _wanted.Add(trail.Id);
            }
            _removal.Clear();
            foreach (long id in _active.Keys)
            {
                if (!_wanted.Contains(id))
                    _removal.Add(id);
            }
            for (int i = 0; i < _removal.Count; i++)
                Remove(_removal[i]);
            _removal.Clear();
            _wanted.Clear();
        }

        /// <summary>모든 흔적 표시를 풀로 돌려보냅니다. 풀은 유지합니다.</summary>
        internal void Clear()
        {
            _removal.Clear();
            _removal.AddRange(_active.Keys);
            for (int i = 0; i < _removal.Count; i++)
                Remove(_removal[i]);
            _removal.Clear();
        }

        /// <summary>Clear는 풀을 유지하고, 최종 Release는 전용 루트와 풀 참조까지 없앱니다.</summary>
        internal void Release()
        {
            if (_isReleased) return;
            _isReleased = true;
            try
            {
                Clear();
            }
            finally
            {
                _pools.Clear();
                if (_root != null)
                {
                    _root.gameObject.SetActive(false);
                    if (Application.isPlaying) Object.Destroy(_root.gameObject);
                    else Object.DestroyImmediate(_root.gameObject);
                }
                _root = null;
            }
        }

        // 그 원본을 놓습니다. 외형이 없으면(특성 외형 줄이 없거나 프리팹이 비었음) 그리지 않고 false입니다.
        private bool TryPlace(TraitTrailFact trail, FieldGridMapper mapper)
        {
            if (!mapper.IsInside(trail.Cell))
                throw new InvalidOperationException($"[FieldTrailPresenter] 흔적 칸이 보드 밖입니다: {trail.Cell}, 원본 {trail.Id}");
            if (_active.TryGetValue(trail.Id, out Instance instance) && instance.Key != trail.Key)
            {
                Remove(trail.Id);
                instance = null;
            }
            if (instance == null)
            {
                if (!TryRent(trail.Key, out instance))
                    return false;
                _active.Add(trail.Id, instance);
            }
            instance.Fact = trail;
            Apply(instance, trail, mapper);
            return true;
        }

        private void Apply(Instance instance, TraitTrailFact trail, FieldGridMapper mapper)
        {
            if (instance.GameObject == null)
                throw new InvalidOperationException($"[FieldTrailPresenter] 사용 중인 흔적 외형이 파괴됐습니다: {trail.Id}");
            Transform transform = instance.GameObject.transform;
            Vector3 local = mapper.CellSurfaceToLocal(trail.Cell) + instance.Offset + Vector3.up * (StackStep * StackIndexOf(trail));
            Vector3 position = mapper.LocalToWorld(local);
            Quaternion rotation = mapper.RotationToWorld(Rotation4.R0) * instance.BaseRotation;
            if (transform.position != position) transform.position = position;
            if (transform.rotation != rotation) transform.rotation = rotation;
            if (transform.localScale != instance.Scale) transform.localScale = instance.Scale;
            instance.GameObject.name = $"FieldTrail_{trail.Key}_{trail.Id}";
            if (!instance.GameObject.activeSelf) instance.GameObject.SetActive(true);
        }

        // 같은 칸에 지금 놓인 다른 흔적 중 원본 ID가 더 작은 것의 수입니다(겹친 차례). 칸 하나의 흔적은 몇 개뿐이라 훑어도 됩니다.
        private int StackIndexOf(TraitTrailFact trail)
        {
            int index = 0;
            foreach (KeyValuePair<long, Instance> pair in _active)
            {
                TraitTrailFact other = pair.Value.Fact;
                if (other != null && pair.Key < trail.Id && other.Cell == trail.Cell)
                    index++;
            }
            return index;
        }

        private void Remove(long id)
        {
            if (!_active.TryGetValue(id, out Instance instance))
                return;
            _active.Remove(id);
            instance.Fact = null;
            if (instance.GameObject == null)
                return;
            instance.GameObject.SetActive(false);
            if (_pools.TryGetValue(instance.Key, out CustomPool<Instance> pool))
                pool.Return(instance);
        }

        private bool TryRent(TraitKey key, out Instance instance)
        {
            instance = null;
            if (!_visuals.TryGetTraitVisual(key, out TraitVisualData visual) || visual == null ||
                !visual.TryGetTrail(out GameObject prefab, out Vector3 offset))
            {
                if (_warnedMissing.Add(key))
                    Debug.LogWarning($"[FieldTrailPresenter] 흔적 외형이 없어 그리지 않습니다(특성 외형 표의 흔적 프리팹). 특성: {key}");
                return false;
            }
            if (!_pools.TryGetValue(key, out CustomPool<Instance> pool))
            {
                pool = new CustomPool<Instance>(new PoolContext<Instance>(() => Create(key, prefab, offset), 0));
                _pools.Add(key, pool);
            }
            do
            {
                instance = pool.Get();
            }
            while (instance.GameObject == null);
            return true;
        }

        private Instance Create(TraitKey key, GameObject prefab, Vector3 offset)
        {
            if (_root == null)
            {
                _root = new GameObject("FieldTrails").transform;
                _root.SetParent(_parent, false);
            }
            GameObject gameObject = Object.Instantiate(prefab, _root, false);
            gameObject.SetActive(false);
            return new Instance(gameObject, key, prefab, offset);
        }

        private void ThrowIfReleased()
        {
            if (_isReleased) throw new InvalidOperationException("[FieldTrailPresenter] 해제된 표시입니다.");
        }
    }
}
