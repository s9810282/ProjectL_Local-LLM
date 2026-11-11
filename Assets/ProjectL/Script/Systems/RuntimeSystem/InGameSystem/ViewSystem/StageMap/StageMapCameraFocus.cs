using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>바닥 노드를 화면 높이에 맞추며 이동 주체가 전달한 전체 시간 진행률만 소비합니다.</summary>
    internal sealed class StageMapCameraFocus
    {
        private readonly StageMapCameraFocusData _settings;
        private IStageMapCameraFocusControl _control;
        private StageMapCameraFocusFrame _frame;
        private long _nextOperation;
        private long _operation;
        private float _destination;
        private bool _applied;
        private bool _released;

        internal StageMapCameraFocus(StageMapCameraFocusData settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _settings.Validate();
        }

        internal void Bind(IStageMapCameraFocusControl control)
        {
            if (control == null) throw new ArgumentNullException(nameof(control));
            if (_released || _control != null)
                throw new InvalidOperationException("[StageMapCameraFocus] 살아 있는 Scope에 한 번만 연결할 수 있습니다.");
            _control = control;
        }

        internal long Begin(int nodeId, Vector3 targetPosition)
        {
            if (_released || _control == null || _operation != 0)
                throw new InvalidOperationException("[StageMapCameraFocus] 연결된 유휴 정렬 부품이 필요합니다.");
            if (nodeId <= 0) throw new ArgumentOutOfRangeException(nameof(nodeId));
            long operation = checked(++_nextOperation);
            _operation = operation;
            _applied = !_settings.Enabled;
            try
            {
                if (_settings.Enabled)
                {
                    _frame = _control.BeginFocus();
                    _destination = CalculateOffset(nodeId, targetPosition, _frame, _settings.PlayerViewportY);
                }
                return operation;
            }
            catch (Exception error)
            {
                List<Exception> errors = new() { error };
                CleanupErrors.Try(() => Cancel(operation, false), ref errors);
                CleanupErrors.ThrowIfAny(errors, "[StageMapCameraFocus] 정렬 시작에 실패했습니다.");
                throw;
            }
        }

        /// <summary>최종 구도 적용 성공만 true입니다. 완료되어도 취소용 토큰은 Commit까지 보관합니다.</summary>
        internal bool SetProgress(long operation, float normalizedTime)
        {
            if (!Owns(operation)) return false;
            if (!float.IsFinite(normalizedTime)) throw new ArgumentOutOfRangeException(nameof(normalizedTime));
            if (!_settings.Enabled) return true;
            RequireCamera();
            if (_applied) return true;
            float u = Mathf.Clamp01(normalizedTime);
            float eased = u * u * (3f - 2f * u);
            float offset = u >= 1f ? _destination : Mathf.Clamp(
                Mathf.LerpUnclamped(_frame.StartOffset, _destination, eased), _frame.MinimumOffset, _frame.MaximumOffset);
            bool applied = _control.TrySetFocusOffset(_frame.Generation, offset);
            if (!Owns(operation)) return false;
            if (!applied) throw new InvalidOperationException("[StageMapCameraFocus] 적용 중 카메라 소유권을 잃었습니다.");
            RequireCamera();
            _applied = u >= 1f;
            return _applied;
        }

        internal bool Commit(long operation)
        {
            if (!Owns(operation) || !_applied) return false;
            if (_settings.Enabled) RequireCamera();
            Cancel(operation, false);
            return true;
        }

        internal void Cancel(long operation, bool restoreStart)
        {
            if (!Owns(operation)) return;
            long generation = _frame.Generation;
            _operation = 0;
            _frame = default;
            _applied = false;
            if (generation != 0) _control.EndFocus(generation, restoreStart);
        }

        internal void AlignImmediately(int nodeId, Vector3 targetPosition)
        {
            long operation = Begin(nodeId, targetPosition);
            try
            {
                if (!SetProgress(operation, 1f) || !Commit(operation))
                    throw new InvalidOperationException("[StageMapCameraFocus] 복귀 구도를 확정할 수 없습니다.");
            }
            finally
            {
                Cancel(operation, false);
            }
        }

        internal void Release(bool restoreStart)
        {
            if (_released) return;
            IStageMapCameraFocusControl control = _control;
            long generation = _frame.Generation;
            _released = true;
            _operation = 0;
            _frame = default;
            _control = null;
            _applied = false;
            if (generation != 0) control.EndFocus(generation, restoreStart);
        }

        private bool Owns(long operation) => !_released && operation > 0 && operation == _operation;

        private void RequireCamera()
        {
            if (!_control.IsFocusCurrent(_frame.Generation))
                throw new InvalidOperationException("[StageMapCameraFocus] 현재 정렬의 카메라 소유권이 유효하지 않습니다.");
        }

        private static float CalculateOffset(int nodeId, Vector3 target, StageMapCameraFocusFrame frame, float viewportY)
        {
            var data = frame.Framing;
            var lens = data.Lens;
            Quaternion rotation = data.Rotation * Quaternion.AngleAxis(lens.Dutch, Vector3.forward);
            Vector3 q = Quaternion.Inverse(rotation) * (target - data.Position);
            Vector3 a = Quaternion.Inverse(rotation) * frame.Axis;
            float v = 2f * viewportY - 1f;
            float k = lens.Orthographic ? 0f : v * Mathf.Tan(lens.FieldOfView * Mathf.Deg2Rad * 0.5f);
            float numerator = lens.Orthographic ? q.y - v * lens.OrthographicSize : q.y - k * q.z;
            float denominator = lens.Orthographic ? a.y : a.y - k * a.z;
            if (!Finite(q) || !Finite(a) || !float.IsFinite(denominator) || Mathf.Abs(denominator) < 0.000001f ||
                !float.IsFinite(frame.Aspect) || frame.Aspect <= 0f ||
                !float.IsFinite(frame.MinimumOffset) || !float.IsFinite(frame.MaximumOffset) ||
                !float.IsFinite(frame.StartOffset) || frame.MinimumOffset > frame.MaximumOffset ||
                !float.IsFinite(lens.NearClipPlane) || !float.IsFinite(lens.FarClipPlane) ||
                (!lens.Orthographic && (!float.IsFinite(lens.FieldOfView) || lens.FieldOfView <= 0f || lens.FieldOfView >= 180f)))
                throw ConfigurationError(nodeId, target, frame, "진행 축 투영 또는 렌즈/범위가 유효하지 않습니다.");
            float desired = numerator / denominator;
            if (!float.IsFinite(desired))
                throw ConfigurationError(nodeId, target, frame, "목표 offset이 유한하지 않습니다.");
            float offset = Mathf.Clamp(desired, frame.MinimumOffset, frame.MaximumOffset);
            Vector3 local = q - a * offset;
            float halfHeight = lens.Orthographic ? lens.OrthographicSize :
                local.z * Mathf.Tan(lens.FieldOfView * Mathf.Deg2Rad * 0.5f);
            float halfWidth = halfHeight * frame.Aspect;
            if (!Finite(local) || !float.IsFinite(halfHeight) || !float.IsFinite(halfWidth) ||
                halfHeight <= 0f || halfWidth <= 0f || local.z <= Mathf.Max(0f, lens.NearClipPlane) ||
                local.z >= lens.FarClipPlane || Mathf.Abs(local.x) > halfWidth || Mathf.Abs(local.y) > halfHeight)
                throw ConfigurationError(nodeId, target, frame, "축 이동 범위 안에서 대상이 화면에 들어오지 않습니다.");
            return offset;
        }

        private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        private static InvalidOperationException ConfigurationError(int nodeId, Vector3 target,
            StageMapCameraFocusFrame frame, string detail) => new(
            $"[StageMapCameraFocus] 노드 {nodeId}, 바닥 {target}, 기준 위치 {frame.Framing.Position}, " +
            $"회전 {frame.Framing.Rotation.eulerAngles}, 축 {frame.Axis}, 범위 [{frame.MinimumOffset}, {frame.MaximumOffset}]: {detail}");
    }
}
