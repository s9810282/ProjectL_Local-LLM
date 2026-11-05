using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>위치 교환의 설정 시간만 기다립니다. 위치·이펙트·사건 완료는 이 조각을 조립한 쪽의 책임입니다.</summary>
    internal sealed class CombatPositionSwapDelayPiece : ICombatPresentationPiece
    {
        private readonly CombatPositionSwapVisualData _visual;
        private float _elapsed;
        private bool _began;

        internal CombatPositionSwapDelayPiece(CombatPositionSwapVisualData visual)
        {
            _visual = visual ?? throw new ArgumentNullException(nameof(visual));
        }

        public bool IsEnded => _began && _elapsed >= _visual.WaitSeconds;

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            _elapsed = 0f;
            _began = true;
        }

        public void Tick(float deltaTime) => _elapsed += deltaTime;

        public void Cancel()
        {
        }
    }
}
