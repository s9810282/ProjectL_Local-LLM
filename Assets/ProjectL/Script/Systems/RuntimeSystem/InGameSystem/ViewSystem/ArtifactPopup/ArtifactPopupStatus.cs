using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.Core;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>문구 상태와 표시 수명을 소유합니다. 도메인 정지 > 대기 > 프리뷰 실패 > 마지막 편집 결과 순입니다.</summary>
    internal sealed class ArtifactPopupStatus
    {
        private const float PlacementFailureDuration = 3f;
        private readonly IArtifactPopupDisplay _screen;
        private readonly IDelayService _delay;
        private bool _active = true;
        private bool _waiting;
        private bool _stopped;
        private string _preview = string.Empty;
        private string _edit = string.Empty;
        private string _shown;
        private int _reportedEditNumber;
        private bool _previewTemporary;
        private bool _editTemporary;
        // 지금 문구에 만료가 걸렸는지와 그 만료 시각입니다. 채널마다 대기 예약은 하나만 둡니다(로비 리팩토링 2차 설계 N-39).
        // Delay에는 예약 제거 API가 없어, 문구가 바뀌면 예약은 그대로 두고 만료 시각만 옮깁니다.
        private bool _previewScheduled;
        private bool _editScheduled;
        private float _previewDeadline;
        private float _editDeadline;
        private bool _previewTimerPending;
        private bool _editTimerPending;
        private readonly Action _expirePreview;
        private readonly Action _expireEdit;

        internal ArtifactPopupStatus(IArtifactPopupDisplay screen, IDelayService delay)
        {
            _screen = screen ?? throw new ArgumentNullException(nameof(screen));
            _delay = delay ?? throw new ArgumentNullException(nameof(delay));
            // 예약 콜백은 한 번만 만들어 실패 칸을 지날 때마다 클로저를 할당하지 않습니다(N-39).
            _expirePreview = () => OnTimer(true);
            _expireEdit = () => OnTimer(false);
        }

        internal void ResetDisplay(bool waiting)
        {
            _active = true;
            _waiting = waiting;
            ResetPreview();
            _shown = null;
        }

        /// <summary>숨긴 화면을 예약이 갱신하지 않도록 막고 임시 배치 오류를 버립니다.</summary>
        internal void Suspend()
        {
            _active = false;
            ResetPreview();
            _editScheduled = false;
            if (_editTemporary)
            {
                _edit = string.Empty;
                _editTemporary = false;
            }
        }

        internal void SetWaiting(bool waiting)
        {
            _waiting = waiting;
            Render();
        }

        /// <summary>아티팩트 도메인 정지를 반영합니다. 바뀐 때만 다시 그립니다(로비 리팩토링 2차 설계 M-06).</summary>
        internal void SetStopped(bool stopped)
        {
            if (_stopped == stopped) return;
            _stopped = stopped;
            Render();
        }

        internal void SetEdit(ArtifactCommandResult edit, bool isVisible)
        {
            if (edit.RequestNumber <= _reportedEditNumber) return;
            _reportedEditNumber = edit.RequestNumber;
            _editScheduled = false;
            _edit = ArtifactPopupText.ForEdit(edit.Result);
            _editTemporary = IsPlacementFailure(edit.Result.FailureReason);
            if (isVisible) Render();
        }

        internal void SetPreview(ArtifactActionFailureReason reason)
        {
            ResetPreview();
            _preview = reason == ArtifactActionFailureReason.None ? string.Empty : ArtifactPopupText.ForFailure(reason);
            _previewTemporary = IsPlacementFailure(reason);
            Render();
        }

        internal void ClearPreview()
        {
            ResetPreview();
            Render();
        }

        private void ResetPreview()
        {
            _preview = string.Empty;
            _previewTemporary = false;
            _previewScheduled = false;
        }

        private void Render()
        {
            if (!_active) return;
            bool preview = _preview.Length != 0;
            string message = _stopped ? ArtifactPopupText.Stopped
                : _waiting ? ArtifactPopupText.Waiting : preview ? _preview : _edit;
            if (_shown != message)
            {
                _screen.ShowStatus(message);
                _shown = message;
            }
            // 같은 문구라도 새 요청이면 새로 3초를 줍니다. 가려진 문구는 처음 표시될 때 시작합니다.
            if (!_stopped && !_waiting) ScheduleExpiry(preview);
        }

        private void ScheduleExpiry(bool preview)
        {
            if (preview ? !_previewTemporary || _previewScheduled : !_editTemporary || _editScheduled) return;
            float deadline = _delay.CurrentTime + PlacementFailureDuration;
            if (preview)
            {
                _previewScheduled = true;
                _previewDeadline = deadline;
                if (_previewTimerPending) return;
                _previewTimerPending = true;
            }
            else
            {
                _editScheduled = true;
                _editDeadline = deadline;
                if (_editTimerPending) return;
                _editTimerPending = true;
            }
            _delay.AddDelayEvent(new DelayDataHandler(preview ? _expirePreview : _expireEdit, null, PlacementFailureDuration));
        }

        /// <summary>채널의 대기 예약이 끝났습니다. 그 사이 만료 시각이 뒤로 옮겨졌으면 남은 시간만큼 한 번 더 기다립니다.</summary>
        private void OnTimer(bool preview)
        {
            if (preview) _previewTimerPending = false;
            else _editTimerPending = false;
            // 교체·닫기·해제로 만료가 풀린 문구는 그대로 둡니다.
            if (!_active || !(preview ? _previewScheduled : _editScheduled)) return;
            float remaining = (preview ? _previewDeadline : _editDeadline) - _delay.CurrentTime;
            if (remaining > 0f)
            {
                if (preview) _previewTimerPending = true;
                else _editTimerPending = true;
                _delay.AddDelayEvent(new DelayDataHandler(preview ? _expirePreview : _expireEdit, null, remaining));
                return;
            }
            if (preview) ResetPreview();
            else
            {
                _edit = string.Empty;
                _editTemporary = false;
                _editScheduled = false;
            }
            Render();
        }

        private static bool IsPlacementFailure(ArtifactActionFailureReason reason) =>
            reason == ArtifactActionFailureReason.OutOfBounds || reason == ArtifactActionFailureReason.CellOccupied;
    }
}
