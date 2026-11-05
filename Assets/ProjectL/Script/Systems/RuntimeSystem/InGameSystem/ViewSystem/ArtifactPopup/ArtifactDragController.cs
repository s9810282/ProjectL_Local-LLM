using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactInput;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ArtifactView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>선택·회전·놓기를 입력 Buffer에 기록하고 자기 번호와 일치하는 Core 결과만 표시합니다.</summary>
    internal sealed class ArtifactDragController : IArtifactInputReceiver
    {
        private readonly ArtifactPointerMapper _pointerMapper;
        private readonly IArtifactBufferReadSink _board;
        private readonly IArtifactViewBufferReadSink _input;
        private readonly IArtifactViewBufferWriteSink _writer;
        private readonly IArtifactCommandResultReadSink _results;
        private readonly ArtifactBoardRefresher _refresher;
        private readonly IArtifactDragPresenter _presenter;
        private readonly ArtifactPopupStatus _status;
        private readonly Func<bool> _allowsInput;
        private readonly ArtifactInteraction _interaction = new();
        private Vector2 _pointer;
        private bool _hasPointer;
        private ArtifactBufferSnapshot _hoverSnapshot;
        // 마지막 보드 호버 판단 때 포인터가 UI 보유 슬롯 위였는지입니다(N-83).
        private bool _hoverPoolSnapshot;
        // 보드 호버가 마지막으로 상세에 쓴 대상입니다(배치물 인스턴스 ID, 빈 칸이면 NoHoverItem). 바뀔 때만 상세를 씁니다(N-36).
        private int _hoverTarget = UnknownHoverTarget;
        private const int NoHoverItem = -1;
        private const int UnknownHoverTarget = int.MinValue;
        private int _previewNumber;
        private int _shownPreviewNumber;
        private bool _stopped;
        // 드래그 표시(모델·숨긴 배치물)를 켰는지입니다. 지울 것이 있을 때만 표시 정리를 합니다(로비 리팩토링 2차 설계 N-33).
        private bool _dragShown;

        internal ArtifactDragController(ArtifactPointerMapper pointerMapper, IArtifactBufferReadSink board,
            IArtifactViewBufferReadSink input, IArtifactViewBufferWriteSink writer, IArtifactCommandResultReadSink results,
            ArtifactBoardRefresher refresher, IArtifactDragPresenter presenter,
            ArtifactPopupStatus status, Func<bool> allowsInput)
        {
            _pointerMapper = pointerMapper ?? throw new ArgumentNullException(nameof(pointerMapper));
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _refresher = refresher ?? throw new ArgumentNullException(nameof(refresher));
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _status = status ?? throw new ArgumentNullException(nameof(status));
            _allowsInput = allowsInput ?? throw new ArgumentNullException(nameof(allowsInput));
        }

        internal bool IsDragging => _interaction.IsDragging;
        /// <summary>보드 호버·상세를 보여도 되는지입니다(입력 허용 + 보드 준비). 끌기 판정은 여기에 대기 편집이 없음을 더합니다(N-43).</summary>
        private bool CanHover => _allowsInput() && _board.Current.IsInitialized;
        private bool AllowsInteraction => CanHover && _input.Commands.Count == 0;

        internal void Tick(bool isVisible)
        {
            if (_stopped) return;
            ConsumeEditResult(isVisible);
            if (!isVisible) return;
            if (!IsDragging)
            {
                // 보드 값이 바뀌었거나 UI 슬롯 호버가 시작·끝났으면 지금 포인터로 다시 고릅니다. 슬롯 이탈(EventSystem)이
                // 포인터 이동(InputSystem)보다 늦게 와도 상세가 빈 채 남지 않습니다(N-83 검수).
                if (_hasPointer && (!ReferenceEquals(_hoverSnapshot, _board.Current) ||
                    _hoverPoolSnapshot != _pointerMapper.IsPoolHovered))
                {
                    // 보드·슬롯 호버가 바뀌었거나 다른 경로가 상세를 썼으면 마지막 대상을 잊고 다시 씁니다.
                    _hoverTarget = UnknownHoverTarget;
                    if (CanHover) UpdateHover();
                    else
                    {
                        _hoverSnapshot = _board.Current;
                        _hoverPoolSnapshot = _pointerMapper.IsPoolHovered;
                        _refresher.ClearDetail();
                    }
                }
                return;
            }
            if (!AllowsInteraction)
            {
                SelectCanceled();
                return;
            }
            // 포인터가 멈춰도 출력 카메라(Cinemachine이 로비 가상 카메라를 따라감)가 섞이는 동안에는 포인터 아래 칸이 바뀝니다.
            // 포인터 이동은 PointerMoved가 바로 반영하므로, Tick은 카메라 자세가 바뀐 프레임에만 다시 봅니다(LR5-9, 로비 리팩토링 2차 설계 N-35).
            if (_pointerMapper.HasCameraMoved) UpdateCandidate();
            var latest = _results.LastAnsweredPreview;
            if (!_input.HasPreview || _input.Preview.Number != _previewNumber || _previewNumber == 0 ||
                _shownPreviewNumber == _previewNumber || !latest.HasValue || latest.Value.RequestNumber != _previewNumber) return;
            if (!latest.Value.Data.Matches(_input.Preview.Data))
                throw new InvalidOperationException("[ArtifactDragController] 프리뷰 결과가 현재 입력과 다릅니다.");
            _shownPreviewNumber = _previewNumber;
            if (latest.Value.IsSkipped) return;
            _presenter.ShowPreview(latest.Value.Preview, _interaction.Rotation, _board.Current.BoardWidth, _board.Current.BoardHeight);
            _status.SetPreview(latest.Value.Preview.FailureReason);
        }

        /// <summary>연결 수명이 소유한 입력과 대응하는 완료만 소비합니다. 숨김 중에는 문구만 보관합니다.</summary>
        internal void ConsumeEditResult(bool isVisible)
        {
            if (_stopped) return;
            var result = _results.LastCommand;
            if (!result.HasValue || !_input.TryGetNextCommand(0, out var pending) || result.Value.RequestNumber < pending.Number) return;
            if (result.Value.RequestNumber != pending.Number || !result.Value.Data.Matches(pending.Data))
                throw new InvalidOperationException("[ArtifactDragController] 편집 결과가 대기 입력과 다릅니다.");
            _status.SetEdit(result.Value, isVisible);
            _writer.RemoveCommandsThrough(result.Value.RequestNumber);
        }

        public void PointerMoved(float screenX, float screenY)
        {
            if (_stopped) return;
            _pointer = new Vector2(screenX, screenY);
            _hasPointer = true;
            if (!IsDragging && CanHover)
            {
                UpdateHover();
                return;
            }
            if (!AllowsInteraction)
            {
                SelectCanceled();
                return;
            }
            if (IsDragging) UpdateCandidate();
        }

        public void SelectPressed(float screenX, float screenY)
        {
            if (_stopped) return;
            _pointer = new Vector2(screenX, screenY);
            _hasPointer = true;
            // 막혔든 아니든 지난 선택을 먼저 끝냅니다(N-25).
            SelectCanceled();
            if (!AllowsInteraction) return;
            ArtifactPointerHit hit = _pointerMapper.Resolve(_pointer);
            if (hit.IsBlocked) return;
            if (hit.HasPoolItem) _interaction.BeginPool(hit.PoolArtifactId);
            else if (hit.PlacedItem != null)
            {
                ArtifactData item = hit.PlacedItem;
                _interaction.BeginPlaced(item.ArtifactId, item.InstanceId, item.Anchor, item.Rotation, hit.Cell);
            }
            if (!IsDragging) return;
            // 보유 목록 슬롯의 상세는 UI(호버·클릭)가 갖습니다. 드래그는 놓인 배치물을 집을 때만 상세를 씁니다(N-83, Q-R2c-1 A).
            if (_interaction.IsPlaced) _refresher.ShowDetail(_interaction.ArtifactId);
            _presenter.BeginDrag(_refresher.GetModel(_interaction.ArtifactId), _interaction.InstanceId, _interaction.IsPlaced);
            _dragShown = true;
            // 같은 누름의 판정 결과를 후보 계산에 그대로 씁니다(N-34).
            UpdateCandidate(hit);
        }

        public void SelectReleased(float screenX, float screenY)
        {
            if (_stopped) return;
            _pointer = new Vector2(screenX, screenY);
            _hasPointer = true;
            if (!AllowsInteraction || !IsDragging)
            {
                SelectCanceled();
                return;
            }
            ArtifactPointerHit hit = _pointerMapper.Resolve(_pointer);
            UpdateCandidate(hit);
            ArtifactViewCommandData? request = _interaction.Release(hit.IsOnBoard, hit.IsReturnArea);
            SelectCanceled();
            if (request.HasValue) _writer.Submit(request.Value);
        }

        public void RotateLeft() => Rotate(-1);
        public void RotateRight() => Rotate(1);

        public void Cancel()
        {
            if (_stopped) return;
            if (!AllowsInteraction)
            {
                SelectCanceled();
                return;
            }
            ArtifactViewCommandData? request = null;
            if (IsDragging)
            {
                if (_interaction.IsPlaced) request = ArtifactViewCommandData.Return(_interaction.InstanceId);
            }
            else if (_hasPointer)
            {
                // 팝업을 다시 연 뒤 아직 좌표를 받지 못했으면 지난 좌표로 회수하지 않습니다(로비 리팩토링 2차 설계 N-09).
                ArtifactData placed = _pointerMapper.Resolve(_pointer).PlacedItem;
                if (placed != null) request = ArtifactViewCommandData.Return(placed.InstanceId);
            }
            SelectCanceled();
            if (request.HasValue) _writer.Submit(request.Value);
        }

        public void SelectCanceled()
        {
            if (_stopped) return;
            // 끌고 있지 않고 보인 프리뷰도 없으면 드래그 표시 정리(Buffer 프리뷰·문구·모델 다시 켜기)를 건너뜁니다(N-33).
            // 상세 지우기는 지금처럼 늘 합니다. 보유 슬롯 상세는 UI가 지키므로 지워지지 않습니다(R2-d Codex 검수).
            bool hasDisplay = _interaction.IsDragging || _dragShown || _previewNumber != 0 || _shownPreviewNumber != 0;
            _interaction.Cancel();
            _hoverSnapshot = null;
            _hoverTarget = UnknownHoverTarget;
            if (hasDisplay) ClearDisplayedPreview(true);
            else _refresher.ClearDetail();
        }

        /// <summary>연결 수명이 끝난 수신자는 새 연결의 입력 Buffer를 변경할 수 없습니다.</summary>
        internal void Stop()
        {
            if (_stopped) return;
            _stopped = true;
            _hasPointer = false;
            _hoverSnapshot = null;
            _hoverTarget = UnknownHoverTarget;
            _interaction.Cancel();
            ClearDisplayedPreview(true);
        }

        /// <summary>팝업을 닫을 때 이전 화면의 포인터 위치와 상세를 버립니다.</summary>
        internal void ResetPointer()
        {
            _hasPointer = false;
            _hoverSnapshot = null;
            _hoverTarget = UnknownHoverTarget;
            _refresher.ClearDetail();
        }

        private void Rotate(int steps)
        {
            if (_stopped) return;
            if (!AllowsInteraction)
            {
                SelectCanceled();
                return;
            }
            if (!IsDragging) return;
            _interaction.Rotate(steps);
            ClearDisplayedPreview(false);
            UpdateCandidate();
        }

        // 끄는 동안 쓰는 값은 칸뿐이라 칸만 구하는 좁은 판정을 씁니다(N-35).
        private void UpdateCandidate()
        {
            bool hasCell = _pointerMapper.TryMapCandidate(_pointer, out CellCoord cell);
            ApplyCandidate(cell, hasCell);
        }

        private void UpdateCandidate(ArtifactPointerHit hit) => ApplyCandidate(hit.Cell, hit.HasCell);

        private void ApplyCandidate(CellCoord cell, bool hasCell)
        {
            uint revision = _interaction.Revision;
            _interaction.SetCandidate(cell, hasCell);
            if (_interaction.Revision != revision) ClearDisplayedPreview(false);
            if (_interaction.TryTakePreview(out ArtifactViewPreviewData request))
                _previewNumber = _writer.SetPreview(request);
        }

        private void UpdateHover()
        {
            _hoverSnapshot = _board.Current;
            _hoverPoolSnapshot = _pointerMapper.IsPoolHovered;
            // 보유 목록의 진입·이탈과 상세 소유권은 UIInputBinding이 맡습니다. 슬롯 위인지는 UI 호버 상태로 묻고
            // 포인터 이동마다 슬롯 영역을 다시 재지 않습니다(N-83).
            if (_hoverPoolSnapshot)
            {
                _hoverTarget = UnknownHoverTarget;
                return;
            }
            ArtifactPointerHit hit = _pointerMapper.Resolve(_pointer, pickPool: false);
            int target = hit.PlacedItem != null ? hit.PlacedItem.InstanceId : NoHoverItem;
            // 같은 배치물·빈 칸 위에서는 상세를 다시 쓰지 않습니다(N-36).
            if (target == _hoverTarget) return;
            _hoverTarget = target;
            if (hit.PlacedItem != null) _refresher.ShowDetail(hit.PlacedItem.ArtifactId);
            else _refresher.ClearDetail();
        }

        private void ClearDisplayedPreview(bool endDrag)
        {
            if (endDrag) _dragShown = false;
            _writer.ClearPreview();
            _previewNumber = _shownPreviewNumber = 0;
            List<Exception> failures = null;
            try
            {
                _status.ClearPreview();
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(error);
            }
            try
            {
                if (endDrag) _presenter.EndDrag();
                else _presenter.ClearPreview();
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(error);
            }
            if (endDrag)
            {
                try
                {
                    _refresher.ClearDetail();
                }
                catch (Exception error)
                {
                    (failures ??= new()).Add(error);
                }
            }
            if (failures != null) throw new AggregateException("[ArtifactDragController] 드래그 표시 정리 중 실패했습니다.", failures);
        }
    }
}
