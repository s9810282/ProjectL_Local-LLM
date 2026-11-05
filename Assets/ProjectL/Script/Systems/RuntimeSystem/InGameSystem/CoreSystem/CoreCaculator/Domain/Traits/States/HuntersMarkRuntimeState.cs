using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    /// <summary>등록용 보유자 표식입니다. 대상의 표식 상태는 보관하지 않습니다.</summary>
    public sealed class HuntersMarkRuntimeState : TraitRuntimeState
    {
        public HuntersMarkRuntimeState() : base(TraitKey.HuntersMark) { }

        internal override TraitRuntimeState Clone() => new HuntersMarkRuntimeState();

        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source is not HuntersMarkRuntimeState)
                throw new ArgumentException("표식 상태에는 같은 타입의 최종 상태가 필요합니다.", nameof(source));
        }
    }
}
