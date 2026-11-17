using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 코드 모션 공격 조각입니다(범용 연출 구조 설계 G5, 계획 Gc.2). 외부 진행 조각이라 진행은 공격 재생기가 하고, 이 조각은 그 완료를 끝으로 받습니다.
    /// 공격 클립이 없는 유닛의 공격(근접 박치기·직선 쏘기·곡사 쏘기)이 이 조각으로 조립 안에서 돕니다(<see cref="CombatCodeMotionAttack"/>).
    /// </summary>
    /// <remarks>
    /// 시작 입구(<c>tryBegin</c>)는 (완료 콜백, 기준 순간 콜백, 타격 순간 콜백)을 받아 모션을 시작하면 true, 시작하지 않으면 false입니다.
    /// false면 바로 끝나 다음 항목이 이어지고, 기준 순간·타격을 기다리던 항목은 시작하지 않고 끝난 것으로 봅니다(설계 BP2 보충).
    /// 기준 순간은 <see cref="CombatPieceSignal.MotionCue"/>, 타격은 <see cref="CombatPieceSignal.Hit"/>로 맥락의 신호 입구를 거쳐 실행에 바로 전합니다.
    /// 타격 신호는 대상을 해제한 뒤 냅니다(순서는 입구를 만드는 조립이 해제 규칙 <see cref="Attack.CombatContactRelease"/>로 지킴).
    /// 진행·취소·공격 자세 대여는 공격 재생기가 맡습니다. 포효의 정면 공격(<see cref="CombatFacingAttackPiece"/>)과 같은 꼴이고, 기준 순간 신호만 더 냅니다.
    /// </remarks>
    internal sealed class CombatCodeMotionPiece : ICombatPresentationPiece
    {
        private readonly Func<Action, Action, Action, bool> _tryBegin;

        /// <param name="tryBegin">코드 모션 입구입니다. 인자는 (모션 완료 콜백, 기준 순간 콜백, 타격 순간 콜백)입니다.</param>
        internal CombatCodeMotionPiece(Func<Action, Action, Action, bool> tryBegin) =>
            _tryBegin = tryBegin ?? throw new ArgumentNullException(nameof(tryBegin));

        public bool IsEnded => false;

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            Action end = context.EndCallback();
            Action cue = context.SignalCallback(CombatPieceSignal.MotionCue);
            Action hit = context.SignalCallback(CombatPieceSignal.Hit);
            if (!_tryBegin(end, cue, hit))
                end();
        }

        public void Tick(float deltaTime)
        {
        }

        public void Cancel()
        {
        }
    }
}
