using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 돌 하나를 출발 자리에서 칸 표면까지 떨어뜨리는 조각입니다(낙석 View 계획 Rc.1, §6.6, 설계 RG5). 옆으로는 일정하게 다가오고 아래로는 점점 빨라져, 옆 변위가 있으면 곡선으로 떨어집니다. 값은 <see cref="CombatRockfallDrop"/>이고 이 조각은 그리기만 합니다.
    /// 칸에 닿거나 취소되면 돌을 돌려줘 사라지게 합니다. 착지 이펙트·흔들림은 조립이 이 조각의 끝 신호에 잇습니다.
    /// </summary>
    internal sealed class CombatRockfallDropPiece : ICombatPresentationPiece
    {
        private readonly ICombatRockfallField _field;
        private readonly CombatRockfallDrop _drop;
        private readonly float _seconds;
        private GameObject _stone;
        private float _elapsed;

        internal CombatRockfallDropPiece(ICombatRockfallField field, CombatRockfallDrop drop, float seconds)
        {
            _field = field ?? throw new ArgumentNullException(nameof(field));
            if (!(seconds > 0f)) throw new ArgumentOutOfRangeException(nameof(seconds));
            _drop = drop;
            _seconds = seconds;
        }

        public bool IsEnded { get; private set; }

        public void Begin(CombatPieceContext context)
        {
            _elapsed = 0f;
            IsEnded = false;
            _stone = _field.RentStone(_drop.Scale);
            Draw();
        }

        public void Tick(float deltaTime)
        {
            if (IsEnded) return;
            _elapsed = Mathf.Min(_elapsed + deltaTime, _seconds);
            Draw();
            if (_elapsed >= _seconds)
                Finish();
        }

        public void Cancel() => Finish();

        private void Draw()
        {
            float progress = _elapsed / _seconds;
            Vector3 position = _drop.Landing + (_drop.Side * (1f - progress)) + (_drop.Rise * (1f - (progress * progress)));
            // 회전은 경과 시간에서 바로 구해 프레임마다 쌓이지 않습니다.
            Quaternion rotation = Quaternion.AngleAxis(_drop.SpinDegrees * _elapsed, _drop.SpinAxis) * _drop.StartRotation;
            _stone.transform.SetPositionAndRotation(position, rotation);
        }

        private void Finish()
        {
            IsEnded = true;
            if (_stone == null) return;
            GameObject stone = _stone;
            _stone = null;
            _field.ReturnStone(stone);
        }
    }
}
