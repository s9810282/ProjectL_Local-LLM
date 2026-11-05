using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 소환 등장 조각입니다(조각·조립 설계 BP7, 보스 활성화 설계 D6b). 시작 즉시 유닛을 제자리에서 깊이만큼 아래에 두고,
    /// 대기 뒤 올라오며 위쪽 축으로 돕니다. 식은 옛 소환 등장과 같습니다. 자세 대여는 실행이 빌리고 되돌립니다(설계 Q-B6 A).
    /// 값은 소환 조립이 설정에서 꺼내 넘기고, 값 조건은 설정 Data 한 곳을 씁니다(연출 조립 설계 §9 I-5).
    /// 회전은 관리 코드만으로 계산해 Unity 없이 시험할 수 있습니다.
    /// </summary>
    internal sealed class CombatRisePiece : ICombatPresentationPiece
    {
        private readonly EntityId _unit;
        private readonly float _delay;
        private readonly float _duration;
        private readonly float _depth;
        private readonly float _turns;
        private readonly Vector3 _up;
        private ICombatUnitPose _pose;
        private float _elapsed;
        private bool _ended;

        /// <param name="unit">자세 채널을 빌린 새 유닛입니다. 조립이 그 채널을 적어야 합니다.</param>
        /// <param name="delaySeconds">올라오기 전 대기(초)입니다.</param>
        /// <param name="riseSeconds">올라오는 시간(초)입니다.</param>
        /// <param name="depth">시작 깊이입니다.</param>
        /// <param name="turns">올라오며 도는 바퀴 수입니다.</param>
        /// <param name="worldUp">필드의 위쪽 축입니다. 길이가 0이면 월드 위쪽을 씁니다.</param>
        internal CombatRisePiece(EntityId unit, float delaySeconds, float riseSeconds, float depth, float turns, Vector3 worldUp)
        {
            if (unit.IsEmpty)
                throw new ArgumentException("등장할 유닛이 비어 있습니다.", nameof(unit));
            CombatPresentationSettingsData.ValidateSeconds(delaySeconds, nameof(delaySeconds));
            CombatPresentationSettingsData.ValidateSeconds(riseSeconds, nameof(riseSeconds));
            CombatPresentationSettingsData.ValidateRiseDepth(depth, nameof(depth));
            CombatPresentationSettingsData.ValidateRiseTurns(turns, nameof(turns));
            _unit = unit;
            _delay = delaySeconds;
            _duration = riseSeconds;
            _depth = depth;
            _turns = turns;
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

        /// <summary>지금 경과로 자세를 놓습니다. 끝났으면 true입니다.</summary>
        private bool Apply()
        {
            float delay = _delay;
            float duration = _duration;
            float progress = _elapsed <= delay ? 0f : duration <= 0f ? 1f : Mathf.Clamp01((_elapsed - delay) / duration);
            // 올라오는 끝에서 느려지도록 ease-out을 쓴다.
            float eased = 1f - ((1f - progress) * (1f - progress));
            Vector3 position = _pose.Position - (_up * (_depth * (1f - eased)));
            Quaternion rotation = Spin(_up, 360f * _turns * eased) * _pose.Rotation;
            _pose.ApplyPose(position, rotation);
            return _elapsed >= delay + duration;
        }

        /// <summary>단위 축 둘레로 각도(도)만큼 도는 회전입니다.</summary>
        private static Quaternion Spin(Vector3 axis, float degrees)
        {
            float half = degrees * Mathf.Deg2Rad * .5f;
            float sin = Mathf.Sin(half);
            return new Quaternion(axis.x * sin, axis.y * sin, axis.z * sin, Mathf.Cos(half));
        }
    }
}
