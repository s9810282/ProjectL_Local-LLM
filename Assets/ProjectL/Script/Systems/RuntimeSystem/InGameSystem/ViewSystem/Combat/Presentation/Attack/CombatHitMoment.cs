using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// 타격 순간의 순서입니다(클립 설계 §11 K16, 계획 T29d). 대상을 해제한 뒤 타격 신호를 냅니다.
    /// 순서 규칙의 원본은 <see cref="CombatPieceSignal.Hit"/> 문서와 해제 규칙 <see cref="CombatContactRelease"/>이고, 근접 클립 해제 조각이 이 함수로 그 규칙을 씁니다
    /// (코드 모션·포효·원거리 접촉 집계는 <see cref="CombatContactRelease"/>를 직접 씀, 관통·죽음의 손 View 설계 D6).
    /// </summary>
    internal static class CombatHitMoment
    {
        /// <summary><paramref name="release"/>(대상 모두 해제) 뒤 <paramref name="signal"/>(타격 신호)을 부르는 동작 하나를 만듭니다.</summary>
        /// <remarks>순서 규칙은 해제 규칙 <see cref="CombatContactRelease"/> 하나가 가집니다(한 번에 모두 해제하는 길, 관통·죽음의 손 View 설계 D6). 지금은 근접 클립 해제 조각이 씁니다.</remarks>
        internal static Action ReleaseThenSignal(Action release, Action signal)
        {
            if (release == null) throw new ArgumentNullException(nameof(release));
            if (signal == null) throw new ArgumentNullException(nameof(signal));
            return CombatContactRelease.AllAtOnce(release, signal).Finish;
        }
    }
}
