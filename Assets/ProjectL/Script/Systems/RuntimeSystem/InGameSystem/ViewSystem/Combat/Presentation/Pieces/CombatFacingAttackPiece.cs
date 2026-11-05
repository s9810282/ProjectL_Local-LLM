using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 정면 공격 모션 조각입니다(조각·조립 설계 BP1·BP7). 외부 진행 조각이라 진행은 공격 재생기가 하고, 이 조각은 그 완료를 끝으로 받습니다.
    /// 포효에서는 커진 채 보스가 보는 방향으로 박치기하고, 그 타격이 출처의 히트입니다(히트 타이밍 설계 §6 Q1).
    /// </summary>
    /// <remarks>
    /// 시작 입구(<c>tryBegin</c>)는 완료 콜백과 타격 콜백을 받아 모션을 시작하면 true, 시작하지 않으면(외형에 공격 모션이 없음 등) false입니다.
    /// false면 바로 끝나 다음 항목이 이어집니다. 끝은 맥락의 끝 입구로, 타격 순간은 맥락의 신호 입구(<see cref="CombatPieceSignal.Hit"/>)로 실행에 바로 전합니다
    /// (카메라 연출 조각 설계 K4). 시작하지 못하면 타격이 없으므로, 타격을 기다리던 항목은 시작하지 않고 끝난 것으로 봅니다(설계 BP2 보충).
    /// 진행·취소·공격 자세 대여는 공격 재생기가 맡습니다.
    /// </remarks>
    internal sealed class CombatFacingAttackPiece : ICombatPresentationPiece
    {
        private readonly Func<Action, Action, bool> _tryBegin;

        /// <param name="tryBegin">정면 공격 입구입니다. 인자는 (모션 완료 콜백, 타격 순간 콜백)입니다.</param>
        internal CombatFacingAttackPiece(Func<Action, Action, bool> tryBegin) =>
            _tryBegin = tryBegin ?? throw new ArgumentNullException(nameof(tryBegin));

        public bool IsEnded => false;

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            Action end = context.EndCallback();
            Action hit = context.SignalCallback(CombatPieceSignal.Hit);
            if (!_tryBegin(end, hit))
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
