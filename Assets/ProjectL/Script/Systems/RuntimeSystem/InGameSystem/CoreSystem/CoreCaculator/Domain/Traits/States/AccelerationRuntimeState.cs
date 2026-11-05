using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    public sealed class AccelerationRuntimeState : TraitRuntimeState
    {
        public int HeldAttackBonus { get; private set; }
        public AccelerationRuntimeState() : this(0) { }
        public AccelerationRuntimeState(int value) : base(TraitKey.Acceleration)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            HeldAttackBonus = value;
        }
        internal override TraitRuntimeState Clone() => new AccelerationRuntimeState(HeldAttackBonus);
        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source is not AccelerationRuntimeState state) throw new ArgumentException("같은 특성 상태가 필요합니다.", nameof(source));
            HeldAttackBonus = state.HeldAttackBonus;
        }
    }
}
