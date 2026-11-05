using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    /// <summary>등록용 보유자 끌어오기입니다. 대상의 위치와 사거리는 보관하지 않습니다.</summary>
    public sealed class PullRuntimeState : TraitRuntimeState
    {
        public PullRuntimeState() : base(TraitKey.Pull) { }

        internal override TraitRuntimeState Clone() => new PullRuntimeState();

        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source is not PullRuntimeState)
                throw new ArgumentException("끌어오기 상태에는 같은 타입의 최종 상태가 필요합니다.", nameof(source));
        }
    }
}
