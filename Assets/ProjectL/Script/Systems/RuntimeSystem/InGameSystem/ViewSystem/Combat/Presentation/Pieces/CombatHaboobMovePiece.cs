using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 하붑 구름을 새 자리로 미끄러뜨리는 조각입니다(하붑 View 설계 §9.4, 계획 HP-26). 천천히 출발해 천천히 멈춥니다. 어느 연기가 어디로 가는지는 표시가 압니다.
    /// 화면 표시일 뿐이라 지나가는 칸·유닛에 아무 판정도 만들지 않습니다.
    /// </summary>
    internal sealed class CombatHaboobMovePiece : ICombatPresentationPiece
    {
        private readonly ICombatHaboobField _field;
        private readonly float _seconds;
        private float _elapsed;

        internal CombatHaboobMovePiece(ICombatHaboobField field, float seconds)
        {
            _field = field ?? throw new ArgumentNullException(nameof(field));
            if (!(seconds > 0f)) throw new ArgumentOutOfRangeException(nameof(seconds));
            _seconds = seconds;
        }

        public bool IsEnded { get; private set; }

        public void Begin(CombatPieceContext context)
        {
            _elapsed = 0f;
            IsEnded = false;
        }

        public void Tick(float deltaTime)
        {
            if (IsEnded) return;
            _elapsed = Mathf.Min(_elapsed + deltaTime, _seconds);
            _field.TickMove(Mathf.SmoothStep(0f, 1f, _elapsed / _seconds));
            IsEnded = _elapsed >= _seconds;
        }

        public void Cancel()
        {
            if (IsEnded) return;
            IsEnded = true;
            _field.TickMove(1f);
        }
    }
}
