using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// 전투 필드의 좌표 변환·칸 집기와 칸 강조 층 API를 소유합니다(설계 §6·§6.1, 계획 D.1, 격자 아웃라인 설계 F2).
    /// 옛 develop-4week의 FieldRoot·FieldRuntime을 이식했습니다.
    /// </summary>
    /// <remarks>
    /// 강조 상태는 <see cref="FieldHighlightState"/> 한 곳에 두고, 그리기는 표시 모드별 <see cref="IFieldHighlightRenderer"/>에 맡깁니다.
    /// 표시 모드·셀 프리팹·간격은 View DB(StageFieldView)에서, 강조 색은 이 컴포넌트의 씬 설정에서 읽습니다.
    /// 정적 구성은 전투 설치 때 한 번 하고, 결과마다 셀을 다시 만들지 않습니다. Buffer·Core를 모르고 값만 받습니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class FieldView : MonoBehaviour
    {
        [Tooltip("셀이 놓일 루트. 월드 크기 (1,1,1), Y축 회전만 허용합니다.")]
        [SerializeField] private Transform cellsRoot;

        [Header("정적 강조 색 (임시)")]
        [SerializeField] private Color playerZoneColor = new(0.55f, 0.75f, 1f, 1f);
        [SerializeField] private Color enemyZoneColor = new(1f, 0.6f, 0.6f, 1f);
        [SerializeField] private Color railColor = new(0.85f, 0.75f, 0.45f, 1f);
        [Header("입력·미리보기 강조 색 (설계 §21.1, 우선순위 위에서부터)")]
        [Tooltip("포인터가 올라간 칸(판단 전).")]
        [SerializeField] private Color hoverColor = new(1f, 1f, 0.6f, 1f);
        [SerializeField] private Color hoverValidColor = new(0.35f, 1f, 0.45f, 1f);
        [SerializeField] private Color hoverInvalidColor = new(1f, 0.3f, 0.3f, 1f);
        [Tooltip("호버한 공격 조준의 영향 칸.")]
        [SerializeField] private Color affectedColor = new(1f, 0.55f, 0.15f, 1f);
        [Tooltip("드래그한 이동 예정 경로. Core의 이동 가능 여부는 끝점 색으로 구분합니다.")]
        [SerializeField] private Color movementDraftColor = new(0.05f, 1f, 0.75f, 1f);
        [Tooltip("이동·공격 범위 응답의 칸.")]
        [SerializeField] private Color rangeColor = new(0.45f, 0.85f, 1f, 1f);
        [Tooltip("현재 행위자가 점유한 칸.")]
        [SerializeField] private Color activeActorColor = new(1f, 0.95f, 0.3f, 1f);

        private readonly FieldHighlightState _state = new();
        private IFieldHighlightRenderer _renderer;
        private FieldGridMapper _mapper;

        /// <summary>현재 필드 좌표 변환입니다. 구성 전에는 null입니다.</summary>
        internal FieldGridMapper Mapper => _mapper;

        /// <summary>셀이 놓이는 기준 Transform입니다. 필드 기준 장식(배경 프랍)도 여기에 붙입니다. FieldView 오브젝트와 위치가 다를 수 있습니다.</summary>
        internal Transform CellsRoot => cellsRoot;

        /// <summary>칸 강조 상태의 읽기 계약입니다(표시기·테스트). 바꾸는 것은 이 컴포넌트의 층 API뿐입니다(D4).</summary>
        internal IFieldHighlightStateReader HighlightState => _state;

        /// <summary>
        /// 보드 크기만큼 필드를 구성합니다. 같은 크기로 이미 구성돼 있으면 아무것도 하지 않습니다.
        /// 그리는 방법은 View DB의 표시 모드가 정합니다(격자 아웃라인 설계 F1·F4).
        /// </summary>
        internal void Build(int width, int height, StageFieldViewData config, IColorSystemService colors,
            IAreaOutlineSystemService outlines)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (colors == null)
                throw new ArgumentNullException(nameof(colors));
            if (outlines == null)
                throw new ArgumentNullException(nameof(outlines));
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));
            if (_mapper != null && _mapper.ColumnCount == width && _mapper.RowCount == height)
                return;

            ValidateCellsRoot();
            Release();
            EnsureRenderer(config, colors, outlines);

            try
            {
                FieldCellSpacing spacing = _renderer.MeasureSpacing();
                var mapper = new FieldGridMapper(cellsRoot, width, height, spacing.Width, spacing.Length);
                _state.Reset(width, height);
                _renderer.Build(mapper, _state);
                _mapper = mapper;
            }
            catch
            {
                Release();
                throw;
            }
        }

        /// <summary>
        /// 배치 구역과 레일 칸을 정합니다. 레일이 구역보다 우선합니다. 종류가 바뀐 칸만 다시 그립니다.
        /// </summary>
        internal void ApplyStaticHighlights(int playerDeploymentDepth, int enemyDeploymentDepth, IReadOnlyList<CellCoord> railCells)
        {
            if (_mapper == null)
                throw new InvalidOperationException("[FieldView] 필드를 구성하기 전에 강조할 수 없습니다.");
            if (railCells == null)
                throw new ArgumentNullException(nameof(railCells));

            _renderer.Repaint(_state.SetStaticZones(playerDeploymentDepth, enemyDeploymentDepth, railCells));
        }

        /// <summary>포인터가 올라간 칸과 가능 여부를 강조합니다. 이전 호버 칸은 아래 층 색으로 되돌립니다.</summary>
        internal void SetHoverCell(CellCoord cell, CombatHoverState state)
        {
            if (_mapper == null)
                return;

            _renderer.Repaint(_state.SetHover(cell, state));
        }

        internal void ClearHoverCell()
        {
            if (_mapper == null)
                return;

            _renderer.Repaint(_state.ClearHover());
        }

        /// <summary>한 층의 칸을 바꿉니다. 그 층이 빠지거나 더해진 칸만 다시 그립니다.</summary>
        internal void SetLayerCells(FieldHighlightLayer layer, IReadOnlyList<CellCoord> cells)
        {
            if (cells == null)
                throw new ArgumentNullException(nameof(cells));
            if (_mapper == null)
                return;

            _renderer.Repaint(_state.SetLayerCells(layer, cells));
        }

        /// <summary>한 층을 모두 지우고 그 칸들을 아래 층 색으로 되돌립니다.</summary>
        internal void ClearLayer(FieldHighlightLayer layer)
        {
            if (_mapper == null)
                return;

            _renderer.Repaint(_state.ClearLayer(layer));
        }

        /// <summary>손패 배치 드래그 중인지 알립니다(격자 아웃라인 설계 F6). 셀 색 모드는 화면이 바뀌지 않습니다.</summary>
        internal void SetPlacementActive(bool active)
        {
            if (_mapper == null)
                return;

            if (_state.SetPlacementActive(active))
                _renderer.Refresh();
        }

        /// <summary>Q로 이동 경로를 지정하는 중인지 알립니다. 셀 색 모드는 화면이 바뀌지 않습니다.</summary>
        internal void SetPathPlanning(bool planning)
        {
            if (_mapper == null)
                return;

            if (_state.SetPathPlanning(planning))
                _renderer.Refresh();
        }

        /// <summary>화면 Ray가 가리키는 칸입니다. 필드를 구성하기 전이거나 격자 밖이면 false입니다.</summary>
        internal bool TryRayToCell(Ray ray, out CellCoord cell)
        {
            if (_mapper == null || !TryRayToFieldPlane(ray, out Vector3 point))
            {
                cell = default;
                return false;
            }

            return _mapper.TryWorldToCell(point, out cell);
        }

        /// <summary>격자 밖이라도 필드 평면과 만나는 점입니다. Drag 고스트가 칸 밖에서도 포인터를 따라갈 때 씁니다.</summary>
        internal bool TryRayToFieldPlane(Ray ray, out Vector3 point)
        {
            if (_mapper == null || cellsRoot == null)
            {
                point = default;
                return false;
            }

            var plane = new Plane(cellsRoot.up, cellsRoot.position);
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
        /// </summary>
        internal void Release()
        {
            _state.Clear();
            _mapper = null;
            _renderer?.Release();
        }

        private void OnDestroy()
        {
            _renderer?.Dispose();
            _renderer = null;
        }

        /// <summary>빠진 씬 참조 이름입니다. 없으면 null입니다.</summary>
        internal string FindMissingReference()
        {
            return cellsRoot == null ? $"{nameof(FieldView)}.{nameof(cellsRoot)}" : null;
        }

        private void EnsureRenderer(StageFieldViewData config, IColorSystemService colors, IAreaOutlineSystemService outlines)
        {
            if (_renderer != null)
            {
                if (!_renderer.CanReuse(config))
                    throw new InvalidOperationException("[FieldView] 한 씬에서 표시 모드나 셀 프리팹을 바꿀 수 없습니다.");
                return;
            }

            switch (config.DisplayMode)
            {
                case FieldDisplayMode.CellPrefab:
                    _renderer = new FieldCellColorRenderer(cellsRoot, config, colors, CreatePalette());
                    break;
                case FieldDisplayMode.GridOutline:
                    _renderer = new FieldOutlineRenderer(cellsRoot, config, outlines, CreatePalette());
                    break;
                default:
                    throw new InvalidOperationException($"[FieldView] 알 수 없는 표시 모드입니다: {config.DisplayMode}.");
            }
        }

        private FieldHighlightPalette CreatePalette()
        {
            return new FieldHighlightPalette(playerZoneColor, enemyZoneColor, railColor, hoverColor, hoverValidColor,
                hoverInvalidColor, affectedColor, movementDraftColor, rangeColor, activeActorColor);
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
