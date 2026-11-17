using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    public sealed class DeadHandRuntimeState : TraitRuntimeState
    {
        public DeadHandRuntimeState() : base(TraitKey.DeadHand)
        {
        }

        internal override TraitRuntimeState Clone() => new DeadHandRuntimeState();

        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source is not DeadHandRuntimeState)
                throw new ArgumentException("같은 특성 상태가 필요합니다.", nameof(source));
        }
    }
}
