using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 피격 플래시 조각입니다(조각·조립 설계 BP9). 시작하자마자 플래시 색으로 바꾸고, 시간 동안 원래 색으로 돌아온 뒤 끝납니다.
    /// 색은 ColorSystem 손잡이로만 바뀌고(표시의 외형 소유자), 플래시 층 대여는 실행이 빌리고 되돌립니다(설계 Q-B6 A).
    /// </summary>
    internal sealed class CombatFlashPiece : ICombatPresentationPiece
    {
        private readonly EntityId _unit;
        private readonly Color _color;
        private readonly float _duration;
        private ICombatUnitFlash _flash;
        private float _elapsed;
        private bool _began;

        /// <param name="unit">플래시 채널을 빌린 유닛입니다. 조립이 그 채널을 적어야 합니다.</param>
        /// <param name="duration">원래 색으로 돌아오는 시간(초)입니다. 0이면 시작 안에서 끝납니다(보이지 않음).</param>
        internal CombatFlashPiece(EntityId unit, Color color, float duration)
        {
            if (unit.IsEmpty)
                throw new ArgumentException("플래시할 유닛이 비어 있습니다.", nameof(unit));
            // 시간 조건은 설정 Data 한 곳을 씁니다(연출 조립 설계 §9 I-5).
            CombatPresentationSettingsData.ValidateSeconds(duration, nameof(duration));
            _unit = unit;
            _color = color;
            _duration = duration;
        }

        public bool IsEnded => _began && _elapsed >= _duration;

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            _flash = context.Flash(_unit);
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

        /// <summary>남은 세기(1 → 0, 선형)로 플래시 색을 씁니다. 끝나면 세기 0(플래시 층 지움)입니다.</summary>
        private void Apply()
        {
            float strength = _duration <= 0f ? 0f : 1f - Mathf.Clamp01(_elapsed / _duration);
            _flash.SetFlash(_color, strength);
        }
    }
}
