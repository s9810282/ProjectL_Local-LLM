using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    /// <summary>
    /// 처치 연쇄 공격 특성의 런타임 상태입니다. 전투 중 바뀌는 값이 없습니다.
    /// </summary>
    public sealed class KillChainAttackRuntimeState : TraitRuntimeState
    {
        public KillChainAttackRuntimeState()
            : base(TraitKey.KillChainAttack)
        {
        }

        internal override TraitRuntimeState Clone()
        {
            return new KillChainAttackRuntimeState();
        }

        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (source is not KillChainAttackRuntimeState)
            {
                throw new ArgumentException(
                    "KillChainAttack 상태에는 같은 타입의 최종 상태가 필요합니다.",
                    nameof(source));
            }
        }
    }
}
