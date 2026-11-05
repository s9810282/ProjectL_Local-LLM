using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    /// <summary>사령술은 지속 진행값이 없다. 생존 소환 수는 소환 기록에서 계산한다.</summary>
    public sealed class NecromancyRuntimeState : TraitRuntimeState
    {
        public NecromancyRuntimeState()
            : base(TraitKey.Necromancy)
        {
        }

        internal override TraitRuntimeState Clone()
        {
            return new NecromancyRuntimeState();
        }

        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (source is not NecromancyRuntimeState)
            {
                throw new ArgumentException(
                    "Necromancy state requires a matching final state.",
                    nameof(source));
            }
        }
    }
}
