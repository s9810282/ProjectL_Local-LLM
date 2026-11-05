using System;
using ProjectL.Systems.DataBaseSystem.Data.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.Core;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.View;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common
{
    /// <summary>
    /// 체스 말 표시 이동의 예약·취소·완료 수명과 정착 자세를 소유합니다. 이동 중 위치·회전 곡선은 경로 모양(<see cref="ChessPiecePath"/>)이 냅니다.
    /// </summary>
    internal sealed class ChessPieceMotion : IDisposable
    {
        /// <summary>전역 예약에 남아도 취소된 표시 대상과 소유자를 보유하지 않는 콜백 문맥입니다.</summary>
        private sealed class Reservation
        {
            private ChessPieceMotion _owner;
            private Transform _target;

            internal Vector3 Origin { get; }
            internal Quaternion OriginRotation { get; }
            internal Vector3 Destination { get; }
            internal float StartTime { get; }
            internal float Seconds { get; }
            internal Transform Target => _target;

            internal Reservation(ChessPieceMotion owner, Transform target, Vector3 origin, Quaternion originRotation,
                Vector3 destination, float startTime, float seconds)
            {
                _owner = owner;
                _target = target;
                Origin = origin;
                OriginRotation = originRotation;
                Destination = destination;
                StartTime = startTime;
                Seconds = seconds;
            }

            internal void Tick()
            {
                _owner?.Tick(this);
            }

            internal void End()
            {
                _owner?.End(this);
            }

            internal void Detach()
            {
                _owner = null;
                _target = null;
            }
        }

        private IDelayService _delay;
        private Transform _target;
        private readonly ChessPiecePath _path;
        private readonly bool _useLocalSpace;
        private Vector3 _settledPosition;
        private Quaternion _settledRotation;
        private Reservation _reservation;
        private object _beginToken;
        private object _completionToken;
        private bool _disposed;

        internal bool IsMoving => !_disposed && _reservation != null;

        /// <summary>들기 → 이동 → 내려놓기 경로(<see cref="ChessPieceLiftPath"/>)로 움직입니다.</summary>
        internal ChessPieceMotion(IDelayService delay, Transform target, ChessPieceMotionSettings settings, bool useLocalSpace = false)
            : this(delay, target, new ChessPieceLiftPath(settings), useLocalSpace)
        {
        }

        /// <summary>넘겨받은 경로 모양으로 움직입니다. 모양의 위치는 <paramref name="useLocalSpace"/>와 같은 좌표계입니다.</summary>
        internal ChessPieceMotion(IDelayService delay, Transform target, ChessPiecePath path, bool useLocalSpace = false)
        {
            _delay = delay ?? throw new ArgumentNullException(nameof(delay));
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _useLocalSpace = useLocalSpace;
            if (!ViewPrefabRules.IsFinite(ReadPosition(target)))
                throw new InvalidOperationException("[ChessPieceMotion] 표시 대상의 시작 위치는 유한해야 합니다.");
            _target = target;
            _settledPosition = ReadPosition(target);
            _settledRotation = ReadRotation(target);
        }

        internal void Begin(Vector3 destination, Vector3 facingPosition)
        {
            BeginCore(destination, facingPosition);
        }

        internal void Begin(Vector3 destination) => BeginCore(destination, null);

        private void BeginCore(Vector3 destination, Vector3? facingPosition)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChessPieceMotion));
            if (!ViewPrefabRules.IsFinite(destination))
                throw new ArgumentOutOfRangeException(nameof(destination), "도착 위치는 유한해야 합니다.");
            if (facingPosition.HasValue && !ViewPrefabRules.IsFinite(facingPosition.Value))
                throw new ArgumentOutOfRangeException(nameof(facingPosition), "바라볼 위치는 유한해야 합니다.");
            if (_target == null)
                throw new InvalidOperationException("[ChessPieceMotion] 이동할 표시 대상이 없습니다.");
            if (IsMoving)
                throw new InvalidOperationException("[ChessPieceMotion] 이동 중에는 추가 목적지를 예약할 수 없습니다.");

            Cancel();
            object beginToken = new object();
            _beginToken = beginToken;
            Vector3 origin = _settledPosition;
            if (origin.Equals(destination))
            {
                SetPosition(_target, destination);
                SetRotation(_target, ChessPiecePath.FacingRotation(destination, facingPosition, _settledRotation));
                _settledPosition = destination;
                _settledRotation = ReadRotation(_target);
                _beginToken = null;
                _completionToken = beginToken;
                return;
            }

            IDelayService delay = _delay;
            // Delay의 시간 읽기는 콜백을 부르지 않는 getter입니다(비재진입 계약). 재진입 검사는 콜백이 도는
            // 예약 등록(AddDelayEvent)에만 둡니다(로비 리팩토링 2차 설계 N-18).
            float startTime = delay.CurrentTime;
            if (!float.IsFinite(startTime))
            {
                Cancel();
                throw new InvalidOperationException("[ChessPieceMotion] 이동 예약 시간은 유한해야 합니다.");
            }

            // 경로 준비가 실패하면 이 시작만 취소합니다.
            float seconds;
            try
            {
                seconds = _path.Plan(origin, _settledRotation, destination, facingPosition);
            }
            catch
            {
                if (ReferenceEquals(_beginToken, beginToken))
                    Cancel();
                throw;
            }
            if (!float.IsFinite(seconds) || seconds <= 0f || !float.IsFinite(startTime + seconds))
            {
                Cancel();
                throw new InvalidOperationException("[ChessPieceMotion] 경로 모양의 이동 시간은 유한한 양수여야 합니다.");
            }

            Reservation reservation = new Reservation(this, _target, origin, _settledRotation,
                destination, startTime, seconds);
            _beginToken = null;
            _reservation = reservation;
            try
            {
                delay.AddDelayEvent(new DelayDataHandler(reservation.End, reservation.Tick, reservation.Seconds));
            }
            catch
            {
                // 예전 예약 실패가 재진입으로 생긴 새 이동을 취소하지 않도록 소유 문맥을 다시 검사합니다.
                if (ReferenceEquals(_reservation, reservation) || ReferenceEquals(_completionToken, reservation))
                {
                    _settledPosition = reservation.Origin;
                    _settledRotation = reservation.OriginRotation;
                    Cancel();
                }
                reservation.Detach();
                throw;
            }
        }

        internal bool TryConsumeCompletion()
        {
            if (_disposed || _completionToken == null)
                return false;
            if (_target == null)
            {
                Cancel();
                return false;
            }
            _completionToken = null;
            return true;
        }

        internal void Cancel()
        {
            if (_disposed)
                return;
            CancelCore();
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            try
            {
                CancelCore();
            }
            finally
            {
                _target = null;
                _delay = null;
            }
        }

        private void CancelCore()
        {
            Reservation reservation = _reservation;
            _reservation = null;
            _beginToken = null;
            _completionToken = null;
            // 먼저 소유권과 참조를 없애야 전역 Delay에 남은 콜백이 표시를 다시 움직이지 않습니다.
            reservation?.Detach();
            if (_target != null)
            {
                SetPosition(_target, _settledPosition);
                SetRotation(_target, _settledRotation);
            }
        }

        private void Tick(Reservation reservation)
        {
            if (!ReferenceEquals(_reservation, reservation))
                return;
            if (reservation.Target == null)
            {
                Cancel();
                return;
            }

            // 시간 읽기는 콜백을 부르지 않으므로 예약·대상 검사를 다시 하지 않습니다(N-18).
            float currentTime = _delay.CurrentTime;
            if (!float.IsFinite(currentTime))
                throw new InvalidOperationException("[ChessPieceMotion] 이동 표시 시간은 유한해야 합니다.");

            float elapsed = Mathf.Clamp(currentTime - reservation.StartTime, 0f, reservation.Seconds);
            _path.Evaluate(elapsed, ReadRotation(reservation.Target), out Vector3 position, out Quaternion rotation);
            SetPosition(reservation.Target, position);
            SetRotation(reservation.Target, rotation);
        }

        private void End(Reservation reservation)
        {
            if (!ReferenceEquals(_reservation, reservation))
                return;
            if (reservation.Target == null)
            {
                Cancel();
                return;
            }

            // 끝 회전은 경로 모양의 마지막 자세에서, 위치는 도착 위치로 정확히 맞춥니다.
            _path.Evaluate(reservation.Seconds, ReadRotation(reservation.Target), out _, out Quaternion rotation);
            SetPosition(reservation.Target, reservation.Destination);
            SetRotation(reservation.Target, rotation);
            _settledPosition = reservation.Destination;
            _settledRotation = ReadRotation(reservation.Target);
            _reservation = null;
            _completionToken = reservation;
            reservation.Detach();
            // 도착 동작은 여기서 호출하지 않습니다. 유효한 View Tick이 완료를 한 번 소비합니다.
        }

        private Vector3 ReadPosition(Transform target) => _useLocalSpace ? target.localPosition : target.position;

        private Quaternion ReadRotation(Transform target) => _useLocalSpace ? target.localRotation : target.rotation;

        private void SetPosition(Transform target, Vector3 position)
        {
            if (_useLocalSpace)
                target.localPosition = position;
            else
                target.position = position;
        }

        private void SetRotation(Transform target, Quaternion rotation)
        {
            if (_useLocalSpace)
                target.localRotation = rotation;
            else
                target.rotation = rotation;
        }
    }
}
