using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// 전투 필드의 좌표 변환·칸 집기와 칸 강조 층 API를 소유합니다(설계 §6·§6.1, 계획 D.1, 격자 아웃라인 설계 F2).
    /// 옛 develop-4week의 FieldRoot·FieldRuntime을 이식했습니다.
    /// </summary>
    /// <remarks>
    /// 강조 상태는 <see cref="FieldHighlightState"/> 한 곳에 두고, 그리기는 표시 모드별 <see cref="IFieldHighlightRenderer"/>에 맡깁니다.
    /// 표시 모드·셀 프리팹·간격은 View DB(StageFieldView)에서, 강조 색은 이 컴포넌트의 씬 설정에서 읽습니다.
    /// 정적 구성은 전투 설치 때 한 번 하고, 결과마다 셀을 다시 만들지 않습니다. Buffer·Core를 모르고 값만 받습니다.
    /// 공격 궤적 점선(<see cref="FieldTrajectoryLine"/>)도 필드 구성·해제 수명을 따르며, 그리는 경로는 궤적 구성 쪽이 정합니다(공격 궤적 설계 TJ2).
    /// 넉백 미리보기 화살표(<see cref="FieldKnockbackMarkers"/>)도 같은 수명이고, 도착 칸은 이 필드의 넉백 도착 층에 칠합니다(넉백 미리보기 계획 TP-1).
    /// 강조 그리기는 프레임 끝입니다(GC 끊김 설계 GH1): 층 API는 상태만 바꾸고 바뀐 칸을 모으며, <c>LateUpdate</c>에서 한 번 그립니다.
    /// 한 프레임에 강조를 여러 번 바꿔도 마지막 상태 하나만 그립니다. 그린 결과를 같은 프레임에 바로 읽는 코드를 만들지 않습니다.
    /// 구성(<see cref="Build"/>)은 바로 그리고, 이 컴포넌트가 꺼져 있거나 재생 중이 아니면(LateUpdate가 돌지 않음) 층 API도 바로 그립니다.
    /// 실행 순서를 맨 뒤로 둬 다른 <c>LateUpdate</c>(예: UI 입력 연결의 호버 해제)가 바꾼 강조도 같은 프레임에 그립니다.
    /// 실행 순서가 정하는 이 컴포넌트의 Unity 콜백은 <c>LateUpdate</c> 하나뿐입니다(<c>OnDestroy</c>·<c>OnValidate</c>는 순서와 무관).
    /// </remarks>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display")]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(DrawOrder)]
    public sealed class FieldView : MonoBehaviour, ICombatTrajectoryCanvas, ICombatKnockbackCanvas
    {
        [Tooltip("셀이 놓일 루트. 월드 크기 (1,1,1)이어야 합니다. CellPrefab 모드는 Y축 회전만 허용하고, GridOutline 모드는 회전을 허용하지 않습니다(돌아가 있으면 전투 설치 때 예외).")]
        [SerializeField] private Transform cellsRoot;

        // 칸 강조 색은 쓰임새별 다섯 묶음입니다. 겹치면 호버 > 막힌 벽 > 넉백·끌어오기 도착 > 영향 > 재생 강조 > 예정 경로 > 위험 범위·적 공격 범위·아군 호버 범위·공격 범위 > 이동 범위
        // > 현재 행위자 > 전투 목표 > 정적 구역 순으로 위가 이깁니다(원본 FieldHighlightLayers). 필드 순서는 인스펙터 묶음만 정하고 우선순위와는 상관없습니다.
        [Header("정적 구역 색 (가장 아래 층)")]
        [Tooltip("아군 배치 구역. CellPrefab 모드는 항상 칸 색, GridOutline 모드는 배치 드래그 중에만 테두리·채우기로 보입니다.")]
        [SerializeField] private Color playerZoneColor = new(0.55f, 0.75f, 1f, 1f);
        [Tooltip("레일 칸. 두 모드 모두 행동 중이 아니어도 보입니다.")]
        [SerializeField] private Color railColor = new(0.85f, 0.75f, 0.45f, 1f);

        [Header("호버 칸 색 (모든 층 위)")]
        [Tooltip("공용 호버 칸(판단 전). 공용 호버 색 셋은 배치·아이템 Drag와 이동 경로 끝점이 씁니다. GridOutline 모드는 호버 칸 채우기도 이 색입니다.")]
        [SerializeField] private Color hoverColor = new(1f, 1f, 0.6f, 1f);
        [Tooltip("공용 호버 칸(가능). Core 응답이 가능일 때입니다.")]
        [SerializeField] private Color hoverValidColor = new(0.35f, 1f, 0.45f, 1f);
        [Tooltip("공용 호버 칸(불가). Core 응답이 불가일 때입니다.")]
        [SerializeField] private Color hoverInvalidColor = new(1f, 0.3f, 0.3f, 1f);
        [Tooltip("공격 조준 호버 칸(판단 전). 공격 호버 색 셋은 GridOutline 모드의 호버 칸·영향 칸 채우기도 따릅니다.")]
        [SerializeField] private Color attackHoverColor = new(1f, 1f, 0.6f, 1f);
        [Tooltip("공격 조준 호버 칸(공격 가능).")]
        [SerializeField] private Color attackHoverValidColor = new(0.35f, 1f, 0.45f, 1f);
        [Tooltip("공격 조준 호버 칸(공격 불가). 막힌 벽 층도 이 색입니다.")]
        [SerializeField] private Color attackHoverInvalidColor = new(1f, 0.3f, 0.3f, 1f);

        [Header("입력·미리보기 층 색 (위 칸이 이김)")]
        [Tooltip("호버한 공격 조준의 영향 칸 테두리(CellPrefab 모드는 칸 색). 결과 재생 강조(포효·소환 칸)도 이 색입니다. GridOutline 채우기는 공격 호버 상태 색입니다.")]
        [SerializeField] private Color affectedColor = new(1f, 0.55f, 0.15f, 1f);
        [Tooltip("드래그한 이동 예정 경로. Core의 이동 가능 여부는 끝점의 공용 호버 색으로 구분합니다.")]
        [SerializeField] private Color movementDraftColor = new(0.05f, 1f, 0.75f, 1f);
        [Tooltip("지뢰·화약통에 호버했을 때 그 피해 칸(진영을 가리지 않는 위험). 유닛 호버 범위와 함께 보이지 않습니다. GridOutline 모드는 채우기도 이 색입니다.")]
        [SerializeField] private Color hazardRangeColor = new(0.75f, 0.45f, 1f, 1f);
        [Tooltip("적 유닛에 호버했을 때 그 유닛의 공격 범위 칸. 공격 범위와 함께 보이지 않습니다. GridOutline 모드는 채우기도 이 색입니다.")]
        [SerializeField] private Color enemyAttackRangeColor = new(1f, 0.45f, 0.35f, 1f);
        [Tooltip("공격 범위 응답의 칸. 공격 모드와 아군 유닛 호버 범위가 씁니다. GridOutline 모드는 채우기도 이 색입니다.")]
        [SerializeField] private Color attackRangeColor = new(0.45f, 0.85f, 1f, 1f);
        [Tooltip("적 유닛에 호버했을 때 그 유닛의 이동 칸. 그 위에 적 공격 범위가 그려집니다. 이동 모드 이동 범위와 겹칠 수 있어 다른 색을 권장합니다. GridOutline 모드는 채우기도 이 색입니다.")]
        [SerializeField] private Color enemyMoveRangeColor = new(0.45f, 0.85f, 1f, 1f);
        [Tooltip("아군 유닛에 호버했을 때 그 유닛의 이동 칸. 그 위에 아군 공격 범위가 그려집니다. 이동 모드 이동 범위와 겹칠 수 있어 다른 색을 권장합니다. GridOutline 모드는 채우기도 이 색입니다.")]
        [SerializeField] private Color allyHoverMoveRangeColor = new(0.45f, 0.85f, 1f, 1f);
        [Tooltip("이동 범위 응답의 칸(이동 모드·이동 Drag). GridOutline 모드는 채우기도 이 색입니다.")]
        [FormerlySerializedAs("rangeColor")]
        [SerializeField] private Color moveRangeColor = new(0.45f, 0.85f, 1f, 1f);

        [Header("상태 층 색 (행동 중이 아니어도 보임)")]
        [Tooltip("현재 행위자가 점유한 칸.")]
        [SerializeField] private Color activeActorColor = new(1f, 0.95f, 0.3f, 1f);
        [Tooltip("전투 목표(지정 보스)가 점유한 칸. 현재 행위자 아래, 정적 구역 위입니다.")]
        [SerializeField] private Color objectiveTargetColor = new(1f, 0.25f, 0.75f, 1f);

        [Header("기믹·먼지 연막 호버 범위 색")]
        [Tooltip("호버한 토네이도·하붑·Rider 먼지 연막 그룹의 현재 영역입니다. 유닛 범위·이동 경로·공격 조준 아래에 표시하며, GridOutline 모드는 채우기도 이 색입니다.")]
        [SerializeField] private Color gimmickHoverRangeColor = new(0.15f, 0.85f, 0.75f, 1f);

        [Header("GridOutline 채우기")]
        [Tooltip("GridOutline 모드에서 칸을 채우는 불투명도입니다. 모든 채우기(배치 구역·범위 일곱·영향·넉백 도착·호버)가 함께 쓰며, 색은 각 강조 색의 RGB를 그대로 쓰고 알파는 강조 색 알파 × 이 값입니다.")]
        [SerializeField, Range(0f, 1f)] private float fillAlpha = 0.25f;

        [Header("공격 궤적 색 (공격 궤적 설계 TJ8)")]
        [Tooltip("공격 모드에서 호버한 칸까지의 궤적 점선(공격 가능). 칸 강조 팔레트와 따로 궤적에만 씁니다.")]
        [SerializeField] private Color trajectoryColor = new(1f, 0.95f, 0.75f, 1f);
        [Tooltip("벽에 막힌 궤적 점선.")]
        [SerializeField] private Color trajectoryBlockedColor = new(1f, 0.3f, 0.3f, 1f);

        [Header("넉백 미리보기 색 (넉백 미리보기 설계 KB6)")]
        [Tooltip("밀린 대상이 도착할 칸의 테두리와 채우기(GridOutline 모드, 채우기 불투명도는 fillAlpha). CellPrefab 모드는 칸 색입니다.")]
        [SerializeField] private Color knockbackDestinationColor = new(0.05f, 1f, 0.75f, 1f);
        [Tooltip("밀리는 대상의 화살표(다 가거나 중간에 멈춤). 칸 강조 팔레트와 따로 화살표에만 씁니다.")]
        [SerializeField] private Color knockbackColor = new(1f, 0.85f, 0.2f, 1f);
        [Tooltip("막혀서 덜 가거나 못 가는 대상의 화살표.")]
        [SerializeField] private Color knockbackBlockedColor = new(1f, 0.3f, 0.3f, 1f);
        [Tooltip("넉백 면역이라 안 움직이는 대상의 화살표.")]
        [SerializeField] private Color knockbackImmuneColor = new(0.65f, 0.65f, 0.65f, 1f);

        // 강조를 바꾸는 다른 LateUpdate보다 늦게 돌기 위한 실행 순서입니다(GC 끊김 계획 §3.7 L4).
        private const int DrawOrder = 10000;

        [Header("낙석 경고 색 (낙석 View 계획 RP-16)")]
        [Tooltip("낙석이 떨어질 칸의 테두리와 채우기입니다. 느낌표는 이미지 색 그대로라, 이 값을 바꾸면 느낌표 이미지(Sprites/rockfall_warning.png)도 함께 맞춥니다.")]
        [SerializeField] private Color rockfallWarningColor = new(0.918f, 0.278f, 0.271f, 1f);

        private readonly FieldHighlightState _state = new();
        private IFieldHighlightRenderer _renderer;
        // 배경·유닛과 분리된 필드 표시만 묶습니다. 진입 중 격자를 숨겨도 생성 타일은 계속 움직입니다.
        private Transform _visualRoot;

        internal void SetEntryVisible(bool visible)
        {
            _renderer?.SetVisible(visible);
        }
        private FieldGridMapper _mapper;
        private FieldTrajectoryLine _trajectory;
        private FieldKnockbackMarkers _knockbackMarkers;
        // 프레임 끝에 그릴 바뀐 칸입니다(GH1). 상태가 돌려주는 목록은 다시 쓰는 목록이라 옮겨 담고, 표시 배열로 같은 칸을 한 번만 담습니다.
        // 둘 다 구성 때 칸 수만큼 한 번 만들어 그리기 대기에 할당이 없습니다. 칸과 무관한 변경은 "전체 다시"로 표시합니다.
        private bool[] _pendingMarks = Array.Empty<bool>();
        private readonly List<int> _pendingCells = new();
        private bool _pendingRefresh;

        /// <summary>현재 필드 좌표 변환입니다. 구성 전에는 null입니다.</summary>
        internal FieldGridMapper Mapper => _mapper;

        /// <summary>셀이 놓이는 기준 Transform입니다. 필드 기준 장식(배경 프랍)도 여기에 붙입니다. FieldView 오브젝트와 위치가 다를 수 있습니다.</summary>
        internal Transform CellsRoot => cellsRoot;

        /// <summary>칸 강조 상태의 읽기 계약입니다(표시기·테스트). 바꾸는 것은 이 컴포넌트의 층 API뿐입니다(D4).</summary>
        internal IFieldHighlightStateReader HighlightState => _state;

        /// <summary>
        /// 보드 크기만큼 필드를 구성합니다. 같은 크기로 이미 구성돼 있으면 아무것도 하지 않습니다.
        /// 그리는 방법은 View DB의 표시 모드가 정합니다(격자 아웃라인 설계 F1·F4).
        /// 표시기 구성이 실패하면(예: GridOutline 모드인데 Cells Root가 돌아가 있음, 평가 C10-08) 만든 것을 거두고 다시 던집니다.
        /// </summary>
        internal void Build(int width, int height, StageFieldViewData config, IColorSystemService colors,
            IAreaOutlineSystemService outlines, IMeshSystemService meshes)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (colors == null)
                throw new ArgumentNullException(nameof(colors));
            if (outlines == null)
                throw new ArgumentNullException(nameof(outlines));
            if (meshes == null)
                throw new ArgumentNullException(nameof(meshes));
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));
            if (_mapper != null && _mapper.ColumnCount == width && _mapper.RowCount == height)
                return;

            ValidateCellsRoot();
            Release();
            EnsureRenderer(config, colors, outlines, meshes);

            try
            {
                FieldCellSpacing spacing = _renderer.MeasureSpacing();
                var mapper = new FieldGridMapper(cellsRoot, width, height, spacing.Width, spacing.Length);
                _state.Reset(width, height);
                if (_pendingMarks.Length != _state.CellCount)
                    _pendingMarks = new bool[_state.CellCount];
                _renderer.Build(mapper, _state);
                BuildTrajectory(config, colors, meshes);
                BuildKnockbackMarkers(config, colors, meshes);
                _mapper = mapper;
            }
            catch
            {
                Release();
                throw;
            }
        }

        /// <summary>
        /// 칸 높이 표의 바탕을 바닥 타일 윗면 높이로 둡니다. 타일 표가 없으면(타일 방식이 아닌 배경·표시 시험) 모두 바닥(0)입니다.
        /// <see cref="Build"/>는 같은 크기면 높이 표를 재사용하므로 설치마다 이 입구로 다시 넣습니다(타일 높이 설계 TH6, 계획 P1·§8.7 L8).
        /// 칸 높이는 다음 Refresh의 다시 채우기(지형 SyncSurface)에서 바탕 + 지형으로 바뀝니다. 타일 표 크기가 이 격자와 다르면 조립 오류라 예외입니다(계획 P11).
        /// </summary>
        internal void SetGroundBase(CombatMapTileSurface tiles)
        {
            if (_mapper == null)
                throw new InvalidOperationException("[FieldView] 필드를 구성하기 전에 바닥 높이를 넣을 수 없습니다.");

            FieldSurfaceHeights surface = _mapper.Surface;
            surface.ClearBase();
            tiles?.ApplyBaseTo(surface);
        }

        /// <summary>
        /// 아군 배치 구역과 레일 칸을 정합니다. 레일이 구역보다 우선합니다. 종류가 바뀐 칸만 다시 그립니다.
        /// 구역은 Buffer의 Core 값을 그대로 받습니다(적 배치 구역은 없습니다).
        /// </summary>
        internal void ApplyStaticHighlights(PlayerDeploymentZone playerZone, IReadOnlyList<CellCoord> railCells)
        {
            if (_mapper == null)
                throw new InvalidOperationException("[FieldView] 필드를 구성하기 전에 강조할 수 없습니다.");
            if (playerZone == null)
                throw new ArgumentNullException(nameof(playerZone));
            if (railCells == null)
                throw new ArgumentNullException(nameof(railCells));

            QueueRepaint(_state.SetStaticZones(playerZone, railCells));
        }

        /// <summary>
        /// 포인터가 올라간 칸과 가능 여부를 강조합니다. 이전 호버 칸은 아래 층 색으로 되돌립니다.
        /// 종류는 색만 가릅니다(공격 조준 = 공격 호버 색).
        /// </summary>
        internal void SetHoverCell(CellCoord cell, CombatHoverState state, CombatHoverKind kind = CombatHoverKind.Default)
        {
            if (_mapper == null)
                return;

            QueueRepaint(_state.SetHover(cell, state, kind));
        }

        internal void ClearHoverCell()
        {
            if (_mapper == null)
                return;

            QueueRepaint(_state.ClearHover());
        }

        /// <summary>한 층의 칸을 바꿉니다. 이동 경로는 입력 순서를 보존하며 순서만 바뀌어도 다시 그립니다.</summary>
        internal void SetHoverRangeSourceCells(IReadOnlyList<CellCoord> cells)
        {
            if (_state.SetHoverRangeSourceCells(cells) && _mapper != null)
                QueueRefresh();
        }

        internal void SetLayerCells(FieldHighlightLayer layer, IReadOnlyList<CellCoord> cells)
        {
            if (cells == null)
                throw new ArgumentNullException(nameof(cells));
            if (_mapper == null)
                return;

            QueueRepaint(_state.SetLayerCells(layer, cells));
        }

        /// <summary>공격 영향 칸과 독립 영역의 닫힌 경계를 함께 바꿉니다. 경계만 달라도 다시 그립니다.</summary>
        internal void SetAffectedCells(IReadOnlyList<CellCoord> cells, bool closeOutline)
        {
            if (cells == null)
                throw new ArgumentNullException(nameof(cells));
            if (_mapper == null)
                return;

            QueueRepaint(_state.SetLayerCells(FieldHighlightLayer.Affected, cells, closeAffectedOutline: closeOutline));
        }

        /// <summary>한 층을 모두 지우고 그 칸들을 아래 층 색으로 되돌립니다.</summary>
        internal void ClearLayer(FieldHighlightLayer layer)
        {
            if (_mapper == null)
                return;

            QueueRepaint(_state.ClearLayer(layer));
        }

        /// <summary>손패 배치 드래그 중인지 알립니다(격자 아웃라인 설계 F6). 셀 색 모드는 화면이 바뀌지 않습니다.</summary>
        internal void SetPlacementActive(bool active)
        {
            if (_mapper == null)
                return;

            if (_state.SetPlacementActive(active))
                QueueRefresh();
        }

        /// <summary>Q로 이동 경로를 지정하는 중인지 알립니다. 셀 색 모드는 화면이 바뀌지 않습니다.</summary>
        internal void SetPathPlanning(bool planning)
        {
            if (_mapper == null)
                return;

            if (_state.SetPathPlanning(planning))
                QueueRefresh();
        }

        /// <summary>
        /// 칸 표면 높이(고지대·바닥 타일)가 바뀌어 높이별로 그린 강조·데칼을 다시 맞춥니다(고지대 View 설계 HG-4a, 타일 높이 설계 TH4).
        /// 구성 전이면 아무 일도 하지 않습니다.
        /// </summary>
        internal void RepaintSurface()
        {
            if (_mapper == null)
                return;

            QueueRefresh();
        }

        // 바뀐 칸을 그리기 대기에 더합니다(같은 칸은 한 번만). LateUpdate가 돌지 않으면 바로 그립니다(GH1).
        private void QueueRepaint(IReadOnlyList<int> changedCells)
        {
            for (int i = 0; i < changedCells.Count; i++)
            {
                int index = changedCells[i];
                if (_pendingMarks[index])
                    continue;
                _pendingMarks[index] = true;
                _pendingCells.Add(index);
            }
            if (!DrawsAtFrameEnd)
                FlushPending();
        }

        // 칸과 무관한 변경(배치 드래그·경로 지정·표면 높이)을 "전체 다시"로 표시합니다.
        private void QueueRefresh()
        {
            _pendingRefresh = true;
            if (!DrawsAtFrameEnd)
                FlushPending();
        }

        // LateUpdate가 도는가: 켜져 있고 재생 중일 때만입니다. 편집 모드(재생 전·EditMode 시험)는 LateUpdate가 돌지 않습니다(계획 §3.7 L3).
        private bool DrawsAtFrameEnd => isActiveAndEnabled && Application.isPlaying;

        private void LateUpdate() => FlushPending();

        /// <summary>
        /// 기다리던 그리기를 한 번에 합니다: 바뀐 칸은 <see cref="IFieldHighlightRenderer.Repaint"/> 한 번, 칸과 무관한 변경은 <see cref="IFieldHighlightRenderer.Refresh"/> 한 번.
        /// 표시기 계약은 그대로라 둘이 같은 프레임에 겹치면 둘 다 부릅니다. 그리기가 실패해도 대기는 비워 다음 프레임에 같은 실패를 되풀이하지 않습니다.
        /// 대기가 있으면 늘 구성돼 있습니다: 층 API는 구성 전이면 대기에 넣지 않고, 해제는 좌표 변환을 비우기 전에 대기를 버립니다(계획 §3.7 L9).
        /// </summary>
        private void FlushPending()
        {
            if (_pendingCells.Count == 0 && !_pendingRefresh)
                return;

            bool refresh = _pendingRefresh;
            _pendingRefresh = false;
            if (_pendingCells.Count > 0)
            {
                for (int i = 0; i < _pendingCells.Count; i++)
                    _pendingMarks[_pendingCells[i]] = false;
                try
                {
                    _renderer.Repaint(_pendingCells);
                }
                finally
                {
                    _pendingCells.Clear();
                }
            }
            if (refresh)
                _renderer.Refresh();
        }

        // 기다리던 그리기를 버립니다. 구성·해제 때 부릅니다(새 구성은 바로 그리고, 해제 뒤에는 그릴 것이 없음).
        private void DropPending()
        {
            for (int i = 0; i < _pendingCells.Count; i++)
                _pendingMarks[_pendingCells[i]] = false;
            _pendingCells.Clear();
            _pendingRefresh = false;
        }

        /// <summary>
        /// 화면 Ray가 가리키는 칸입니다. 칸은 <see cref="TryRayToSurface"/>의 점으로 정하므로 고지대 칸은 블록 윗면을, 그 밖은 바닥면을 가리키면 잡힙니다
        /// (바닥 타일 높이와 상관없음).
        /// 필드를 구성하기 전이거나 격자 밖이면 false입니다.
        /// </summary>
        internal bool TryRayToCell(Ray ray, out CellCoord cell)
        {
            if (_mapper == null || !TryRayToSurface(ray, out Vector3 point))
            {
                cell = default;
                return false;
            }

            return _mapper.TryWorldToCell(point, out cell);
        }

        /// <summary>
        /// 화면 Ray가 가리키는 격자 위의 점입니다(고지대 View 계획 P14). 격자 높이(<see cref="FieldSurfaceHeights.OverlayHeightAt"/>, 지형이 올린 높이만)가
        /// 있는 평면을 높은 것부터 보고, Ray가 그 평면에서 그 높이의 칸에 닿으면 그 점입니다. 아니면 바닥 평면의 점이고 격자 밖도 포함합니다.
        /// 바닥 타일 높이는 보지 않습니다(타일 높이 설계 TH4 개정 2 — 격자 테두리·호버와 같은 높이라 커서와 어긋나지 않음).
        /// 칸 고르기·경로/공격 Drag가 이 한 규칙을 쓰고, 고스트는 이 점을 그 칸 표면으로 맞춥니다(<see cref="FieldGridMapper.OnSurface"/>).
        /// 필드를 구성하기 전이면 false입니다.
        /// </summary>
        internal bool TryRayToSurface(Ray ray, out Vector3 point)
        {
            if (_mapper != null)
            {
                FieldSurfaceHeights surface = _mapper.Surface;
                IReadOnlyList<float> levels = surface.OverlayLevels;
                for (int i = levels.Count - 1; i >= 0; i--)
                {
                    if (TryRayToPlane(ray, _mapper.SurfaceOffset(levels[i]), out point)
                        && _mapper.TryWorldToCell(point, out CellCoord cell) && surface.OverlayHeightAt(cell) == levels[i])
                        return true;
                }
            }

            return TryRayToFieldPlane(ray, out point);
        }

        /// <summary>격자 밖이라도 바닥 필드 평면과 만나는 점입니다. 높이를 모르는 점이라 포인터 위치에는 <see cref="TryRayToSurface"/>를 씁니다.</summary>
        internal bool TryRayToFieldPlane(Ray ray, out Vector3 point) => TryRayToPlane(ray, Vector3.zero, out point);

        // 필드 평면을 월드 변위만큼 옮긴 평면과 Ray가 만나는 점입니다. 바닥은 변위 0, 솟은 칸은 표면 높이입니다.
        private bool TryRayToPlane(Ray ray, Vector3 worldOffset, out Vector3 point)
        {
            if (_mapper == null || cellsRoot == null)
            {
                point = default;
                return false;
            }

            var plane = new Plane(cellsRoot.up, cellsRoot.position + worldOffset);
            if (!plane.Raycast(ray, out float distance))
            {
                point = default;
                return false;
            }

            point = ray.GetPoint(distance);
            return true;
        }

        /// <summary>
        /// 구성한 표시를 거두고 상태·좌표 변환을 비웁니다. 여러 번 불려도 안전합니다.
        /// 표시기 해제가 실패해도 "구성 안 됨"으로 남도록 상태·좌표 변환을 먼저 비운 뒤 표시기 예외를 그대로 전파합니다.
        /// 궤적 → 넉백 표지 → 표시기 순서로 거두고, 앞 부품 해제가 실패해도 뒤 부품 해제는 합니다(넉백 미리보기 계획 TP-11).
        /// </summary>
        internal void Release()
        {
            DropPending();
            _state.Clear();
            _mapper = null;
            FieldTrajectoryLine trajectory = _trajectory;
            FieldKnockbackMarkers knockbackMarkers = _knockbackMarkers;
            _trajectory = null;
            _knockbackMarkers = null;
            try
            {
                trajectory?.Release();
            }
            finally
            {
                try
                {
                    knockbackMarkers?.Release();
                }
                finally
                {
                    _renderer?.Release();
                }
            }
        }

        private void OnDestroy()
        {
            FieldTrajectoryLine trajectory = _trajectory;
            FieldKnockbackMarkers knockbackMarkers = _knockbackMarkers;
            _trajectory = null;
            _knockbackMarkers = null;
            try
            {
                trajectory?.Release();
            }
            finally
            {
                try
                {
                    knockbackMarkers?.Release();
                }
                finally
                {
                    try
                    {
                        _renderer?.Dispose();
                    }
                    finally
                    {
                        _renderer = null;
                        if (_visualRoot != null)
                        {
                            if (Application.isPlaying) Destroy(_visualRoot.gameObject);
                            else DestroyImmediate(_visualRoot.gameObject);
                        }
                        _visualRoot = null;
                    }
                }
            }
        }

        FieldGridMapper ICombatTrajectoryCanvas.Mapper => _mapper;

        void ICombatTrajectoryCanvas.ShowAttackTrajectory(Vector3[] localPath, int count, bool blocked)
        {
            // 구성 전이거나 재질 없는 설정(시험)이면 궤적 부품이 없어 아무것도 하지 않습니다(공격 궤적 계획 P1).
            if (_mapper == null || _trajectory == null)
                return;

            _trajectory.Show(localPath, count, blocked);
        }

        void ICombatTrajectoryCanvas.ClearAttackTrajectory() => _trajectory?.Clear();

        FieldGridMapper ICombatKnockbackCanvas.Mapper => _mapper;

        void ICombatKnockbackCanvas.ShowKnockbacks(IReadOnlyList<CellCoord> destinations, IReadOnlyList<FieldKnockbackMark> marks)
        {
            if (marks == null)
                throw new ArgumentNullException(nameof(marks));

            // 구성 전이면 층 API가 무시하고 표지 부품도 없습니다(부품은 구성 성공 때만 생기고 해제 때 함께 비움).
            // 화살촉 재질 없는 맨 설정(시험)도 표지 부품이 없어 도착 칸만 칠합니다(넉백 미리보기 설계 P7).
            SetLayerCells(FieldHighlightLayer.KnockbackDestination, destinations);
            _knockbackMarkers?.Show(marks);
        }

        void ICombatKnockbackCanvas.ClearKnockbacks()
        {
            ClearLayer(FieldHighlightLayer.KnockbackDestination);
            _knockbackMarkers?.Clear();
        }

        /// <summary>
        /// 설정에 궤적 머티리얼이 있으면 궤적 부품을 만들고 구성합니다. 머티리얼이 없는 맨 설정(시험)은 궤적 없이 둡니다.
        /// 운영 설정은 DB 검사(<see cref="StageFieldViewData.Validate"/>)가 머티리얼을 강제합니다(공격 궤적 계획 P1).
        /// </summary>
        private void BuildTrajectory(StageFieldViewData config, IColorSystemService colors, IMeshSystemService meshes)
        {
            if (config.TrajectoryMaterial == null)
                return;

            var trajectory = new FieldTrajectoryLine(_visualRoot, config, trajectoryColor, trajectoryBlockedColor, meshes, colors);
            trajectory.Build();
            _trajectory = trajectory;
        }

        /// <summary>
        /// 설정에 넉백 화살촉 재질이 있으면 넉백 표지 부품을 만들고 구성합니다. 재질이 없는 맨 설정(시험)은 표지 없이 둡니다.
        /// 운영 설정은 DB 검사(<see cref="StageFieldViewData.Validate"/>)가 재질을 강제합니다(공용 화살촉 설계 §1).
        /// </summary>
        private void BuildKnockbackMarkers(StageFieldViewData config, IColorSystemService colors, IMeshSystemService meshes)
        {
            if (config.KnockbackArrowMaterial == null)
                return;

            var knockbackMarkers = new FieldKnockbackMarkers(_visualRoot, config, knockbackColor, knockbackBlockedColor,
                knockbackImmuneColor, meshes, colors);
            knockbackMarkers.Build();
            _knockbackMarkers = knockbackMarkers;
        }

        /// <summary>빠진 씬 참조 이름입니다. 없으면 null입니다.</summary>
        internal string FindMissingReference()
        {
            return cellsRoot == null ? $"{nameof(FieldView)}.{nameof(cellsRoot)}" : null;
        }

        private void EnsureRenderer(StageFieldViewData config, IColorSystemService colors,
            IAreaOutlineSystemService outlines, IMeshSystemService meshes)
        {
            if (_visualRoot == null)
            {
                _visualRoot = new GameObject("FieldVisualRoot").transform;
                _visualRoot.SetParent(cellsRoot, false);
            }
            if (_renderer != null)
            {
                if (!_renderer.CanReuse(config))
                    throw new InvalidOperationException("[FieldView] 한 씬에서 표시 모드나 셀 프리팹을 바꿀 수 없습니다.");
                return;
            }

            switch (config.DisplayMode)
            {
                case FieldDisplayMode.CellPrefab:
                    _renderer = new FieldCellColorRenderer(_visualRoot, config, colors, CreatePalette());
                    break;
                case FieldDisplayMode.GridOutline:
                    _renderer = new FieldOutlineRenderer(_visualRoot, config, outlines, CreatePalette(), meshes, colors);
                    break;
                default:
                    throw new InvalidOperationException($"[FieldView] 알 수 없는 표시 모드입니다: {config.DisplayMode}.");
            }
        }

        private FieldHighlightPalette CreatePalette()
        {
            return new FieldHighlightPalette(playerZoneColor, railColor, hoverColor, hoverValidColor,
                hoverInvalidColor, attackHoverColor, attackHoverValidColor, attackHoverInvalidColor, affectedColor, movementDraftColor, moveRangeColor, attackRangeColor, enemyAttackRangeColor, hazardRangeColor,
                activeActorColor, objectiveTargetColor, knockbackDestinationColor, allyHoverMoveRangeColor, enemyMoveRangeColor, rockfallWarningColor, fillAlpha, gimmickHoverRangeColor);
        }

        private void ValidateCellsRoot()
        {
            if (cellsRoot == null)
                throw new InvalidOperationException("[FieldView] Cells Root가 연결되지 않았습니다.");
            if (!HasUnitWorldScale())
                throw new InvalidOperationException("[FieldView] Cells Root의 월드 크기는 (1, 1, 1)이어야 합니다.");
            if (!IsHorizontal())
                throw new InvalidOperationException("[FieldView] Cells Root는 Y축 회전만 지원합니다.");
        }

        private bool HasUnitWorldScale()
        {
            Vector3 scale = cellsRoot.lossyScale;
            return Mathf.Approximately(scale.x, 1f) && Mathf.Approximately(scale.y, 1f) && Mathf.Approximately(scale.z, 1f);
        }

        private bool IsHorizontal()
        {
            return Vector3.Dot(cellsRoot.up, Vector3.up) >= 0.9999f;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (cellsRoot == null)
                return;
            if (!HasUnitWorldScale())
                Debug.LogWarning("[FieldView] Cells Root의 월드 크기는 (1, 1, 1)이어야 합니다.", this);
            if (!IsHorizontal())
                Debug.LogWarning("[FieldView] Cells Root는 Y축 회전만 지원합니다.", this);
        }
#endif
    }
}
