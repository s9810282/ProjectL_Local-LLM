using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 유닛 크기 배율을 시간 동안 옮기는 조각입니다(조각·조립 설계 BP7). 포효의 커짐·작아짐(맥박 절반씩)이 씁니다.
    /// 식은 옛 포효 동작의 크기 보간과 같습니다. 크기 대여는 실행이 빌리고 되돌립니다(설계 Q-B6 A).
    /// </summary>
    internal sealed class CombatScalePiece : ICombatPresentationPiece
    {
        private readonly EntityId _unit;
        private readonly float _from;
        private readonly float _to;
        private readonly float _duration;
        private ICombatUnitScale _scale;
        private float _elapsed;
        private bool _began;

        /// <param name="unit">크기 채널을 빌린 유닛입니다. 조립이 그 채널을 적어야 합니다.</param>
        /// <param name="duration">옮기는 시간(초)입니다. 0이면 시작하자마자 <paramref name="to"/>에 닿고 끝납니다.</param>
        internal CombatScalePiece(EntityId unit, float from, float to, float duration)
        {
            if (unit.IsEmpty)
                throw new ArgumentException("크기를 바꿀 유닛이 비어 있습니다.", nameof(unit));
            // 시간 조건은 설정 Data 한 곳을 씁니다(연출 조립 설계 §9 I-5).
            CombatPresentationSettingsData.ValidateSeconds(duration, nameof(duration));
            _unit = unit;
            _from = from;
            _to = to;
            _duration = duration;
        }

        public bool IsEnded => _began && _elapsed >= _duration;

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            _scale = context.Scale(_unit);
            _elapsed = 0f;
            _began = true;
            Apply();
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            Apply();
        }

        public void Cancel()
        {
        }

        private void Apply()
        {
            float progress = _duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / _duration);
            _scale.ApplyScale(Mathf.Lerp(_from, _to, progress));
        }
    }
}
