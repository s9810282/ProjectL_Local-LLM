using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 재생 강조를 끄는 시간 없는 조각입니다(조각·조립 설계 BP7). 포효·소환 조립의 마지막 항목입니다.
    /// 강조를 켜는 쪽은 지금처럼 사건 틀입니다(설계 §5 "강조 소유가 둘로 나뉨").
    /// </summary>
    internal sealed class CombatHighlightClearPiece : ICombatPresentationPiece
    {
        private readonly CombatPlaybackHighlight _highlight;

        internal CombatHighlightClearPiece(CombatPlaybackHighlight highlight) =>
            _highlight = highlight ?? throw new ArgumentNullException(nameof(highlight));

        public bool IsEnded { get; private set; }

        public void Begin(CombatPieceContext context)
        {
            _highlight.Clear();
            IsEnded = true;
        }

        public void Tick(float deltaTime)
        {
        }

        public void Cancel()
        {
        }
    }
}
