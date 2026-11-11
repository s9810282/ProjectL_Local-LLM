using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    /// <summary>공포 특성의 런타임 상태입니다. 전투 중 바뀌는 값이 없습니다.</summary>
    public sealed class FearRuntimeState : TraitRuntimeState
    {
        public FearRuntimeState() : base(TraitKey.Fear)
        {
        }

        internal override TraitRuntimeState Clone() => new FearRuntimeState();

        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source is not FearRuntimeState) throw new ArgumentException("같은 특성 상태가 필요합니다.", nameof(source));
        }
    }
}
