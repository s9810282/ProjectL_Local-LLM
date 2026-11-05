using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 피격·사망 반응 조각입니다(조각·조립 설계 BP1·BP8, 결정 Q-B7 A). 외부 진행 조각이라 진행은 반응 재생기가 하고, 이 조각은 그 완료를 끝으로 받습니다.
    /// </summary>
    /// <remarks>
    /// 시작 입구(<c>begin</c>)는 완료 콜백을 받아 반응을 시작합니다. 반응이 정상으로 끝나면 끝을 알리고, 시작할 수 없으면 예외입니다.
    /// 반응 자세·사망 유지 대여와 반응 불가 처리는 지금처럼 처리기·반응 재생기가 가집니다(설계 BP1). 그래서 표시가 사라졌을 때의 오류는 반응 재생기가 알립니다(B14 O13).
    /// 취소·표시 사라짐으로 반응이 끝을 알리지 않으면 이 실행은 남습니다. 그 경로는 반응 재생기의 오류로 재생이 멈추고, 사건 Presenter의 취소가 실행까지 정리합니다.
    /// </remarks>
    internal sealed class CombatReactionPiece : ICombatPresentationPiece
    {
        private readonly Action<Action> _begin;

        internal CombatReactionPiece(Action<Action> begin) => _begin = begin ?? throw new ArgumentNullException(nameof(begin));

        public bool IsEnded => false;

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            _begin(context.EndCallback());
        }

        public void Tick(float deltaTime)
        {
        }

        public void Cancel()
        {
        }
    }
}
