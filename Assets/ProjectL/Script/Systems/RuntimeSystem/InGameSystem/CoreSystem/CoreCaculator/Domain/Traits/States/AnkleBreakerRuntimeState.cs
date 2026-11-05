using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    public sealed class AnkleBreakerRuntimeState : TraitRuntimeState
    {
        public AnkleBreakerRuntimeState() : base(TraitKey.AnkleBreaker)
        {
        }
        internal override TraitRuntimeState Clone() => new AnkleBreakerRuntimeState();
        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source is not AnkleBreakerRuntimeState) throw new ArgumentException("같은 특성 상태가 필요합니다.", nameof(source));
        }
    }
}
