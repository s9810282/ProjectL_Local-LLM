using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>상세창의 기본 격자에 범위와 원점 Image를 겹쳐 표시합니다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UnitRangeImages : MonoBehaviour
    {
        private const int DefaultSide = 7;
        private const int ExpandedSide = 9;
        private const int SplashSide = 11;
        [SerializeField] private GridLayoutGroup defaultGrid;
        [SerializeField] private GridLayoutGroup expandedGridPrefab;
        [SerializeField] private Image cellImage;
        [SerializeField] private Image rangeImage;
        [SerializeField] private Image originImage;
        [SerializeField] private Image splashImage;
        private Grid _default;
        private Grid _expanded;
        private Grid _splashExpanded;
        private Grid _shown;
        private AttackPatternSnapshot _attackPattern;
        private int? _movementRadius;
        private bool _dirty;

        internal void Validate(string field, bool showSplash)
        {
            DeckSlotInfoReferences.RequireChild(defaultGrid, transform, field + ".defaultGrid");
            ValidateGrid(defaultGrid, DefaultSide, _default?.Cells.Length ?? 0, field + ".defaultGrid");
            ValidateGrid(expandedGridPrefab, ExpandedSide, 0, field + ".expandedGridPrefab");
            ValidateImage(cellImage, field + ".cellImage");
            ValidateImage(rangeImage, field + ".rangeImage");
            ValidateImage(originImage, field + ".originImage");
            if (showSplash || splashImage != null) ValidateImage(splashImage, field + ".splashImage");
            if (defaultGrid.transform.parent != transform || cellImage == rangeImage ||
                cellImage == originImage || rangeImage == originImage ||
                splashImage == cellImage || splashImage == rangeImage || splashImage == originImage)
                throw DeckSlotInfoReferences.Error(transform, $"{field}에는 직계 자식 격자와 서로 다른 Image 원본이 필요합니다.");
        }

        private void ValidateGrid(GridLayoutGroup layout, int side, int childCount, string field)
        {
            if (layout == null || layout.transform.childCount != childCount ||
                layout.constraint != GridLayoutGroup.Constraint.FixedColumnCount || layout.constraintCount != side ||
                layout.startCorner != GridLayoutGroup.Corner.LowerLeft || layout.startAxis != GridLayoutGroup.Axis.Horizontal)
                throw DeckSlotInfoReferences.Error(transform,
                    $"{field}에는 왼쪽 아래부터 가로로 배치하는 {side}열 GridLayoutGroup이 필요합니다. 칸은 표시기가 생성합니다.");
        }

        private void ValidateImage(Image image, string field)
        {
            DeckSlotInfoReferences.RequireChild(image, transform, field);
            if (image.transform.parent != transform || image.sprite == null ||
                image.gameObject.activeSelf || image.raycastTarget)
                throw DeckSlotInfoReferences.Error(transform, $"{field}에는 Sprite가 있고 입력을 받지 않는 비활성 직계 자식 Image가 필요합니다.");
        }

        // null은 미게시, 범위 칸이 비어 있는 패턴은 지정할 수 있는 칸이 없다는 뜻입니다.
        internal void SetAttackPattern(AttackPatternSnapshot pattern)
        {
            if (_movementRadius == null && ReferenceEquals(_attackPattern, pattern)) return;
            _movementRadius = null;
            _attackPattern = pattern;
            _dirty = true;
        }

        internal void SetMovementRadius(int? radius)
        {
            if (_attackPattern == null && _movementRadius == radius) return;
            _attackPattern = null;
            _movementRadius = radius;
            _dirty = true;
        }

        private void OnEnable() => _dirty = true;
        private void OnRectTransformDimensionsChange() => _dirty = true;

        private void LateUpdate()
        {
            if (!_dirty) return;
            _dirty = false;
            CellCoord? splashTarget = FindSplashTarget(_attackPattern);
            Grid grid = GetGrid(splashTarget);
            Rect rect = ((RectTransform)transform).rect;
            Vector2 spacing = grid.Layout.spacing * (grid.Side - 1);
            float size = Mathf.Max(0f, Mathf.Min(rect.width - spacing.x, rect.height - spacing.y) / grid.Side);
            grid.Layout.cellSize = Vector2.one * size;
            ShowMarkers(grid, splashTarget);

            if (_shown != grid)
            {
                if (_shown != null) _shown.Layout.gameObject.SetActive(false);
                grid.Layout.gameObject.SetActive(true);
                _shown = grid;
            }
        }

        private static CellCoord? FindSplashTarget(AttackPatternSnapshot pattern)
        {
            if (pattern == null || pattern.SplashOffsets.Count == 0 || pattern.RangeCells.Count == 0) return null;
            // 보유 카드의 +Y가 전방입니다. 끝 줄에서 중앙에 가장 가까운 실제 지정 칸을 예시로 씁니다.
            CellCoord target = pattern.RangeCells[0];
            for (int i = 1; i < pattern.RangeCells.Count; i++)
            {
                CellCoord candidate = pattern.RangeCells[i];
                if (candidate.Y > target.Y || (candidate.Y == target.Y &&
                    Math.Abs((long)candidate.X) < Math.Abs((long)target.X)))
                    target = candidate;
            }
            return target;
        }

        private Grid GetGrid(CellCoord? splashTarget)
        {
            long radius = _movementRadius ?? 0;
            if (_attackPattern != null)
                for (int i = 0; i < _attackPattern.RangeCells.Count; i++)
                    radius = Math.Max(radius, Radius(_attackPattern.RangeCells[i]));
            if (splashTarget is CellCoord target)
                for (int i = 0; i < _attackPattern.SplashOffsets.Count; i++)
                    radius = Math.Max(radius, Radius(target + _attackPattern.SplashOffsets[i]));

            if (radius <= DefaultSide / 2) return _default ??= CreateGrid(defaultGrid);
            if (splashTarget.HasValue && radius > ExpandedSide / 2)
                return _splashExpanded ??= CreateExpandedGrid(SplashSide);
            return _expanded ??= CreateExpandedGrid(ExpandedSide);
        }

        private static long Radius(CellCoord cell) => Math.Max(Math.Abs((long)cell.X), Math.Abs((long)cell.Y));

        private Grid CreateExpandedGrid(int side)
        {
            GridLayoutGroup layout = Instantiate(expandedGridPrefab, transform, false);
            layout.constraintCount = side;
            return CreateGrid(layout);
        }

        private Grid CreateGrid(GridLayoutGroup layout)
        {
            var grid = new Grid(layout);
            for (int i = 0; i < grid.Cells.Length; i++)
            {
                Image cell = Instantiate(cellImage, layout.transform, false);
                cell.gameObject.SetActive(true);
                grid.Cells[i] = cell;
            }
            int center = grid.Cells.Length / 2;
            grid.Origin = CreateOverlay(originImage, grid.Cells[center].rectTransform);
            return grid;
        }

        private void ShowMarkers(Grid grid, CellCoord? splashTarget)
        {
            Array.Clear(grid.Highlighted, 0, grid.Highlighted.Length);
            int limit = grid.Side / 2;
            if (_movementRadius is int radius)
            {
                for (int y = -limit; y <= limit; y++)
                    for (int x = -limit; x <= limit; x++)
                        if (Math.Abs(x) + Math.Abs(y) <= radius)
                            grid.Highlighted[(y + limit) * grid.Side + x + limit] = Marker.Range;
            }
            else if (_attackPattern != null)
            {
                for (int i = 0; i < _attackPattern.RangeCells.Count; i++)
                    MarkCell(grid, _attackPattern.RangeCells[i], Marker.Range);
                if (splashTarget is CellCoord target)
                {
                    MarkCell(grid, target, Marker.Splash);
                    for (int i = 0; i < _attackPattern.SplashOffsets.Count; i++)
                        MarkCell(grid, target + _attackPattern.SplashOffsets[i], Marker.Splash);
                }
            }

            grid.Highlighted[grid.Cells.Length / 2] = Marker.None;
            for (int i = 0; i < grid.Cells.Length; i++)
            {
                Marker marker = grid.Highlighted[i];
                RectTransform cell = grid.Cells[i].rectTransform;
                ShowOverlay(ref grid.Ranges[i], rangeImage, cell, marker == Marker.Range);
                ShowOverlay(ref grid.Splashes[i], splashImage, cell, marker == Marker.Splash);
            }
            grid.Origin.gameObject.SetActive(_movementRadius.HasValue || _attackPattern != null);
        }

        private static void MarkCell(Grid grid, CellCoord cell, Marker marker)
        {
            int limit = grid.Side / 2;
            if (Radius(cell) <= limit)
                grid.Highlighted[(cell.Y + limit) * grid.Side + cell.X + limit] = marker;
        }

        private void ShowOverlay(ref Image overlay, Image source, RectTransform cell, bool show)
        {
            if (show && overlay == null) overlay = CreateOverlay(source, cell);
            if (overlay != null) overlay.gameObject.SetActive(show);
        }

        private Image CreateOverlay(Image source, RectTransform parent)
        {
            Image overlay = Instantiate(source, parent, false);
            RectTransform rect = overlay.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return overlay;
        }

        private sealed class Grid
        {
            internal readonly GridLayoutGroup Layout;
            internal readonly int Side;
            internal readonly Image[] Cells;
            internal readonly Image[] Ranges;
            internal readonly Image[] Splashes;
            internal readonly Marker[] Highlighted;
            internal Image Origin;

            internal Grid(GridLayoutGroup layout)
            {
                Layout = layout;
                Side = layout.constraintCount;
                Cells = new Image[Side * Side];
                Ranges = new Image[Cells.Length];
                Splashes = new Image[Cells.Length];
                Highlighted = new Marker[Cells.Length];
            }
        }

        private enum Marker { None, Range, Splash }
    }
}
