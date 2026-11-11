using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using System;
using System.Linq;
using System.Collections.Generic;
using ProjectL.Framework.Utility;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.UiEntry;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Selection;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 필드의 유닛·구조물을 그립니다(옛 FieldUnitPresenter 이식, 계획 D.2·D.5).
    /// 값만 받아 ID별 기존 View를 다시 쓰고, 새 대상만 빌리고 사라진 대상만 돌려줍니다(W19).
    /// </summary>
    /// <remarks>
    /// 그리는 값은 Buffer 캐릭터 사실(<c>CharacterData</c>)과 Core 계산기의 표시 값(HUD 공격력 <c>CombatEffectiveStatsCalculator.AttackPower</c>)에서 읽고 판단은 하지 않습니다. 외형은 View DB(UnitVisual)의 프리팹을 종류별 풀로 만듭니다.
    /// 부르는 쪽은 원하는 표시 상태만 말하고 View 찾기는 여기서 합니다(<see cref="SetHiddenEntity"/>, 설계 §3).
    /// 재생 쪽의 유닛 표시 채널(자세·서는 자리·크기·걸음 배치·표시 회전·사망 유지·이동 투명도)은 창구(<see cref="Presentation"/>)가 소유하고,
    /// 사실 그리기는 그 창구에 소유 사실(배치 유지·회전 덮기)을 묻습니다(연출 조립 설계 B13). 사망 모션이 유지하는 대여는 완료 뒤 기존 풀로 반환합니다.
    /// 피해 숫자·공격 예상 체력 같은 HUD 일시 피드백은 <see cref="HudFeedback"/>이 소유합니다(설계 §9 I-2).
    /// </remarks>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display")]
    [DisallowMultipleComponent]
    public sealed class FieldUnitPresenter : MonoBehaviour, ICombatDeckHoverDisplay, ICombatUnitTargets, ICombatUnitPlacementReader
    {
        [Tooltip("필드 유닛 인스턴스를 둘 루트입니다.")]
        [SerializeField] private Transform unitsRoot;
        [Tooltip("체력 숫자 템플릿(TextMeshPro 3D 텍스트). 씬에 비활성으로 두고 연결합니다.")]
        [SerializeField] private TMP_Text healthLabelTemplate;
        [SerializeField] private Vector3 healthLabelOffset = new(0f, 1.5f, 0f);
        [Header("Unit Action Cursor")]
        [Tooltip("아군 행동 가능 표시용 월드 프리팹입니다.")]
        [SerializeField] private GameObject unitActionCursorPrefab;
        [Tooltip("유닛 원점에서 커서 형상 중심까지의 월드 위치 보정입니다.")]
        [SerializeField] private Vector3 unitActionCursorOffset = new(0f, 2.2f, 0f);
        [Tooltip("이동을 마치고 공격 행동이 남은 유닛의 커서 색입니다. 미이동 색은 프리팹 머티리얼을 따릅니다.")]
        [SerializeField] private Color unitActionCursorMovedColor = new(1f, 0.75f, 0.15f, 1f);
        [Header("Unit HUD")]
        [SerializeField] private UnitHUD unitHudPrefab;
        internal UnitDamageText DamageTemplate => unitHudPrefab != null ? unitHudPrefab.DamageTemplate :
            throw new InvalidOperationException("[FieldUnitPresenter] unitHudPrefab이 필요합니다.");
        internal float DamageScreenScale => unitHudScreenScale;
        [SerializeField] private Vector3 unitHudOffset = new(0f, 1.5f, 0f);
        [Tooltip("기준 구도에서의 유닛 HUD 배율입니다. 실제 화면 크기는 현재 카메라 투영에 비례합니다.")]
        [SerializeField, Min(0.001f)] private float unitHudScreenScale = 1.5428572f;
        [Header("Structure HUD")]
        [Tooltip("건물 중심에서 HUD까지의 월드 위치 보정입니다.")]
        [SerializeField] private Vector3 structureHudOffset = new(0f, 1.5f, 0f);
        [Header("Obstacle HUD")]
        [Tooltip("장애물 중심에서 HUD까지의 월드 위치 보정입니다.")]
        [SerializeField] private Vector3 obstacleHudOffset = new(0f, 1.5f, 0f);
        [Tooltip("기준 구도에서의 장애물 HUD 배율입니다. 유닛과 별도로 조절하며 실제 화면 크기는 카메라 투영에 비례합니다.")]
        [SerializeField, Min(0.001f)] private float obstacleHudScreenScale = 1.5428572f;

        private readonly Dictionary<EntityKey, CustomPool<FieldUnitView>> _pools = new();
        private static readonly ProfilerMarker CreateMarker = new("Combat.Units.CreateModel");
        private static readonly ProfilerMarker HudMarker = new("Combat.Units.CreateHUD");
        private static readonly ProfilerMarker ApplyHudMarker = new("Combat.Units.ApplyHUD");
        private static readonly ProfilerMarker PrepareMarker = new("Combat.Units.PrepareSelected");
        private static readonly ProfilerMarker ExtraCreateMarker = new("Combat.Units.CreateOutsidePreparedCapacity");
        private static readonly ProfilerMarker MissingCreateMarker = new("Combat.Units.PreparedCapacityMiss");
        private readonly Dictionary<EntityKey, int> _rentedCounts = new();
        private readonly Dictionary<EntityKey, int> _preparedCounts = new();
        private EntityViewSet<FieldUnitView> _views;
        private IUnitVisualTableQuery _visuals;
        private float _unitScale = 1f;
        private EntityId _hiddenEntityId = EntityId.Empty;
        private CombatUnitPresentationWindow _presentation;
        private CombatUnitHudFeedback _hudFeedback;
        private IConsumableItemVisualTableQuery _itemVisuals;
        private CombatUnitStatuses _statuses;
        // 사실 → 보일 모습(재구성 연출 설계 RE2). 그리기마다 대리자를 만들지 않게 처음 Configure에서 한 번 만듭니다.
        private Func<CharacterCombatFacts, GameObject> _resolveAppearance;
        private CombatUnitActionCursorPresenter _actionCursors;
        // HUD 아이템 아이콘 우클릭의 해제 입구입니다(가방 설계 P6). 입력이 표시보다 늦게 만들어져 조정자가 따로 겁니다.
        private Action<EntityId> _itemUnequip;
        private UnitHUDLayer _hudLayer;
        // 덱 hover 계약은 유지하지만 HUD의 화살표 표시는 제거되었다.
        public void SetDeckHoveredUnit(EntityId id) { }

        private readonly List<UnitHUDStatus> _hudStatuses = new();
        private IColorSystemService _movementColors;
        private readonly List<FieldUnitView> _createdViews = new List<FieldUnitView>();
        // 키별로 만든 표시 수입니다(빌려 간 것 포함, GC 끊김 설계 GH6). 풀과 같은 수명이라 최종 풀 해제(ReleasePools)에서만 비웁니다.
        // 하나만 버리는 길(DiscardView)은 키를 몰라 빼지 않습니다. 그때는 미리 만들기가 하나 덜 만들 뿐입니다.
        private readonly Dictionary<EntityKey, int> _createdCounts = new();
        // 외형 프리팹 루트 BoxCollider 모양(프리팹 루트 크기 반영)입니다(히트 타이밍 설계 H4). 한 Configure 구간 안에서 프리팹마다 한 번 읽고,
        // 콜라이더가 없거나 루트 크기를 곱한 XZ가 0이면 그때 한 번 경고합니다. 현재 배율·배치·접촉 시각은 넣지 않습니다.
        private readonly CombatBodyShapeCache _bodyShapes = new("[FieldUnitPresenter]", includeRootScale: true);
        // 외형마다 한 번 읽은 연출 값 셋(공격 표시·피격 반응·클립 묶음)입니다. 외형 표의 읽기는 매번 SO에서 새 값을 만들고 클립 이벤트를 다시 검사하므로
        // 사건·궤적 갱신마다 읽지 않고 표시 준비 때 채웁니다(클립 계획 T12, 범용 연출 구조 계획 P2). 한 Configure 구간 안에서만 유효합니다.
        private readonly Dictionary<EntityKey, CombatUnitPresentationValues> _presentationValues = new();

        /// <summary>
        /// 재생 쪽이 유닛 표시를 건드리는 창구입니다(연출 조립 설계 B13). 처음 읽을 때 한 번 만들고, 이 표시 집합이 수명 동안 하나를 소유합니다.
        /// <see cref="Configure"/> 전에 만든 사건 Presenter·입력 표시도 같은 창구를 받습니다.
        /// </summary>
        internal CombatUnitPresentationWindow Presentation => _presentation ??= new CombatUnitPresentationWindow(this);

        /// <summary>
        /// 유닛 HUD의 일시 피드백입니다(연출 조립 설계 §9 I-2). 처음 읽을 때 한 번 만들고, 이 표시 집합이 수명 동안 하나를 소유합니다.
        /// </summary>
        internal CombatUnitHudFeedback HudFeedback => _hudFeedback ??= new CombatUnitHudFeedback(this);

        internal void ConfigureMovementAppearance(IColorSystemService colors)
        {
            _movementColors = colors ?? throw new ArgumentNullException(nameof(colors));
        }

        /// <summary>전투 표시를 시작할 때 외형 DB와 크기를 받습니다. 같은 씬에서 다시 불러도 됩니다.</summary>
        /// <param name="statuses">
        /// 걸린 상태(버프·디버프) HUD 칸의 줄 표입니다(특성 발동 연출 설계 TA9). 필수입니다 — 빠뜨리면 사냥꾼의 표식을 포함한 걸린 상태 칸이 조용히 사라지므로
        /// 선택 인자로 두지 않습니다(묶음 끝 검토 L8, View 2라운드 "운영 입구 필수"와 같은 규칙).
        /// </param>
        /// <param name="itemVisuals">장착 아이템 HUD 아이콘의 외형 표입니다(가방 설계 P5). 없으면 기본 아이콘과 키 숫자를 씁니다.</param>
        /// <param name="traitVisuals">기존 조립 호출 호환용입니다. 보유 특성 아이콘을 표시하지 않아 사용하지 않습니다(UH1).</param>
        internal void Configure(IUnitVisualTableQuery visuals, float unitScale, CombatUnitStatuses statuses,
            ITraitVisualTableQuery traitVisuals = null, UnitHUDLayer hudLayer = null,
            IConsumableItemVisualTableQuery itemVisuals = null)
        {
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
            if (!(unitScale > 0f) || float.IsInfinity(unitScale))
                throw new ArgumentOutOfRangeException(nameof(unitScale));

            _unitScale = unitScale;
            _statuses = statuses ?? throw new ArgumentNullException(nameof(statuses));
            _itemVisuals = itemVisuals;
            if (_hudLayer != hudLayer) ReleaseHudLayer();
            _hudLayer = hudLayer;
            _bodyShapes.Clear();
            _presentationValues.Clear();
            // HUD 내용은 사실과 표로 정해지므로 표가 바뀔 수 있는 Configure 뒤에는 모두 다시 씁니다(GH3). 상태가 정하는 모습도 같습니다.
            ForgetHudFacts();
            _resolveAppearance ??= ResolveAppearance;
            _views ??= new EntityViewSet<FieldUnitView>(Rent, Return);
            // 특성 미리보기 씬도 이 Presenter를 사용하지만 행동 커서는 운영 전투에서만 요청합니다.
            if (unitActionCursorPrefab != null)
                _actionCursors ??= new CombatUnitActionCursorPresenter(CursorAnchorOf, unitActionCursorPrefab,
                    unitActionCursorOffset, unitActionCursorMovedColor);
        }

        internal void ValidateActionCursorReference()
        {
            if (unitActionCursorPrefab == null)
                throw new InvalidOperationException("[FieldUnitPresenter] Combat 씬의 Unit Action Cursor 프리팹 연결이 필요합니다.");
        }

        internal void RefreshActionCursors(IReadOnlyList<CharacterData> characters, CombatTurnFacts turn, CombatFlowPhase phase,
            EntityId selectedActorId) =>
            (_actionCursors ?? throw new InvalidOperationException("[FieldUnitPresenter] 전투 행동 커서 프리팹 연결과 Configure가 필요합니다."))
            .Refresh(characters, turn, phase, selectedActorId);

        private Transform CursorAnchorOf(EntityId id)
        {
            if (_views == null || !_views.TryGet(id, out FieldUnitView view) || view == null)
                return null;
            return view.transform;
        }

        // 만든 표시 모두의 HUD 사실 기억을 비웁니다.
        private void ForgetHudFacts()
        {
            for (int i = 0; i < _createdViews.Count; i++)
            {
                if (_createdViews[i] == null) continue;
                _createdViews[i].ForgetHudFacts();
                _createdViews[i].ForgetStatusAppearance();
            }
        }

        // 원본은 상태 줄 표 한 곳입니다(계획 TP-3). 모습을 정하는 상태가 없으면 null(원래 모습)입니다.
        private GameObject ResolveAppearance(CharacterCombatFacts character) =>
            _statuses.TryGetAppearance(character, out GameObject prefab) ? prefab : null;

        /// <summary>
        /// 전투 설치 때 한 번, 키마다 <paramref name="counts"/>만큼 표시와 HUD를 미리 만들어 풀에 넣습니다(GC 끊김 설계 GH6, 계획 TP-6).
        /// 이미 만든 수(빌려 간 것 포함)는 빼고 모자란 만큼만 만들고, 만든 것은 보통 반환 길(<see cref="FieldUnitView.ResetForPool"/>)로 넣습니다.
        /// 연출 값도 이때 읽어 둡니다. 실패하면 그대로 던집니다(계획 TP-7, 처음 나올 때 났을 실패가 앞당겨질 뿐).
        /// </summary>
        /// <param name="counts">키별로 미리 둘 표시 수입니다. 어떤 캐릭터를 셀지는 부르는 쪽(설치)이 정합니다.</param>
        internal void Prewarm(IReadOnlyDictionary<EntityKey, int> counts)
        {
            if (counts == null) throw new ArgumentNullException(nameof(counts));
            if (_views == null)
                throw new InvalidOperationException("[FieldUnitPresenter] Configure가 먼저 필요합니다.");
            foreach (KeyValuePair<EntityKey, int> pair in counts)
            {
                PresentationValuesOf(pair.Key);
                CustomPool<FieldUnitView> pool = PoolOf(pair.Key);
                int created = _createdCounts.TryGetValue(pair.Key, out int count) ? count : 0;
                for (int i = created; i < pair.Value; i++)
                {
                    FieldUnitView view = CreateView(pair.Key);
                    // HUD 생성이 실패해도 표시는 숨겨 풀에 둡니다(켜진 채 원점에 남지 않게). 셀 캐릭터만 세므로 구조물 HUD 자리는 쓰지 않습니다.
                    try { EnsureUnitHud(view, pair.Key, isStructure: false); }
                    finally { pool.Return(view); }
                }
            }
        }

        /// <summary>현재 표시 인스턴스의 대여 수명을 포착하여 이동 중 몸 중심을 조회합니다. 지금 보이지 않으면 null이고, 읽기는 보이지 않게 되면 null을 돌려줍니다.</summary>
        internal Func<Vector3?> TrackBody(EntityId id)
        {
            Func<(bool Bound, Vector3? Body)> presence = TrackBodyPresence(id);
            if (presence == null || !presence().Body.HasValue) return null;
            return () => presence().Body;
        }

        /// <summary>
        /// 현재 표시 인스턴스의 대여 수명을 포착하여 몸 중심과 "숨었을 뿐인지"를 함께 조회합니다(이펙트 따라가기·회수 설계 FE11).
        /// 읽기 결과: <c>Bound</c>가 거짓이면 표시가 사라졌습니다(풀 반환·다른 유닛에 붙음·파괴). 참이고 <c>Body</c>가 null이면 같은 표시가 숨었을 뿐입니다
        /// (배치 끌기에서 원래 유닛 숨김 등). 지금 그 유닛의 표시가 없으면 null입니다.
        /// </summary>
        internal Func<(bool Bound, Vector3? Body)> TrackBodyPresence(EntityId id)
        {
            if (_views == null || !_views.TryGet(id, out var view) || view == null) return null;
            int version = view.BodyTrackingVersion;
            TryGetBodyShape(view.EntityKey, out Vector3 center, out _);
            Vector3 scale = _visuals.GetUnitVisual(view.EntityKey).Prefab.transform.localScale;
            Vector3 local = new(scale.x == 0 ? 0 : center.x / scale.x, scale.y == 0 ? 0 : center.y / scale.y, scale.z == 0 ? 0 : center.z / scale.z);
            return () =>
            {
                bool bound = view != null && view.EntityId == id && view.BodyTrackingVersion == version &&
                    _views != null && _views.TryGet(id, out var current) && ReferenceEquals(current, view);
                return (bound, bound && view.isActiveAndEnabled ? view.transform.TransformPoint(local) : (Vector3?)null);
            };
        }

        /// <summary>
        /// 효과 Follow 받침의 표면 위치입니다. 현재 유닛 View의 XZ를 따라가되 높이는 필드의 표면 높이 표에서 읽습니다.
        /// 도약·자세 연출로 유닛 루트가 높아져도 받침은 표면에 남습니다. 반환·숨김 수명은 몸 추적과 같습니다.
        /// </summary>
        internal Func<(bool Bound, Vector3? Position)> TrackSurfacePresence(EntityId id, FieldGridMapper mapper)
        {
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            if (_views == null || !_views.TryGet(id, out FieldUnitView view) || view == null) return null;
            int version = view.BodyTrackingVersion;
            return () =>
            {
                bool bound = view != null && view.EntityId == id && view.BodyTrackingVersion == version &&
                    _views != null && _views.TryGet(id, out FieldUnitView current) && ReferenceEquals(current, view);
                return (bound, bound && view.isActiveAndEnabled
                    ? mapper.OnSurface(view.transform.position) : (Vector3?)null);
            };
        }

        /// <summary>
        /// 그 키의 외형 프리팹 루트 BoxCollider 모양(프리팹 루트 크기 반영, 루트 로컬 좌표)입니다. 콜라이더가 없거나 XZ 크기가 0이면 false이며
        /// 이 설정 구간에서 프리팹마다 한 번 경고합니다(점 몸 대체, 설계 H4). 콜라이더의 켜짐 여부와 관계없이 값만 읽고 물리는 쓰지 않습니다.
        /// </summary>
        internal bool TryGetBodyShape(EntityKey key, out Vector3 center, out Vector3 size)
        {
            if (_visuals == null)
                throw new InvalidOperationException("[FieldUnitPresenter] Configure가 먼저 필요합니다.");
            return _bodyShapes.TryGet(_visuals.GetUnitVisual(key).Prefab, key, out center, out size);
        }

        /// <summary>
        /// 캐릭터 사실의 접촉 몸(필드 로컬 XZ)입니다. 배치는 표시와 같은 <see cref="CenterOf"/>·<see cref="ModelRotation"/>, 모양은
        /// <see cref="TryGetBodyShape"/>, 크기는 현재 배율입니다. 대여 중인 인스턴스의 현재 Transform은 쓰지 않습니다(자세·크기 대여가 섞임).
        /// 필드 위 위치(<see cref="CharacterCombatFacts.Anchor"/>)가 없으면 예외입니다. 사실 목록은 보관하지 않습니다.
        /// </summary>
        internal CombatBody BodyOf(CharacterCombatFacts facts, FieldGridMapper mapper)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            return BodyOf(facts, ModelRotation(facts.Owner, facts.Rotation, mapper), mapper);
        }

        /// <summary>
        /// 캐릭터 사실의 칸 배치에 지정한 표시 회전을 둔 접촉 몸입니다(이동 접촉 설계 M2). 걸음 보간은 걸음 방향으로 돈 뒤 진행하므로
        /// Core 사실의 방향 대신 보간 중 표시 방향으로 이동자 몸을 만듭니다. 필드 위 위치가 없으면 예외입니다.
        /// </summary>
        internal CombatBody BodyOf(CharacterCombatFacts facts, Quaternion worldRotation, FieldGridMapper mapper)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            if (!facts.Anchor.HasValue)
                throw new InvalidOperationException($"[FieldUnitPresenter] 필드 위치가 없는 대상은 몸을 만들 수 없습니다: {facts.EntityId}.");
            return BodyOf(facts.CharacterKey, CenterOf(facts.OccupiedCells, facts.Anchor.Value, mapper), worldRotation, mapper);
        }

        /// <summary>구조물 사실의 접촉 몸입니다. 회전은 현재 표시와 같은 <c>mapper.RotationToWorld(structure.Rotation)</c>입니다(적 보정 없음).</summary>
        internal CombatBody BodyOf(StructureCombatFacts facts, FieldGridMapper mapper)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            return BodyOf(facts.Definition.Key, CenterOf(facts.OccupiedCells, facts.Anchor, mapper),
                mapper.RotationToWorld(facts.Rotation), mapper);
        }

        /// <summary>
        /// 캐릭터 사실의 몸 상자 중심(월드, 높이 포함)입니다(피격 이펙트 설계 HE3). 배치·모양·크기는 접촉 몸
        /// (<see cref="BodyOf(CharacterCombatFacts, FieldGridMapper)"/>)과 같은 도우미 하나를 따르고, 콜라이더가 없으면 배치 중심(발밑)입니다.
        /// 필드 위 위치가 없으면 예외입니다.
        /// </summary>
        internal Vector3 BodyCenterOf(CharacterCombatFacts facts, FieldGridMapper mapper)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            if (!facts.Anchor.HasValue)
                throw new InvalidOperationException($"[FieldUnitPresenter] 필드 위치가 없는 대상은 몸 중심을 정할 수 없습니다: {facts.EntityId}.");
            PlaceBodyBox(facts.CharacterKey, CenterOf(facts.OccupiedCells, facts.Anchor.Value, mapper),
                ModelRotation(facts.Owner, facts.Rotation, mapper), out Vector3 center, out _);
            return center;
        }

        /// <summary>
        /// 구조물 사실의 몸 상자 중심(월드, 높이 포함)입니다(특성 발동 큐의 대상 몸 자리). 배치·회전은 구조물 접촉 몸
        /// (<see cref="BodyOf(StructureCombatFacts, FieldGridMapper)"/>)과 같고, 콜라이더가 없으면 배치 중심(발밑)입니다.
        /// </summary>
        internal Vector3 BodyCenterOf(StructureCombatFacts facts, FieldGridMapper mapper)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            PlaceBodyBox(facts.Definition.Key, CenterOf(facts.OccupiedCells, facts.Anchor, mapper),
                mapper.RotationToWorld(facts.Rotation), out Vector3 center, out _);
            return center;
        }

        /// <summary>
        /// 캐릭터 몸을 주어진 서는 자세(월드 위치·방향)에 놓은 접촉 몸입니다(유닛 행동 설계 AA3, 계획 TP-20). 유닛 행동이 옮긴 서는 자리의 몸이고,
        /// 상자 배치 식은 사실 몸과 같은 한 곳(<c>PlaceBodyBox</c>)입니다.
        /// </summary>
        internal CombatBody BodyAt(CharacterCombatFacts facts, Pose standing, FieldGridMapper mapper)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            return BodyOf(facts.CharacterKey, standing.position, standing.rotation, mapper);
        }

        /// <summary>캐릭터 몸을 주어진 서는 자세에 놓았을 때의 몸 상자 중심(월드)입니다(유닛 행동 설계 AA3). 콜라이더가 없으면 서는 자리(발밑)입니다.</summary>
        internal Vector3 BodyCenterAt(CharacterCombatFacts facts, Pose standing)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            PlaceBodyBox(facts.CharacterKey, standing.position, standing.rotation, out Vector3 center, out _);
            return center;
        }

        // 상자를 놓은 뒤 필드 로컬 외접 사각형으로 바꿉니다(CombatBodyPlacement, 90도 단위면 정확). 상자가 없으면 점 몸입니다.
        private CombatBody BodyOf(EntityKey key, Vector3 worldCenter, Quaternion worldRotation, FieldGridMapper mapper)
        {
            if (!PlaceBodyBox(key, worldCenter, worldRotation, out Vector3 boxCenter, out Vector3 boxSize))
                return CombatBody.Point(CombatBody.Flatten(mapper.WorldToLocal(worldCenter)));
            return CombatBodyPlacement.FromWorldBox(boxCenter,
                worldRotation * new Vector3(boxSize.x * .5f, 0f, 0f),
                worldRotation * new Vector3(0f, 0f, boxSize.z * .5f), mapper);
        }

        // 몸 상자를 표시와 같은 월드 배치로 놓습니다. 접촉 몸(BodyOf)과 몸 중심(BodyCenterOf)이 함께 쓰는 한 곳입니다.
        // 인스턴스 월드 크기 = unitsRoot 크기 × 프리팹 루트 크기 × 현재 배율로 봅니다(unitsRoot는 균일 크기를 전제).
        // 대여 중인 인스턴스 Transform은 자세·크기 대여가 섞이므로 쓰지 않습니다.
        // 상자가 없으면 false이고 중심은 배치 중심, 크기는 0입니다.
        private bool PlaceBodyBox(EntityKey key, Vector3 worldCenter, Quaternion worldRotation, out Vector3 boxCenter, out Vector3 boxSize)
        {
            if (!TryGetBodyShape(key, out Vector3 shapeCenter, out Vector3 shapeSize))
            {
                boxCenter = worldCenter;
                boxSize = Vector3.zero;
                return false;
            }
            Vector3 scale = (unitsRoot != null ? unitsRoot.lossyScale : Vector3.one) * _unitScale;
            boxCenter = worldCenter + worldRotation * Vector3.Scale(shapeCenter, scale);
            boxSize = Vector3.Scale(shapeSize, scale);
            return true;
        }

        /// <summary>
        /// 캐릭터 사실의 유닛이 칸에 서 있을 때 그려지는 위치·회전입니다. 다시 그리기와 토네이도 떨어지기의 끝 자세가 이 한 곳을 씁니다.
        /// 위치는 <see cref="CenterOf"/>, 회전은 표시 회전이 있으면 그것이고 없으면 사실 회전입니다. 필드 위 위치가 없으면 예외입니다.
        /// </summary>
        internal Pose StandingPoseOf(CharacterCombatFacts facts, FieldGridMapper mapper)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            if (!facts.Anchor.HasValue)
                throw new InvalidOperationException($"[FieldUnitPresenter] 필드 위치가 없는 대상은 서 있는 자세를 정할 수 없습니다: {facts.EntityId}.");
            return StandingPoseOf(facts, mapper, Presentation);
        }

        private static Pose StandingPoseOf(CharacterCombatFacts facts, FieldGridMapper mapper, CombatUnitPresentationWindow presentation) =>
            new(CenterOf(facts.OccupiedCells, facts.Anchor.Value, mapper),
                presentation.TryGetFacing(facts.EntityId, out Quaternion facing) ? facing : ModelRotation(facts.Owner, facts.Rotation, mapper));

        /// <summary>첫 화면의 필드 적과 같은 순서의 기준 칸 Y를 수집합니다. 손패·사망·구조물은 포함하지 않습니다.</summary>
        internal void CollectEntryEnemies(IReadOnlyList<CharacterData> characters, List<FieldUnitView> targets, List<int> gridRows)
        {
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (gridRows == null) throw new ArgumentNullException(nameof(gridRows));
            foreach (CharacterData character in characters)
            {
                CharacterCombatFacts facts = character.ToFacts();
                if (facts.Owner != Owner.Enemy || facts.Area != CharacterRuntimeArea.Field || !facts.Anchor.HasValue || facts.Health.IsDead) continue;
                if (_views == null || !_views.TryGet(facts.EntityId, out FieldUnitView view) || view == null)
                    throw new InvalidOperationException($"[FieldUnitPresenter] 진입할 적 표시가 없습니다: {facts.EntityId}");
                targets.Add(view);
                gridRows.Add(facts.Anchor.Value.Y);
            }
        }

        /// <summary>첫 화면의 살아 있는 적 Nexus와 착지할 점유 칸을 수집합니다. 배경 장식·아군 본부·다른 구조물은 포함하지 않습니다.</summary>
        internal void CollectEntryNexuses(IReadOnlyList<StructureCombatFacts> structures, List<FieldUnitView> targets,
            List<CellCoord> occupiedCells)
        {
            if (structures == null) throw new ArgumentNullException(nameof(structures));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (occupiedCells == null) throw new ArgumentNullException(nameof(occupiedCells));
            foreach (StructureCombatFacts structure in structures)
            {
                if (!IsEnemyNexus(structure) || structure.Health.IsDead) continue;
                if (_views == null || !_views.TryGet(structure.EntityId, out FieldUnitView view) || view == null)
                    throw new InvalidOperationException($"[FieldUnitPresenter] 진입할 적 Nexus 표시가 없습니다: {structure.EntityId}");
                targets.Add(view);
                occupiedCells.AddRange(structure.OccupiedCells);
            }
        }

        /// <summary>
        /// 첫 화면의 살아 있는 보드 위 구조물(장애물·본부 등)을 수집합니다(평가 C10-06). 적 Nexus는 낙하가 맡을 때(<paramref name="includeEnemyNexus"/>가 false)
        /// 빼고, 낙하가 꺼졌으면 다른 구조물과 함께 넣습니다. 적 Nexus를 가르는 조건은 <see cref="CollectEntryNexuses"/>와 같은 <see cref="IsEnemyNexus"/>입니다.
        /// </summary>
        internal void CollectEntryStructures(IReadOnlyList<StructureCombatFacts> structures, bool includeEnemyNexus, List<FieldUnitView> targets)
        {
            if (structures == null) throw new ArgumentNullException(nameof(structures));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            foreach (StructureCombatFacts structure in structures)
            {
                if (structure.Health.IsDead || (!includeEnemyNexus && IsEnemyNexus(structure))) continue;
                if (_views == null || !_views.TryGet(structure.EntityId, out FieldUnitView view) || view == null)
                    throw new InvalidOperationException($"[FieldUnitPresenter] 진입할 구조물 표시가 없습니다: {structure.EntityId}");
                targets.Add(view);
            }
        }

        private static bool IsEnemyNexus(StructureCombatFacts structure) =>
            structure.Owner == Owner.Enemy && structure.Definition.Key == EntityKey.Structure_Headquarters;

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

            HudFeedback.ClearAttackHealthPreview();
            CombatUnitPresentationWindow presentation = Presentation;
            _views.Begin();
            try
            {
                for (int i = 0; i < characters.Count; i++)
                {
                    CharacterCombatFacts character = characters[i];
                    if (character.Area != CharacterRuntimeArea.Field || character.Health.IsDead)
                        presentation.EndMovementOpacityFor(character.EntityId);
                    bool heldDeath = presentation.HoldsDeath(character.EntityId);
                    if (!heldDeath && (character.Area != CharacterRuntimeArea.Field || !character.Anchor.HasValue ||
                        character.Health.IsDead))
                    {
                        presentation.ForgetFacing(character.EntityId);
                        continue;
                    }

                    FieldUnitView view = _views.Acquire(character.EntityId, character.CharacterKey);
                    view.Bind(character.EntityId, character.CharacterKey);
                    view.SetCartoonMaterialFallback(character.Owner == Owner.Enemy && character.Health.MaxHealth > 0);
                    EnsureUnitHud(view, character.CharacterKey, isStructure: false);
                    // 사망 Final은 Excluded로 위치가 없어도 기존 표시 대여의 자세만 유지합니다.
                    // 걸음 보간 중인 유닛은 보간이 Transform을 소유하므로 위치·회전을 그대로 둡니다(이동 접촉 설계 M5 (a)).
                    // 두 소유 사실과 표시 회전의 원본은 창구입니다(연출 조립 설계 B13, B14 O19).
                    bool keepPlacement = presentation.KeepsPlacement(character.EntityId);
                    Pose standing = keepPlacement ? default : StandingPoseOf(character, mapper, presentation);
                    view.Apply(
                        keepPlacement ? view.transform.position : standing.position,
                        keepPlacement ? view.transform.rotation : standing.rotation,
                        character.Health.CurrentHealth,
                        character.Health.MaxHealth,
                        character.EntityId != _hiddenEntityId);
                    // 걸린 상태가 정하는 모습(재구성 중 잔해 등)입니다. 사실 객체가 같으면 상태를 다시 읽지 않습니다(설계 §9.2).
                    // 사망을 붙잡은 동안은 그대로 둡니다. 사망 사실은 반응 시작 직후 반영되고 상태가 지워져 있어, 맞추면 잔해가 반응 도중 원래 모델로 돌아갑니다(Q-RE2 A).
                    if (!heldDeath) view.SyncStatusAppearance(character, _resolveAppearance);
                    RenderUnitHud(view, character);
                }

                for (int i = 0; i < structures.Count; i++)
                {
                    StructureCombatFacts structure = structures[i];
                    if (structure.Health.IsDead)
                        continue;

                    FieldUnitView view = _views.Acquire(structure.EntityId, structure.Definition.Key);
                    view.Bind(structure.EntityId, structure.Definition.Key);
                    view.SetCartoonMaterialFallback(structure.Owner == Owner.Enemy && structure.Health.MaxHealth > 0);
                    EnsureUnitHud(view, structure.Definition.Key, isStructure: true);
                    bool showHealthHud = view.Hud != null &&
                        (structure.Owner == Owner.Enemy || ObstacleContentProfile.IsNativeObstacleKey(structure.Definition.Key));
                    view.ShowUnitHud(showHealthHud);
                    if (showHealthHud)
                    {
                        view.Hud.SetStructure(structure);
                        if (CombatInspectionFacts.CanInspectStructure(structure))
                        {
                            var visual = _visuals.GetUnitVisual(structure.Definition.Key);
                            view.Hud.SetStructureInfo(visual.DisplayName, structure.Definition.AttackPower);
                        }
                    }
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
            using var sample = ApplyHudMarker.Auto();
            UnitHUD hud = view.Hud;
            if (hud == null) return;
            // HUD 내용은 이 사실(불변)과 전투 동안 바뀌지 않는 표로만 정해집니다. 같은 사실 객체를 이미 썼으면 보이기만 맞추고 다시 쓰지 않습니다(GC 끊김 설계 GH3).
            // 사건마다 모든 유닛을 다시 그려도 HUD는 사실이 바뀐 유닛만 씁니다. 위치·체력 숫자는 view.Apply가 지금처럼 늘 맞춥니다.
            if (view.ShowsHudFacts(character))
            {
                view.ShowUnitHud(true);
                return;
            }
            hud.ValidateReferences();
            var unitVisual = _visuals.GetUnitVisual(character.CharacterKey);
            view.ShowUnitHud(true);
            hud.SetUnit(character);
            hud.SetInfo(unitVisual.DisplayName,
                CombatEffectiveStatsCalculator.AttackPower(character),
                character.TraitEffects.ActiveShieldTotal);
            _hudStatuses.Clear();
            // 아이템은 고정 슬롯으로 전달하고, 상태 영역에는 버프/디버프만 둡니다.
            hud.SetItem(character.Items.EquippedItemKey is int itemKey
                ? ItemStatus(itemKey, character.Owner == Owner.Player) : (UnitHUDStatus?)null);
            // 현재 효과만 버프 → 디버프로 더합니다. 격파 목표·보유 특성은 이 영역에 표시하지 않습니다(UH1).
            _statuses.AddTo(character, _hudStatuses);
            hud.SetStatuses(_hudStatuses);
            view.RememberHudFacts(character);
        }

        internal void SetHudActionAvailability(EntityId entityId, CombatActionAvailability availability)
        {
            if (_views != null && _views.TryGet(entityId, out FieldUnitView view) && view != null && view.Hud != null)
            {
                view.Hud.SetActionAvailability(availability);
                return;
            }

            // 사망·풀 반환 뒤의 숨김은 이미 정리된 HUD에도 요청할 수 있습니다.
            if (availability != null)
                throw new InvalidOperationException($"[FieldUnitPresenter] UIId.UnitHUDLayer, '{entityId}': 행동 상태를 표시할 유닛 HUD가 필요합니다.");
        }

        /// <summary>준비 성공 때만 계측 기준을 고정합니다. 선택 원본이나 별도 자원 풀로 쓰지 않습니다.</summary>
        internal void MarkPreparationComplete(IReadOnlyDictionary<EntityKey, int> counts)
        {
            _preparedCounts.Clear();
            foreach (var pair in counts) _preparedCounts.Add(pair.Key, pair.Value);
        }

        /// <summary>실제 필드 대여가 있으면 사용하고, 없으면 기존 종류별 풀에서 별도로 빌립니다.</summary>
        internal CombatUnitPreparationLease PrepareSelectedUnit(in CombatUnitPreparationTarget target, bool isCovered)
        {
            using var sample = PrepareMarker.Auto();
            if (_views == null || _hudLayer == null || unitHudPrefab == null)
                throw new InvalidOperationException("[FieldUnitPresenter] 선택 유닛 준비에는 Configure와 HUD 참조가 필요합니다.");
            FieldUnitView view = null;
            bool existing = target.Character != null && _views.TryGet(target.Character.EntityId, out view);
            if (existing && (view == null || view.EntityKey != target.Key))
                throw new InvalidOperationException($"[FieldUnitPresenter] 준비할 기존 필드 대여가 유효하지 않습니다: {target.OwnedId}.");
            if (!existing) view = Rent(target.Key);
            var lease = new CombatUnitPreparationLease(view, target.Key, !existing, _hudLayer, Return);
            try
            {
                EnsureUnitHud(view, target.Key, isStructure: false);
                if (!existing)
                {
                    if (target.Character != null) RenderUnitHud(view, target.Character.ToFacts());
                    else view.Hud.PrepareResources();
                }
                lease.Activate(isCovered);
                return lease;
            }
            catch (Exception error)
            {
                try { lease.Dispose(); }
                catch (Exception cleanup) { throw new AggregateException(error, cleanup); }
                throw;
            }
        }

        /// <summary>장착 아이템 HUD 상태의 키입니다. HUD 우클릭(가방 설계 P6)이 이 키로 아이템 아이콘을 가립니다.</summary>
        internal const string ItemStatusKey = "Item";

        private UnitHUDStatus ItemStatus(int itemKey, bool canUnequip)
        {
            if (_itemVisuals != null && _itemVisuals.TryGet(itemKey, out var visual))
                return new UnitHUDStatus(ItemStatusKey, visual.Icon,
                    string.IsNullOrEmpty(visual.Description) ? visual.DisplayName : $"{visual.DisplayName}\n{visual.Description}",
                    canUnequip);
            return new UnitHUDStatus(ItemStatusKey, null, itemKey.ToString(), canUnequip);
        }

        /// <summary>
        /// HUD 아이템 아이콘 우클릭을 보낼 해제 입구입니다(가방 설계 P6). 조정자가 입력을 만든 뒤 걸고, 입력을 정리할 때 null로 풉니다.
        /// 걸려 있지 않으면 우클릭은 아무것도 하지 않습니다. 해제 가능 여부(국면·아군·장착 칸)는 입구가 판단합니다.
        /// </summary>
        internal void BindItemUnequip(Action<EntityId> unequip) => _itemUnequip = unequip;

        // HUD는 (개체, 상태 Key)만 넘깁니다. 아이템 상태일 때만 해제 입구로 보냅니다.
        private void OnHudStatusSecondary(EntityId id, string statusKey)
        {
            if (statusKey == ItemStatusKey)
                _itemUnequip?.Invoke(id);
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
            _actionCursors?.Clear();
            _preparedCounts.Clear();
            SetDeckHoveredUnit(EntityId.Empty);
            List<Exception> errors = null;
            try { Presentation.EndMovementOpacity(); }
            catch (Exception error) { (errors ??= new List<Exception>()).Add(error); }
            // 반환 콜백의 반응 취소가 같은 대여를 다시 반환하지 않도록 유지권부터 무효화합니다. 표시 회전·걸음 배치도 함께 버립니다.
            Presentation.Reset();
            try { HudFeedback.ClearAttackHealthPreview(); }
            catch (Exception error) { (errors ??= new List<Exception>()).Add(error); }
            _hiddenEntityId = EntityId.Empty;
            _bodyShapes.Clear();
            try { _views?.Clear(); }
            catch (Exception error) { (errors ??= new List<Exception>()).Add(error); }
            foreach (FieldUnitView view in _createdViews)
            {
                if (view == null) continue;
                try { view.ReleaseMovementAppearance(); }
                catch (Exception error) { (errors ??= new List<Exception>()).Add(error); }
                try { view.ReleaseClipChannel(); }
                catch (Exception error) { (errors ??= new List<Exception>()).Add(error); }
            }
            if (errors != null) throw new AggregateException("[FieldUnitPresenter] 이동 재질·클립 채널 자원 정리에 실패했습니다.", errors);
        }

        /// <summary>최종 씬 연결 해제 때 풀 안의 HUD까지 해제합니다. 일반 Clear는 풀과 HUD를 재사용합니다.</summary>
        internal void ReleaseHudLayer()
        {
            foreach (FieldUnitView view in _createdViews)
                if (view != null) view.ReleaseUnitHud();
            _hudLayer = null;
        }

        /// <summary>
        /// 최종 표시 연결 해제 때 모델 풀도 종료합니다. 일반 Clear/풀 반환은 이 메서드를 부르지 않습니다.
        /// 같은 씬을 재사용하더라도 이전 연결에서 해제한 HUD·클립을 가진 여분 객체가 다음 풀 앞에 남지 않습니다.
        /// </summary>
        internal void ReleasePools()
        {
            _actionCursors?.Release();
            _actionCursors = null;
            List<Exception> errors = null;
            foreach (FieldUnitView view in _createdViews.ToArray())
                CleanupErrors.Try(() => DiscardView(view), ref errors);
            _createdViews.Clear();
            _pools.Clear();
            _createdCounts.Clear();
            _rentedCounts.Clear();
            _preparedCounts.Clear();
            CleanupErrors.ThrowIfAny(errors, "[FieldUnitPresenter] 모델 풀 최종 해제에 실패했습니다.");
        }

        private void OnDestroy() => ReleaseHudLayer();

        /// <summary>빠진 씬 참조 이름입니다. 없으면 null입니다.</summary>
        internal string FindMissingReference()
        {
            if (unitsRoot == null)
                return $"{nameof(FieldUnitPresenter)}.{nameof(unitsRoot)}";
            if (healthLabelTemplate == null)
                return $"{nameof(FieldUnitPresenter)}.{nameof(healthLabelTemplate)}";
            // 피해 숫자 틀(DamageTemplate)·선택 유닛 준비와 같은 참조입니다. 운영과 특성 미리보기가 모두 이 검사를 지나게 합니다(평가 C10-17).
            if (unitHudPrefab == null)
                return $"{nameof(FieldUnitPresenter)}.{nameof(unitHudPrefab)}";
            return null;
        }

        bool ICombatUnitTargets.TryGet(EntityId id, out ICombatUnitPresentationTarget target)
        {
            target = null;
            if (_views == null || !_views.TryGet(id, out var view) || view == null)
                return false;
            target = view;
            return true;
        }

        /// <summary>그 ID가 지금 대여 중인 표시의 강조 계약입니다(튜토리얼 강조). 켜짐 여부와 관계없습니다. 없으면 false입니다.</summary>
        internal bool TryGetHighlightTarget(EntityId id, out ICombatUnitHighlightTarget target)
        {
            target = null;
            if (_views == null || !_views.TryGet(id, out FieldUnitView view) || view == null)
                return false;
            target = view;
            return true;
        }

        bool ICombatUnitPlacementReader.TryGetPlacedPosition(EntityId id, out Vector3 position)
        {
            position = default;
            if (_views == null || !_views.TryGet(id, out var view) || view == null || !view.isActiveAndEnabled)
                return false;
            position = view.transform.position;
            return true;
        }

        bool ICombatUnitTargets.TryGetHud(EntityId id, out ICombatUnitHudTarget target)
        {
            target = null;
            if (_views == null || !_views.TryGet(id, out var view) || view == null)
                return false;
            target = view;
            return true;
        }

        bool ICombatUnitTargets.IsRented(EntityId id, ICombatUnitPresentationTarget target) =>
            target is FieldUnitView view && view != null && _views != null &&
            _views.TryGet(id, out var rented) && ReferenceEquals(rented, view);

        void ICombatUnitTargets.ReleaseRental(EntityId id, ICombatUnitPresentationTarget target) =>
            _views.Release(id, (FieldUnitView)target);

        CombatAttackPresentationData ICombatUnitTargets.AttackSettingsOf(EntityKey key) => PresentationValuesOf(key).Attack;

        CombatUnitReactionPresentationData ICombatUnitTargets.ReactionSettingsOf(EntityKey key) => PresentationValuesOf(key).Reaction;

        CombatUnitAnimationData ICombatUnitTargets.AnimationSettingsOf(EntityKey key) => PresentationValuesOf(key).Animation;

        private CombatUnitPresentationValues PresentationValuesOf(EntityKey key)
        {
            if (!_presentationValues.TryGetValue(key, out CombatUnitPresentationValues values))
            {
                var visual = _visuals.GetUnitVisual(key);
                values = new CombatUnitPresentationValues(visual.AttackPresentation, visual.ReactionPresentation, visual.AnimationPresentation);
                _presentationValues.Add(key, values);
            }
            return values;
        }

        Vector3 ICombatUnitTargets.FactPosition(CharacterCombatFacts facts, FieldGridMapper mapper) =>
            CenterOf(facts.OccupiedCells, facts.Anchor.Value, mapper);

        Quaternion ICombatUnitTargets.FactRotation(CharacterCombatFacts facts, FieldGridMapper mapper) =>
            ModelRotation(facts.Owner, facts.Rotation, mapper);

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

        /// <summary>
        /// 점유 칸에 선 대상의 그리기 위치입니다: 칸 중심(바닥)의 평균 + 칸 표면 높이(고지대 View 설계 HG-2).
        /// 서 있는 위치·걸음 양 끝·몸 상자가 이 한 곳을 씁니다. 평지에서는 바닥 평균 그대로입니다.
        /// </summary>
        internal static Vector3 CenterOf(IReadOnlyList<CellCoord> cells, CellCoord anchor, FieldGridMapper mapper)
        {
            Vector3 ground;
            if (cells == null || cells.Count == 0)
                ground = mapper.CellCenterToWorld(anchor);
            else
            {
                Vector3 sum = Vector3.zero;
                for (int i = 0; i < cells.Count; i++)
                    sum += mapper.CellCenterToWorld(cells[i]);
                ground = sum / cells.Count;
            }
            return ground + mapper.SurfaceOffset(mapper.SurfaceHeightOf(cells, anchor));
        }

        private FieldUnitView Rent(EntityKey key)
        {
            CustomPool<FieldUnitView> pool = PoolOf(key);
            FieldUnitView view = pool.Get();
            try
            {
                view.PrepareMovementAppearance(_movementColors);
                view.PrepareClipChannel();
                // 연출 값 셋은 표시 준비 때 읽어 둡니다(사건 재생 중에 처음 읽지 않게).
                PresentationValuesOf(key);
                _rentedCounts.TryGetValue(key, out int rented);
                _rentedCounts[key] = rented + 1;
                return view;
            }
            catch (Exception error)
            {
                try { pool.Return(view); }
                catch (Exception cleanup)
                {
                    var errors = new List<Exception> { error, cleanup };
                    try { DiscardView(view); }
                    catch (Exception discard) { errors.Add(discard); }
                    throw new AggregateException(errors);
                }
                throw;
            }
        }

        // 그 키의 풀입니다. 처음 부를 때 만듭니다.
        private CustomPool<FieldUnitView> PoolOf(EntityKey key)
        {
            if (!_pools.TryGetValue(key, out CustomPool<FieldUnitView> pool))
            {
                pool = new CustomPool<FieldUnitView>(
                    new PoolContext<FieldUnitView>(() => CreateView(key), 0, null, new ViewLifecycle()));
                _pools.Add(key, pool);
            }
            return pool;
        }

        private void Return(EntityKey key, FieldUnitView view)
        {
            List<Exception> errors = null;
            if (view != null)
            {
                _actionCursors?.Hide(view.transform);
                try { HudFeedback.OnReturned(view.EntityId); }
                catch (Exception error) { (errors ??= new List<Exception>()).Add(error); }
                try { Presentation.OnReturned(view.EntityId); }
                catch (Exception error) { (errors ??= new List<Exception>()).Add(error); }
                try { _pools[key].Return(view); }
                catch (Exception error)
                {
                    (errors ??= new List<Exception>()).Add(error);
                    // OnBeforeReturn 실패라 큐에 들어가지 못했습니다. 재시도하지 않고 소유 자원을 해제합니다.
                    try { DiscardView(view); }
                    catch (Exception discard) { errors.Add(discard); }
                }
            }
            // 빈 반환(C# null)만 대여 수를 줄이지 않습니다(평가 C10-16). 외부에서 파괴된 View(Unity null)도 대여 계측 수명은 끝냅니다
            // (CombatUnitPreparationLease 정리와 같은 약속).
            if (!ReferenceEquals(view, null) && _rentedCounts.TryGetValue(key, out int count)) _rentedCounts[key] = count - 1;
            CleanupErrors.ThrowIfAny(errors, "[FieldUnitPresenter] 유닛 반환에 실패했습니다.");
        }

        private void DiscardView(FieldUnitView view)
        {
            _createdViews.Remove(view);
            if (view == null) return;
            _actionCursors?.Forget(view.transform);
            List<Exception> errors = null;
            CleanupErrors.Try(() => view.gameObject.SetActive(false), ref errors);
            CleanupErrors.Try(view.ReleaseUnitHud, ref errors);
            CleanupErrors.Try(view.ReleaseMovementAppearance, ref errors);
            CleanupErrors.Try(view.ReleaseClipChannel, ref errors);
            if (Application.isPlaying) Destroy(view.gameObject);
            else DestroyImmediate(view.gameObject);
            CleanupErrors.ThrowIfAny(errors, "[FieldUnitPresenter] 실패한 유닛 자원 해제에 실패했습니다.");
        }

        private FieldUnitView CreateView(EntityKey key)
        {
            using var sample = CreateMarker.Auto();
            _rentedCounts.TryGetValue(key, out int rented);
            bool missing = _preparedCounts.TryGetValue(key, out int prepared) && rented < prepared;
            using var capacitySample = (missing ? MissingCreateMarker : ExtraCreateMarker).Auto();
            GameObject instance = Instantiate(_visuals.GetUnitVisual(key).Prefab, unitsRoot, false);
            FieldUnitView view = null;
            try
            {
                instance.transform.localScale *= _unitScale;
                view = instance.GetComponent<FieldUnitView>();
                if (view == null) view = instance.AddComponent<FieldUnitView>();
                view.PrepareMovementAppearance(_movementColors);
                view.PrepareClipChannel();
                view.AttachHealthLabel(healthLabelTemplate, healthLabelOffset);
                _createdViews.Add(view);
                // 만든 수는 성공한 생성만 셉니다(미리 만들기가 모자란 수를 계산, GC 끊김 설계 GH6). 최종 풀 해제(ReleasePools)가 비웁니다.
                _createdCounts[key] = _createdCounts.TryGetValue(key, out int count) ? count + 1 : 1;
                return view;
            }
            catch (Exception error)
            {
                var errors = new List<Exception> { error };
                if (view != null)
                {
                    CleanupErrors.Try(view.ReleaseMovementAppearance, ref errors);
                    CleanupErrors.Try(view.ReleaseClipChannel, ref errors);
                }
                if (Application.isPlaying) Destroy(instance);
                else DestroyImmediate(instance);
                throw new AggregateException("[FieldUnitPresenter] 유닛 생성에 실패했습니다.", errors);
            }
        }

        private void EnsureUnitHud(FieldUnitView view, EntityKey key, bool isStructure)
        {
            if (view.Hud != null) return;
            // 빠진 HUD 프리팹은 진입 참조 검사(FindMissingReference)가 막습니다. 여기서 조용히 건너뛰지 않습니다(평가 C10-17).
            if (unitHudPrefab == null) throw new InvalidOperationException("[FieldUnitPresenter] unitHudPrefab 연결이 필요합니다.");
            using var sample = HudMarker.Auto();
            if (_hudLayer == null) throw new InvalidOperationException("[FieldUnitPresenter] UnitHUDLayer 연결이 필요합니다.");
            bool isObstacle = ObstacleContentProfile.IsNativeObstacleKey(key);
            float scale = isObstacle ? obstacleHudScreenScale : unitHudScreenScale;
            if (!(scale > 0f) || float.IsInfinity(scale))
                throw new InvalidOperationException("[FieldUnitPresenter] HUD 화면 배율은 유한한 양수여야 합니다.");
            UnitHUD hud = Instantiate(unitHudPrefab, _hudLayer.transform, false);
            var rect = (RectTransform)hud.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.localRotation = Quaternion.identity;
            rect.localScale *= scale;
            Vector3 offset = isObstacle ? obstacleHudOffset : isStructure ? structureHudOffset : unitHudOffset;
            try { view.AttachUnitHud(hud, offset, _hudLayer); }
            catch
            {
                if (Application.isPlaying) Destroy(hud.gameObject);
                else DestroyImmediate(hud.gameObject);
                throw;
            }
            hud.SetStatusSecondaryHandler(OnHudStatusSecondary);
        }

        private sealed class ViewLifecycle : IPoolLifecycle<FieldUnitView>
        {
            public void OnGet(FieldUnitView item)
            {
                // 비활성화로 취소·반환된 표시도 다음 대여에서는 반응 수명을 사용할 수 있어야 합니다.
                if (item != null) item.enabled = true;
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
