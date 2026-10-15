using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    public sealed class KnockbackRuntimeState : TraitRuntimeState
    {
        public KnockbackRuntimeState()
            : base(TraitKey.Knockback)
        {
        }

        internal override TraitRuntimeState Clone()
        {
            return new KnockbackRuntimeState();
        }

        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (source is not KnockbackRuntimeState)
            {
                throw new ArgumentException(
                    "Knockback state requires a matching final state.",
                    nameof(source));
            }
        }
    }
}
