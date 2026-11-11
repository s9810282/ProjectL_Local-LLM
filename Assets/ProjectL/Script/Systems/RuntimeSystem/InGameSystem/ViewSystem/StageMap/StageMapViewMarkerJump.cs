using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 현재 위치 말이 노드로 뛰어 가는 연출 하나입니다. 시작(목적지·점프 리듬·지연 감싸개·이동 소리), 진행(카메라 동행 진행률),
    /// 끝(말 움직임 정리)을 맡습니다. 비전투 노드 이동과 출전 미리 이동이 하나씩 갖고, 차이(카메라 확정 시점·취소 때 카메라 되돌림·
    /// 정리 범위)는 그 두 부품에 둡니다(스테이지 선택 리팩토링 설계 §2.8 C-02).
    /// </summary>
    /// <remarks>
    /// 문맥 확인(<c>checkContext</c>)은 소유 부품의 것입니다. 확인 중 소유 부품이 이 연출을 정리할 수 있으므로,
    /// 재진입할 수 있는 호출(표시 바꾸기·지연 예약·카메라 시작) 뒤마다 다시 묻고 거짓이면 바로 멈춥니다.
    /// </remarks>
    internal sealed class StageMapViewMarkerJump
    {
        // 점프 한 번의 시간 비율입니다(오르기 12 : 날기 35 : 내리기 12, 합 59). 전체 시간은 DB의 마커 이동 시간입니다.
        private const float RiseShare = 12f / 59f;
        private const float TravelShare = 35f / 59f;
        private const float LandShare = 12f / 59f;

        private readonly StageMapViewDisplay _display;
        private readonly Transform _mapRoot;
        private readonly StageMapViewData _data;
        private readonly IDelayService _delay;
        private readonly IAudioSystemService _audio;
        private readonly StageMapCameraFocus _cameraFocus;
        private readonly string _owner;
        private ChessPieceMotion _motion;
        private StageMapMovementDelay _motionDelay;
        private long _focusOperation;
        private float _startTime;
        private float _duration;
        private bool _markerComplete;
        private StageMarkerJumpKind _kind;

        internal StageMapViewMarkerJump(StageMapViewDisplay display, Transform mapRoot, StageMapViewData data,
            IDelayService delay, IAudioSystemService audio, StageMapCameraFocus cameraFocus, string owner)
        {
            _display = display ?? throw new ArgumentNullException(nameof(display));
            _mapRoot = mapRoot != null ? mapRoot : throw new ArgumentNullException(nameof(mapRoot));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _delay = delay ?? throw new ArgumentNullException(nameof(delay));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _cameraFocus = cameraFocus ?? throw new ArgumentNullException(nameof(cameraFocus));
            _owner = string.IsNullOrEmpty(owner) ? throw new ArgumentException("소유 부품 이름이 필요합니다.", nameof(owner)) : owner;
        }

        /// <summary>
        /// 노드로 뛰기 시작합니다. 말을 이동 상태로 바꾸고, 점프·카메라 동행을 시작하고, 실제로 움직이면 이동 소리를 냅니다.
        /// 도중에 문맥이 끝나면 거기서 멈춥니다(정리는 소유 부품이 합니다).
        /// </summary>
        /// <param name="isMotionCurrent">늦게 도착한 지연 콜백이 말을 움직여도 되는지입니다(소유 부품의 세대·문맥).</param>
        /// <param name="checkContext">소유 부품의 문맥 확인입니다. 무효면 소유 부품이 이 연출을 정리하고 거짓을 돌려줍니다.</param>
        internal void Begin(int nodeId, StageMarkerJumpKind kind, Func<bool> isMotionCurrent, Func<bool> checkContext)
        {
            if (kind != StageMarkerJumpKind.Approach && kind != StageMarkerJumpKind.Settle)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (isMotionCurrent == null) throw new ArgumentNullException(nameof(isMotionCurrent));
            if (checkContext == null) throw new ArgumentNullException(nameof(checkContext));
            _kind = kind;
            _markerComplete = false;
            StageMarkerLanding landing = kind == StageMarkerJumpKind.Approach
                ? StageMarkerLanding.Front : StageMarkerLanding.Center;
            Transform moving = _display.BeginMarkerMove(nodeId, _mapRoot, landing);
            if (!checkContext()) return;
            Vector3 destination = _display.GetMarkerDestination(nodeId, landing);
            float duration = kind == StageMarkerJumpKind.Approach
                ? _data.MarkerMoveDuration : _data.MarkerVisit.SettleJumpSeconds;
            float height = kind == StageMarkerJumpKind.Approach
                ? _data.MarkerJumpHeight : _data.MarkerVisit.SettleJumpHeight;
            var settings = new ChessPieceMotionSettings(height,
                duration * RiseShare, duration * TravelShare, duration * LandShare);
            _duration = settings.TotalSeconds;
            _motionDelay = new StageMapMovementDelay(_delay, isMotionCurrent);
            _motion = new ChessPieceMotion(_motionDelay, moving, settings, true);
            _startTime = _delay.CurrentTime;
            _motion.Begin(destination, destination);
            if (!checkContext()) return;
            if (kind == StageMarkerJumpKind.Approach)
                _focusOperation = _cameraFocus.Begin(nodeId, _mapRoot.TransformPoint(destination));
            // 사전 출전 이동 뒤 같은 노드의 수락을 표시하는 경우에는 이동이 없어 소리를 중복 재생하지 않습니다.
            if (checkContext() && _motion?.IsMoving == true)
                _audio.Play(AmbgmEvent.LobbyWalk);
            // 예약 서비스의 동기 콜백에서 소유권을 잃으면 늦은 Delay가 Transform을 붙들지 않습니다.
            checkContext();
        }

        /// <summary>
        /// 접근은 말과 카메라가 모두 도착하면 참입니다. 작은 점프는 카메라를 움직이지 않고 말의 완료만 기다립니다.
        /// </summary>
        internal bool TryArrive(Func<bool> checkContext)
        {
            if (checkContext == null) throw new ArgumentNullException(nameof(checkContext));
            if (!_markerComplete && _motion != null) _markerComplete = _motion.TryConsumeCompletion();
            if (_kind == StageMarkerJumpKind.Settle) return checkContext() && _markerComplete;
            float now = _delay.CurrentTime;
            if (!float.IsFinite(now) || !float.IsFinite(_startTime) || !float.IsFinite(_duration) || _duration <= 0f)
                throw new InvalidOperationException($"[{_owner}] 이동 시각과 전체 시간은 유한한 양수 기간이어야 합니다.");
            bool cameraComplete = _cameraFocus.SetProgress(_focusOperation,
                _markerComplete ? 1f : Mathf.Clamp01((now - _startTime) / _duration));
            return checkContext() && _markerComplete && cameraComplete;
        }

        /// <summary>카메라 정렬을 확정합니다. 일반 이동은 착지 때, 출전은 출전 수락 때 부릅니다.</summary>
        internal void CommitCamera()
        {
            if (_kind == StageMarkerJumpKind.Settle) return;
            if (!_cameraFocus.Commit(_focusOperation))
                throw new InvalidOperationException($"[{_owner}] 카메라 정렬을 확정할 수 없습니다.");
            _focusOperation = 0;
        }

        /// <summary>말 움직임과 지연 감싸개를 끝냅니다. 카메라 정렬은 남깁니다(확정은 <see cref="CommitCamera"/>).</summary>
        internal void FinishMotion()
        {
            ChessPieceMotion finished = _motion;
            _motion = null;
            finished?.Dispose();
            _motionDelay?.Dispose();
            _motionDelay = null;
        }

        /// <summary>
        /// 지연 감싸개 → 말 움직임 → 카메라 정렬 순으로 거둡니다. <paramref name="restoreCamera"/>면 카메라를 출발 자리로 되돌립니다.
        /// 실패는 모아서 소유 부품의 정리 목록에 더합니다.
        /// </summary>
        internal void Cancel(bool restoreCamera, ref List<Exception> errors)
        {
            ChessPieceMotion motion = _motion;
            StageMapMovementDelay delay = _motionDelay;
            long operation = _focusOperation;
            bool cancelCamera = _kind == StageMarkerJumpKind.Approach;
            _motion = null;
            _motionDelay = null;
            _focusOperation = 0;
            _markerComplete = false;
            CleanupErrors.Try(() => delay?.Dispose(), ref errors);
            CleanupErrors.Try(() => motion?.Dispose(), ref errors);
            if (cancelCamera) CleanupErrors.Try(() => _cameraFocus.Cancel(operation, restoreCamera), ref errors);
        }
    }
}
