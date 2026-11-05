using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>UI 영역·출력 카메라·보드 칸과 그 칸의 배치물을 한 곳에서 판정합니다.</summary>
    internal sealed class ArtifactPointerMapper
    {
        private readonly IArtifactPopupScreenAreas _screen;
        private readonly IArtifactBufferReadSink _board;
        private readonly Camera _camera;
        private readonly ArtifactBoardMapper _mapper;
        // 마지막으로 칸을 구할 때 쓴 출력 카메라 자세입니다. 끄는 동안 카메라가 섞일 때만 다시 판정합니다(로비 리팩토링 2차 설계 N-35).
        private Matrix4x4 _lastView;
        private Matrix4x4 _lastProjection;
        private Rect _lastPixelRect;

        internal ArtifactPointerMapper(IArtifactPopupScreenAreas screen, IArtifactBufferReadSink board,
            Camera camera, ArtifactBoardMapper mapper)
        {
            _screen = screen ?? throw new ArgumentNullException(nameof(screen));
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _camera = camera != null ? camera : throw new ArgumentNullException(nameof(camera));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>포인터가 보유 목록 슬롯 위에 있는지입니다(UI 호버 상태).</summary>
        internal bool IsPoolHovered => _screen.IsPoolHovered;

        /// <param name="pickPool">보유 목록 슬롯을 고를지입니다. 누름만 고르고, 보드 호버는 슬롯을 다시 재지 않습니다(N-83).</param>
        internal ArtifactPointerHit Resolve(Vector2 point, bool pickPool = true)
        {
            if (_screen.IsBlockingArea(point)) return new ArtifactPointerHit(true, false, false, 0, false, false, default, null);
            int artifactId = 0;
            bool pool = pickPool && _screen.TryPickPool(point, out artifactId);
            bool returning = _screen.IsReturnArea(point);
            ArtifactBufferSnapshot board = _board.Current;
            CellCoord cell = default;
            bool mapped = !returning && MapCell(point, board, out cell);
            bool onBoard = mapped && ArtifactBoardMapper.Contains(cell, board.BoardWidth, board.BoardHeight);
            ArtifactData placed = null;
            if (onBoard)
            {
                for (int i = 0; i < board.PlacedItems.Count; i++)
                {
                    ArtifactData item = board.PlacedItems[i];
                    for (int j = 0; j < item.Cells.Count; j++)
                        if (item.Cells[j] == cell) { placed = item; break; }
                    if (placed != null) break;
                }
            }
            return new ArtifactPointerHit(false, returning, pool, artifactId, mapped, onBoard, cell, placed);
        }

        /// <summary>
        /// 끄는 동안의 후보 칸만 구하는 좁은 판정입니다. 보유 슬롯 고르기와 배치물 훑기를 하지 않습니다(N-35).
        /// 가림·반환 영역이면 칸이 없습니다(전체 판정과 같은 규칙).
        /// </summary>
        internal bool TryMapCandidate(Vector2 point, out CellCoord cell)
        {
            cell = default;
            RecordCamera();
            if (_screen.IsBlockingArea(point) || _screen.IsReturnArea(point)) return false;
            return MapCell(point, _board.Current, out cell);
        }

        /// <summary>마지막 칸 판정 뒤 출력 카메라 자세·투영·화면 영역이 바뀌었는지입니다.</summary>
        /// <remarks>Matrix4x4의 == 연산자는 근사 비교라 작은 섞임을 놓칠 수 있어 정확히 비교합니다(R2-e Codex 검수).</remarks>
        internal bool HasCameraMoved => !_camera.worldToCameraMatrix.Equals(_lastView) ||
            !_camera.projectionMatrix.Equals(_lastProjection) || _camera.pixelRect != _lastPixelRect;

        private void RecordCamera()
        {
            _lastView = _camera.worldToCameraMatrix;
            _lastProjection = _camera.projectionMatrix;
            _lastPixelRect = _camera.pixelRect;
        }

        private bool MapCell(Vector2 point, ArtifactBufferSnapshot board, out CellCoord cell)
        {
            RecordCamera();
            cell = default;
            return board.IsInitialized && _lastPixelRect.Contains(point) &&
                _mapper.TryMap(_camera.ScreenPointToRay(point), board.BoardWidth, board.BoardHeight, out cell);
        }
    }

    /// <summary>화면 판정 결과입니다. 보드 밖의 매핑 칸도 Core 프리뷰 요청에 전달할 수 있습니다.</summary>
    internal readonly struct ArtifactPointerHit
    {
        internal bool IsBlocked { get; }
        internal bool IsReturnArea { get; }
        internal bool HasPoolItem { get; }
        internal int PoolArtifactId { get; }
        internal bool HasCell { get; }
        internal bool IsOnBoard { get; }
        internal CellCoord Cell { get; }
        internal ArtifactData PlacedItem { get; }

        internal ArtifactPointerHit(bool blocked, bool returning, bool pool, int poolId,
            bool mapped, bool onBoard, CellCoord cell, ArtifactData placed)
        {
            IsBlocked = blocked;
            IsReturnArea = returning;
            HasPoolItem = pool;
            PoolArtifactId = poolId;
            HasCell = mapped;
            IsOnBoard = onBoard;
            Cell = cell;
            PlacedItem = placed;
        }
    }
}
