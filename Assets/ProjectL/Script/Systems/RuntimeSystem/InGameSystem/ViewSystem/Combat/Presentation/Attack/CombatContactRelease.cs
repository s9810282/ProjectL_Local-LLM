using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// 공격 하나의 피격 해제 규칙입니다(관통·죽음의 손 View 설계 D6·§3.1, 계획 TP-3). 해제는 Core에 보내는 것이 아니라 Gate가 붙잡은
    /// 결과 사건(체력·피격·숫자·사망·넉백)을 화면에 내보내기 시작하는 순간입니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 마지막 탄의 접촉마다 그 대상만 해제하고(<see cref="Contact"/>), 그 탄의 마지막 접촉에서 남은 대상을 모두 해제합니다.
    /// 첫 해제 뒤 타격 신호(<c>Hit</c>)를 한 번 냅니다. 대상 해제 → 모두 해제 → 신호 순서라, 접촉이 하나인 탄은 옛 "모두 해제 → 신호"
    /// (<see cref="CombatHitMoment.ReleaseThenSignal"/>)와 같습니다.
    /// </para>
    /// <para>
    /// 코드 모션(근접·직선·곡사·포효)과 클립 원거리의 접촉 집계가 같이 씁니다. 근접·곡사·포효는 대상 없이 한 번에 끝냅니다(<see cref="Finish"/>).
    /// 어느 탄이 마지막 탄인지·앞 탄의 표시는 부르는 쪽(접촉 집계)이 정합니다.
    /// </para>
    /// </remarks>
    internal sealed class CombatContactRelease
    {
        private readonly Action<EntityId> _releaseTarget;
        private readonly Action _releaseAll;
        private readonly Action _signal;
        private bool _signaled;
        private bool _finished;

        /// <param name="releaseTarget">그 대상 하나를 해제하는 입구입니다(<c>ICombatSourceLifetime.Release</c>). 대상별 해제가 없는 길은 null입니다.</param>
        /// <param name="releaseAll">남은 대상을 모두 해제하는 입구입니다(<c>ICombatSourceLifetime.ReleaseAll</c>).</param>
        /// <param name="signal">타격 신호입니다. 첫 해제 뒤 한 번 냅니다.</param>
        internal CombatContactRelease(Action<EntityId> releaseTarget, Action releaseAll, Action signal)
        {
            _releaseTarget = releaseTarget;
            _releaseAll = releaseAll ?? throw new ArgumentNullException(nameof(releaseAll));
            _signal = signal ?? throw new ArgumentNullException(nameof(signal));
        }

        /// <summary>대상별 해제 없이 한 번에 모두 해제하는 길(포효 박치기 등)입니다.</summary>
        internal static CombatContactRelease AllAtOnce(Action releaseAll, Action signal) => new(null, releaseAll, signal);

        /// <summary>남은 대상까지 모두 해제했는지입니다.</summary>
        internal bool IsFinished => _finished;

        /// <summary>
        /// 대상 하나가 지금 닿았습니다. <paramref name="final"/>이면 그 뒤 남은 대상을 모두 해제하고 끝냅니다. 첫 해제 뒤 타격 신호를 한 번 냅니다.
        /// 끝난 뒤에 오는 접촉은 무시합니다.
        /// </summary>
        /// <param name="target">닿은 대상입니다. 비어 있으면 대상 해제 없이 지나갑니다.</param>
        internal void Contact(EntityId target, bool final)
        {
            if (_finished) return;
            bool released = false;
            if (!target.IsEmpty && _releaseTarget != null)
            {
                _releaseTarget(target);
                released = true;
            }
            if (final)
            {
                _finished = true;
                _releaseAll();
                released = true;
            }
            // 아무것도 풀지 않은 접촉(대상별 해제가 없는 길의 앞 접촉)은 신호를 내지 않습니다(신호는 첫 해제 뒤, PD-a 검토 L3).
            if (!released || _signaled) return;
            _signaled = true;
            _signal();
        }

        /// <summary>남은 대상을 모두 해제하고 끝냅니다(대상 없는 마지막 접촉, 근접·곡사의 타격).</summary>
        internal void Finish() => Contact(EntityId.Empty, true);
    }
}
