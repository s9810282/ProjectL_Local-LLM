using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Terrain.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Environment;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 자세만 중심으로 당긴 뒤 설정 높이까지 올리고, 목적지 바로 위로 옮겨 목적지까지 수직으로 떨어뜨립니다.
    /// 끝 자세는 호출자가 넘긴 착지 자세(목적지의 서 있는 자세)라서, 사실 반영과 자세 반환을 하는 호출자·기존 실행기와 이어져도 튀지 않습니다.
    /// </summary>
    internal sealed class CombatTornadoLiftPiece : ICombatPresentationPiece
    {
        private readonly EntityId _unit;
        private readonly CombatTornadoLiftTarget _target;
        private readonly Pose _landing;
        private readonly Vector3 _up;
        private readonly TornadoPresentationSettingsData _settings;
        private readonly Vector3 _spinAxis;
        private readonly float _turnsPerSecond;
        private ICombatUnitPose _pose;
        private float _approachSeconds;
        private float _fallStart;
        private float _duration;
        private float _elapsed;
        private float _unwindDegrees;
        private Vector3 _unwindAxis;
        private bool _ended;

        /// <param name="landing">목적지에 서 있을 표시 자세입니다. 떨어지기의 끝 위치·회전입니다.</param>
        /// <param name="seed">연출 전용 seed입니다. Core·Unity 전역 난수를 읽거나 소비하지 않습니다.</param>
        internal CombatTornadoLiftPiece(EntityId unit, CombatTornadoLiftTarget target, Pose landing, Vector3 worldUp, int seed)
        {
            if (unit.IsEmpty) throw new ArgumentException("토네이도 대상 유닛이 비어 있습니다.", nameof(unit));
            if (!float.IsFinite(worldUp.sqrMagnitude) || worldUp.sqrMagnitude <= 0f)
                throw new ArgumentException("필드 위쪽 축이 올바르지 않습니다.", nameof(worldUp));
            if (!float.IsFinite(landing.position.sqrMagnitude))
                throw new ArgumentException("토네이도 착지 위치가 올바르지 않습니다.", nameof(landing));
            _unit = unit;
            _target = target;
            _landing = landing;
            _settings = target.Settings ?? throw new InvalidOperationException("토네이도 연출 설정이 없습니다.");
            _up = worldUp.normalized;
            var random = new System.Random(seed);
            float z = (float)(random.NextDouble() * 2d - 1d);
            double angle = random.NextDouble() * Math.PI * 2d;
            float radius = Mathf.Sqrt(Mathf.Max(0f, 1f - z * z));
            _spinAxis = new Vector3(radius * (float)Math.Cos(angle), z, radius * (float)Math.Sin(angle));
            _turnsPerSecond = Mathf.Lerp(_settings.SpinTurnsPerSecondRange.x, _settings.SpinTurnsPerSecondRange.y,
                (float)random.NextDouble());
        }

        public bool IsEnded => _ended;

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            _pose = context.Pose(_unit);
            // 이미 중심에 있으면 별도 접근 대기 없이 회전·상승을 시작합니다.
            _approachSeconds = _pose.Position == _target.Center ? 0f : _settings.ApproachSeconds;
            // 제한과 완료 판정에 같은 float 값을 써서 합산 시 중간 정밀도 차이로 끝나지 않는 일을 막습니다.
            _fallStart = _approachSeconds + _settings.RiseSeconds;
            _duration = _fallStart + _settings.DescendSeconds;
            // 꼭대기 회전에서 착지 회전까지 남은 회전을 한 축으로 풉니다. 상승 때와 비슷한 속도로 돌도록 온 바퀴를 더하며,
            // 온 바퀴는 같은 축이라 시작 회전을 바꾸지 않습니다. 착지 순간 회전은 정확히 착지 회전입니다.
            (SpinAt(_fallStart) * Quaternion.Inverse(_landing.rotation)).ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            _unwindAxis = float.IsFinite(axis.sqrMagnitude) && axis.sqrMagnitude > .5f ? axis : _spinAxis;
            _unwindDegrees = angle + Mathf.Sign(angle) * 360f * Mathf.Round(_turnsPerSecond * _settings.DescendSeconds);
            _elapsed = 0f;
            _ended = false;
            Apply();
        }

        public void Tick(float deltaTime)
        {
            if (_ended) return;
            _elapsed = Mathf.Min(_elapsed + deltaTime, _duration);
            Apply();
        }

        public void Cancel()
        {
            _ended = true;
            _pose = null;
        }

        private void Apply()
        {
            if (_elapsed >= _fallStart)
            {
                // 목적지 바로 위 RiseHeight에서 수직으로 떨어집니다. 회전은 시간에 고르게 풀려 착지 회전으로 끝납니다.
                float fall = _settings.Descend((_elapsed - _fallStart) / _settings.DescendSeconds);
                float unwind = 1f - (_elapsed - _fallStart) / _settings.DescendSeconds;
                _pose.ApplyPose(_landing.position + _up * (_settings.RiseHeight * (1f - fall)),
                    Quaternion.AngleAxis(_unwindDegrees * unwind, _unwindAxis) * _landing.rotation);
            }
            else if (_elapsed >= _approachSeconds)
            {
                Vector3 position = _target.Center +
                    _up * (_settings.RiseHeight * _settings.Rise((_elapsed - _approachSeconds) / _settings.RiseSeconds));
                _pose.ApplyPose(position, SpinAt(_elapsed));
            }
            else
                _pose.ApplyPose(Vector3.LerpUnclamped(_pose.Position, _target.Center, _settings.Approach(_elapsed / _approachSeconds)),
                    SpinAt(_elapsed));
            _ended = _elapsed >= _duration;
        }

        private Quaternion SpinAt(float time) => Quaternion.AngleAxis(360f * _turnsPerSecond * time, _spinAxis) * _pose.Rotation;
    }
}
