using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    /// <summary>작렬은 전투 중 바뀌는 값이 없다. 등록 계약을 맞추는 빈 상태다.</summary>
    public sealed class CrashImpactRuntimeState : TraitRuntimeState
    {
        public CrashImpactRuntimeState()
            : base(TraitKey.CrashImpact)
        {
        }

        internal override TraitRuntimeState Clone()
        {
            return new CrashImpactRuntimeState();
        }

        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (source is not CrashImpactRuntimeState)
            {
                throw new ArgumentException(
                    "CrashImpact state requires a matching final state.",
                    nameof(source));
            }
        }
    }
}
