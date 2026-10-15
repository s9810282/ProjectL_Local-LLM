using System;
using System.Linq;
using System.Collections.Generic;
using ProjectL.Framework.Utility;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using TMPro;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// 필드의 유닛·구조물을 그립니다(옛 FieldUnitPresenter 이식, 계획 D.2·D.5).
    /// 값만 받아 ID별 기존 View를 다시 쓰고, 새 대상만 빌리고 사라진 대상만 돌려줍니다(W19).
    /// </summary>
    /// <remarks>
    /// Buffer·Core를 모릅니다. 외형은 View DB(UnitVisual)의 프리팹을 종류별 풀로 만듭니다.
    /// 부르는 쪽은 원하는 표시 상태만 말하고 View 찾기는 여기서 합니다(<see cref="SetHiddenEntity"/>, 설계 §3).
    /// 사망·피격 연출은 이번 범위가 아니므로 쓰러진 대상은 바로 거둡니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class FieldUnitPresenter : MonoBehaviour
    {
        [Tooltip("필드 유닛 인스턴스를 둘 루트입니다.")]
        [SerializeField] private Transform unitsRoot;
        [Tooltip("체력 숫자 템플릿(TextMeshPro 3D 텍스트). 씬에 비활성으로 두고 연결합니다.")]
        [SerializeField] private TMP_Text healthLabelTemplate;
        [SerializeField] private Vector3 healthLabelOffset = new(0f, 1.5f, 0f);
        [Header("Unit HUD")]
        [SerializeField] private UnitHUD unitHudPrefab;
        [SerializeField] private Vector3 unitHudOffset = new(0f, 1.5f, 0f);
        [SerializeField, Min(0.001f)] private float unitHudScale = 0.02f;

        private readonly Dictionary<EntityKey, CustomPool<FieldUnitView>> _pools = new();
        private EntityViewSet<FieldUnitView> _views;
        private IUnitVisualTableQuery _visuals;
        private float _unitScale = 1f;
        private EntityId _hiddenEntityId = EntityId.Empty;
        private ITraitVisualTableQuery _traitVisuals;
        private Camera _hudCamera;
        private EntityId _activeActorId;
        private readonly Dictionary<EntityId, int> _turnOrders = new();
        private readonly List<UnitHUDStatus> _hudStatuses = new();

        /// <summary>전투 표시를 시작할 때 외형 DB와 크기를 받습니다. 같은 씬에서 다시 불러도 됩니다.</summary>
        internal void Configure(IUnitVisualTableQuery visuals, float unitScale,
            ITraitVisualTableQuery traitVisuals = null, Camera hudCamera = null)
        {
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
            if (!(unitScale > 0f) || float.IsInfinity(unitScale))
                throw new ArgumentOutOfRangeException(nameof(unitScale));

            _unitScale = unitScale;
            _traitVisuals = traitVisuals;
            _hudCamera = hudCamera;
            _views ??= new EntityViewSet<FieldUnitView>(Rent, Return);
        }

        /// <summary>필드에 있는 살아 있는 캐릭터와 구조물을 그립니다.</summary>
        internal void Render(IReadOnlyList<CharacterData> characters, IReadOnlyList<StructureCombatFacts> structures, FieldGridMapper mapper)
            => RenderFacts(characters.Select(c => c.ToFacts()).ToArray(), structures, mapper);

        internal void RenderFacts(IReadOnlyList<CharacterCombatFacts> characters, IReadOnlyList<StructureCombatFacts> structures, FieldGridMapper mapper)
        {
            if (characters == null)
                throw new ArgumentNullException(nameof(characters));
            if (structures == null)
                throw new ArgumentNullException(nameof(structures));
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));
            if (_views == null)
                throw new InvalidOperationException("[FieldUnitPresenter] Configure가 먼저 필요합니다.");

            _views.Begin();
            try
            {
                for (int i = 0; i < characters.Count; i++)
                {
                    CharacterCombatFacts character = characters[i];
                    if (character.Area != CharacterRuntimeArea.Field || !character.Anchor.HasValue || character.Health.IsDead)
                        continue;

                    FieldUnitView view = _views.Acquire(character.EntityId, character.CharacterKey);
                    view.Bind(character.EntityId, character.CharacterKey);
                    view.Apply(
                        CenterOf(character.OccupiedCells, character.Anchor.Value, mapper),
                        ModelRotation(character.Owner, character.Rotation, mapper),
                        character.Health.CurrentHealth,
                        character.Health.MaxHealth,
                        character.EntityId != _hiddenEntityId);
                    RenderUnitHud(view, character);
                }

                for (int i = 0; i < structures.Count; i++)
                {
                    StructureCombatFacts structure = structures[i];
                    if (structure.Health.IsDead)
                        continue;

                    FieldUnitView view = _views.Acquire(structure.EntityId, structure.Definition.Key);
                    view.Bind(structure.EntityId, structure.Definition.Key);
                    bool showHealthHud = structure.Owner == Owner.Enemy && view.Hud != null;
                    view.ShowUnitHud(showHealthHud);
                    if (showHealthHud) view.Hud.SetStructure(structure);
                    view.Apply(
                        CenterOf(structure.OccupiedCells, structure.Anchor, mapper),
                        mapper.RotationToWorld(structure.Rotation),
                        structure.Health.CurrentHealth,
                        structure.Health.MaxHealth,
                        structure.EntityId != _hiddenEntityId);
                }
            }
            finally
            {
                _views.End();
            }
        }

        private void RenderUnitHud(FieldUnitView view, CharacterCombatFacts character)
        {
            UnitHUD hud = view.Hud;
            if (hud == null) return;
            view.ShowUnitHud(true);
            hud.SetUnit(character);
            hud.SetTurnOrder(character.EntityId == _activeActorId,
                _turnOrders.TryGetValue(character.EntityId, out int order) ? order : (int?)null);
            _hudStatuses.Clear();
            foreach (var status in character.Stats.TraitStatuses.Span)
            {
                if (status == null) continue;
                Sprite icon = null;
                if (_traitVisuals != null && _traitVisuals.TryGetTraitVisual(status.Key, out var visual))
                    icon = visual?.Icon;
                // 설명은 아직 연결하지 않는다. 중복 Trait는 아이콘 하나로 표시한다.
                string key = status.Key.ToString();
                bool duplicate = false;
                for (int i = 0; i < _hudStatuses.Count; i++)
                    if (_hudStatuses[i].Key == key) { duplicate = true; break; }
                if (!duplicate) _hudStatuses.Add(new UnitHUDStatus(key, icon, string.Empty));
            }
            hud.SetStatuses(_hudStatuses);
        }

        /// <summary>Core가 계산한 Forecast 순서를 표시값으로 옮긴다. 정렬/Timeline 계산은 하지 않는다.</summary>
        internal void SetHudTimeline(EntityId activeActor, IReadOnlyList<CombatTimelineEntrySnapshot> timeline)
        {
            _activeActorId = activeActor;
            _turnOrders.Clear();
            int order = 0;
            if (timeline == null) return;
            for (int i = 0; i < timeline.Count; i++)
            {
                var entry = timeline[i];
                if (entry.Kind != CombatTimelineEntryKind.UnitTurn || !entry.UnitId.HasValue || entry.IsActive) continue;
                order++;
                if (!_turnOrders.ContainsKey(entry.UnitId.Value)) _turnOrders.Add(entry.UnitId.Value, order);
            }
        }

        internal void ShowHudCostPreview(EntityId actorId, int cost)
        {
            if (_views != null && _views.TryGet(actorId, out var view) && view != null && view.Hud != null)
                view.Hud.SetActionCostPreview(cost);
        }

        internal void ClearHudPreview(EntityId actorId)
        {
            if (_views != null && _views.TryGet(actorId, out var view) && view != null && view.Hud != null)
                view.Hud.ClearPreview();
        }

        /// <summary>지정한 개체의 View만 숨깁니다(Drag 중 원본 숨김 등). Core 상태는 바꾸지 않습니다.</summary>
        internal void SetHiddenEntity(EntityId entityId)
        {
            if (_hiddenEntityId == entityId)
                return;

            SetActive(_hiddenEntityId, true);
            _hiddenEntityId = entityId;
            SetActive(_hiddenEntityId, false);
        }

        internal void ClearHiddenEntity()
        {
            SetHiddenEntity(EntityId.Empty);
        }

        /// <summary>그린 View를 모두 풀에 돌려줍니다. 여러 번 불려도 안전합니다.</summary>
        internal void Release()
        {
            _hiddenEntityId = EntityId.Empty;
            _activeActorId = EntityId.Empty;
            _turnOrders.Clear();
            _views?.Clear();
        }

        /// <summary>빠진 씬 참조 이름입니다. 없으면 null입니다.</summary>
        internal string FindMissingReference()
        {
            if (unitsRoot == null)
                return $"{nameof(FieldUnitPresenter)}.{nameof(unitsRoot)}";
            if (healthLabelTemplate == null)
                return $"{nameof(FieldUnitPresenter)}.{nameof(healthLabelTemplate)}";
            return null;
        }

        internal void Interpolate(CharacterCombatFacts from, CharacterCombatFacts to, FieldGridMapper mapper, float progress)
        {
            if (_views.TryGet(from.EntityId, out var view))
                view.transform.position = Vector3.Lerp(CenterOf(from.OccupiedCells, from.Anchor.Value, mapper),
                    CenterOf(to.OccupiedCells, to.Anchor.Value, mapper), progress);
        }

        private void SetActive(EntityId entityId, bool active)
        {
            if (entityId.IsEmpty || _views == null || !_views.TryGet(entityId, out FieldUnitView view) || view == null)
                return;
            if (view.gameObject.activeSelf != active)
                view.gameObject.SetActive(active);
        }

        private static readonly Quaternion EnemyFacing = Quaternion.Euler(0f, 180f, 0f);

        /// <summary>
        /// 유닛 모델의 월드 회전입니다. 적 유닛은 아군 쪽을 보도록 180도 더 돌려 보입니다(2026-10-09).
        /// Core는 적을 R0(+Y)으로 만들고 전투 중 바꾸지 않으므로 화면에서만 보정합니다. Core가 적 방향을 직접 주게 되면 이 보정을 지웁니다.
        /// </summary>
        internal static Quaternion ModelRotation(Owner owner, Rotation4? rotation, FieldGridMapper mapper)
        {
            Quaternion world = mapper.RotationToWorld(rotation ?? Rotation4.R0);
            return owner == Owner.Enemy ? world * EnemyFacing : world;
        }

        private static Vector3 CenterOf(IReadOnlyList<CellCoord> cells, CellCoord anchor, FieldGridMapper mapper)
        {
            if (cells == null || cells.Count == 0)
                return mapper.CellCenterToWorld(anchor);

            Vector3 sum = Vector3.zero;
            for (int i = 0; i < cells.Count; i++)
                sum += mapper.CellCenterToWorld(cells[i]);
            return sum / cells.Count;
        }

        private FieldUnitView Rent(EntityKey key)
        {
            if (!_pools.TryGetValue(key, out CustomPool<FieldUnitView> pool))
            {
                pool = new CustomPool<FieldUnitView>(
                    new PoolContext<FieldUnitView>(() => CreateView(key), 0, null, new ViewLifecycle()));
                _pools.Add(key, pool);
            }

            return pool.Get();
        }

        private void Return(EntityKey key, FieldUnitView view)
        {
            if (view != null)
                _pools[key].Return(view);
        }

        private FieldUnitView CreateView(EntityKey key)
        {
            GameObject instance = Instantiate(_visuals.GetUnitVisual(key).Prefab, unitsRoot, false);
            instance.transform.localScale *= _unitScale;
            FieldUnitView view = instance.GetComponent<FieldUnitView>();
            if (view == null)
                view = instance.AddComponent<FieldUnitView>();
            view.AttachHealthLabel(healthLabelTemplate, healthLabelOffset);
            view.AttachUnitHud(unitHudPrefab, unitHudOffset, unitHudScale, _hudCamera);
            return view;
        }

        private sealed class ViewLifecycle : IPoolLifecycle<FieldUnitView>
        {
            public void OnGet(FieldUnitView item)
            {
            }

            public void OnBeforeReturn(FieldUnitView item)
            {
                // 씬과 함께 이미 파괴된 View는 건드리지 않는다.
                if (item != null)
                    item.ResetForPool();
            }

            public void OnReturn(FieldUnitView item)
            {
            }
        }
    }
}
