using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 유닛을 제자리에서 깊이만큼 가라앉히는 조각입니다(낭떠러지 추락, 임시). 점점 빨라지고(ease-in) 회전은 그대로 둡니다.
    /// 소환 등장(<see cref="CombatRisePiece"/>)을 거꾸로 한 움직임입니다. 자세 대여는 실행이 빌리고 되돌립니다(설계 Q-B6 A).
    /// </summary>
    internal sealed class CombatSinkPiece : ICombatPresentationPiece
    {
        private readonly EntityId _unit;
        private readonly float _duration;
        private readonly float _depth;
        private readonly Vector3 _up;
        private ICombatUnitPose _pose;
        private float _elapsed;
        private bool _ended;

        /// <param name="unit">자세 채널을 빌린 유닛입니다. 조립이 그 채널을 적어야 합니다.</param>
        /// <param name="seconds">가라앉는 시간(초)입니다. 0이면 시작하자마자 깊이에 닿고 끝납니다.</param>
        /// <param name="depth">가라앉는 깊이입니다.</param>
        /// <param name="worldUp">필드의 위쪽 축입니다. 길이가 0이면 월드 위쪽을 씁니다.</param>
        internal CombatSinkPiece(EntityId unit, float seconds, float depth, Vector3 worldUp)
        {
            if (unit.IsEmpty)
                throw new ArgumentException("가라앉을 유닛이 비어 있습니다.", nameof(unit));
            CombatPresentationSettingsData.ValidateSeconds(seconds, nameof(seconds));
            CombatPresentationSettingsData.ValidateFallDepth(depth, nameof(depth));
            _unit = unit;
            _duration = seconds;
            _depth = depth;
            _up = worldUp.sqrMagnitude > 0f ? worldUp.normalized : Vector3.up;
        }

        public bool IsEnded => _ended;

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            _pose = context.Pose(_unit);
            _elapsed = 0f;
            _ended = Apply();
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            _ended = Apply();
        }

        public void Cancel()
        {
        }

        /// <summary>
        /// 가라앉은 거리입니다. 떨어지는 느낌이 나도록 점점 빨라집니다(ease-in). 유닛 추락과 약한 지반 붕괴(타일 교체 설계 TR8)가 같은 곡선을 쓰는 한 곳입니다.
        /// </summary>
        /// <param name="elapsed">지난 시간(초)입니다.</param>
        /// <param name="seconds">전체 시간(초)입니다. 0 이하면 바로 깊이에 닿습니다.</param>
        /// <param name="depth">끝 깊이입니다.</param>
        internal static float SinkDistance(float elapsed, float seconds, float depth)
        {
            float progress = seconds <= 0f ? 1f : Mathf.Clamp01(elapsed / seconds);
            return depth * progress * progress;
        }

        /// <summary>지금 경과로 자세를 놓습니다. 끝났으면 true입니다.</summary>
        private bool Apply()
        {
            _pose.ApplyPose(_pose.Position - (_up * SinkDistance(_elapsed, _duration, _depth)), _pose.Rotation);
            return _elapsed >= _duration;
        }
    }
}
