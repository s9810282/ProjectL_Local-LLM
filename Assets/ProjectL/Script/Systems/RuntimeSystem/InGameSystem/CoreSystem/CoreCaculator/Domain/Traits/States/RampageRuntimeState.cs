using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    public sealed class RampageRuntimeState : TraitRuntimeState
    {
        public int KillAttackBonus { get; private set; }

        public RampageRuntimeState(int killAttackBonus = 0) : base(TraitKey.Rampage)
        {
            if (killAttackBonus < 0) throw new ArgumentOutOfRangeException(nameof(killAttackBonus));
            KillAttackBonus = killAttackBonus;
        }

        internal override TraitRuntimeState Clone() => new RampageRuntimeState(KillAttackBonus);

        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source is not RampageRuntimeState state) throw new ArgumentException("같은 특성 상태가 필요합니다.", nameof(source));
            KillAttackBonus = state.KillAttackBonus;
        }
    }
}
