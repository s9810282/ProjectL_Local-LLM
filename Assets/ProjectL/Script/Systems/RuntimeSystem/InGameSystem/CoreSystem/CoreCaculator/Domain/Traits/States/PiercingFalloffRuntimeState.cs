using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    public sealed class PiercingFalloffRuntimeState : TraitRuntimeState
    {
        public PiercingFalloffRuntimeState()
            : base(TraitKey.PiercingFalloff)
        {
        }

        internal override TraitRuntimeState Clone()
        {
            return new PiercingFalloffRuntimeState();
        }

        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (source is not PiercingFalloffRuntimeState)
            {
                throw new ArgumentException(
                    "PiercingFalloff state requires a matching final state.",
                    nameof(source));
            }
        }
    }
}
