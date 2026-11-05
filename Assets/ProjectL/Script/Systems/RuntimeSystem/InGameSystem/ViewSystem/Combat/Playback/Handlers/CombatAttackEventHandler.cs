using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers
{
    /// <summary>
    /// 공격 사건의 표시입니다(Presenter 분리 설계 S3). 공격 연출 설정이 있으면 사실 반영 전에 공격 재생을 시작하고,
    /// 히트·완료는 공격 재생이 Gate 정책대로 알립니다(히트 타이밍 설계 H3). 시작하지 못하면 틀의 일반 소리로 이어집니다.
    /// </summary>
    /// <remarks>
    /// 공격 사건만의 판단(이동만 한 공격·실패한 공격)은 여기서 하고, 공격 모양 하나를 코드 모션·클립 조립으로 시작하는 일은
    /// 특성 처리기(추가 공격)와 함께 쓰는 <see cref="CombatAttackStart"/>가 합니다(특성 발동 연출 계획 Td.2). 공격 사건의 모양은 첫 해제에 사건 완료입니다.
    /// </remarks>
    internal sealed class CombatAttackEventHandler : CombatEventHandler
    {
        private readonly CombatAttackStart _start;

        internal CombatAttackEventHandler(CombatAttackStart start)
        {
            _start = start ?? throw new ArgumentNullException(nameof(start));
        }

        // 공격 사건만 조립을 만든다(공격 소리는 조립의 소리 큐가 낸다, 범용 연출 구조 설계 G2).
        internal override bool TryBeginBeforeFacts(CombatEvent e, Action completed, int version)
        {
            var attack = (CombatAttackEvent)e;
            if (CombatAttackTarget.IsMovementOnly(attack.Attack))
            {
                completed?.Invoke();
                return true;
            }
            if (!attack.Attack.Succeeded)
                return false;
            return _start.TryBegin(CombatAttackShape.Of(attack), null, completed, version);
        }
    }
}
