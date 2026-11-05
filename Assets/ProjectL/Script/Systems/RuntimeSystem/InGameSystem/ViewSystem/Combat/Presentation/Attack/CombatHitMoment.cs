using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// 타격 순간의 순서입니다(클립 설계 §11 K16, 계획 T29d). 대상을 해제한 뒤 타격 신호를 냅니다.
    /// 순서 규칙의 원본은 <see cref="CombatPieceSignal.Hit"/> 문서이고, 정면 공격 모션·해제 조각·접촉 집계가 이 함수 하나로 그 순서를 지킵니다.
    /// </summary>
    internal static class CombatHitMoment
    {
        /// <summary><paramref name="release"/>(대상 모두 해제) 뒤 <paramref name="signal"/>(타격 신호)을 부르는 동작 하나를 만듭니다.</summary>
        internal static Action ReleaseThenSignal(Action release, Action signal)
        {
            if (release == null) throw new ArgumentNullException(nameof(release));
            if (signal == null) throw new ArgumentNullException(nameof(signal));
            return () =>
            {
                release();
                signal();
            };
        }
    }
}
