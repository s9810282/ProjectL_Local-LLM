using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    public sealed class ThornFieldRuntimeState : TraitRuntimeState
    {
        public ThornFieldRuntimeState()
            : base(TraitKey.ThornField)
        {
        }

        internal override TraitRuntimeState Clone()
        {
            return new ThornFieldRuntimeState();
        }

        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (source is not ThornFieldRuntimeState)
            {
                throw new ArgumentException(
                    "ThornField state requires a matching final state.",
                    nameof(source));
            }
        }
    }
}
