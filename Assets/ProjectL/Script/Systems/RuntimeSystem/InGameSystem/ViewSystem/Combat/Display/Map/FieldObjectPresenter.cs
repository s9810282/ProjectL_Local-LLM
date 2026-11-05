using System;
using System.Collections.Generic;
using ProjectL.Framework.Utility;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Object.Enum;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Object.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageEnvironment.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>Object의 외형·격자 배치·풀 수명입니다. Buffer는 읽지 않고 전달받은 사실만 표시합니다.</summary>
    internal sealed class FieldObjectPresenter
    {
        private sealed class Instance
        {
            internal readonly GameObject GameObject;
            internal readonly Quaternion BaseRotation;
            internal readonly Vector3 Scale;
            internal readonly Vector3 Offset;
            internal long SourceId;

            internal Instance(GameObject gameObject, ObjectVisualData visual)
            {
                GameObject = gameObject;
                BaseRotation = visual.Prefab.transform.localRotation;
                Scale = Vector3.Scale(visual.Prefab.transform.localScale, visual.LocalScale);
                Offset = visual.LocalOffset;
            }
        }

        private readonly Transform _parent;
        private readonly IObjectVisualTableQuery _visuals;
        private readonly IStageEnvironmentVisualTableQuery _stages;
        private readonly Dictionary<ObjectContentKind, CustomPool<Instance>> _pools = new();
        private readonly ObjectViewSet<Instance> _views;
        private readonly Action<Instance, ObjectSourceFact> _apply;
        // 외형 프리팹 루트 BoxCollider 모양 캐시입니다(접촉 몸, 이동 접촉 설계 M2). Clear마다 다시 읽습니다.
        private readonly CombatBodyShapeCache _shapes = new("[FieldObjectPresenter]");
        private Transform _root;
        // 아래 두 참조는 Render 호출 중에만 사용합니다. 전투 상태는 저장하지 않습니다.
        private FieldGridMapper _mapper;
        private StageEnvironmentVisualData _rotations;
        private bool _isReleased;

        internal FieldObjectPresenter(Transform parent, IObjectVisualTableQuery visuals,
            IStageEnvironmentVisualTableQuery stages)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            _parent = parent;
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
            _stages = stages ?? throw new ArgumentNullException(nameof(stages));
            _views = new ObjectViewSet<Instance>(Rent, Return);
            _apply = Apply;
        }

        internal void Render(MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings,
            int stageKey, FieldGridMapper mapper)
        {
            if (_isReleased) throw new InvalidOperationException("[FieldObjectPresenter] 해제된 표시입니다.");
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            _mapper = mapper;
            try
            {
                _rotations = stageKey > 0 && _stages.TryGetStageEnvironmentVisual(stageKey, out var data) ? data : null;
                ValidateObjectRotations(_rotations, bindings);
                _views.Render(environment, bindings, _apply);
            }
            catch (Exception error)
            {
                try
                {
                    Clear();
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(error, cleanup);
                }
                throw;
            }
            finally
            {
                _mapper = null;
                _rotations = null;
            }
        }

        /// <summary>Object 회전만 검증합니다. 아직 소비하지 않는 Terrain 회전을 요구하지 않습니다.</summary>
        internal static void ValidateObjectRotations(StageEnvironmentVisualData rotations, EnvironmentSemanticBindings bindings)
        {
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            if (rotations == null) return;
            foreach (EnvironmentVisualRotation rotation in rotations.ObjectRotations)
            {
                bool found = false;
                foreach (long id in bindings.ObjectSourceIds)
                {
                    if (id == rotation.SourceId)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    throw new InvalidOperationException($"[FieldObjectPresenter] Object 회전의 Source Id가 Buffer 분류에 없습니다: {rotation.SourceId}.");
            }
        }

        /// <summary>
        /// 원본의 접촉 몸(필드 로컬 XZ)입니다(이동 접촉 설계 M2). 모양은 그 종류 외형 프리팹 루트 BoxCollider, 배치는 지금 표시된 인스턴스
        /// (<see cref="Apply"/>가 놓은 칸 중심 + 오프셋·스테이지 회전·크기)입니다. 오브젝트는 자세 대여가 없어 인스턴스 배치가 곧 표시 배치입니다.
        /// 표시되지 않은 원본(플레이어에게 비공개·결과 도중 생성 등)은 콜라이더가 있어도 표시 배치가 없으므로 칸 중심 + 오프셋의 점 몸이고,
        /// 콜라이더가 없어도 같습니다. 외형 행이 없으면 칸 중심의 점 몸입니다(표시 쪽 Render가 그 누락을 보고합니다).
        /// </summary>
        internal CombatBody BodyOf(ObjectSourceFact source, FieldGridMapper mapper)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            ObjectContentKind kind = EnvironmentVisualKinds.ObjectKind(source);
            if (!_visuals.TryGetObjectVisual(kind, out ObjectVisualData visual) || visual == null)
                return CombatBody.Point(CombatBody.Flatten(mapper.CellCenterToLocal(source.Cell)));
            if (_views.TryGet(source.Id, out Instance instance) && instance.GameObject != null && instance.GameObject.activeInHierarchy &&
                _shapes.TryGet(visual.Prefab, kind, out Vector3 center, out Vector3 size))
                return CombatBodyPlacement.FromPlacedBox(instance.GameObject.transform, center, size, mapper);
            return CombatBody.Point(CombatBody.Flatten(LocalPosition(source.Cell, visual.LocalOffset, mapper)));
        }

        /// <summary>
        /// 발동한 원본의 표시 인스턴스를 사건 시점에 숨깁니다(이동 접촉 설계 M6, Q-M2 A). 발동한 오브젝트의 최종 사실은 비활성이기 때문입니다.
        /// 연결과 풀은 그대로 두며, 결과 끝 <see cref="Render"/>가 최종 사실로 다시 맞춥니다(비활성이면 반환, 활성이면 다시 보임).
        /// 표시되지 않은 원본·해제 뒤에는 아무것도 하지 않습니다. 원본 목록은 따로 들지 않습니다.
        /// </summary>
        internal void HideActivated(long sourceId)
        {
            if (_isReleased || !_views.TryGet(sourceId, out Instance instance) || instance.GameObject == null) return;
            if (instance.GameObject.activeSelf) instance.GameObject.SetActive(false);
        }

        // 원본 표시 위치(필드 로컬)입니다. 표시(Apply)와 접촉 몸(BodyOf)이 함께 쓰는 한 곳입니다.
        // 오브젝트는 칸 표면(바닥 타일 + 지형) 위에 놓습니다(타일 높이 설계 TH5). 접촉 몸은 가로만 쓰므로 높이와 무관합니다.
        private static Vector3 LocalPosition(CellCoord cell, Vector3 offset, FieldGridMapper mapper) =>
            mapper.CellSurfaceToLocal(cell) + offset;

        internal void Clear()
        {
            _views.Clear();
            _shapes.Clear();
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
                    if (Application.isPlaying)
                    {
                        Object.Destroy(_root.gameObject);
                    }
                    else
                    {
                        Object.DestroyImmediate(_root.gameObject);
                    }
                }
                _root = null;
            }
        }

        private void Apply(Instance instance, ObjectSourceFact source)
        {
            if (instance.GameObject == null)
                throw new InvalidOperationException("[FieldObjectPresenter] 사용 중인 Object 외형이 파괴됐습니다.");
            Transform transform = instance.GameObject.transform;
            Rotation4 rotation = _rotations?.GetObjectRotation(source.Id) ?? Rotation4.R0;
            Vector3 position = _mapper.LocalToWorld(LocalPosition(source.Cell, instance.Offset, _mapper));
            Quaternion worldRotation = _mapper.RotationToWorld(rotation) * instance.BaseRotation;
            if (transform.position != position) transform.position = position;
            if (transform.rotation != worldRotation) transform.rotation = worldRotation;
            if (transform.localScale != instance.Scale) transform.localScale = instance.Scale;
            if (instance.SourceId != source.Id)
            {
                instance.GameObject.name = $"FieldObject_{source.GetType().Name}_{source.Id}";
                instance.SourceId = source.Id;
            }
            if (!instance.GameObject.activeSelf) instance.GameObject.SetActive(true);
        }

        private Instance Rent(ObjectContentKind kind)
        {
            if (!_pools.TryGetValue(kind, out var pool))
            {
                pool = new CustomPool<Instance>(new PoolContext<Instance>(() => Create(kind), 0));
                _pools.Add(kind, pool);
            }
            Instance instance;
            do
            {
                instance = pool.Get();
            }
            while (instance.GameObject == null);
            return instance;
        }

        private void Return(ObjectContentKind kind, Instance instance)
        {
            if (instance.GameObject == null) return;
            instance.GameObject.SetActive(false);
            _pools[kind].Return(instance);
        }

        private Instance Create(ObjectContentKind kind)
        {
            ObjectVisualData visual = _visuals.GetObjectVisual(kind);
            if (visual.Prefab == null)
                throw new InvalidOperationException($"[FieldObjectPresenter] {kind}의 프리팹이 없습니다.");
            if (_root == null)
            {
                _root = new GameObject("FieldObjects").transform;
                _root.SetParent(_parent, false);
            }
            GameObject gameObject = Object.Instantiate(visual.Prefab, _root, false);
            gameObject.SetActive(false);
            return new Instance(gameObject, visual);
        }
    }
}
