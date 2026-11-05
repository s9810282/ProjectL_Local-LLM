using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common
{
    /// <summary>
    /// 들기 → 든 높이로 직선 이동 → 내려놓기 경로입니다. 들기 구간 안에서 바라볼 방향으로 돕니다.
    /// <see cref="ChessPieceMotion"/>의 설정 생성자가 이 모양을 씁니다(지금은 StageSelect 노드 말 이동과 전투 출발 연출 StageCombatDeparture).
    /// </summary>
    internal sealed class ChessPieceLiftPath : ChessPiecePath
    {
        private readonly ChessPieceMotionSettings _settings;
        private Vector3 _origin;
        private Quaternion _originRotation;
        private Vector3 _destination;
        private Vector3? _facingPosition;

        internal ChessPieceLiftPath(ChessPieceMotionSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        internal override float Plan(Vector3 origin, Quaternion originRotation, Vector3 destination, Vector3? facingPosition)
        {
            _origin = origin;
            _originRotation = originRotation;
            _destination = destination;
            _facingPosition = facingPosition;
            return _settings.TotalSeconds;
        }

        internal override void Evaluate(float elapsed, Quaternion currentRotation, out Vector3 position, out Quaternion rotation)
        {
            Vector3 liftedOrigin = _origin + Vector3.up * _settings.LiftHeight;
            Vector3 liftedDestination = _destination + Vector3.up * _settings.LiftHeight;
            float travelEnd = _settings.LiftSeconds + _settings.TravelSeconds;
            if (elapsed < _settings.LiftSeconds)
            {
                position = Vector3.Lerp(_origin, liftedOrigin, elapsed / _settings.LiftSeconds);
            }
            else if (elapsed < travelEnd)
            {
                position = Vector3.Lerp(liftedOrigin, liftedDestination,
                    (elapsed - _settings.LiftSeconds) / _settings.TravelSeconds);
            }
            else
            {
                position = Vector3.Lerp(liftedDestination, _destination,
                    (elapsed - travelEnd) / _settings.LowerSeconds);
            }
            Quaternion facing = FacingRotation(position, _facingPosition, currentRotation);
            rotation = elapsed < _settings.LiftSeconds
                ? Quaternion.Slerp(_originRotation, facing, elapsed / _settings.LiftSeconds)
                : facing;
        }
    }
}
